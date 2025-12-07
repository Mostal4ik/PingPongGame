using System;
using System.Drawing;

namespace PingPongGame.GameLogic
{
    // GameEngine управляет всей игровой логикой (мяч, ракетки, счёт, ИИ)
    public class GameEngine
    {
        public Ball Ball { get; private set; }
        public Paddle LeftPaddle { get; private set; }
        public Paddle RightPaddle { get; private set; }

        public int ScoreLeft { get; private set; }
        public int ScoreRight { get; private set; }

        public GameState State { get; private set; } = GameState.WaitingToStart;
        public GameSettings Settings { get; }

        private readonly Random _random = new Random();

        // ------- ИИ правой ракетки -------
        private float _aiTargetY;   // текущая целевая позиция по Y
        private bool _aiHasGuess;   // сделал ли первую "сомнительную" догадку

        // от чего был последний отскок
        private enum BounceSource
        {
            None,
            LeftPaddle,
            RightPaddle,
            Wall
        }

        private BounceSource _lastBounceSource = BounceSource.None;

        public GameEngine(GameSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            InitializeObjects();
        }

        private void InitializeObjects()
        {
            float centerX = Settings.FieldWidth / 2f;
            float centerY = Settings.FieldHeight / 2f;

            Ball = new Ball(
                x: centerX,
                y: centerY,
                radius: Settings.BallRadius,
                velocityX: 0,
                velocityY: 0);

            LeftPaddle = new Paddle(
                x: 30f,
                y: (Settings.FieldHeight - Settings.PaddleHeight) / 2f,
                width: Settings.PaddleWidth,
                height: Settings.PaddleHeight,
                speed: Settings.PaddleSpeed);

            RightPaddle = new Paddle(
                x: Settings.FieldWidth - 30f - Settings.PaddleWidth,
                y: (Settings.FieldHeight - Settings.PaddleHeight) / 2f,
                width: Settings.PaddleWidth,
                height: Settings.PaddleHeight,
                speed: Settings.PaddleSpeed);

            _aiTargetY = Settings.FieldHeight / 2f;
            _aiHasGuess = false;
            _lastBounceSource = BounceSource.None;
        }

        private float Lerp(float a, float b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return a + (b - a) * t;
        }

        private float RandomRange(float min, float max)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        /// <summary>
        /// Относительная скорость мяча: 1.0 = базовая, >1 = быстрый мяч.
        /// Ограничиваем сверху, чтобы не улетать в безумие.
        /// </summary>
        private float GetBallSpeedRatio()
        {
            float vx = Ball.VelocityX;
            float vy = Ball.VelocityY;
            float speed = (float)Math.Sqrt(vx * vx + vy * vy);

            float ratio = speed / Settings.BallSpeed;
            if (ratio < 0.5f) ratio = 0.5f;
            if (ratio > 2.5f) ratio = 2.5f;
            return ratio;
        }

        /// <summary>Полный сброс игры (счёт и позиции).</summary>
        public void ResetGame()
        {
            ScoreLeft = 0;
            ScoreRight = 0;
            State = GameState.WaitingToStart;
            InitializeObjects();
        }

        /// <summary>Запуск очередного розыгрыша (мяч летит из центра).</summary>
        public void StartRound()
        {
            if (State == GameState.GameOver)
                return;

            State = GameState.Playing;

            float centerX = Settings.FieldWidth / 2f;
            float centerY = Settings.FieldHeight / 2f;

            // Случайное направление мяча
            float dirX = _random.Next(0, 2) == 0 ? -1f : 1f;
            float dirY = (float)(_random.NextDouble() * 2 - 1); // -1..1

            // Нормализуем вектор
            float length = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            dirX /= length;
            dirY /= length;

            Ball.Reset(
                x: centerX,
                y: centerY,
                vx: dirX * Settings.BallSpeed,
                vy: dirY * Settings.BallSpeed);

            _aiHasGuess = false;
            _aiTargetY = Settings.FieldHeight / 2f;
            _lastBounceSource = BounceSource.None;
        }

        public void Pause()
        {
            if (State == GameState.Playing)
                State = GameState.Paused;
        }

        public void Resume()
        {
            if (State == GameState.Paused)
                State = GameState.Playing;
        }

        /// <summary>
        /// Основное обновление логики. Вызывается на каждом тике таймера.
        /// dt — прошедшее время в секундах.
        /// </summary>
        public void Update(float dt)
        {
            if (State != GameState.Playing)
                return;

            // --- динамическое ускорение ракеток в зависимости от скорости мяча ---
            float speedRatio = GetBallSpeedRatio();

            // Ракетки игрока и ИИ ускоряются одинаково (честность)
            float paddleSpeedFactor = 1f + (speedRatio - 1f) * 0.6f; // до +60%
            if (paddleSpeedFactor < 1f) paddleSpeedFactor = 1f;
            if (paddleSpeedFactor > 1.7f) paddleSpeedFactor = 1.7f;

            // Левую ракетку двигает игрок (W/S), но с учётом ускорения
            LeftPaddle.Update(dt * paddleSpeedFactor, Settings.FieldHeight);

            // Правую — ИИ
            UpdateRightPaddleAI(dt, speedRatio);
            RightPaddle.Update(dt * paddleSpeedFactor, Settings.FieldHeight);

            // Мяч
            Ball.Move(dt);

            HandleWallCollisions();
            HandlePaddleCollisions();
            HandleGoal();
        }

