using System.Drawing;
using PingPongGame.GameLogic;

namespace PingPongGame.GameUI
{
    public class GameRenderer
    {
        private GameEngine _engine;

        public GameRenderer(GameEngine engine)
        {
            _engine = engine;
        }

        public void Render(Graphics g, Rectangle bounds)
        {
            // 1. Фон и поле
            UIManager.DrawBackground(g, bounds);

            // 2. Ракетки
            var leftPaddle = _engine.LeftPaddle;
            var rightPaddle = _engine.RightPaddle;

            Rectangle leftRect = new Rectangle(
                (int)leftPaddle.X,
                (int)leftPaddle.Y,
                (int)leftPaddle.Width,
                (int)leftPaddle.Height);

            Rectangle rightRect = new Rectangle(
                (int)rightPaddle.X,
                (int)rightPaddle.Y,
                (int)rightPaddle.Width,
                (int)rightPaddle.Height);

            UIManager.DrawPaddle(g, leftRect, true);
            UIManager.DrawPaddle(g, rightRect, false);

            // 3. Мяч
            var ball = _engine.Ball;
            Rectangle ballRect = new Rectangle(
                (int)(ball.X - ball.Radius),
                (int)(ball.Y - ball.Radius),
                (int)(ball.Radius * 2),
                (int)(ball.Radius * 2));

            UIManager.DrawBall(g, ballRect);
        }
    }
}