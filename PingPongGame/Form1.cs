using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using PingPongGame.GameLogic;
using PingPongGame.GameUI;

namespace PingPongGame
{
    public partial class Form1 : BufferedForm
    {
        private GameEngine _engine;
        private GameSettings _settings;
        private GameRenderer _gameRenderer;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Timer _timer = new Timer();
        private readonly Timer _uiTimer = new Timer();

        private string _playerName;
        private AIDifficulty _currentDifficulty;

        // Для отслеживания звуков
        private int _lastScoreLeft = 0;
        private int _lastScoreRight = 0;
        private float _lastBallX = 0;
        private bool _lastHitLeft = false;
        private bool _lastHitRight = false;

        // Панели интерфейса
        private Panel _topPanel;
        private BufferedPanel _gamePanel;
        private Label _lblPlayerName;
        private Label _lblDifficulty;
        private Label _lblScore;
        private Label _lblGameTime;
        private Label _lblHelp;

        public Form1(string playerName, AIDifficulty difficulty)
        {
            _playerName = playerName;
            _currentDifficulty = difficulty;

            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            this.KeyPreview = true;
            this.Text = $"Ping Pong - {_playerName}";
            this.BackColor = ThemeManager.Colors.Background;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1200, 800);

            // Подписываемся на событие смены темы
            ThemeManager.OnThemeChanged += ThemeManager_OnThemeChanged;

            // Создаем верхнюю панель
            _topPanel = new Panel
            {
                Name = "topPanel",
                BackColor = Color.FromArgb(40, 40, 60),
                ForeColor = Color.White,
                Height = ThemeManager.Scaled(80),
                Dock = DockStyle.Top
            };

            // Создаем игровую панель с двойной буферизацией
            _gamePanel = new BufferedPanel()
            {
                Name = "gamePanel",
                BackColor = ThemeManager.Colors.Background,
                Dock = DockStyle.Fill
            };

            // Добавляем панели на форму
            this.Controls.Add(_gamePanel);
            this.Controls.Add(_topPanel);

            // Инициализируем верхнюю панель
            InitializeTopPanel();

            // ОБРАБОТЧИКИ
            this.Load += Form1_Load;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
            this.FormClosing += Form1_FormClosing;
            _gamePanel.Paint += GamePanel_Paint;
            this.Resize += Form1_Resize;

            // Таймер обновления игры
            _timer.Interval = 16;
            _timer.Tick += Timer_Tick;

            // Таймер обновления UI
            _uiTimer.Interval = 1000;
            _uiTimer.Tick += UiTimer_Tick;
        }

        private void ThemeManager_OnThemeChanged()
        {
            // Мгновенное обновление цветов в игре
            this.BackColor = ThemeManager.Colors.Background;
            _gamePanel.BackColor = ThemeManager.Colors.Background;
            _topPanel.Invalidate();
            _gamePanel.Invalidate();

            // Обновляем цвета верхней панели
            UpdateTopPanelColors();
        }

