using PingPongGame.GameLogic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PingPongGame.Data;


namespace PingPongGame
{
    public partial class MenuForm : Form
    {
        public string PlayerName { get; private set; } = "Player";
        public AIDifficulty SelectedDifficulty { get; private set; } = AIDifficulty.Normal;
        public GameSettingsManager.GameSettingsData Settings { get; private set; }

        // Элементы интерфейса
        private Panel _menuPanel;
        private Panel _contentPanel;
        private Panel _currentContent;
        private Button _currentButton;
        private List<Control> _allControls = new List<Control>();

        public MenuForm()
        {
            Settings = GameSettingsManager.LoadSettings();

            // Подписываемся на событие смены темы
            ThemeManager.OnThemeChanged += ThemeManager_OnThemeChanged;

            InitializeForm();
            CreateInterface();
        }

        private void InitializeForm()
        {
            this.Text = "Ping Pong - Меню";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = ThemeManager.Colors.Background;
            this.Paint += MenuForm_Paint;
        }

        private void CreateInterface()
        {
            this.Controls.Clear();
            _allControls.Clear();

            // Заголовок
            Label titleLabel = new Label
            {
                Text = "PING PONG",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 48, FontStyle.Bold), 1.2f),
                ForeColor = ThemeManager.Colors.Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = ThemeManager.ScaledSize(new Size(1000, 120)),
                Location = ThemeManager.ScaledPoint(new Point(0, 20)),
                BackColor = Color.Transparent
            };
            _allControls.Add(titleLabel);

