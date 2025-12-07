using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace PingPongGame
{
    public static class ThemeManager
    {
        public static GameSettingsManager.GameSettingsData CurrentTheme { get; private set; }
        public static float ScaleFactor { get; private set; } = 1.0f;

        // Звуковые файлы (можно скачать или использовать системные)
        private static SoundPlayer _hitSound;
        private static SoundPlayer _scoreSound;
        private static SoundPlayer _backgroundMusic;

        // Настройки звука
        public static bool MusicEnabled { get; set; } = true;
        public static bool SoundsEnabled { get; set; } = true;

        // Событие для обновления интерфейса
        public static event Action OnThemeChanged;

        static ThemeManager()
        {
            CurrentTheme = GameSettingsManager.LoadSettings();
            LoadSounds();
        }

        public static void SetTheme(GameSettingsManager.GameSettingsData theme)
        {
            CurrentTheme = theme;

            // Вызываем событие обновления темы
            OnThemeChanged?.Invoke();
        }

        public static void UpdateScale(Size formSize)
        {
            float widthScale = formSize.Width / 1000f;
            float heightScale = formSize.Height / 700f;
            ScaleFactor = Math.Min(widthScale, heightScale) * 1.2f;

            if (ScaleFactor < 0.8f) ScaleFactor = 0.8f;
            if (ScaleFactor > 2.0f) ScaleFactor = 2.0f;
        }

        public static int Scaled(int value)
        {
            return (int)(value * ScaleFactor);
        }

        public static float Scaled(float value)
        {
            return value * ScaleFactor;
        }

        public static Font ScaledFont(Font baseFont, float multiplier = 1.0f)
        {
            return new Font(baseFont.FontFamily, baseFont.Size * ScaleFactor * multiplier, baseFont.Style);
        }

        public static Size ScaledSize(Size size)
        {
            return new Size(Scaled(size.Width), Scaled(size.Height));
        }

        public static Point ScaledPoint(Point point)
        {
            return new Point(Scaled(point.X), Scaled(point.Y));
        }

        private static void LoadSounds()
        {
            try
            {
                // Можно использовать системные звуки
                _hitSound = new SoundPlayer();
                _scoreSound = new SoundPlayer();

                // Или загрузить из файлов (раскомментируй если есть файлы):
                /*
                if (File.Exists("sounds\\hit.wav"))
                    _hitSound = new SoundPlayer("sounds\\hit.wav");
                if (File.Exists("sounds\\score.wav"))
                    _scoreSound = new SoundPlayer("sounds\\score.wav");
                if (File.Exists("sounds\\music.wav"))
                    _backgroundMusic = new SoundPlayer("sounds\\music.wav");
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки звуков: " + ex.Message);
            }
        }

        public static void PlayHitSound()
        {
            if (SoundsEnabled)
            {
                try
                {
                    // Используем системный звук
                    SystemSounds.Beep.Play();

                    // Или свой звук
                    // _hitSound?.Play();
                }
                catch { }
            }
        }

        public static void PlayScoreSound()
        {
            if (SoundsEnabled)
            {
                try
                {
                    // Используем системный звук
                    SystemSounds.Exclamation.Play();

                    // Или свой звук
                    // _scoreSound?.Play();
                }
                catch { }
            }
        }

        public static void PlayBackgroundMusic()
        {
            if (MusicEnabled)
            {
                try
                {
                    // Если есть свой файл музыки
                    // _backgroundMusic?.PlayLooping();

                    // Или просто играем без музыки
                    Console.WriteLine("Музыка включена");
                }
                catch { }
            }
        }

        public static void StopBackgroundMusic()
        {
            try
            {
                // _backgroundMusic?.Stop();
            }
            catch { }
        }

        public static class Colors
        {
            public static Color Background => GameSettingsManager.HexToColor(CurrentTheme.BackgroundColor);
            public static Color Court => GameSettingsManager.HexToColor(CurrentTheme.CourtColor);
            public static Color Accent => GameSettingsManager.HexToColor(CurrentTheme.AccentColor);
            public static Color Player1 => GameSettingsManager.HexToColor(CurrentTheme.Paddle1Color);
            public static Color Player2 => GameSettingsManager.HexToColor(CurrentTheme.Paddle2Color);
            public static Color Ball => GameSettingsManager.HexToColor(CurrentTheme.BallColor);
            public static Color Text => GameSettingsManager.HexToColor(CurrentTheme.TextColor);
        }
    }
}