        // ---------- "человеческий" ИИ правой ракетки ----------
        private float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private void UpdateRightPaddleAI(float dt, float ballSpeedRatio)
        {
            if (State != GameState.Playing)
            {
                RightPaddle.DirectionY = Lerp(RightPaddle.DirectionY, 0f, 6f * dt);
                return;
            }

            float fieldWidth = Settings.FieldWidth;
            float fieldHeight = Settings.FieldHeight;

            // 1) БАЗОВЫЕ ПАРАМЕТРЫ ПО СЛОЖНОСТИ
            float followSpeed;           // как быстро "следит глазами"
            float predictionNoiseLarge;  // разброс первой догадки
            float predictionNoiseSmall;  // разброс при корректировке
            float correctionSpeed;       // скорость подтягивания к новой цели
            float centerReturnSpeed;     // как быстро возвращается к центру, когда мяч улетает

            switch (Settings.AIDifficulty)
            {
                case AIDifficulty.Easy:
                    followSpeed = 1.3f;
                    predictionNoiseLarge = 120f;
                    predictionNoiseSmall = 60f;
                    correctionSpeed = 1.8f;
                    centerReturnSpeed = 1.4f;
                    break;

                case AIDifficulty.Hard:
                    followSpeed = 3.0f;
                    predictionNoiseLarge = 40f;
                    predictionNoiseSmall = 20f;
                    correctionSpeed = 2.7f;
                    centerReturnSpeed = 2.4f;
                    break;

                default: // Normal
                    followSpeed = 2.0f;
                    predictionNoiseLarge = 80f;
                    predictionNoiseSmall = 40f;
                    correctionSpeed = 2.2f;
                    centerReturnSpeed = 1.9f;
                    break;
            }

            // 2) ДВИЖЕНИЕ ЦЕЛИ (_aiTargetY) В ЗАВИСИМОСТИ ОТ НАПРАВЛЕНИЯ МЯЧА

            if (Ball.VelocityX <= 0)
            {
                // Мяч летит влево (от ИИ) → он не "замирает", а
                // потихоньку возвращается к центру, но всё равно
                // смотрит на мяч (чтобы было живее).

                float centerY = fieldHeight / 2f;
                // целевая точка где-то между центром и высотой мяча
                float visualTarget = Lerp(centerY, Ball.Y, 0.3f); // 30% влияния мяча

                _aiTargetY = Lerp(_aiTargetY, visualTarget, centerReturnSpeed * dt);
                _aiHasGuess = false; // забываем старую догадку
            }
            else
            {
                // Мяч летит вправо (к ИИ)

                // 2.1. Ещё не делали первую догадку
                if (!_aiHasGuess)
                {
                    // следим за мячом "глазами", но не идеально
                    _aiTargetY = Lerp(_aiTargetY, Ball.Y, followSpeed * dt);

                    // делаем первую, довольно кривую догадку,
                    // когда мяч прошёл 35% поля
                    if (Ball.X > fieldWidth * 0.35f)
                    {
                        float guess = Ball.Y + RandomRange(-predictionNoiseLarge, predictionNoiseLarge);
                        guess = Clamp(guess, 0, fieldHeight);

                        _aiTargetY = guess;
                        _aiHasGuess = true;
                    }
                }
                else
                {
                    // 2.2. Мяч прошёл середину поля — корректируем догадку
                    if (Ball.X > fieldWidth * 0.6f)
                    {
                        float corrected = Ball.Y + RandomRange(
                            -predictionNoiseSmall,
                            predictionNoiseSmall
                        );

                        corrected = Clamp(corrected, 0, fieldHeight);
                        _aiTargetY = Lerp(_aiTargetY, corrected, correctionSpeed * dt);
                    }

                    // 2.3. Мяч уже совсем рядом с ракеткой
                    if (Ball.X > fieldWidth * 0.8f)
                    {
                        float lateTarget = Ball.Y + RandomRange(
                            -predictionNoiseSmall * 0.7f,
                            predictionNoiseSmall * 0.7f
                        );

                        lateTarget = Clamp(lateTarget, 0, fieldHeight);
                        _aiTargetY = Lerp(_aiTargetY, lateTarget, (correctionSpeed + 0.5f) * dt);
                    }
                }
            }

            // 3) ДВИЖЕНИЕ РАКЕТКИ К ЦЕЛИ _aiTargetY

            float paddleCenter = RightPaddle.Y + RightPaddle.Height / 2f;
            float diff = _aiTargetY - paddleCenter;

            const float deadZone = 7f;
            if (Math.Abs(diff) <= deadZone)
            {
                // если почти на нужном месте — притормаживаем
                RightPaddle.DirectionY = Lerp(RightPaddle.DirectionY, 0f, 6f * dt);
                return;
            }

            float dir = diff / (RightPaddle.Height * 0.6f); // примерно -1..1
            dir = Clamp(dir, -1f, 1f);

            RightPaddle.DirectionY = Lerp(RightPaddle.DirectionY, dir, 8f * dt);
        }




