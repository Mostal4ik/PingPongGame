using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using PingPongGame.GameLogic;

namespace PingPongGame
{
    public partial class Form1 : Form
    {
        private GameEngine _engine;
        private GameSettings _settings;

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Timer _timer = new Timer();

        public Form1()
        {
            InitializeComponent();

            // Чтобы не мерцало
            this.DoubleBuffered = true;
            // Чтобы форма ловила клавиши
            this.KeyPreview = true;

            // Подписываемся на события клавиатуры
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
            this.Load += Form1_Load;

            // Настройка таймера (~60 кадров в секунду)
            _timer.Interval = 16;
            _timer.Tick += Timer_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Настройки игры под размер окна
            _settings = new GameSettings(
                fieldWidth: this.ClientSize.Width,
                fieldHeight: this.ClientSize.Height,
                maxScore: 5);

            _engine = new GameEngine(_settings);

            _engine.StartRound();

            _stopwatch.Start();
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Считаем прошедшее время
            float dt = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            // Простейший ИИ для правой ракетки
            UpdateRightPaddleAI();

            // Обновляем логику
            _engine.Update(dt);

            // Перерисовать форму
            Invalidate();
        }

        private void UpdateRightPaddleAI()
        {
            // Если игра не идёт — не двигаем
            if (_engine.State != GameState.Playing)
            {
                _engine.RightPaddle.DirectionY = 0;
                return;
            }

            float paddleCenter = _engine.RightPaddle.Y + _engine.RightPaddle.Height / 2f;
            float ballY = _engine.Ball.Y;

            const float deadZone = 5f; // зона, в которой ракетка не дёргается

            if (ballY < paddleCenter - deadZone)
                _engine.RightPaddle.DirectionY = -1;
            else if (ballY > paddleCenter + deadZone)
                _engine.RightPaddle.DirectionY = 1;
            else
                _engine.RightPaddle.DirectionY = 0;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // ЛЕВАЯ ракетка: W / S
            if (e.KeyCode == Keys.W)
                _engine.LeftPaddle.DirectionY = -1;
            else if (e.KeyCode == Keys.S)
                _engine.LeftPaddle.DirectionY = 1;

            // Пауза
            if (e.KeyCode == Keys.Escape)
            {
                if (_engine.State == GameState.Playing)
                    _engine.Pause();
                else if (_engine.State == GameState.Paused)
                    _engine.Resume();
            }

            // Старт/рестарт розыгрыша пробелом
            if (e.KeyCode == Keys.Space)
            {
                if (_engine.State == GameState.WaitingToStart)
                    _engine.StartRound();
                else if (_engine.State == GameState.GameOver)
                    _engine.ResetGame();
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            // Когда отпускаем W/S — останавливаем ракетку
            if (e.KeyCode == Keys.W && _engine.LeftPaddle.DirectionY < 0)
                _engine.LeftPaddle.DirectionY = 0;
            else if (e.KeyCode == Keys.S && _engine.LeftPaddle.DirectionY > 0)
                _engine.LeftPaddle.DirectionY = 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_engine == null)
                return;

            Graphics g = e.Graphics;

            // фон
            g.Clear(Color.Black);

            using (Pen pen = new Pen(Color.DarkGray, 2))
            {
                // центральная линия
                float centerX = this.ClientSize.Width / 2f;
                g.DrawLine(pen, centerX, 0, centerX, this.ClientSize.Height);
            }

            // Мяч
            var ball = _engine.Ball;
            g.FillEllipse(
                Brushes.White,
                ball.X - ball.Radius,
                ball.Y - ball.Radius,
                ball.Radius * 2,
                ball.Radius * 2);

            // Левая ракетка
            var lp = _engine.LeftPaddle;
            g.FillRectangle(
                Brushes.White,
                lp.X, lp.Y, lp.Width, lp.Height);

            // Правая ракетка
            var rp = _engine.RightPaddle;
            g.FillRectangle(
                Brushes.White,
                rp.X, rp.Y, rp.Width, rp.Height);

            // Счёт
            using (Font font = new Font("Consolas", 18, FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.White))
            {
                string scoreText = $"{_engine.ScoreLeft} : {_engine.ScoreRight}";
                SizeF size = g.MeasureString(scoreText, font);
                g.DrawString(scoreText, font, brush,
                    (ClientSize.Width - size.Width) / 2,
                    10);
            }

            // Сообщения
            using (Font font = new Font("Consolas", 12))
            using (Brush brush = new SolidBrush(Color.LightGray))
            {
                string msg = "";

                if (_engine.State == GameState.WaitingToStart)
                    msg = "SPACE - начать розыгрыш";
                else if (_engine.State == GameState.Paused)
                    msg = "Пауза (ESC - продолжить)";
                else if (_engine.State == GameState.GameOver)
                    msg = "Игра окончена. SPACE - новая игра";

                if (!string.IsNullOrEmpty(msg))
                {
                    SizeF s = g.MeasureString(msg, font);
                    g.DrawString(msg, font, brush,
                        (ClientSize.Width - s.Width) / 2,
                        ClientSize.Height - s.Height - 10);
                }
            }
        }
    }
}