            // Кнопка закрытия
            Button closeButton = new Button
            {
                Text = "✕",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 14)),
                ForeColor = Color.White,
                BackColor = ThemeManager.Colors.Accent,
                FlatStyle = FlatStyle.Flat,
                Size = ThemeManager.ScaledSize(new Size(40, 40)),
                Location = ThemeManager.ScaledPoint(new Point(940, 20)),
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => Application.Exit();
            _allControls.Add(closeButton);

            // Основной контейнер
            Panel mainContainer = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(900, 480)),
                Location = ThemeManager.ScaledPoint(new Point(50, 150)),
                BackColor = Color.Transparent
            };
            _allControls.Add(mainContainer);

            // Левая панель меню
            _menuPanel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(300, 480)),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(40, 40, 60)
            };
            _menuPanel.Paint += MenuPanel_Paint;
            _allControls.Add(_menuPanel);

            // Правая панель контента
            _contentPanel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(580, 480)),
                Location = new Point(320, 0),
                BackColor = Color.FromArgb(40, 40, 60)
            };
            _contentPanel.Paint += ContentPanel_Paint;
            _allControls.Add(_contentPanel);

            // Заголовок меню
            Label menuTitle = new Label
            {
                Text = "МЕНЮ",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 18, FontStyle.Bold)),
                ForeColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(280, 40)),
                Location = ThemeManager.ScaledPoint(new Point(10, 10)),
                BackColor = Color.Transparent
            };
            _menuPanel.Controls.Add(menuTitle);
            _allControls.Add(menuTitle);

            // Кнопки меню
            Button playButton = CreateMenuButton("🎮 ИГРАТЬ", 30, 50);
            Button settingsButton = CreateMenuButton("⚙️ НАСТРОЙКИ", 30, 120);
            Button scoresButton = CreateMenuButton("🏆 ТАБЛИЦА ЛИДЕРОВ", 30, 190);
            Button aboutButton = CreateMenuButton("ℹ️ О ПРОЕКТЕ", 30, 260);
            Button exitButton = CreateMenuButton("🚪 ВЫХОД", 30, 380);

            // Создаем контент панели
            Panel playContent = CreatePlayContent();
            Panel settingsContent = CreateSettingsContent();
            Panel scoresContent = CreateScoresContent();
            Panel aboutContent = CreateAboutContent();

            // Добавляем контент
            _contentPanel.Controls.Add(playContent);
            _contentPanel.Controls.Add(settingsContent);
            _contentPanel.Controls.Add(scoresContent);
            _contentPanel.Controls.Add(aboutContent);

            // Показываем только первый контент
            playContent.Visible = true;
            settingsContent.Visible = false;
            scoresContent.Visible = false;
            aboutContent.Visible = false;
            _currentContent = playContent;

            // Обработчики для кнопок
            playButton.Click += (s, e) => SwitchContent(playContent, playButton);
            settingsButton.Click += (s, e) => SwitchContent(settingsContent, settingsButton);
            scoresButton.Click += (s, e) => SwitchContent(scoresContent, scoresButton);
            aboutButton.Click += (s, e) => SwitchContent(aboutContent, aboutButton);
            exitButton.Click += (s, e) => Application.Exit();

            // Активируем первую кнопку
            playButton.BackColor = ThemeManager.Colors.Accent;
            playButton.Font = new Font(playButton.Font, FontStyle.Bold);
            _currentButton = playButton;

            // Добавляем кнопки в панель
            _menuPanel.Controls.Add(playButton);
            _menuPanel.Controls.Add(settingsButton);
            _menuPanel.Controls.Add(scoresButton);
            _menuPanel.Controls.Add(aboutButton);
            _menuPanel.Controls.Add(exitButton);

            // Добавляем все кнопки в список для обновления
            _allControls.AddRange(new Control[] { playButton, settingsButton, scoresButton, aboutButton, exitButton });

            mainContainer.Controls.Add(_menuPanel);
            mainContainer.Controls.Add(_contentPanel);

            this.Controls.Add(titleLabel);
            this.Controls.Add(closeButton);
            this.Controls.Add(mainContainer);
        }

        private void MenuPanel_Paint(object sender, PaintEventArgs e)
        {
            // Рисуем рамку с цветом акцента текущей темы
            using (Pen borderPen = new Pen(ThemeManager.Colors.Accent, 2))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, _menuPanel.Width - 1, _menuPanel.Height - 1);
            }

            // Верхняя акцентная полоса
            using (Brush accentBrush = new SolidBrush(ThemeManager.Colors.Accent))
            {
                e.Graphics.FillRectangle(accentBrush, 0, 0, _menuPanel.Width, 3);
            }
        }

        private void ContentPanel_Paint(object sender, PaintEventArgs e)
        {
            // Рисуем рамку с цветом акцента текущей темы
            using (Pen borderPen = new Pen(ThemeManager.Colors.Accent, 2))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, _contentPanel.Width - 1, _contentPanel.Height - 1);
            }

            // Верхняя акцентная полоса
            using (Brush accentBrush = new SolidBrush(ThemeManager.Colors.Accent))
            {
                e.Graphics.FillRectangle(accentBrush, 0, 0, _contentPanel.Width, 3);
            }
        }

        private Button CreateMenuButton(string text, int x, int y)
        {
            Button button = new Button
            {
                Text = text,
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 13)),
                ForeColor = ThemeManager.Colors.Text,
                BackColor = Color.FromArgb(60, 60, 80),
                Size = ThemeManager.ScaledSize(new Size(240, 50)),
                Location = ThemeManager.ScaledPoint(new Point(x, y)),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(ThemeManager.Scaled(15), 0, 0, 0)
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ThemeManager.Colors.Accent;

            return button;
        }

        private void SwitchContent(Panel content, Button button)
        {
            if (_currentContent != null)
                _currentContent.Visible = false;

            if (_currentButton != null)
            {
                _currentButton.BackColor = Color.FromArgb(60, 60, 80);
                _currentButton.Font = new Font(_currentButton.Font, FontStyle.Regular);
            }

            content.Visible = true;
            _currentContent = content;

            button.BackColor = ThemeManager.Colors.Accent;
            button.Font = new Font(button.Font, FontStyle.Bold);
            _currentButton = button;
        }

        private Panel CreatePlayContent()
        {
            Panel panel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(560, 460)),
                Location = ThemeManager.ScaledPoint(new Point(10, 10)),
                BackColor = Color.Transparent
            };

            int yPos = 30;

            // Никнейм
            Label nameLabel = new Label
            {
                Text = "Ваш никнейм:",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 13, FontStyle.Bold)),
                ForeColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(160, 30)),
                Location = ThemeManager.ScaledPoint(new Point(30, yPos)),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(nameLabel);
            _allControls.Add(nameLabel);

            TextBox nameTextBox = new TextBox
            {
                Text = Settings.PlayerName,
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 12)),
                Size = ThemeManager.ScaledSize(new Size(250, 35)),
                Location = ThemeManager.ScaledPoint(new Point(200, yPos)),
                BackColor = Color.FromArgb(60, 60, 80),
                ForeColor = ThemeManager.Colors.Text,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(nameTextBox);
            _allControls.Add(nameTextBox);

            yPos += 60;

            // Сложность
            Label diffLabel = new Label
            {
                Text = "Сложность:",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 13, FontStyle.Bold)),
                ForeColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(160, 30)),
                Location = ThemeManager.ScaledPoint(new Point(30, yPos)),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(diffLabel);
            _allControls.Add(diffLabel);

            ComboBox diffComboBox = new ComboBox
            {
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 12)),
                Size = ThemeManager.ScaledSize(new Size(250, 35)),
                Location = ThemeManager.ScaledPoint(new Point(200, yPos)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 80),
                ForeColor = ThemeManager.Colors.Text
            };
            diffComboBox.Items.AddRange(new[] { "ЛЕГКИЙ", "НОРМАЛЬНЫЙ", "СЛОЖНЫЙ" });
            diffComboBox.SelectedIndex = (int)Settings.Difficulty;
            panel.Controls.Add(diffComboBox);
            _allControls.Add(diffComboBox);

            yPos += 100;

            // Кнопка старта
            Button startButton = new Button
            {
                Text = "🎮 НАЧАТЬ ИГРУ",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 16, FontStyle.Bold)),
                ForeColor = Color.Black,
                BackColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(300, 50)),
                Location = ThemeManager.ScaledPoint(new Point(130, yPos)),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            startButton.FlatAppearance.BorderSize = 0;
            startButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 100, 200);
            startButton.Click += (s, e) =>
            {
                PlayerName = string.IsNullOrWhiteSpace(nameTextBox.Text) ? "Player" : nameTextBox.Text;
                SelectedDifficulty = (AIDifficulty)diffComboBox.SelectedIndex;

                Settings.PlayerName = PlayerName;
                Settings.Difficulty = SelectedDifficulty;
                GameSettingsManager.SaveSettings(Settings);

                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            panel.Controls.Add(startButton);
            _allControls.Add(startButton);

            return panel;
        }

        private Panel CreateSettingsContent()
        {
            Panel panel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(560, 460)),
                Location = ThemeManager.ScaledPoint(new Point(10, 10)),
                BackColor = Color.Transparent
            };

            int yPos = 30;

            // Цветовые темы
            string[,] themes = {
                { "Неон Розовый", "0A0A0F", "FF00FF", "FF00FF", "FFFFFF", "FFFFFF", "1A1A2E", "FF00FF" },
                { "Неон Фиолет", "0F0F1A", "9D00FF", "9D00FF", "00FFFF", "F0F0FF", "1E1B2E", "9D00FF" },
                { "Неон Оранж", "1A0F0A", "FF7700", "FF7700", "FFFF00", "FFFFFF", "2E1A1A", "FF7700" }
            };

            Label themeLabel = new Label
            {
                Text = "Цветовая тема:",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 13, FontStyle.Bold)),
                ForeColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(160, 30)),
                Location = ThemeManager.ScaledPoint(new Point(30, yPos)),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(themeLabel);
            _allControls.Add(themeLabel);

            ComboBox themeComboBox = new ComboBox
            {
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 12)),
                Size = ThemeManager.ScaledSize(new Size(250, 35)),
                Location = ThemeManager.ScaledPoint(new Point(200, yPos)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 80),
                ForeColor = ThemeManager.Colors.Text
            };

            for (int i = 0; i < themes.GetLength(0); i++)
            {
                themeComboBox.Items.Add(themes[i, 0]);
                if (themes[i, 0] == Settings.ThemeName)
                    themeComboBox.SelectedIndex = i;
            }
            panel.Controls.Add(themeComboBox);
            _allControls.Add(themeComboBox);

            yPos += 60;

            // Предпросмотр темы
            Panel previewPanel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(300, 150)),
                Location = ThemeManager.ScaledPoint(new Point(130, yPos)),
                BackColor = Color.FromArgb(60, 60, 80)
            };
            panel.Controls.Add(previewPanel);
            _allControls.Add(previewPanel);

            Panel colorPreview = new Panel
            {
                Size = new Size(previewPanel.Width - 4, previewPanel.Height - 4),
                Location = new Point(2, 2),
                BackColor = ThemeManager.Colors.Background
            };
            colorPreview.Paint += (s, e) => DrawThemePreview(e.Graphics, colorPreview.ClientRectangle);
            previewPanel.Controls.Add(colorPreview);
            _allControls.Add(colorPreview);

            yPos += 160;

            // Кнопка применения темы
            Button applyButton = new Button
            {
                Text = "ПРИМЕНИТЬ ТЕМУ",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 12, FontStyle.Bold)),
                ForeColor = Color.White,
                BackColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(200, 40)),
                Location = ThemeManager.ScaledPoint(new Point(180, yPos)),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            applyButton.Click += (s, e) =>
            {
                int index = themeComboBox.SelectedIndex;
                if (index >= 0)
                {
                    Settings.ThemeName = themes[index, 0];
                    Settings.BackgroundColor = themes[index, 1];
                    Settings.Paddle1Color = themes[index, 2];
                    Settings.Paddle2Color = themes[index, 3];
                    Settings.BallColor = themes[index, 4];
                    Settings.TextColor = themes[index, 5];
                    Settings.CourtColor = themes[index, 6];
                    Settings.AccentColor = themes[index, 7];

                    GameSettingsManager.SaveSettings(Settings);
                    ThemeManager.SetTheme(Settings);

                    // Обновляем предпросмотр
                    colorPreview.Invalidate();
                }
            };
            panel.Controls.Add(applyButton);
            _allControls.Add(applyButton);

            yPos += 60;

            // Настройки звука
            Label soundLabel = new Label
            {
                Text = "Настройки звука:",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 13, FontStyle.Bold)),
                ForeColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(200, 30)),
                Location = ThemeManager.ScaledPoint(new Point(30, yPos)),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(soundLabel);
            _allControls.Add(soundLabel);

            yPos += 40;

            // Музыка
            Label musicLabel = new Label
            {
                Text = "Фоновая музыка:",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 11)),
                ForeColor = ThemeManager.Colors.Text,
                Size = ThemeManager.ScaledSize(new Size(150, 25)),
                Location = ThemeManager.ScaledPoint(new Point(50, yPos)),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(musicLabel);
            _allControls.Add(musicLabel);

            CheckBox musicCheckbox = new CheckBox
            {
                Checked = ThemeManager.MusicEnabled,
                Size = ThemeManager.ScaledSize(new Size(20, 20)),
                Location = ThemeManager.ScaledPoint(new Point(220, yPos))
            };
            musicCheckbox.CheckedChanged += (s, e) =>
                ThemeManager.MusicEnabled = musicCheckbox.Checked;
            panel.Controls.Add(musicCheckbox);
            _allControls.Add(musicCheckbox);

            yPos += 40;

            // Звуковые эффекты
            Label soundsLabel = new Label
            {
                Text = "Звуковые эффекты:",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 11)),
                ForeColor = ThemeManager.Colors.Text,
                Size = ThemeManager.ScaledSize(new Size(150, 25)),
                Location = ThemeManager.ScaledPoint(new Point(50, yPos)),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(soundsLabel);
            _allControls.Add(soundsLabel);

            CheckBox soundsCheckbox = new CheckBox
            {
                Checked = ThemeManager.SoundsEnabled,
                Size = ThemeManager.ScaledSize(new Size(20, 20)),
                Location = ThemeManager.ScaledPoint(new Point(220, yPos))
            };
            soundsCheckbox.CheckedChanged += (s, e) =>
                ThemeManager.SoundsEnabled = soundsCheckbox.Checked;
            panel.Controls.Add(soundsCheckbox);
            _allControls.Add(soundsCheckbox);

            return panel;
        }

        private void DrawThemePreview(Graphics g, Rectangle bounds)
        {
            // Фон
            g.FillRectangle(new SolidBrush(ThemeManager.Colors.Background), bounds);

            // Игровое поле
            Rectangle courtBounds = new Rectangle(
                bounds.X + 20, bounds.Y + 20,
                bounds.Width - 40, bounds.Height - 40);
            g.FillRectangle(new SolidBrush(ThemeManager.Colors.Court), courtBounds);

            // Центральная линия
            using (Pen linePen = new Pen(ThemeManager.Colors.Text, 1))
            {
                linePen.DashStyle = DashStyle.Dash;
                g.DrawLine(linePen,
                    courtBounds.X + courtBounds.Width / 2, courtBounds.Y,
                    courtBounds.X + courtBounds.Width / 2, courtBounds.Y + courtBounds.Height);
            }

            // Ракетки (одинакового цвета)
            Color paddleColor = ThemeManager.Colors.Player1;
            int paddleWidth = 10;
            int paddleHeight = 40;

            // Левая ракетка
            g.FillRectangle(new SolidBrush(paddleColor),
                courtBounds.X + 20,
                courtBounds.Y + (courtBounds.Height - paddleHeight) / 2,
                paddleWidth, paddleHeight);

            // Правая ракетка
            g.FillRectangle(new SolidBrush(paddleColor),
                courtBounds.X + courtBounds.Width - 20 - paddleWidth,
                courtBounds.Y + (courtBounds.Height - paddleHeight) / 2,
                paddleWidth, paddleHeight);

            // Мяч
            int ballSize = 15;
            g.FillEllipse(new SolidBrush(ThemeManager.Colors.Ball),
                courtBounds.X + (courtBounds.Width - ballSize) / 2,
                courtBounds.Y + (courtBounds.Height - ballSize) / 2,
                ballSize, ballSize);
        }

        private Panel CreateScoresContent()
        {
            Panel panel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(560, 460)),
                Location = ThemeManager.ScaledPoint(new Point(10, 10)),
                BackColor = Color.Transparent
            };

            // Заголовок
            Label title = new Label
            {
                Text = "ТАБЛИЦА ЛИДЕРОВ",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 18, FontStyle.Bold)),
                ForeColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(520, 40)),
                Location = ThemeManager.ScaledPoint(new Point(20, 10)),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(title);
            _allControls.Add(title);

            // Список результатов
            ListView listView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Size = ThemeManager.ScaledSize(new Size(520, 360)),
                Location = ThemeManager.ScaledPoint(new Point(20, 60)),
                BackColor = Color.FromArgb(40, 40, 60),
                ForeColor = ThemeManager.Colors.Text,
            };

            listView.Columns.Add("№", 40);
            listView.Columns.Add("Игрок", 140);
            listView.Columns.Add("Сложность", 90);
            listView.Columns.Add("Счёт", 70);
            listView.Columns.Add("Время", 80);
            listView.Columns.Add("Дата", 90);

            panel.Controls.Add(listView);
            _allControls.Add(listView);

            // Загружаем данные из БД
            // Загружаем данные из БД
            try
            {
                var scores = Database.GetTopScores(10);
                int place = 1;

                foreach (var s in scores)
                {
                    // сложность
                    string diffText;
                    switch (s.Difficulty)
                    {
                        case AIDifficulty.Easy:
                            diffText = "Лёгкий";
                            break;
                        case AIDifficulty.Hard:
                            diffText = "Сложный";
                            break;
                        default:
                            diffText = "Нормальный";
                            break;
                    }

                    // победа/поражение определяем по счёту
                    bool isWin = s.PlayerScore > s.OpponentScore;
                    string resultText = isWin ? "Победа" : "Поражение";

                    string scoreText = $"{s.PlayerScore}:{s.OpponentScore}";

                    TimeSpan t = TimeSpan.FromSeconds(s.DurationSeconds);
                    string timeText = $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
                    string dateText = s.PlayedAt.ToString("dd.MM.yyyy");

                    var item = new ListViewItem(place.ToString());
                    item.SubItems.Add(s.PlayerName);
                    item.SubItems.Add(diffText);
                    item.SubItems.Add(scoreText);
                    item.SubItems.Add(resultText);
                    item.SubItems.Add(timeText);
                    item.SubItems.Add(dateText);

                    listView.Items.Add(item);
                    place++;
                }

            }
            catch (Exception ex)
            {
                string msg = ex.Message;

                if (ex is TypeInitializationException tie && tie.InnerException != null)
                {
                    msg += "\n\nВнутренняя ошибка:\n" + tie.InnerException.Message;
                }

                MessageBox.Show(
                    "Ошибка загрузки таблицы лидеров:\n" + msg,
                    "Ошибка БД",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }




            return panel;
        }


        private Panel CreateAboutContent()
        {
            Panel panel = new Panel
            {
                Size = ThemeManager.ScaledSize(new Size(560, 460)),
                Location = ThemeManager.ScaledPoint(new Point(10, 10)),
                BackColor = Color.Transparent
            };

            Label label = new Label
            {
                Text = "PING PONG GAME\n\nВерсия 2.0\n\nКлассическая игра Ping Pong\nс современным интерфейсом",
                Font = ThemeManager.ScaledFont(new Font("Segoe UI", 16)),
                ForeColor = ThemeManager.Colors.Accent,
                Size = ThemeManager.ScaledSize(new Size(500, 200)),
                Location = ThemeManager.ScaledPoint(new Point(30, 100)),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(label);
            _allControls.Add(label);

            return panel;
        }

        private void ThemeManager_OnThemeChanged()
        {
            // Мгновенно обновляем все цвета
            this.BackColor = ThemeManager.Colors.Background;

            // Перерисовываем панели с новыми цветами рамок
            if (_menuPanel != null)
            {
                _menuPanel.Invalidate();
                _contentPanel.Invalidate();
            }

            foreach (var control in _allControls)
            {
                UpdateControlColors(control);
            }

            // Перерисовываем активную кнопку
            if (_currentButton != null)
            {
                _currentButton.BackColor = ThemeManager.Colors.Accent;
            }

            this.Invalidate();
        }

        private void UpdateControlColors(Control control)
        {
            // ----- Label -----
            var label = control as Label;
            if (label != null)
            {
                string text = label.Text ?? string.Empty;

                if (text == "PING PONG" ||
                    text == "МЕНЮ" ||
                    text.Contains("ТАБЛИЦА") ||
                    text.Contains("Настройки") ||
                    text.Contains("Цветовая") ||
                    text.Contains("Сложность") ||
                    text.Contains("Ваш никнейм") ||
                    text.Contains("Версия") ||
                    text.Contains("PING PONG GAME"))
                {
                    label.ForeColor = ThemeManager.Colors.Accent;
                }
                else if (text.Contains("Фоновая") || text.Contains("Звуковые"))
                {
                    label.ForeColor = ThemeManager.Colors.Text;
                }
                else
                {
                    label.ForeColor = ThemeManager.Colors.Text;
                }
            }
            // ----- Button -----
            else if (control is Button)
            {
                var button = (Button)control;

                if (button.Text == "✕" ||
                    button.Text == "🎮 НАЧАТЬ ИГРУ" ||
                    button.Text == "ПРИМЕНИТЬ ТЕМУ" ||
                    button == _currentButton)
                {
                    button.BackColor = ThemeManager.Colors.Accent;
                    button.ForeColor = Color.Black;
                }
                else
                {
                    button.BackColor = Color.FromArgb(60, 60, 80);
                    button.ForeColor = ThemeManager.Colors.Text;
                }

                button.FlatAppearance.MouseOverBackColor = ThemeManager.Colors.Accent;
            }
            // ----- TextBox -----
            else if (control is TextBox)
            {
                var textBox = (TextBox)control;
                textBox.BackColor = Color.FromArgb(60, 60, 80);
                textBox.ForeColor = ThemeManager.Colors.Text;
            }
            // ----- ComboBox -----
            else if (control is ComboBox)
            {
                var comboBox = (ComboBox)control;
                comboBox.BackColor = Color.FromArgb(60, 60, 80);
                comboBox.ForeColor = ThemeManager.Colors.Text;
            }
            // ----- Panel -----
            else if (control is Panel)
            {
                var panel = (Panel)control;
                if (panel.Name != "menuPanel" && panel.Name != "contentPanel")
                {
                    panel.BackColor = Color.FromArgb(60, 60, 80);
                }
            }

            // рекурсивно для всех дочерних контролов
            foreach (Control child in control.Controls)
            {
                UpdateControlColors(child);
            }
        }



        private void MenuForm_Paint(object sender, PaintEventArgs e)
        {
            // Градиентный фон
            using (LinearGradientBrush brush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(20, 20, 30),
                Color.FromArgb(30, 30, 40),
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            // Клетчатый фон с цветом акцента текущей темы
            int gridSize = 30;
            using (Pen gridPen = new Pen(Color.FromArgb(20, ThemeManager.Colors.Accent), 1))
            {
                for (int y = 0; y < this.Height; y += gridSize)
                    e.Graphics.DrawLine(gridPen, 0, y, this.Width, y);
                for (int x = 0; x < this.Width; x += gridSize)
                    e.Graphics.DrawLine(gridPen, x, 0, x, this.Height);
            }

            // Акцентные линии по углам с цветом акцента
            int cornerSize = 80;
            using (Pen accentPen = new Pen(Color.FromArgb(60, ThemeManager.Colors.Accent), 2))
            {
                // Левый верхний
                e.Graphics.DrawLine(accentPen, 0, 0, cornerSize, 0);
                e.Graphics.DrawLine(accentPen, 0, 0, 0, cornerSize);

                // Правый верхний
                e.Graphics.DrawLine(accentPen, this.Width, 0, this.Width - cornerSize, 0);
                e.Graphics.DrawLine(accentPen, this.Width, 0, this.Width, cornerSize);

                // Левый нижний
                e.Graphics.DrawLine(accentPen, 0, this.Height, cornerSize, this.Height);
                e.Graphics.DrawLine(accentPen, 0, this.Height, 0, this.Height - cornerSize);

                // Правый нижний
                e.Graphics.DrawLine(accentPen, this.Width, this.Height, this.Width - cornerSize, this.Height);
                e.Graphics.DrawLine(accentPen, this.Width, this.Height, this.Width, this.Height - cornerSize);
            }
        }
    }
}