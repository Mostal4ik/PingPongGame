using System;
using System.Drawing;

namespace PingPongGame
{
    public static class ThemeManager
    {
        public static GameSettingsManager.GameSettingsData CurrentTheme { get; private set; }
        public static float ScaleFactor { get; private set; } = 1.0f;

        // Событие для обновления интерфейса
        public static event Action OnThemeChanged;

        static ThemeManager()
        {
            CurrentTheme = GameSettingsManager.LoadSettings();
        }

        public static void SetTheme(GameSettingsManager.GameSettingsData theme)
        {
            CurrentTheme = theme;
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

        public static int Scaled(int value) => (int)(value * ScaleFactor);
        public static float Scaled(float value) => value * ScaleFactor;

        public static Font ScaledFont(Font baseFont, float multiplier = 1.0f)
            => new Font(baseFont.FontFamily, baseFont.Size * ScaleFactor * multiplier, baseFont.Style);

        public static Size ScaledSize(Size size) => new Size(Scaled(size.Width), Scaled(size.Height));
        public static Point ScaledPoint(Point point) => new Point(Scaled(point.X), Scaled(point.Y));

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