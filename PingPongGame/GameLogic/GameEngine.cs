using System;
using System.Drawing;

namespace PingPongGame.GameLogic
{
    // Управляет всей игровой логикой (мяч, ракетки, счёт, ИИ)
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

        // время до следующего пересчёта цели ИИ
        private float _aiReactionTimer;

        // предыдущая позиция мяча (для защиты от "пролёта")
        private float _prevBallX;
        private float _prevBallY;

        // время текущего розыгрыша
        private float _roundTime;

        // Ускорение мяча по сложностям (в процентах за удар)
        public float BallAccelerationEasy = 8f;    // +8% за удар
        public float BallAccelerationNormal = 12f; // +12%
        public float BallAccelerationHard = 16f;   // +16%

        // ИИ правой ракетки
        private float _aiTargetY;
        private bool _aiHasGuess;

        // Постоянный рандомный сдвиг логической линии (0 = без смещения)
        private float _predictRandomOffset = 0f;

        public GameEngine(GameSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            Settings = settings;
            InitializeObjects();
        }

        private void InitializeObjects()
        {
            float centerX = Settings.FieldWidth / 2f;
            float centerY = Settings.FieldHeight / 2f;

            Ball = new Ball(centerX, centerY, Settings.BallRadius, 0f, 0f);

            LeftPaddle = new Paddle(
                30f,
                (Settings.FieldHeight - Settings.PaddleHeight) / 2f,
                Settings.PaddleWidth,
                Settings.PaddleHeight,
                Settings.PaddleSpeed);

            RightPaddle = new Paddle(
                Settings.FieldWidth - 30f - Settings.PaddleWidth,
                (Settings.FieldHeight - Settings.PaddleHeight) / 2f,
                Settings.PaddleWidth,
                Settings.PaddleHeight,
                Settings.PaddleSpeed);

            _prevBallX = Ball.X;
            _prevBallY = Ball.Y;

            _aiTargetY = centerY;
            _aiHasGuess = false;
            _aiReactionTimer = 0f;
            _roundTime = 0f;

            _predictRandomOffset = 0f;
        }

        private float Lerp(float a, float b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return a + (b - a) * t;
        }

        private float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private float RandomRange(float min, float max)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        /// <summary>Относительная скорость мяча.</summary>
        private float GetBallSpeedRatio()
        {
            float vx = Ball.VelocityX;
            float vy = Ball.VelocityY;
            float speed = (float)Math.Sqrt(vx * vx + vy * vy);
            float ratio = speed / Settings.BallSpeed;
            return Clamp(ratio, 0.5f, 3.0f);
        }

        /// <summary>Коэффициент увеличения скорости мяча при ударе.</summary>
        private float GetBallSpeedIncreaseFactor()
        {
            switch (Settings.AIDifficulty)
            {
                case AIDifficulty.Easy:
                    return 1f + BallAccelerationEasy / 100f;
                case AIDifficulty.Hard:
                    return 1f + BallAccelerationHard / 100f;
                default:
                    return 1f + BallAccelerationNormal / 100f;
            }
        }

        // ---------------- Публичные методы управления ----------------

        public void ResetGame()
        {
            ScoreLeft = 0;
            ScoreRight = 0;
            State = GameState.WaitingToStart;
            InitializeObjects();
        }

