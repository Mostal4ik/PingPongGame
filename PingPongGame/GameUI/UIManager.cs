using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PingPongGame.GameUI
{
    public static class UIManager
    {
        private static DateTime _gameStartTime;
        private static bool _isTimerStarted = false;

        // Для подтверждения выхода
        private static bool _showExitConfirm = false;
        private static string _confirmMessage = "";
        private static bool _confirmResult = false;

        public static void StartGameTimer()
        {
            _gameStartTime = DateTime.Now;
            _isTimerStarted = true;
        }

        public static void StopGameTimer()
        {
            _isTimerStarted = false;
        }

        public static string GetGameTime()
        {
            if (!_isTimerStarted) return "00:00";

            TimeSpan elapsed = DateTime.Now - _gameStartTime;
            return $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        }

        // Метод для показа диалога подтверждения
        public static void ShowExitConfirm(string message)
        {
            _showExitConfirm = true;
            _confirmMessage = message;
            _confirmResult = false;
        }

        public static void HideExitConfirm()
        {
            _showExitConfirm = false;
        }

        public static bool IsExitConfirmVisible => _showExitConfirm;
        public static bool GetConfirmResult() => _confirmResult;

        // Обновленный метод для отрисовки фона
        public static void DrawBackground(Graphics g, Rectangle bounds)
        {
            // 1. Весь фон
            g.FillRectangle(new SolidBrush(ThemeManager.Colors.Background), bounds);

            // 2. Игровое поле на все окно
            Rectangle courtBounds = new Rectangle(0, 0, bounds.Width, bounds.Height);

            // Градиент для поля
            using (LinearGradientBrush courtBrush = new LinearGradientBrush(
                courtBounds,
                DarkenColor(ThemeManager.Colors.Court, 0.1f),
                LightenColor(ThemeManager.Colors.Court, 0.1f),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(courtBrush, courtBounds);
            }

            // 3. Тонкая рамка поля с свечением
            using (Pen borderPen = new Pen(Color.FromArgb(150, ThemeManager.Colors.Accent), ThemeManager.Scaled(3)))
            {
                g.DrawRectangle(borderPen, courtBounds);
            }

            // 4. Центральная линия (пунктир)
            int centerX = courtBounds.Width / 2;
            using (Pen centerPen = new Pen(Color.FromArgb(80, ThemeManager.Colors.Text), ThemeManager.Scaled(2)))
            {
                centerPen.DashStyle = DashStyle.Dash;
                centerPen.DashPattern = new float[] {
                    ThemeManager.Scaled(20f),
                    ThemeManager.Scaled(15f)
                };
                g.DrawLine(centerPen, centerX, ThemeManager.Scaled(20),
                    centerX, bounds.Height - ThemeManager.Scaled(20));
            }

            // 5. Круги в центре
            int centerY = courtBounds.Height / 2;
            using (Pen circlePen = new Pen(Color.FromArgb(60, ThemeManager.Colors.Accent), 1))
            {
                // Внешний круг
                g.DrawEllipse(circlePen,
                    centerX - ThemeManager.Scaled(100),
                    centerY - ThemeManager.Scaled(100),
                    ThemeManager.Scaled(200),
                    ThemeManager.Scaled(200));

                // Внутренний круг
                g.DrawEllipse(circlePen,
                    centerX - ThemeManager.Scaled(50),
                    centerY - ThemeManager.Scaled(50),
                    ThemeManager.Scaled(100),
                    ThemeManager.Scaled(100));
            }
        }

        // Новый метод: Отрисовка счета
        public static void DrawScore(Graphics g, Rectangle bounds, int player1Score, int player2Score)
        {
            using (Font scoreFont = ThemeManager.ScaledFont(new Font("Segoe UI", 48, FontStyle.Bold), 1.5f))
            {
                string scoreText = $"{player1Score} : {player2Score}";
                SizeF textSize = g.MeasureString(scoreText, scoreFont);

                // Поднимаем счет выше - позиция Y уменьшена (было 0.15f, теперь 0.08f)
                float yPos = bounds.Height * 0.08f;

                // Тень текста
                g.DrawString(scoreText, scoreFont,
                    new SolidBrush(Color.FromArgb(80, 0, 0, 0)),
                    bounds.Width / 2 - textSize.Width / 2 + ThemeManager.Scaled(3),
                    yPos + ThemeManager.Scaled(3));

                // Основной текст с градиентом
                using (LinearGradientBrush textBrush = new LinearGradientBrush(
                    new PointF(bounds.Width / 2 - textSize.Width / 2, yPos),
                    new PointF(bounds.Width / 2 + textSize.Width / 2, yPos + textSize.Height),
                    LightenColor(ThemeManager.Colors.Text, 0.3f),
                    ThemeManager.Colors.Text))
                {
                    g.DrawString(scoreText, scoreFont, textBrush,
                        bounds.Width / 2 - textSize.Width / 2,
                        yPos);
                }
            }
        }

        // Новый метод: Отрисовка информации об игре
        public static void DrawGameInfo(Graphics g, Rectangle bounds, string player1Name, string player2Name, string gameTime)
        {
            using (Font infoFont = ThemeManager.ScaledFont(new Font("Segoe UI", 14, FontStyle.Bold)))
            {
                /// Имя игрока 1 (слева)
                g.DrawString(player1Name, infoFont,
                    new SolidBrush(ThemeManager.Colors.Player1),
                    ThemeManager.Scaled(10),
                    ThemeManager.Scaled(15));

                // Имя игрока 2 (справа)
                SizeF player2Size = g.MeasureString(player2Name, infoFont);
                g.DrawString(player2Name, infoFont,
                    new SolidBrush(ThemeManager.Colors.Player2),
                    bounds.Width - player2Size.Width - ThemeManager.Scaled(10),
                    ThemeManager.Scaled(15));

                // Время игры (вверху по центру)
                SizeF timeSize = g.MeasureString(gameTime, infoFont);
                g.DrawString(gameTime, infoFont,
                    new SolidBrush(ThemeManager.Colors.Text),
                    bounds.Width / 2 - timeSize.Width / 2,
                    ThemeManager.Scaled(10));
            }
        }

        // Обновленный метод диалога паузы для подтверждения выхода
        public static void DrawConfirmDialog(Graphics g, Rectangle bounds)
        {
            if (!_showExitConfirm) return;

            // Затемнение
            using (Brush darkenBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                g.FillRectangle(darkenBrush, bounds);
            }

            // Панель диалога
            RectangleF dialogRect = new RectangleF(
                bounds.Width / 2 - ThemeManager.Scaled(200),
                bounds.Height / 2 - ThemeManager.Scaled(100),
                ThemeManager.Scaled(400),
                ThemeManager.Scaled(200));

            // Скругленные углы
            using (GraphicsPath path = RoundedRectangle(dialogRect, ThemeManager.Scaled(20)))
            {
                // Градиентный фон
                using (LinearGradientBrush panelBrush = new LinearGradientBrush(
                    dialogRect,
                    Color.FromArgb(240, 40, 40, 60),
                    Color.FromArgb(240, 60, 60, 80),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(panelBrush, path);
                }

                // Обводка
                using (Pen borderPen = new Pen(Color.FromArgb(200, ThemeManager.Colors.Accent), ThemeManager.Scaled(3)))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Текст подтверждения
            using (Font confirmFont = ThemeManager.ScaledFont(new Font("Segoe UI", 18, FontStyle.Bold)))
            {
                SizeF textSize = g.MeasureString(_confirmMessage, confirmFont);
                g.DrawString(_confirmMessage, confirmFont,
                    new SolidBrush(ThemeManager.Colors.Text),
                    bounds.Width / 2 - textSize.Width / 2,
                    bounds.Height / 2 - ThemeManager.Scaled(40));
            }

            // Кнопки
            using (Font buttonFont = ThemeManager.ScaledFont(new Font("Segoe UI", 14, FontStyle.Bold)))
            {
                // Кнопка Да
                RectangleF yesButton = new RectangleF(
                    bounds.Width / 2 - ThemeManager.Scaled(120),
                    bounds.Height / 2 + ThemeManager.Scaled(20),
                    ThemeManager.Scaled(100),
                    ThemeManager.Scaled(40));

                DrawButton(g, yesButton, "ДА", buttonFont, true);

                // Кнопка Нет
                RectangleF noButton = new RectangleF(
                    bounds.Width / 2 + ThemeManager.Scaled(20),
                    bounds.Height / 2 + ThemeManager.Scaled(20),
                    ThemeManager.Scaled(100),
                    ThemeManager.Scaled(40));

                DrawButton(g, noButton, "НЕТ", buttonFont, false);
            }
        }

        // Метод для отрисовки кнопок в диалоге
        private static void DrawButton(Graphics g, RectangleF buttonRect, string text, Font font, bool isYes)
        {
            // Фон кнопки
            using (GraphicsPath buttonPath = RoundedRectangle(buttonRect, ThemeManager.Scaled(8)))
            {
                Color buttonColor = isYes ? Color.FromArgb(200, 255, 100, 100) : Color.FromArgb(200, 100, 100, 255);

                using (SolidBrush buttonBrush = new SolidBrush(buttonColor))
                {
                    g.FillPath(buttonBrush, buttonPath);
                }

                // Обводка
                using (Pen borderPen = new Pen(ThemeManager.Colors.Accent, ThemeManager.Scaled(2)))
                {
                    g.DrawPath(borderPen, buttonPath);
                }
            }

            // Текст кнопки
            SizeF textSize = g.MeasureString(text, font);
            g.DrawString(text, font,
                new SolidBrush(Color.White),
                buttonRect.X + (buttonRect.Width - textSize.Width) / 2,
                buttonRect.Y + (buttonRect.Height - textSize.Height) / 2);
        }

        // Остальные методы остаются без изменений...
        public static void DrawPaddle(Graphics g, Rectangle paddleRect, bool isPlayer1)
        {
            Color paddleColor = isPlayer1 ? ThemeManager.Colors.Player1 : ThemeManager.Colors.Player2;

            // Градиентная ракетка с 3D эффектом
            RectangleF rect = new RectangleF(paddleRect.X, paddleRect.Y,
                paddleRect.Width, paddleRect.Height);

            // Основной градиент
            using (LinearGradientBrush paddleBrush = new LinearGradientBrush(
                rect,
                LightenColor(paddleColor, 0.3f),
                paddleColor,
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(paddleBrush, rect);
            }

            // Боковые градиенты для 3D эффекта
            RectangleF leftSide = new RectangleF(rect.X, rect.Y,
                rect.Width * 0.3f, rect.Height);
            using (LinearGradientBrush leftBrush = new LinearGradientBrush(
                leftSide,
                DarkenColor(paddleColor, 0.3f),
                Color.Transparent,
                LinearGradientMode.Horizontal))
            {
                g.FillRectangle(leftBrush, leftSide);
            }

            RectangleF rightSide = new RectangleF(rect.Right - rect.Width * 0.3f, rect.Y,
                rect.Width * 0.3f, rect.Height);
            using (LinearGradientBrush rightBrush = new LinearGradientBrush(
                rightSide,
                Color.Transparent,
                DarkenColor(paddleColor, 0.3f),
                LinearGradientMode.Horizontal))
            {
                g.FillRectangle(rightBrush, rightSide);
            }

            // Обводка с свечением
            using (Pen borderPen = new Pen(LightenColor(paddleColor, 0.5f), ThemeManager.Scaled(2)))
            {
                g.DrawRectangle(borderPen, paddleRect.X, paddleRect.Y,
                    paddleRect.Width, paddleRect.Height);
            }

            // Внутренние линии
            using (Pen innerPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
            {
                int lineCount = 5;
                float lineSpacing = paddleRect.Height / (lineCount + 1);
                for (int i = 1; i <= lineCount; i++)
                {
                    float y = paddleRect.Y + lineSpacing * i;
                    g.DrawLine(innerPen,
                        paddleRect.X + 2, y,
                        paddleRect.Right - 2, y);
                }
            }
        }

        public static void DrawBall(Graphics g, Rectangle ballRect)
        {
            // Мяч с градиентом и 3D эффектом
            using (GraphicsPath ballPath = new GraphicsPath())
            {
                ballPath.AddEllipse(ballRect);

                // Радиальный градиент
                using (PathGradientBrush gradient = new PathGradientBrush(ballPath))
                {
                    gradient.CenterColor = LightenColor(ThemeManager.Colors.Ball, 0.5f);
                    gradient.SurroundColors = new Color[] { ThemeManager.Colors.Ball };
                    gradient.CenterPoint = new PointF(
                        ballRect.X + ballRect.Width * 0.3f,
                        ballRect.Y + ballRect.Height * 0.3f);

                    g.FillEllipse(gradient, ballRect);
                }
            }

            // Внешняя обводка
            using (Pen borderPen = new Pen(LightenColor(ThemeManager.Colors.Ball, 0.7f), 2))
            {
                g.DrawEllipse(borderPen, ballRect);
            }

            // Блик
            Rectangle highlight = new Rectangle(
                ballRect.X + ThemeManager.Scaled(ballRect.Width / 4),
                ballRect.Y + ThemeManager.Scaled(ballRect.Height / 4),
                ThemeManager.Scaled(ballRect.Width / 3),
                ThemeManager.Scaled(ballRect.Height / 3));

            using (GraphicsPath highlightPath = new GraphicsPath())
            {
                highlightPath.AddEllipse(highlight);
                using (PathGradientBrush highlightBrush = new PathGradientBrush(highlightPath))
                {
                    highlightBrush.CenterColor = Color.FromArgb(200, 255, 255, 255);
                    highlightBrush.SurroundColors = new Color[] { Color.Transparent };
                    g.FillEllipse(highlightBrush, highlight);
                }
            }

            // Тень внизу
            Rectangle shadow = new Rectangle(
                ballRect.X + ThemeManager.Scaled(3),
                ballRect.Y + ThemeManager.Scaled(3),
                ballRect.Width,
                ballRect.Height);

            using (GraphicsPath shadowPath = new GraphicsPath())
            {
                shadowPath.AddEllipse(shadow);
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.Transparent;
                    shadowBrush.SurroundColors = new Color[] { Color.FromArgb(80, 0, 0, 0) };
                    g.FillEllipse(shadowBrush, shadow);
                }
            }
        }

        public static void DrawPauseOverlay(Graphics g, Rectangle bounds)
        {
            // Затемнение
            using (Brush darkenBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                g.FillRectangle(darkenBrush, bounds);
            }

            // Панель паузы
            RectangleF pausePanel = new RectangleF(
                bounds.Width / 2 - ThemeManager.Scaled(175),
                bounds.Height / 2 - ThemeManager.Scaled(100),
                ThemeManager.Scaled(350),
                ThemeManager.Scaled(200));

            using (GraphicsPath panelPath = RoundedRectangle(pausePanel, ThemeManager.Scaled(20)))
            {
                // Градиентный фон
                using (LinearGradientBrush panelBrush = new LinearGradientBrush(
                    pausePanel,
                    Color.FromArgb(240, 40, 40, 60),
                    Color.FromArgb(240, 60, 60, 80),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(panelBrush, panelPath);
                }

                // Неоновая обводка
                using (Pen borderPen = new Pen(Color.FromArgb(200, ThemeManager.Colors.Accent), ThemeManager.Scaled(3)))
                {
                    g.DrawPath(borderPen, panelPath);
                }
            }

            // Текст паузы
            using (Font pauseFont = ThemeManager.ScaledFont(new Font("Segoe UI", 42, FontStyle.Bold)))
            {
                string pauseText = "ПАУЗА";
                SizeF textSize = g.MeasureString(pauseText, pauseFont);

                // Тень
                g.DrawString(pauseText, pauseFont,
                    new SolidBrush(Color.FromArgb(100, 0, 0, 0)),
                    bounds.Width / 2 - textSize.Width / 2 + ThemeManager.Scaled(3),
                    bounds.Height / 2 - textSize.Height / 2 + ThemeManager.Scaled(3));

                // Основной текст с градиентом
                using (LinearGradientBrush textBrush = new LinearGradientBrush(
                    new PointF(bounds.Width / 2 - textSize.Width / 2, bounds.Height / 2 - textSize.Height / 2),
                    new PointF(bounds.Width / 2 + textSize.Width / 2, bounds.Height / 2 + textSize.Height / 2),
                    LightenColor(ThemeManager.Colors.Accent, 0.5f),
                    ThemeManager.Colors.Accent))
                {
                    g.DrawString(pauseText, pauseFont, textBrush,
                        bounds.Width / 2 - textSize.Width / 2,
                        bounds.Height / 2 - textSize.Height / 2);
                }
            }

            // Инструкция
            using (Font instructionFont = ThemeManager.ScaledFont(new Font("Segoe UI", 14, FontStyle.Bold)))
            {
                string instruction = "Нажми ESC для продолжения";
                SizeF instSize = g.MeasureString(instruction, instructionFont);

                g.DrawString(instruction, instructionFont,
                    new SolidBrush(Color.FromArgb(220, ThemeManager.Colors.Text)),
                    bounds.Width / 2 - instSize.Width / 2,
                    bounds.Height / 2 + ThemeManager.Scaled(60));
            }
        }

        // Вспомогательные методы
        private static GraphicsPath RoundedRectangle(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();

            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();

            return path;
        }

        private static Color LightenColor(Color color, float factor)
        {
            return Color.FromArgb(color.A,
                (int)Math.Min(255, color.R + (255 - color.R) * factor),
                (int)Math.Min(255, color.G + (255 - color.G) * factor),
                (int)Math.Min(255, color.B + (255 - color.B) * factor));
        }

        private static Color DarkenColor(Color color, float factor)
        {
            return Color.FromArgb(color.A,
                (int)(color.R * (1 - factor)),
                (int)(color.G * (1 - factor)),
                (int)(color.B * (1 - factor)));
        }
    }
}