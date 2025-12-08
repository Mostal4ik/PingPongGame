using PingPongGame;
using System.Windows.Forms;
using System;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Останавливаем музыку при запуске (на всякий случай)
        SoundManager.StopMusic();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using (MenuForm menu = new MenuForm())
        {
            if (menu.ShowDialog() == DialogResult.OK)
            {
                // Останавливаем музыку перед запуском игры
                SoundManager.StopMusic();

                // Запускаем игру
                Application.Run(new Form1(menu.PlayerName, menu.SelectedDifficulty));
            }
        }
    }
}