        public void StartRound()
        {
            if (State == GameState.GameOver)
                return;

            State = GameState.Playing;

            float centerX = Settings.FieldWidth / 2f;
            float centerY = Settings.FieldHeight / 2f;

            float dirX = _random.Next(0, 2) == 0 ? -1f : 1f;
            float dirY = (float)(_random.NextDouble() * 2 - 1); // -1..1

            float length = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            dirX /= length;
            dirY /= length;

            Ball.Reset(centerX, centerY, dirX * Settings.BallSpeed, dirY * Settings.BallSpeed);

            _prevBallX = Ball.X;
            _prevBallY = Ball.Y;

            _aiHasGuess = false;
            _aiTargetY = centerY;
            _roundTime = 0f;
            _predictRandomOffset = 0f;
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

        // ---------------- Главный апдейт ----------------

        public void Update(float dt)
        {
            if (State != GameState.Playing)
                return;

            _roundTime += dt;

            // ускорение ракеток в зависимости от скорости мяча
            float speedRatio = GetBallSpeedRatio();
            float paddleSpeedFactor = 1f + (speedRatio - 1f) * 0.6f;
            paddleSpeedFactor = Clamp(paddleSpeedFactor, 1f, 1.7f);

            // левая ракетка (игрок)
            LeftPaddle.Update(dt * paddleSpeedFactor, Settings.FieldHeight);

            // правая ракетка (ИИ)
            UpdateRightPaddleAI(dt, speedRatio);
            RightPaddle.Update(dt * paddleSpeedFactor, Settings.FieldHeight);

            // сохранение прошлой позиции мяча
            _prevBallX = Ball.X;
            _prevBallY = Ball.Y;

            // мяч
            Ball.Move(dt);

            HandleWallCollisions();
            HandlePaddleCollisions();
            HandleGoal();
        }

        // ---------------- ИИ правой ракетки ----------------

        private void UpdateRightPaddleAI(float dt, float ballSpeedRatio)
        {
            if (State != GameState.Playing)
            {
                RightPaddle.DirectionY = Lerp(RightPaddle.DirectionY, 0f, 6f * dt);
                return;
            }

            float fieldWidth = Settings.FieldWidth;
            float fieldHeight = Settings.FieldHeight;

            // параметры по сложности
            float baseReactionInterval;
            float roughError;
            float predictError;
            float aiBaseSpeedFactor;
            float moveSmoothness;
            float predictMinFactor;
            float predictTimeToLimit;

            switch (Settings.AIDifficulty)
            {
                case AIDifficulty.Easy:
                    // Easy: довольно слабый, но уже не "самоубийца"
                    baseReactionInterval = 0.23f;
                    roughError = 55f;
                    predictError = 26f;
                    aiBaseSpeedFactor = 0.96f;
                    moveSmoothness = 7f;

                    predictMinFactor = 0.80f;    // ближе к правой стороне
                    predictTimeToLimit = 9f;
                    break;

                case AIDifficulty.Hard:
                    // Hard: сильный, но не нечестный
                    baseReactionInterval = 0.18f;
                    roughError = 40f;
                    predictError = 18f;
                    aiBaseSpeedFactor = 1.0f;
                    moveSmoothness = 9.5f;

                    predictMinFactor = 0.70f;    // ближе к центру
                    predictTimeToLimit = 7.5f;
                    break;

                default: // Normal
                    baseReactionInterval = 0.21f;
                    roughError = 52f;
                    predictError = 22f;
                    aiBaseSpeedFactor = 0.95f;
                    moveSmoothness = 8.3f;

                    predictMinFactor = 0.76f;
                    predictTimeToLimit = 8.5f;
                    break;
            }

            // учёт скорости мяча
            float r = Clamp(ballSpeedRatio, 0.7f, 3.0f);

            // быстрый мяч → ИИ реагирует чаще
            float reactionInterval = baseReactionInterval - (r - 1f) * 0.04f;
            float minReaction = baseReactionInterval * 0.55f;
            float maxReaction = baseReactionInterval * 1.1f;
            reactionInterval = Clamp(reactionInterval, minReaction, maxReaction);

            float currentRoughError = roughError * (1f + (r - 1f) * 0.4f);
            float currentPredictError = predictError * (1f + (r - 1f) * 0.4f);

            // быстрый мяч → ИИ немного быстрее двигается
            float speedScale = 1f + (r - 1f) * 0.25f;
            speedScale = Clamp(speedScale, 1f, 1.35f);
            float aiSpeedFactor = aiBaseSpeedFactor * speedScale;

            // -------- постоянный рандомный дрейф логической линии --------

            float jitterSpeed;      // скорость "дрожания" линии
            float negativeLimit;    // максимум смещения к центру (влево)
            float positiveLimit;    // максимум смещения к правому краю (вправо)

            switch (Settings.AIDifficulty)
            {
                case AIDifficulty.Easy:
                    jitterSpeed = 0.35f;
                    negativeLimit = 0.02f; // к центру почти не двигается
                    positiveLimit = 0.05f; // чаще уходит от центра
                    break;
                case AIDifficulty.Hard:
                    jitterSpeed = 0.22f;
                    negativeLimit = 0.05f; // может немного податься к центру
                    positiveLimit = 0.06f;
                    break;
                default: // Normal
                    jitterSpeed = 0.28f;
                    negativeLimit = 0.035f;
                    positiveLimit = 0.055f;
                    break;
            }

            // маленький случайный шаг
            float delta = RandomRange(-1f, 1f) * jitterSpeed * dt;
            _predictRandomOffset += delta;

            // лёгкое стремление к 0, чтобы не уплывать навсегда
            float decay = 0.6f; // чем больше, тем быстрее стягивает к 0
            _predictRandomOffset = Lerp(_predictRandomOffset, 0f, decay * dt);

            // асимметричное ограничение:
            // к центру (отрицательное смещение) меньше, чем от центра
            if (_predictRandomOffset < -negativeLimit)
                _predictRandomOffset = -negativeLimit;
            if (_predictRandomOffset > positiveLimit)
                _predictRandomOffset = positiveLimit;

            // -------- логическая линия --------

            const float startFactor = 0.90f; // старт у правого края
            float tTime = Clamp(_roundTime / predictTimeToLimit, 0f, 1f);

            // на быстрых мячах линия доезжает до порога чуть быстрее
            float speedBoost = 1f + (r - 1f) * 0.2f;
            float t = Clamp(tTime * speedBoost, 0f, 1f);

            float baseFactor = Lerp(startFactor, predictMinFactor, t);

            // добавляем небольшой постоянный шум
            float finalFactor = baseFactor + _predictRandomOffset;
            // не двигаем линию левее центра и правее 0.95 поля
            finalFactor = Clamp(finalFactor, 0.50f, 0.95f);

            float predictStartX = fieldWidth * finalFactor;

            // -------- периодический пересчёт цели ИИ --------

            _aiReactionTimer -= dt;
            if (_aiReactionTimer <= 0f)
            {
                _aiReactionTimer = reactionInterval;

                if (Ball.VelocityX > 0f)
                {
                    // мяч летит к ИИ
                    if (Ball.X >= predictStartX)
                    {
                        // считаем траекторию (с ошибкой)
                        float predictedY = PredictBallYAtPaddleX();
                        float guess = predictedY + RandomRange(-currentPredictError, currentPredictError);
                        guess = Clamp(guess, 0f, fieldHeight);

                        if (!_aiHasGuess)
                        {
                            _aiTargetY = guess;
                            _aiHasGuess = true;
                        }
                        else
                        {
                            // "сомнение" — не сразу верит новой оценке
                            _aiTargetY = Lerp(_aiTargetY, guess, 0.55f);
                        }
                    }
                    else
                    {
                        // мяч ещё не в зоне расчёта — ИИ двигается "на глаз"
                        float centerY = fieldHeight / 2f;
                        float follow = Lerp(centerY, Ball.Y, 0.65f);
                        float guess = follow + RandomRange(-currentRoughError, currentRoughError);
                        guess = Clamp(guess, 0f, fieldHeight);

                        _aiTargetY = Lerp(_aiTargetY, guess, 0.4f);
                        _aiHasGuess = false;
                    }
                }
                else
                {
                    // мяч летит от ИИ — он занимает удобную позицию
                    float centerY = fieldHeight / 2f;
                    float idle = Lerp(centerY, Ball.Y, 0.25f);

                    _aiTargetY = Lerp(_aiTargetY, idle, 0.4f);
                    _aiHasGuess = false;
                }

                _aiTargetY = Clamp(_aiTargetY, 0f, fieldHeight);
            }

            // -------- движение ракетки к цели --------

            float paddleCenter = RightPaddle.Y + RightPaddle.Height / 2f;
            float diff = _aiTargetY - paddleCenter;
            const float deadZone = 6f;

            if (Math.Abs(diff) <= deadZone)
            {
                RightPaddle.DirectionY = Lerp(RightPaddle.DirectionY, 0f, moveSmoothness * dt);
                return;
            }

            float desiredDir = diff / (RightPaddle.Height * 0.55f);
            if (desiredDir < -1f) desiredDir = -1f;
            if (desiredDir > 1f) desiredDir = 1f;

            desiredDir *= aiSpeedFactor;

            RightPaddle.DirectionY = Lerp(RightPaddle.DirectionY, desiredDir, moveSmoothness * dt);
        }

        /// <summary>
        /// Предсказывает, на какой высоте мяч достигнет X правой ракетки.
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
            const int maxSteps = 5000;

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
                    return simY;
            }