        /// <summary>
        /// Предсказывает, на какой высоте мяч достигнет X правой ракетки,
        /// учитывая отскоки от верхней и нижней границы.
        /// </summary>
        private float PredictBallYAtPaddleX()
        {
            if (Math.Abs(Ball.VelocityX) < 0.01f)
                return Settings.FieldHeight / 2f;

            float simX = Ball.X;
            float simY = Ball.Y;
            float vx = Ball.VelocityX;
            float vy = Ball.VelocityY;
            float radius = Ball.Radius;

            float targetX = RightPaddle.X;

            const float step = 1f / 240f;
            int maxSteps = 5000;

            for (int i = 0; i < maxSteps; i++)
            {
                simX += vx * step;
                simY += vy * step;

                if (simY - radius <= 0f && vy < 0f)
                {
                    simY = radius;
                    vy = -vy;
                }
                else if (simY + radius >= Settings.FieldHeight && vy > 0f)
                {
                    simY = Settings.FieldHeight - radius;
                    vy = -vy;
                }

                if (simX + radius >= targetX)
                {
                    return simY;
                }
            }

            return Settings.FieldHeight / 2f;
        }

        // ---------- столкновения и голы ----------

        private void HandleWallCollisions()
        {
            if (Ball.Y - Ball.Radius <= 0 && Ball.VelocityY < 0)
            {
                Ball.SetVelocity(Ball.VelocityX, -Ball.VelocityY);
                _lastBounceSource = BounceSource.Wall;
            }
            else if (Ball.Y + Ball.Radius >= Settings.FieldHeight && Ball.VelocityY > 0)
            {
                Ball.SetVelocity(Ball.VelocityX, -Ball.VelocityY);
                _lastBounceSource = BounceSource.Wall;
            }
        }

        private void HandlePaddleCollisions()
        {
            RectangleF ballRect = Ball.GetBounds();
            RectangleF leftRect = LeftPaddle.GetBounds();
            RectangleF rightRect = RightPaddle.GetBounds();

            if (ballRect.IntersectsWith(leftRect) && Ball.VelocityX < 0)
            {
                ReflectFromPaddle(LeftPaddle);
                _lastBounceSource = BounceSource.LeftPaddle;
            }

            if (ballRect.IntersectsWith(rightRect) && Ball.VelocityX > 0)
            {
                ReflectFromPaddle(RightPaddle);
                _lastBounceSource = BounceSource.RightPaddle;
            }
        }

        private void ReflectFromPaddle(Paddle paddle)
        {
            // Отражаем по X
            float newVx = -Ball.VelocityX;

            float paddleCenterY = paddle.Y + paddle.Height / 2f;
            float offset = (Ball.Y - paddleCenterY) / (paddle.Height / 2f); // -1..1

            float newVy = Ball.VelocityY + offset * 80f;

            // Ускоряем мяч при каждом ударе
            const float speedIncreaseFactor = 1.06f; // +6% скорости за удар
            newVx *= speedIncreaseFactor;
            newVy *= speedIncreaseFactor;

            Ball.SetVelocity(newVx, newVy);
        }

        private void HandleGoal()
        {
            if (Ball.X + Ball.Radius < 0)
            {
                ScoreRight++;
                AfterGoal();
            }
            else if (Ball.X - Ball.Radius > Settings.FieldWidth)
            {
                ScoreLeft++;
                AfterGoal();
            }
        }

        private void AfterGoal()
        {
            if (ScoreLeft >= Settings.MaxScore || ScoreRight >= Settings.MaxScore)
            {
                State = GameState.GameOver;
                float centerX = Settings.FieldWidth / 2f;
                float centerY = Settings.FieldHeight / 2f;
                Ball.Reset(centerX, centerY, 0, 0);
            }
            else
            {
                State = GameState.WaitingToStart;
                float centerX = Settings.FieldWidth / 2f;
                float centerY = Settings.FieldHeight / 2f;
                Ball.Reset(centerX, centerY, 0, 0);

                _aiHasGuess = false;
                _aiTargetY = Settings.FieldHeight / 2f;
                _lastBounceSource = BounceSource.None;
            }
        }
    }
}
