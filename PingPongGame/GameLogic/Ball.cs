using System.Drawing;

namespace PingPongGame.GameLogic
{
    public class Ball
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Radius { get; }

        public float VelocityX { get; private set; }
        public float VelocityY { get; private set; }

        public Ball(float x, float y, float radius, float velocityX, float velocityY)
        {
            X = x;
            Y = y;
            Radius = radius;
            VelocityX = velocityX;
            VelocityY = velocityY;
        }

        /// <summary>Двигаем мяч на dt секунд.</summary>
        public void Move(float dt)
        {
            X += VelocityX * dt;
            Y += VelocityY * dt;
        }

        public void Reset(float x, float y, float vx, float vy)
        {
            X = x;
            Y = y;
            VelocityX = vx;
            VelocityY = vy;
        }

        public void SetVelocity(float vx, float vy)
        {
            VelocityX = vx;
            VelocityY = vy;
        }

        public RectangleF GetBounds()
        {
            return new RectangleF(
                X - Radius,
                Y - Radius,
                Radius * 2,
                Radius * 2);
        }
    }
}
