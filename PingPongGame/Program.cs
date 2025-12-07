using System;
using System.Windows.Forms;

namespace PingPongGame
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Загружаем настройки
            var settings = GameSettingsManager.LoadSettings();
            ThemeManager.SetTheme(settings);

            // Запускаем фоновую музыку
            ThemeManager.PlayBackgroundMusic();

            // Меню
            using (MenuForm menuForm = new MenuForm())
            {
                if (menuForm.ShowDialog() == DialogResult.OK)
                {
                    // Обновляем тему
                    var currentSettings = GameSettingsManager.LoadSettings();
                    ThemeManager.SetTheme(currentSettings);

                    // Запускаем игру
                    Application.Run(new Form1(
                        menuForm.PlayerName,
                        menuForm.SelectedDifficulty));
                }
            }

            // Останавливаем музыку при выходе
            ThemeManager.StopBackgroundMusic();
        }
    }
}