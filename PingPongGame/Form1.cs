using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using PingPongGame.GameLogic;
using PingPongGame.GameUI;
using PingPongGame.Data;

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

        private DateTime _gameStartTime;
        private bool _scoreSaved;

        // ДЛЯ ДИАЛОГОВ (UI-ветка)
        private DialogState _dialogState = DialogState.None;
        private DialogAction _pendingAction = DialogAction.None;
        private string _dialogTitle = "";
        private string _dialogMessage = "";

        private enum DialogState
        {
            None,
            Showing
        }

        private enum DialogAction
        {
            None,
            Restart,
            Menu,
            Help
        }

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
            this.MinimumSize = new Size(800, 600);

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

            // Инициализируем верхнюю панель (адаптивная разметка из ui)
            InitializeTopPanel();

            // ОБРАБОТЧИКИ
            this.Load += Form1_Load;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
            this.FormClosing += Form1_FormClosing;
            _gamePanel.Paint += GamePanel_Paint;
            this.Resize += Form1_Resize;
            _gamePanel.MouseClick += GamePanel_MouseClick;

            // Таймер обновления игры
            _timer.Interval = 16;
            _timer.Tick += Timer_Tick;

            // Таймер обновления UI (часы)
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

            // Обработчик изменения размера верхней панели
            _topPanel.Resize += TopPanel_Resize;

            // Создаем содержимое верхней панели
            CreateTopPanelContent();
        }

        private void CreateTopPanelContent()
        {
            _topPanel.Controls.Clear();

            // Рассчитываем размеры пропорционально ширине окна (ui-ветка)
            int totalWidth = _topPanel.Width;
            int leftWidth = (int)(totalWidth * 0.25);  // 25% ширины
            int centerWidth = (int)(totalWidth * 0.5); // 50% ширины
            int rightWidth = (int)(totalWidth * 0.25); // 25% ширины

            // Левый блок: Никнейм и сложность
            Panel leftPanel = new Panel
            {
                Size = new Size(leftWidth - 20, _topPanel.Height),
                Location = new Point(10, 0),
                BackColor = Color.Transparent
            };

            _lblPlayerName = new Label
            {
                Text = $"👤 {_playerName}",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ThemeManager.Colors.Accent,
                Size = new Size(leftWidth - 40, 30),
                Location = new Point(15, 15),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            _lblDifficulty = new Label
            {
                Text = $"Сложность: {DifficultyToText(_currentDifficulty)}",
                Font = new Font("Segoe UI", 11),
                ForeColor = GetDifficultyColor(_currentDifficulty),
                Size = new Size(leftWidth - 40, 25),
                Location = new Point(15, 45),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            leftPanel.Controls.Add(_lblPlayerName);
            leftPanel.Controls.Add(_lblDifficulty);

            // Центральный блок: Счет
            Panel centerPanel = new Panel
            {
                Size = new Size(centerWidth, _topPanel.Height),
                Location = new Point(leftWidth, 0),
                BackColor = Color.Transparent
            };

            _lblScore = new Label
            {
                Text = "0 : 0",
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(centerWidth, _topPanel.Height),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            centerPanel.Controls.Add(_lblScore);

            // Правый блок: Таймер и справка
            Panel rightPanel = new Panel
            {
                Size = new Size(rightWidth - 10, _topPanel.Height),
                Location = new Point(leftWidth + centerWidth, 0),
                BackColor = Color.Transparent
            };

            _lblGameTime = new Label
            {
                Text = "⏱ 00:00",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ThemeManager.Colors.Accent,
                Size = new Size(rightWidth - 40, 30),
                Location = new Point(10, 15),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            _lblHelp = new Label
            {
                Text = "F1 - Справка • ESC - Пауза",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(180, 180, 180),
                Size = new Size(rightWidth - 40, 25),
                Location = new Point(10, 45),
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

        private void TopPanel_Resize(object sender, EventArgs e)
        {
            // Пересоздаем содержимое при изменении размера
            CreateTopPanelContent();
            UpdateTopPanelColors();
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

                // Цвет счета (ui-логика + лидирующий игрок)
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
                        _lblScore.ForeColor = ThemeManager.Colors.Accent;
                    }
                }
                else
                {
                    _lblScore.ForeColor = ThemeManager.Colors.Accent;
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
            _gameStartTime = DateTime.Now;
            _scoreSaved = false;

            UIManager.StartGameTimer();
            _stopwatch.Start();
            _timer.Start();
            _uiTimer.Start();
            UpdateTopPanel();

            // Сохраняем начальные значения для отслеживания звуков
            _lastScoreLeft = _engine.ScoreLeft;
            _lastScoreRight = _engine.ScoreRight;
            _lastBallX = _engine.Ball.X;

            // Загрузка настроек звука и запуск музыки (из ui-ветки)
            SoundManager.LoadSettings();
            if (SoundManager.MusicEnabled)
            {
                SoundManager.PlayMusic();
            }
        }

        private void CreateEngine()
        {
            // Масштаб темы под текущий размер формы
            ThemeManager.UpdateScale(this.Size);

            // Масштабируем размеры объектов
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
            if (this.WindowState != FormWindowState.Minimized)
            {
                // Обновляем масштаб темы
                ThemeManager.UpdateScale(this.Size);

                if (_gamePanel != null)
                {
                    CreateEngine();

                    // Обновляем верхнюю панель при ресайзе
                    if (_topPanel != null)
                    {
                        CreateTopPanelContent();
                    }

                    _gamePanel.Invalidate();
                    UpdateTopPanel();
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_engine == null) return;

            float dt = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            // Если показывается диалог - не обновляем игру, только перерисовываем
            if (_dialogState == DialogState.Showing)
            {
                _gamePanel.Invalidate();
                return;
            }

            // Если игра на паузе - только перерисовываем
            if (_engine.State == GameState.Paused)
            {
                _gamePanel.Invalidate();
                return;
            }

            // Положение мяча до обновления (для хит-детекта звуков)
            float ballXBefore = _engine.Ball.X;
            float ballYBefore = _engine.Ball.Y;

            _engine.Update(dt);

            // --- СОХРАНЕНИЕ РЕЗУЛЬТАТА В БД ПРИ GAME OVER (из game-logic) ---
            if (_engine.State == GameState.GameOver && !_scoreSaved)
            {
                _scoreSaved = true;

                bool isWin = _engine.ScoreLeft > _engine.ScoreRight; // Игрок = левая ракетка
                int playerScore = _engine.ScoreLeft;
                int opponentScore = _engine.ScoreRight;
                int durationSec = (int)(DateTime.Now - _gameStartTime).TotalSeconds;

                try
                {
                    Database.SaveScore(new Database.ScoreRecord
                    {
                        PlayerName = _playerName,
                        Difficulty = _currentDifficulty,
                        PlayerScore = playerScore,
                        OpponentScore = opponentScore,
                        DurationSeconds = durationSec,
                        IsWin = isWin,
                        PlayedAt = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Ошибка сохранения результата в БД:\n" + ex.Message,
                        "БД",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            // --------------------------------------------------------------

            // Продвинутые звуки (гол + отскок от ракеток)
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

            // 1. Изменение счета (гол)
            if (_engine.ScoreLeft != _lastScoreLeft || _engine.ScoreRight != _lastScoreRight)
            {
                // Используем SoundManager из ui-ветки
                SoundManager.PlayGoalSound();
                _lastScoreLeft = _engine.ScoreLeft;
                _lastScoreRight = _engine.ScoreRight;
            }

            // 2. Столкновение мяча с ракетками (хит)
            bool hitLeft = ballXBefore <= leftPaddle.X + leftPaddle.Width &&
                           ballXBefore + ball.Radius >= leftPaddle.X &&
                           ballYBefore + ball.Radius >= leftPaddle.Y &&
                           ballYBefore <= leftPaddle.Y + leftPaddle.Height &&
                           ball.VelocityX > 0; // летел вправо

            bool hitRight = ballXBefore + ball.Radius >= rightPaddle.X &&
                            ballXBefore <= rightPaddle.X + rightPaddle.Width &&
                            ballYBefore + ball.Radius >= rightPaddle.Y &&
                            ballYBefore <= rightPaddle.Y + rightPaddle.Height &&
                            ball.VelocityX < 0; // летел влево

            if ((hitLeft && !_lastHitLeft) || (hitRight && !_lastHitRight))
            {
             
       
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

            // Если показывается диалог - рисуем только его поверх игры
            if (_dialogState == DialogState.Showing)
            {
                if (_pendingAction == DialogAction.Help)
                {
                    UIManager.DrawHelpDialog(e.Graphics, _gamePanel.ClientRectangle);
                }
                else
                {
                    UIManager.DrawCustomConfirmDialog(
                        e.Graphics,
                        _gamePanel.ClientRectangle,
                        _dialogTitle,
                        _dialogMessage);
                }
                return;
            }

            // Пауза
            if (_engine.State == GameState.Paused)
            {
                UIManager.DrawPauseOverlay(e.Graphics, _gamePanel.ClientRectangle);
            }

            // Сообщения
            if (_engine.State == GameState.WaitingToStart)
            {
                string text = "НАЖМИТЕ SPACE ДЛЯ НАЧАЛА";
                using (Font font = new Font("Segoe UI", 24, FontStyle.Bold))
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
                Color winnerColor = _engine.ScoreLeft > _engine.ScoreRight
                    ? ThemeManager.Colors.Player1
                    : ThemeManager.Colors.Player2;

                using (Font font1 = new Font("Segoe UI", 28, FontStyle.Bold))
                using (Font font2 = new Font("Segoe UI", 18, FontStyle.Regular))
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
                _lblScore.Text = $"{_engine.ScoreLeft} : {_engine.ScoreRight}";

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
                    _lblScore.ForeColor = ThemeManager.Colors.Accent;
                }
            }
        }

        // ---------- Диалоговая система (из ui-ветки) ----------

        private void ShowDialog(DialogAction action, string title = "", string message = "")
        {
            _dialogState = DialogState.Showing;
            _pendingAction = action;
            _dialogTitle = title;
            _dialogMessage = message;

            // Паузим игру и таймер
            if (_engine != null && _engine.State == GameState.Playing)
            {
                _engine.Pause();
                UIManager.PauseTimer();
            }

            _gamePanel.Invalidate();
        }

        private void CloseDialog()
        {
            _dialogState = DialogState.None;
            _pendingAction = DialogAction.None;
            _dialogTitle = "";
            _dialogMessage = "";

            // Возобновляем игру и таймер, если была пауза
            if (_engine != null &&
                _engine.State == GameState.Paused &&
                !(_engine.State == GameState.WaitingToStart || _engine.State == GameState.GameOver))
            {
                _engine.Resume();
                UIManager.ResumeTimer();
            }

            _gamePanel.Invalidate();
        }

        private void ConfirmDialog(bool result)
        {
            if (result)
            {
                switch (_pendingAction)
                {
                    case DialogAction.Restart:
                        _engine.ResetGame();
                        _engine.StartRound();
                        UIManager.StartGameTimer(); // Сбрасываем и запускаем таймер
                        _lastScoreLeft = 0;
                        _lastScoreRight = 0;
                        _gameStartTime = DateTime.Now;
                        _scoreSaved = false;
                        break;

                    case DialogAction.Menu:
                        ThemeManager.OnThemeChanged -= ThemeManager_OnThemeChanged;
                        this.Close();
                        Application.Restart();
                        return;
                }
            }

            CloseDialog();
        }

        // ------------------------------------------------------

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (_engine == null) return;

            // Если открыт диалог - обрабатываем только ESC и Enter
            if (_dialogState == DialogState.Showing)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    if (_pendingAction == DialogAction.Help)
                    {
                        CloseDialog();
                    }
                    else
                    {
                        ConfirmDialog(false); // ESC = Нет
                    }
                }
                else if (e.KeyCode == Keys.Enter && _pendingAction != DialogAction.Help)
                {
                    // Enter = Да
                    ConfirmDialog(true);
                }
                return;
            }

            // Управление
            if (e.KeyCode == Keys.W) _engine.LeftPaddle.DirectionY = -1;
            if (e.KeyCode == Keys.S) _engine.LeftPaddle.DirectionY = 1;
            if (e.KeyCode == Keys.Up) _engine.RightPaddle.DirectionY = -1;
            if (e.KeyCode == Keys.Down) _engine.RightPaddle.DirectionY = 1;

            // Пауза
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.P)
            {
                if (_engine.State == GameState.Playing)
                {
                    _engine.Pause();
                    UIManager.PauseTimer();
                }
                else if (_engine.State == GameState.Paused)
                {
                    _engine.Resume();
                    UIManager.ResumeTimer();
                }
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
                    _gameStartTime = DateTime.Now;
                    _scoreSaved = false;
                }
                else if (_engine.State == GameState.GameOver)
                {
                    _engine.ResetGame();
                    _engine.StartRound();
                    UIManager.StartGameTimer();
                    _lastScoreLeft = 0;
                    _lastScoreRight = 0;
                    _gameStartTime = DateTime.Now;
                    _scoreSaved = false;
                }
            }

            // Рестарт (через кастомный диалог)
            if (e.KeyCode == Keys.R)
            {
                ShowDialog(DialogAction.Restart, "РЕСТАРТ", "Начать новую игру?");
            }

            // Меню (через кастомный диалог)
            if (e.KeyCode == Keys.M)
            {
                ShowDialog(DialogAction.Menu, "ВЫХОД В МЕНЮ", "Вернуться в главное меню?");
            }

            // Справка (отрисовывается поверх игры)
            if (e.KeyCode == Keys.F1)
            {
                ShowDialog(DialogAction.Help);
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

        private void GamePanel_MouseClick(object sender, MouseEventArgs e)
        {
            // Обработка кликов по кнопкам Да/Нет в диалоге подтверждения
            if (_dialogState != DialogState.Showing || _pendingAction == DialogAction.Help)
                return;

            Rectangle bounds = _gamePanel.ClientRectangle;

            // Координаты кнопок (должны совпадать с DrawCustomConfirmDialog)
            RectangleF yesButton = new RectangleF(
                bounds.Width / 2 - ThemeManager.Scaled(120),
                bounds.Height / 2 + ThemeManager.Scaled(30),
                ThemeManager.Scaled(100),
                ThemeManager.Scaled(40));

            RectangleF noButton = new RectangleF(
                bounds.Width / 2 + ThemeManager.Scaled(20),
                bounds.Height / 2 + ThemeManager.Scaled(30),
                ThemeManager.Scaled(100),
                ThemeManager.Scaled(40));

            if (yesButton.Contains(e.Location))
            {
                ConfirmDialog(true);
            }
            else if (noButton.Contains(e.Location))
            {
                ConfirmDialog(false);
            }
        }

        private string DifficultyToText(AIDifficulty difficulty)
        {
            if (difficulty == AIDifficulty.Easy) return "ЛЕГКИЙ";
            if (difficulty == AIDifficulty.Hard) return "СЛОЖНЫЙ";
            return "НОРМАЛЬНЫЙ";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Если игра идет и диалога еще нет - показываем диалог выхода
            if (_engine != null &&
                _engine.State == GameState.Playing &&
                _dialogState == DialogState.None)
            {
                ShowDialog(DialogAction.Menu, "ВЫХОД ИЗ ИГРЫ", "Вы действительно хотите выйти?");
                e.Cancel = true;
                return;
            }

            // Останавливаем таймеры
            if (_timer != null && _timer.Enabled)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            if (_uiTimer != null && _uiTimer.Enabled)
            {
                _uiTimer.Stop();
                _uiTimer.Dispose();
            }

            // Отписываемся от события
            ThemeManager.OnThemeChanged -= ThemeManager_OnThemeChanged;
        }
    }
}
