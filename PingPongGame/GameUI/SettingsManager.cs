using PingPongGame.GameLogic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace PingPongGame
{
    public class GameSettingsManager
    {
        private static string SettingsPath = "pingpong_settings.xml";
        private static string ScoresPath = "pingpong_scores.xml";

        [Serializable]
        public class GameSettingsData
        {
            public string PlayerName { get; set; } = "Player";
            public AIDifficulty Difficulty { get; set; } = AIDifficulty.Normal;

            // ИСПРАВЛЕНО ЗДЕСЬ - разные цвета для ракеток
            public string ThemeName { get; set; } = "Неон Розовый";
            public string BackgroundColor { get; set; } = "0A0A0F";
            public string Paddle1Color { get; set; } = "00FFFF"; // Голубой
            public string Paddle2Color { get; set; } = "FF00FF"; // Розовый
            public string BallColor { get; set; } = "FFFFFF";
            public string TextColor { get; set; } = "FFFFFF";
            public string CourtColor { get; set; } = "1A1A2E";
            public string AccentColor { get; set; } = "FF00FF";
        }

        [Serializable]
        public class ScoreEntry
        {
            public string PlayerName { get; set; }
            public int Score { get; set; }
            public AIDifficulty Difficulty { get; set; }
            public DateTime Date { get; set; }
            public bool IsWin { get; set; }
        }

        public static void SaveSettings(GameSettingsData settings)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GameSettingsData));
                using (StreamWriter writer = new StreamWriter(SettingsPath))
                {
                    serializer.Serialize(writer, settings);
                }
            }
            catch { }
        }

        public static GameSettingsData LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(GameSettingsData));
                    using (StreamReader reader = new StreamReader(SettingsPath))
                    {
                        return (GameSettingsData)serializer.Deserialize(reader);
                    }
                }
            }
            catch { }

            return new GameSettingsData();
        }

        public static void SaveScore(ScoreEntry score)
        {
            try
            {
                List<ScoreEntry> scores = LoadScores();
                scores.Add(score);
                scores = scores.OrderByDescending(s => s.Score)
                             .ThenByDescending(s => s.Date)
                             .Take(10)
                             .ToList();

                XmlSerializer serializer = new XmlSerializer(typeof(List<ScoreEntry>));
                using (StreamWriter writer = new StreamWriter(ScoresPath))
                {
                    serializer.Serialize(writer, scores);
                }
            }
            catch { }
        }

        public static List<ScoreEntry> LoadScores()
        {
            try
            {
                if (File.Exists(ScoresPath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<ScoreEntry>));
                    using (StreamReader reader = new StreamReader(ScoresPath))
                    {
                        return (List<ScoreEntry>)serializer.Deserialize(reader);
                    }
                }
            }
            catch { }

            return new List<ScoreEntry>();
        }

        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.White;

            hex = hex.Trim().Replace("#", "");

            if (hex.Length == 6)
            {
                try
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    return Color.FromArgb(r, g, b);
                }
                catch
                {
                    return Color.White;
                }
            }

            return Color.White;
        }

        public static string ColorToHex(Color color)
        {
            return color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
    }
}