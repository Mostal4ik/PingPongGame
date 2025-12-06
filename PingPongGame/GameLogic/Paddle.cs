using System.Drawing;

namespace PingPongGame.GameLogic
{
    public class Paddle
    {
        public float X { get; }
        public float Y { get; private set; }
        public float Width { get; }
        public float Height { get; }

        /// <summary>Базовая скорость ракетки (пикселей в секунду).</summary>
        public float Speed { get; }

        /// <summary>
        /// Текущее направление движения:
        /// -1 = вверх, 0 = стоит, 1 = вниз.
        /// Это значение будет задавать UI/управление.
        /// </summary>
        public float DirectionY { get; set; }

        public Paddle(float x, float y, float width, float height, float speed)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Speed = speed;
            DirectionY = 0;
        }

        /// <summary>Обновление позиции ракетки.</summary>
        public void Update(float dt, float fieldHeight)
        {
            Y += DirectionY * Speed * dt;

            // Ограничение по игровому полю
            if (Y < 0)
                Y = 0;

            if (Y + Height > fieldHeight)
                Y = fieldHeight - Height;
        }

        public RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }
    }
}