        private void InitializeTopPanel()
        {
            _topPanel.Controls.Clear();

            // Левый блок: Никнейм и сложность
            Panel leftPanel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(300, 80)),
                Location = new Point(20, 0),
                BackColor = Color.Transparent
            };

            _lblPlayerName = new Label
            {
                Text = $"👤 {_playerName}",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 14, FontStyle.Bold)),
                ForeColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(250, 30)),
                Location = ThemeManager.ScaledPoint(new Point(35, 15)),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            _lblDifficulty = new Label
            {
                Text = $"Сложность: {DifficultyToText(_currentDifficulty)}",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 11)),
                ForeColor = GetDifficultyColor(_currentDifficulty),
                Size = ThemeManager.ScaledSize(new Size(250, 25)),
                Location = ThemeManager.ScaledPoint(new Point(35, 45)),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            leftPanel.Controls.Add(_lblPlayerName);
            leftPanel.Controls.Add(_lblDifficulty);

            // Центральный блок: Счет
            Panel centerPanel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(400, 80)),
                Location = new Point(400, 0),
                BackColor = Color.Transparent
            };

            _lblScore = new Label
            {
                Text = "0 : 0",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 36, FontStyle.Bold), 1.2f),
                ForeColor = Color.White,
                Size = ThemeManager.ScaledSize(new Size(400, 80)),
                Location = ThemeManager.ScaledPoint(new Point(0, 0)),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            centerPanel.Controls.Add(_lblScore);

            // Правый блок: Таймер и справка
            Panel rightPanel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(300, 80)),
                Location = new Point(820, 0),
                BackColor = Color.Transparent
            };

            _lblGameTime = new Label
            {
                Text = "⏱ 00:00",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 14, FontStyle.Bold)),
                ForeColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(150, 30)),
                Location = ThemeManager.ScaledPoint(new Point(153, 15)),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            _lblHelp = new Label
            {
                Text = "F1 - Справка • ESC - Пауза",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 11)),
                ForeColor = Color.FromArgb(180, 180, 180),
                Size = ThemeManager.ScaledSize(new Size(250, 25)),
                Location = ThemeManager.ScaledPoint(new Point(50, 45)),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            rightPanel.Controls.Add(_lblGameTime);
            rightPanel.Controls.Add(_lblHelp);

            // Добавляем все блоки на верхнюю панель
            _topPanel.Controls.Add(leftPanel);
            _topPanel.Controls.Add(centerPanel);
            _topPanel.Controls.Add(rightPanel);

            // Линия разделения с эффектом свечения
            _topPanel.Paint += (s, e) =>
            {
                using (Pen linePen = new Pen(Color.FromArgb(100, ThemeManager.Colors.Accent), 3))
                {
                    e.Graphics.DrawLine(linePen, 0, _topPanel.Height - 2,
                        _topPanel.Width, _topPanel.Height - 2);
                }

                // Свечение под линией
                using (Pen glowPen = new Pen(Color.FromArgb(30, ThemeManager.Colors.Accent), 5))
                {
                    e.Graphics.DrawLine(glowPen, 0, _topPanel.Height,
                        _topPanel.Width, _topPanel.Height);
                }
            };
        }

        private void UpdateTopPanelColors()
        {
            if (_lblPlayerName != null)
            {
                _lblPlayerName.ForeColor = ThemeManager.Colors.Accent;
                _lblGameTime.ForeColor = ThemeManager.Colors.Accent;
                _lblHelp.ForeColor = Color.FromArgb(180, 180, 180);

                // Обновляем цвет сложности
                _lblDifficulty.ForeColor = GetDifficultyColor(_currentDifficulty);

                // Обновляем цвет счета
                if (_engine != null)
                {
                    if (_engine.ScoreLeft > _engine.ScoreRight)
                    {
                        _lblScore.ForeColor = ThemeManager.Colors.Player1;
                    }
                    else if (_engine.ScoreRight > _engine.ScoreLeft)
                    {
                        _lblScore.ForeColor = ThemeManager.Colors.Player2;
                    }
                    else
                    {
                        _lblScore.ForeColor = Color.White;
                    }
                }
            }
        }

        private Color GetDifficultyColor(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return Color.LightGreen;
                case AIDifficulty.Normal: return Color.LightYellow;
                case AIDifficulty.Hard: return Color.LightCoral;
                default: return Color.White;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CreateEngine();
            _engine.StartRound();
            UIManager.StartGameTimer();
            _stopwatch.Start();
            _timer.Start();
            _uiTimer.Start();
            UpdateTopPanel();

            // Сохраняем начальные значения для отслеживания звуков
            _lastScoreLeft = _engine.ScoreLeft;
            _lastScoreRight = _engine.ScoreRight;
            _lastBallX = _engine.Ball.X;
        }

        private void CreateEngine()
        {
            ThemeManager.UpdateScale(_gamePanel.ClientSize);

            // Масштабируем размеры объектов - ДЕЛАЕМ ИХ БОЛЬШЕ
            _settings = new GameSettings(
                fieldWidth: _gamePanel.ClientSize.Width,
                fieldHeight: _gamePanel.ClientSize.Height,
                maxScore: 5,
                aiDifficulty: _currentDifficulty);

            _engine = new GameEngine(_settings);
            _gameRenderer = new GameRenderer(_engine);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (_engine != null && this.WindowState != FormWindowState.Minimized)
            {
                CreateEngine();
                _gamePanel.Invalidate();
                UpdateTopPanel();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_engine == null) return;

            float dt = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            // Сохраняем положение мяча до обновления
            float ballXBefore = _engine.Ball.X;
            float ballYBefore = _engine.Ball.Y;

            _engine.Update(dt);

            // Проверяем звуки ПОСЛЕ обновления игры
            CheckSounds(ballXBefore, ballYBefore);

            // Обновляем счет
            UpdateTopPanel();

            _gamePanel.Invalidate();
        }

        private void CheckSounds(float ballXBefore, float ballYBefore)
        {
            if (_engine == null) return;

            var ball = _engine.Ball;
            var leftPaddle = _engine.LeftPaddle;
            var rightPaddle = _engine.RightPaddle;

            // 1. Проверяем изменение счета
            if (_engine.ScoreLeft != _lastScoreLeft || _engine.ScoreRight != _lastScoreRight)
            {
                ThemeManager.PlayScoreSound();
                _lastScoreLeft = _engine.ScoreLeft;
                _lastScoreRight = _engine.ScoreRight;
            }

            // 2. Проверяем столкновение мяча с ракетками
            // Проверяем левую ракетку
            bool hitLeft = ballXBefore <= leftPaddle.X + leftPaddle.Width &&
                          ballXBefore + ball.Radius >= leftPaddle.X &&
                          ballYBefore + ball.Radius >= leftPaddle.Y &&
                          ballYBefore <= leftPaddle.Y + leftPaddle.Height &&
                          ball.VelocityX > 0; // Двигался вправо

            // Проверяем правую ракетку
            bool hitRight = ballXBefore + ball.Radius >= rightPaddle.X &&
                           ballXBefore <= rightPaddle.X + rightPaddle.Width &&
                           ballYBefore + ball.Radius >= rightPaddle.Y &&
                           ballYBefore <= rightPaddle.Y + rightPaddle.Height &&
                           ball.VelocityX < 0; // Двигался влево

            if ((hitLeft && !_lastHitLeft) || (hitRight && !_lastHitRight))
            {
                ThemeManager.PlayHitSound();
            }

            _lastHitLeft = hitLeft;
            _lastHitRight = hitRight;
            _lastBallX = ball.X;
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            if (_engine != null)
            {
                _lblGameTime.Text = $"⏱ {UIManager.GetGameTime()}";
            }
        }

        private void GamePanel_Paint(object sender, PaintEventArgs e)
        {
            if (_engine == null || _gameRenderer == null) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            _gameRenderer.Render(e.Graphics, _gamePanel.ClientRectangle);

            // Пауза
            if (_engine.State == GameState.Paused)
            {
                UIManager.DrawPauseOverlay(e.Graphics, _gamePanel.ClientRectangle);
            }

            // Сообщения
            if (_engine.State == GameState.WaitingToStart)
            {
                string text = "НАЖМИТЕ SPACE ДЛЯ НАЧАЛА";
                using (Font font = ThemeManager.ScaledFont(new Font("Segoe UI", 24, FontStyle.Bold)))
                {
                    SizeF textSize = e.Graphics.MeasureString(text, font);

                    // Тень текста
                    e.Graphics.DrawString(text, font,
                        new SolidBrush(Color.FromArgb(100, 0, 0, 0)),
                        _gamePanel.ClientSize.Width / 2 - textSize.Width / 2 + 2,
                        _gamePanel.ClientSize.Height / 2 - textSize.Height / 2 + 2);

                    // Основной текст
                    e.Graphics.DrawString(text, font,
                        new SolidBrush(ThemeManager.Colors.Accent),
                        _gamePanel.ClientSize.Width / 2 - textSize.Width / 2,
                        _gamePanel.ClientSize.Height / 2 - textSize.Height / 2);
                }
            }
            else if (_engine.State == GameState.GameOver)
            {
                UIManager.StopGameTimer();

                string winner = _engine.ScoreLeft > _engine.ScoreRight ? "ИГРОК 1" : "ИГРОК 2";
                Color winnerColor = _engine.ScoreLeft > _engine.ScoreRight ?
                    ThemeManager.Colors.Player1 : ThemeManager.Colors.Player2;

                using (Font font1 = ThemeManager.ScaledFont(new Font("Segoe UI", 28, FontStyle.Bold)))
                using (Font font2 = ThemeManager.ScaledFont(new Font("Segoe UI", 18, FontStyle.Regular)))
                {
                    string text1 = $"ИГРА ОКОНЧЕНА! ПОБЕДИТЕЛЬ: {winner}";
                    string text2 = "SPACE - НОВАЯ ИГРА, M - МЕНЮ";

                    SizeF textSize1 = e.Graphics.MeasureString(text1, font1);
                    SizeF textSize2 = e.Graphics.MeasureString(text2, font2);

                    // Фон для текста
                    RectangleF textBg = new RectangleF(
                        _gamePanel.ClientSize.Width / 2 - textSize1.Width / 2 - 20,
                        _gamePanel.ClientSize.Height / 2 - textSize1.Height - 10,
                        textSize1.Width + 40,
                        textSize1.Height + textSize2.Height + 40);

                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(200, 30, 30, 40)))
                    using (Pen borderPen = new Pen(ThemeManager.Colors.Accent, 2))
                    {
                        e.Graphics.FillRectangle(bgBrush, textBg);
                        e.Graphics.DrawRectangle(borderPen,
                            textBg.X, textBg.Y, textBg.Width, textBg.Height);
                    }

                    // Тень текста
                    e.Graphics.DrawString(text1, font1,
                        new SolidBrush(Color.FromArgb(100, 0, 0, 0)),
                        _gamePanel.ClientSize.Width / 2 - textSize1.Width / 2 + 2,
                        _gamePanel.ClientSize.Height / 2 - textSize1.Height + 2);

                    // Основной текст
                    e.Graphics.DrawString(text1, font1,
                        new SolidBrush(winnerColor),
                        _gamePanel.ClientSize.Width / 2 - textSize1.Width / 2,
                        _gamePanel.ClientSize.Height / 2 - textSize1.Height);

                    // Инструкция
                    e.Graphics.DrawString(text2, font2,
                        new SolidBrush(ThemeManager.Colors.Text),
                        _gamePanel.ClientSize.Width / 2 - textSize2.Width / 2,
                        _gamePanel.ClientSize.Height / 2 + 20);
                }
            }
        }

        private void UpdateTopPanel()
        {
            if (_engine != null)
            {
                // Обновляем счет
                _lblScore.Text = $"{_engine.ScoreLeft} : {_engine.ScoreRight}";

                // Обновляем цвет счета в зависимости от лидера
                if (_engine.ScoreLeft > _engine.ScoreRight)
                {
                    _lblScore.ForeColor = ThemeManager.Colors.Player1;
                }
                else if (_engine.ScoreRight > _engine.ScoreLeft)
                {
                    _lblScore.ForeColor = ThemeManager.Colors.Player2;
                }
                else
                {
                    _lblScore.ForeColor = Color.White;
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (_engine == null) return;

            // Управление
            if (e.KeyCode == Keys.W) _engine.LeftPaddle.DirectionY = -1;
            if (e.KeyCode == Keys.S) _engine.LeftPaddle.DirectionY = 1;
            if (e.KeyCode == Keys.Up) _engine.RightPaddle.DirectionY = -1;
            if (e.KeyCode == Keys.Down) _engine.RightPaddle.DirectionY = 1;

            // Пауза
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.P)
            {
                if (_engine.State == GameState.Playing) _engine.Pause();
                else if (_engine.State == GameState.Paused) _engine.Resume();
            }

            // Старт/Новая игра
            if (e.KeyCode == Keys.Space)
            {
                if (_engine.State == GameState.WaitingToStart)
                {
                    _engine.StartRound();
                    UIManager.StartGameTimer();
                    _lastScoreLeft = 0;
                    _lastScoreRight = 0;
                }
                else if (_engine.State == GameState.GameOver)
                {
                    _engine.ResetGame();
                    _engine.StartRound();
                    UIManager.StartGameTimer();
                    _lastScoreLeft = 0;
                    _lastScoreRight = 0;
                }
            }

            // Рестарт
            if (e.KeyCode == Keys.R && MessageBox.Show("Начать новую игру?", "Рестарт",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _engine.ResetGame();
                _engine.StartRound();
                UIManager.StartGameTimer();
                _lastScoreLeft = 0;
                _lastScoreRight = 0;
            }

            // Меню
            if (e.KeyCode == Keys.M && MessageBox.Show("Вернуться в меню?", "Меню",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ThemeManager.OnThemeChanged -= ThemeManager_OnThemeChanged;
                this.Close();
                Application.Restart();
            }

            // Справка
            if (e.KeyCode == Keys.F1)
            {
                if (_engine.State == GameState.Playing) _engine.Pause();
                new ControlsForm().ShowDialog();
                if (_engine.State == GameState.Paused) _engine.Resume();
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (_engine == null) return;

            if (e.KeyCode == Keys.W || e.KeyCode == Keys.S)
                _engine.LeftPaddle.DirectionY = 0;
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
                _engine.RightPaddle.DirectionY = 0;
        }

        private string DifficultyToText(AIDifficulty difficulty)
        {
            if (difficulty == AIDifficulty.Easy) return "ЛЕГКИЙ";
            if (difficulty == AIDifficulty.Hard) return "СЛОЖНЫЙ";
            return "НОРМАЛЬНЫЙ";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_engine != null && _engine.State == GameState.Playing)
            {
                if (MessageBox.Show("Выйти из игры?", "Выход",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }

            // Отписываемся от события
            ThemeManager.OnThemeChanged -= ThemeManager_OnThemeChanged;
        }
    }

    // Форма справки
    public class ControlsForm : Form
    {
        public ControlsForm()
        {
            this.Text = "Управление";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(40, 40, 60);

            Label label = new Label();
            label.Text = "W/S - Игрок 1 (левая ракетка)\n" +
                         "↑/↓ - Игрок 2 (правая ракетка)\n" +
                         "ESC или P - Пауза\n" +
                         "SPACE - Старт/Новая игра\n" +
                         "R - Рестарт\n" +
                         "M - Меню\n" +
                         "F1 - Справка";
            label.Font = new Font("Arial", 14);
            label.ForeColor = Color.White;
            label.Size = new Size(380, 250);
            label.Location = new Point(10, 10);

            this.Controls.Add(label);
        }
    }
}