            return Settings.FieldHeight / 2f;
        }

        // ---------------- Столкновения и голы ----------------

        private void HandleWallCollisions()
        {
            if (Ball.Y - Ball.Radius <= 0f && Ball.VelocityY < 0f)
            {
                Ball.SetVelocity(Ball.VelocityX, -Ball.VelocityY);
            }
            else if (Ball.Y + Ball.Radius >= Settings.FieldHeight && Ball.VelocityY > 0f)
            {
                Ball.SetVelocity(Ball.VelocityX, -Ball.VelocityY);
            }
        }

        private void HandlePaddleCollisions()
        {
            RectangleF ballRect = Ball.GetBounds();
            RectangleF leftRect = LeftPaddle.GetBounds();
            RectangleF rightRect = RightPaddle.GetBounds();

            // "протянутый" прямоугольник между прошлой и новой позицией
            float minX = Math.Min(_prevBallX, Ball.X) - Ball.Radius;
            float maxX = Math.Max(_prevBallX, Ball.X) + Ball.Radius;
            float minY = Math.Min(_prevBallY, Ball.Y) - Ball.Radius;
            float maxY = Math.Max(_prevBallY, Ball.Y) + Ball.Radius;

            RectangleF sweptRect = new RectangleF(
                minX,
                minY,
                maxX - minX,
                maxY - minY);

            // левая ракетка
            if ((ballRect.IntersectsWith(leftRect) || sweptRect.IntersectsWith(leftRect)) &&
                Ball.VelocityX < 0f)
            {
                float newX = leftRect.Right + Ball.Radius;
                Ball.Reset(newX, Ball.Y, Ball.VelocityX, Ball.VelocityY);

                ReflectFromPaddle(LeftPaddle);
            }

            // правая ракетка
            if ((ballRect.IntersectsWith(rightRect) || sweptRect.IntersectsWith(rightRect)) &&
                Ball.VelocityX > 0f)
            {
                float newX = rightRect.Left - Ball.Radius;
                Ball.Reset(newX, Ball.Y, Ball.VelocityX, Ball.VelocityY);

                ReflectFromPaddle(RightPaddle);
            }
        }

        private void ReflectFromPaddle(Paddle paddle)
        {
            // отражаем по X
            float newVx = -Ball.VelocityX;

            // добавляем вертикальный компонент в зависимости от места попадания
            float paddleCenterY = paddle.Y + paddle.Height / 2f;
            float offset = (Ball.Y - paddleCenterY) / (paddle.Height / 2f); // -1..1
            float newVy = Ball.VelocityY + offset * 80f;

            // ускоряем мяч
            float speedIncrease = GetBallSpeedIncreaseFactor();
            newVx *= speedIncrease;
            newVy *= speedIncrease;

            Ball.SetVelocity(newVx, newVy);
        }

        private void HandleGoal()
        {
            if (Ball.X + Ball.Radius < 0f)
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
            float centerX = Settings.FieldWidth / 2f;
            float centerY = Settings.FieldHeight / 2f;

            if (ScoreLeft >= Settings.MaxScore || ScoreRight >= Settings.MaxScore)
            {
                State = GameState.GameOver;
                Ball.Reset(centerX, centerY, 0f, 0f);
            }
            else
            {
                State = GameState.WaitingToStart;
                Ball.Reset(centerX, centerY, 0f, 0f);
            }

            _prevBallX = Ball.X;
            _prevBallY = Ball.Y;
            _roundTime = 0f;
            _aiHasGuess = false;
            _predictRandomOffset = 0f;
        }
    }
}
