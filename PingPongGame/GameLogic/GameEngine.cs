using System;
using System.Drawing;

namespace PingPongGame.GameLogic
{
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

            // Обновляем ракетки (направления задаёт UI)
            LeftPaddle.Update(dt, Settings.FieldHeight);
            RightPaddle.Update(dt, Settings.FieldHeight);

            // Двигаем мяч
            Ball.Move(dt);

            HandleWallCollisions();
            HandlePaddleCollisions();
            HandleGoal();
        }

        private void HandleWallCollisions()
        {
            // Столкновение с верхней/нижней границей
            if (Ball.Y - Ball.Radius <= 0 && Ball.VelocityY < 0)
            {
                Ball.SetVelocity(Ball.VelocityX, -Ball.VelocityY);
            }
            else if (Ball.Y + Ball.Radius >= Settings.FieldHeight && Ball.VelocityY > 0)
            {
                Ball.SetVelocity(Ball.VelocityX, -Ball.VelocityY);
            }
        }

        private void HandlePaddleCollisions()
        {
            RectangleF ballRect = Ball.GetBounds();
            RectangleF leftRect = LeftPaddle.GetBounds();
            RectangleF rightRect = RightPaddle.GetBounds();

            // Столкновение с левой ракеткой
            if (ballRect.IntersectsWith(leftRect) && Ball.VelocityX < 0)
            {
                ReflectFromPaddle(LeftPaddle);
            }

            // Столкновение с правой ракеткой
            if (ballRect.IntersectsWith(rightRect) && Ball.VelocityX > 0)
            {
                ReflectFromPaddle(RightPaddle);
            }
        }

        private void ReflectFromPaddle(Paddle paddle)
        {
            // Отразить по X
            float newVx = -Ball.VelocityX;

            // Добавим небольшой эффект «куда попали по ракетке»
            float paddleCenterY = paddle.Y + paddle.Height / 2f;
            float offset = (Ball.Y - paddleCenterY) / (paddle.Height / 2f); // -1..1

            float newVy = Ball.VelocityY + offset * 80f;

            Ball.SetVelocity(newVx, newVy);
        }

        private void HandleGoal()
        {
            // Мяч улетел за левую границу — гол правому
            if (Ball.X + Ball.Radius < 0)
            {
                ScoreRight++;
                AfterGoal();
            }
            // Мяч улетел за правую границу — гол левому
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
                // Мяч останавливаем в центре
                float centerX = Settings.FieldWidth / 2f;
                float centerY = Settings.FieldHeight / 2f;
                Ball.Reset(centerX, centerY, 0, 0);
            }
            else
            {
                // Останавливаем игру и ждём вызов StartRound()
                State = GameState.WaitingToStart;

                float centerX = Settings.FieldWidth / 2f;
                float centerY = Settings.FieldHeight / 2f;
                Ball.Reset(centerX, centerY, 0, 0);
            }
        }
    }
}
