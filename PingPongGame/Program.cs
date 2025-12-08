using System;
using System.Windows.Forms;

namespace PingPongGame
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // На всякий случай — останавливаем возможную музыку
            SoundManager.StopMusic();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Загружаем настройки игры и применяем тему
            var settings = GameSettingsManager.LoadSettings();
            ThemeManager.SetTheme(settings);

            // Загружаем настройки звука и запускаем музыку в меню (если включена)
            SoundManager.LoadSettings();
            if (SoundManager.MusicEnabled)
            {
                SoundManager.PlayMusic();
            }

            using (MenuForm menuForm = new MenuForm())
            {
                if (menuForm.ShowDialog() == DialogResult.OK)
                {
                    // Перед запуском игры можно перезапустить тему (если настройки поменялись)
                    var currentSettings = GameSettingsManager.LoadSettings();
                    ThemeManager.SetTheme(currentSettings);

                    // Останавливаем музыку меню — игру сам Form1 запустит свою музыку
                    SoundManager.StopMusic();

                    Application.Run(new Form1(
                        menuForm.PlayerName,
                        menuForm.SelectedDifficulty));
                }
            }
        }
    }
}
