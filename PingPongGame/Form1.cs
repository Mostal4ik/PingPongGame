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

        private AIDifficulty _currentDifficulty = AIDifficulty.Normal;

        public Form1()
        {
            InitializeComponent();

            // чтобы не мерцало
            this.DoubleBuffered = true;
            // чтобы форма получала события клавиатуры
            this.KeyPreview = true;

            this.Load += Form1_Load;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;

            // таймер ~60 FPS
            _timer.Interval = 16;
            _timer.Tick += Timer_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CreateEngine();
            _engine.StartRound();

            _stopwatch.Start();
            _timer.Start();
        }

        /// <summary>
        /// Создаёт GameSettings и GameEngine с текущей сложностью.
        /// </summary>
        private void CreateEngine()
        {
            _settings = new GameSettings(
                fieldWidth: this.ClientSize.Width,
                fieldHeight: this.ClientSize.Height,
                maxScore: 5,
                aiDifficulty: _currentDifficulty);

            _engine = new GameEngine(_settings);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_engine == null)
                return;

            float dt = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            _engine.Update(dt);

            Invalidate(); // перерисовать форму
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (_engine == null)
                return;

            // ЛЕВАЯ ракетка: W / S
            if (e.KeyCode == Keys.W)
            {
                _engine.LeftPaddle.DirectionY = -1;
            }
            else if (e.KeyCode == Keys.S)
            {
                _engine.LeftPaddle.DirectionY = 1;
            }

            // Пауза / продолжить: ESC
            if (e.KeyCode == Keys.Escape)
            {
                if (_engine.State == GameState.Playing)
                    _engine.Pause();
                else if (_engine.State == GameState.Paused)
                    _engine.Resume();
            }

            // SPACE — старт / новая игра
            if (e.KeyCode == Keys.Space)
            {
                if (_engine.State == GameState.WaitingToStart)
                {
                    _engine.StartRound();
                }
                else if (_engine.State == GameState.GameOver)
                {
                    _engine.ResetGame();
                    _engine.StartRound();
                }
            }

            // Выбор сложности: 1 / 2 / 3
            if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
            {
                ChangeDifficulty(AIDifficulty.Easy);
            }
            else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                ChangeDifficulty(AIDifficulty.Normal);
            }
            else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
            {
                ChangeDifficulty(AIDifficulty.Hard);
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (_engine == null)
                return;

            // Останавливаем левую ракетку, когда отпускаем W/S
            if (e.KeyCode == Keys.W && _engine.LeftPaddle.DirectionY < 0)
            {
                _engine.LeftPaddle.DirectionY = 0;
            }
            else if (e.KeyCode == Keys.S && _engine.LeftPaddle.DirectionY > 0)
            {
                _engine.LeftPaddle.DirectionY = 0;
            }
        }

        /// <summary>
        /// Меняет сложность ИИ, пересоздаёт движок и начинает новый раунд.
        /// </summary>
        private void ChangeDifficulty(AIDifficulty difficulty)
        {
            _currentDifficulty = difficulty;
            CreateEngine();
            _engine.StartRound();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_engine == null)
                return;

            Graphics g = e.Graphics;
            g.Clear(Color.Black);

            // центральная линия
            using (Pen pen = new Pen(Color.DarkGray, 2))
            {
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

            // Текущая сложность
            using (Font font = new Font("Consolas", 10, FontStyle.Regular))
            using (Brush brush = new SolidBrush(Color.LightGray))
            {
                string diffText = "Сложность: " + DifficultyToText(_currentDifficulty);
                g.DrawString(diffText, font, brush, 10, 10);
            }

            // Подсказки управления
            using (Font font = new Font("Consolas", 9))
            using (Brush brush = new SolidBrush(Color.LightGray))
            {
                string controls1 = "Управление: W / S - левая ракетка";
                string controls2 = "SPACE - старт / новая игра, ESC - пауза";
                string controls3 = "1 - Лёгкий, 2 - Нормальный, 3 - Сложный";

                float y = ClientSize.Height - 50;

                g.DrawString(controls1, font, brush, 10, y);
                g.DrawString(controls2, font, brush, 10, y + 15);
                g.DrawString(controls3, font, brush, 10, y + 30);
            }

            // Сообщения состояния
            using (Font font = new Font("Consolas", 12))
            using (Brush brush = new SolidBrush(Color.LightGray))
            {
                string msg = null;

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
                        ClientSize.Height / 2f - s.Height / 2f);
                }
            }
        }

        private string DifficultyToText(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return "Лёгкий";
                case AIDifficulty.Hard:
                    return "Сложный";
                default:
                    return "Нормальный";
            }
        }
    }
}
