using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using PingPongGame.GameLogic;

namespace PingPongGame.Data
{
    public static class Database
    {
        private const string DbFileName = "pingpong.db";

        private static readonly string DbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);

        // Делаем публичным, чтобы другие классы могли использовать при необходимости
        public static readonly string ConnectionString =
            $"Data Source={DbPath};Version=3;";

        // Этот статический конструктор вызывается один раз при первом обращении к классу
        static Database()
        {
            EnsureDatabase();
        }

        // Запись результата игры
        public class ScoreRecord
        {
            public long Id { get; set; }
            public string PlayerName { get; set; }
            public AIDifficulty Difficulty { get; set; }
            public int PlayerScore { get; set; }
            public int OpponentScore { get; set; }
            public int DurationSeconds { get; set; }
            public bool IsWin { get; set; }          // <-- ВАЖНО: это свойство, из-за него была ошибка
            public DateTime PlayedAt { get; set; }
        }

        // Создание БД и таблицы, плюс стартовые данные
        private static void EnsureDatabase()
        {
            bool needSeed = !File.Exists(DbPath);

            if (needSeed)
            {
                SQLiteConnection.CreateFile(DbPath);
            }

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS scores
                    (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PlayerName      TEXT NOT NULL,
                        Difficulty      INTEGER NOT NULL,
                        PlayerScore     INTEGER NOT NULL,
                        OpponentScore   INTEGER NOT NULL,
                        DurationSeconds INTEGER NOT NULL,
                        IsWin           INTEGER NOT NULL,
                        PlayedAt        TEXT NOT NULL
                    );";

                using (var cmd = new SQLiteCommand(createTableSql, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                if (needSeed)
                {
                    SeedInitialScores(connection);
                }
            }
        }

        // Первоначальные три записи, которые ты уже видел в таблице
        private static void SeedInitialScores(SQLiteConnection connection)
        {
            string insertSql = @"
                INSERT INTO scores
                    (PlayerName, Difficulty, PlayerScore, OpponentScore,
                     DurationSeconds, IsWin, PlayedAt)
                VALUES
                    (@name, @diff, @pScore, @oScore,
                     @duration, @isWin, @playedAt);";

            using (var cmd = new SQLiteCommand(insertSql, connection))
            {
                cmd.Parameters.Add("@name", System.Data.DbType.String);
                cmd.Parameters.Add("@diff", System.Data.DbType.Int32);
                cmd.Parameters.Add("@pScore", System.Data.DbType.Int32);
                cmd.Parameters.Add("@oScore", System.Data.DbType.Int32);
                cmd.Parameters.Add("@duration", System.Data.DbType.Int32);
                cmd.Parameters.Add("@isWin", System.Data.DbType.Int32);
                cmd.Parameters.Add("@playedAt", System.Data.DbType.String);

                void AddRow(string name, AIDifficulty diff, int p, int o, int dur, bool win, DateTime at)
                {
                    cmd.Parameters["@name"].Value = name;
                    cmd.Parameters["@diff"].Value = (int)diff;
                    cmd.Parameters["@pScore"].Value = p;
                    cmd.Parameters["@oScore"].Value = o;
                    cmd.Parameters["@duration"].Value = dur;
                    cmd.Parameters["@isWin"].Value = win ? 1 : 0;
                    cmd.Parameters["@playedAt"].Value = at.ToString("o");

                    cmd.ExecuteNonQuery();
                }

                AddRow("TestPlayer", AIDifficulty.Easy, 5, 3, 125, true, DateTime.Now.AddDays(-3));
                AddRow("ProGamer", AIDifficulty.Normal, 5, 4, 160, true, DateTime.Now.AddDays(-2));
                AddRow("Boss", AIDifficulty.Hard, 5, 4, 200, true, DateTime.Now.AddDays(-1));
            }
        }

        // Сохранение одного результата игры
        public static void SaveScore(ScoreRecord score)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                string sql = @"
                    INSERT INTO scores
                        (PlayerName, Difficulty, PlayerScore, OpponentScore,
                         DurationSeconds, IsWin, PlayedAt)
                    VALUES
                        (@name, @diff, @pScore, @oScore,
                         @duration, @isWin, @playedAt);";

                using (var cmd = new SQLiteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@name", score.PlayerName);
                    cmd.Parameters.AddWithValue("@diff", (int)score.Difficulty);
                    cmd.Parameters.AddWithValue("@pScore", score.PlayerScore);
                    cmd.Parameters.AddWithValue("@oScore", score.OpponentScore);
                    cmd.Parameters.AddWithValue("@duration", score.DurationSeconds);
                    cmd.Parameters.AddWithValue("@isWin", score.IsWin ? 1 : 0);
                    cmd.Parameters.AddWithValue("@playedAt", score.PlayedAt.ToString("o"));

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Чтение топ-результатов для таблицы лидеров
        public static List<ScoreRecord> GetTopScores(int limit)
        {
            var result = new List<ScoreRecord>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                string sql = @"
                    SELECT Id, PlayerName, Difficulty, PlayerScore, OpponentScore,
                           DurationSeconds, IsWin, PlayedAt
                    FROM scores
                    ORDER BY PlayerScore DESC, DurationSeconds ASC
                    LIMIT @limit;";

                using (var cmd = new SQLiteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@limit", limit);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var s = new ScoreRecord
                            {
                                Id = reader.GetInt64(0),
                                PlayerName = reader.GetString(1),
                                Difficulty = (AIDifficulty)reader.GetInt32(2),
                                PlayerScore = reader.GetInt32(3),
                                OpponentScore = reader.GetInt32(4),
                                DurationSeconds = reader.GetInt32(5),
                                IsWin = reader.GetInt32(6) != 0,
                                PlayedAt = DateTime.Parse(reader.GetString(7))
                            };
                            result.Add(s);
                        }
                    }
                }
            }

            return result;
        }
    }
}
