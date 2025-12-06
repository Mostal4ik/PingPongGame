namespace PingPongGame.GameLogic
{
    public class GameSettings
    {
        public int FieldWidth { get; }
        public int FieldHeight { get; }
        public int MaxScore { get; }

        public float BallRadius { get; } = 8f;
        public float BallSpeed { get; } = 250f;      // пикселей в секунду
        public float PaddleWidth { get; } = 10f;
        public float PaddleHeight { get; } = 80f;
        public float PaddleSpeed { get; } = 300f;    // пикселей в секунду

        public GameSettings(int fieldWidth, int fieldHeight, int maxScore = 5)
        {
            FieldWidth = fieldWidth;
            FieldHeight = fieldHeight;
            MaxScore = maxScore;
        }
    }
}
