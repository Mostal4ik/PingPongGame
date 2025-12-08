using System;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace PingPongGame
{
    public static class SoundManager
    {
        private static SoundPlayer _musicPlayer;

        public static bool MusicEnabled { get; set; } = true;
        public static bool SoundsEnabled { get; set; } = true;

        static SoundManager()
        {
            LoadSounds();
        }

        private static void LoadSounds()
        {
            try
            {
                string soundsPath = "sounds";

                if (!Directory.Exists(soundsPath))
                {
                    Directory.CreateDirectory(soundsPath);
                    return;
                }

                // Загружаем только музыку
                string musicPath = Path.Combine(soundsPath, "music.wav");
                if (File.Exists(musicPath))
                {
                    _musicPlayer = new SoundPlayer(musicPath);
                    _musicPlayer.Load();
                }
            }
            catch { }
        }

        public static void PlayGoalSound()
        {
            if (!SoundsEnabled) return;

            try
            {
                // Используем системный звук для голов
                SystemSounds.Exclamation.Play();
            }
            catch { }
        }

        public static void PlayMusic()
        {
            if (!MusicEnabled || _musicPlayer == null) return;

            try
            {
                _musicPlayer.PlayLooping();
            }
            catch { }
        }

        public static void StopMusic()
        {
            try
            {
                _musicPlayer?.Stop();
            }
            catch { }
        }

        public static void SaveSettings()
        {
            var settings = GameSettingsManager.LoadSettings();
            settings.MusicEnabled = MusicEnabled;
            settings.SoundsEnabled = SoundsEnabled;
            GameSettingsManager.SaveSettings(settings);
        }

        public static void LoadSettings()
        {
            var settings = GameSettingsManager.LoadSettings();
            MusicEnabled = settings.MusicEnabled;
            SoundsEnabled = settings.SoundsEnabled;
        }
    }
}