using System.Windows.Forms;

namespace PingPongGame
{
    public class BufferedForm : Form
    {
        public BufferedForm()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
        }
    }
}