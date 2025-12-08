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

            var settings = GameSettingsManager.LoadSettings();
            ThemeManager.SetTheme(settings);
            ThemeManager.PlayBackgroundMusic();

            using (MenuForm menuForm = new MenuForm())
            {
                if (menuForm.ShowDialog() == DialogResult.OK)
                {
                    var currentSettings = GameSettingsManager.LoadSettings();
                    ThemeManager.SetTheme(currentSettings);

                    Application.Run(new Form1(
                        menuForm.PlayerName,
                        menuForm.SelectedDifficulty));
                }
            }
        }
    }
}
