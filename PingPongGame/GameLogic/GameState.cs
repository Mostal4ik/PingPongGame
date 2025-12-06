namespace PingPongGame.GameLogic
{
    public enum GameState
    {
        WaitingToStart, // ждём начала розыгрыша (мяч стоит)
        Playing,        // идёт игра
        Paused,         // пауза
        GameOver        // игра закончена (кто-то набрал MaxScore)
    }
}
