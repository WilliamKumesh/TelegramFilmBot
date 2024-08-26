using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using WebSiteParser;

namespace TelegramBotFirst
{
    public static class DataBaseEditor
    {
        private static readonly string connectionStringStart = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\Users\\User\\source\\repos\\C# Projects\\Новая папка\\TelegramBotFirst\\";

        private static readonly string connectionStringEnd = "\";Integrated Security=True";

        private static readonly string baseFilePath = $"C:\\Users\\User\\source\\repos\\C# Projects\\Новая папка\\TelegramBotFirst\\";

        private static string connectionString = "";

        public static void MakeConnectionString(string userId)
        {
            string databaseName = $"{userId}_Database.mdf";

            connectionString.Remove(0);

            connectionString = connectionStringStart + databaseName + connectionStringEnd;
        }

        public static bool CheckUserInSystem(string userId)
        {
            string databaseName = $"{userId}_Database.mdf";

            if (!File.Exists(baseFilePath + databaseName))
            {
                return false;
            }

            return true;
        }

        public static void CreateNewDataBase(string userId)
        {

            string databaseFileName = $"{userId}_Database.mdf";
            string databaseName = databaseFileName.Replace(".mdf", "");

            string fileName = baseFilePath + databaseFileName;

            // Создаем подключение к серверу без привязки к файлу
            string serverConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(serverConnectionString))
            {
                connection.Open();

                string checkDatabaseQuery = $@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
                BEGIN
                    DROP DATABASE [{databaseName}];
                END";

                using (SqlCommand command = new SqlCommand(checkDatabaseQuery, connection))
                {
                    command.ExecuteNonQuery();
                }


                // Корректный синтаксис для создания базы данных
                string createDatabaseQuery = $@"
                    CREATE DATABASE [{databaseName}] 
                    ON PRIMARY (
                        NAME = [{databaseName}], 
                        FILENAME = '{fileName}'
                    )";

                using (SqlCommand command = new SqlCommand(createDatabaseQuery, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine($"База данных '{databaseFileName}' успешно создана.");
                }
            }
        }

        public static void CreateTablesInDataBase()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string createFilmsTable = @"
        CREATE TABLE Films (
            FilmId NVARCHAR(50) PRIMARY KEY,
            Name NVARCHAR(50),
            Year INT,
            Description NVARCHAR(MAX),
            Rate FLOAT,
            Watched BIT
        );";

                string createGenresTable = @"
        CREATE TABLE Genres (
            GenreId NVARCHAR(50) PRIMARY KEY
        );";

                string createFilmGenresTable = @"
        CREATE TABLE FilmGenres (
            FilmId NVARCHAR(50),
            GenreId NVARCHAR(50),
            PRIMARY KEY (FilmId, GenreId),
            FOREIGN KEY (FilmId) REFERENCES Films(FilmId),
            FOREIGN KEY (GenreId) REFERENCES Genres(GenreId)
        );";

                using (SqlCommand command = new SqlCommand(createFilmsTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SqlCommand command = new SqlCommand(createGenresTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SqlCommand command = new SqlCommand(createFilmGenresTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                Console.WriteLine("Таблицы успешно созданы.");
            }
        }

        private static void SaveGenreToDatabase(string genre)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string checkQuery = "SELECT COUNT(*) FROM Genres WHERE GenreId = @Id";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", genre);
                    int count = (int)checkCommand.ExecuteScalar();

                    if (count == 0)
                    {
                        string insertQuery = "INSERT INTO Genres (GenreId) VALUES (@Id)";
                        using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Id", genre);
                            insertCommand.ExecuteNonQuery();
                            Console.WriteLine("GenreID добавлен в таблицу.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("GenreID уже существует в таблице.");
                    }
                }
            }
        }

        public static void SaveFilmToDatabase(FilmInfo info, bool watched = false)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string checkQuery = "SELECT COUNT(*) FROM Films WHERE FilmId = @Id";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", info.Url);
                    int count = (int)checkCommand.ExecuteScalar();

                    if (count == 0)
                    {
                        string insertQuery = "INSERT INTO Films (FilmId, Name, Year, Description, Rate, Watched) VALUES (@Id, @Name, @Year, @Description, @Rate, @Watched)";
                        using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Id", info.Url);
                            insertCommand.Parameters.AddWithValue("@Name", info.Name);
                            insertCommand.Parameters.AddWithValue("@Year", int.Parse(info.Year));
                            insertCommand.Parameters.AddWithValue("@Description", info.Description);
                            insertCommand.Parameters.AddWithValue("@Watched", watched);

                            float.TryParse(info.Rate, NumberStyles.Float, CultureInfo.InvariantCulture, out float value);

                            insertCommand.Parameters.AddWithValue("@Rate", value);
                            insertCommand.ExecuteNonQuery();
                            Console.WriteLine("FilmID добавлен в таблицу.");
                        }

                        foreach (var genre in info.Genres)
                        {
                            SaveGenreToDatabase(genre);

                            string insertGenreMovieQuery = "INSERT INTO FilmGenres (FilmId, GenreId) VALUES (@FilmId, @GenreId)";
                            using (SqlCommand insertGenreMovieCommand = new SqlCommand(insertGenreMovieQuery, connection))
                            {
                                insertGenreMovieCommand.Parameters.AddWithValue("@FilmId", info.Url);
                                insertGenreMovieCommand.Parameters.AddWithValue("@GenreId", genre);
                                insertGenreMovieCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("FilmID уже существует в таблице.");
                    }
                }
            }
        }

        public static bool MakeWatchedFilm(string filmName, bool watched)
        {
            string sqlQuery = watched ? "UPDATE films SET Watched = 0 WHERE Watched = 1 AND Name = @filmName" : "UPDATE films SET Watched = 1 WHERE Watched = 0 AND Name = @filmName";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@filmName", filmName);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public static FilmInfo GetInfoByName(string filmName)
        {
            FilmInfo film = new FilmInfo();

            using (SqlConnection connection = new SqlConnection(connectionString)) 
            {
                string query = "SELECT FilmId, Year, Description, Rate FROM Films WHERE Name = @Name";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@Name", filmName));
                
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while(reader.Read()) 
                    {
                        film.Name = filmName;
                        film.Url = reader["FilmId"].ToString();
                        film.Year = reader["Year"].ToString();
                        film.Description = reader["Description"].ToString();

                        var notNormalRate = reader["Rate"].ToString();

                        film.Rate = notNormalRate.Remove(3);
                    }
                }

                connection.Close();
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT GenreId FROM FilmGenres WHERE FilmId = @FilmId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@FilmId", film.Url));

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        List<string> genres = new List<string>();

                        while (reader.Read())
                        {
                            genres.Add(reader["GenreId"].ToString());
                        }

                        film.Genres = genres;
                    }
                }
            }


            return film;
        }

        public static string GetRandomFilm(bool watched = false)
        {
            string film_name = "";

            string query = "";

            if(watched)
            {
                query += "SELECT TOP 1 * FROM Films WHERE Watched = 1 ORDER BY NEWID()";
            }
            else
            {
                query += "SELECT TOP 1 * FROM Films WHERE Watched = 0 ORDER BY NEWID()";
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        film_name = reader.GetString(reader.GetOrdinal("Name"));
                    }
                }
            }

            return film_name;
        }

        private static string updateComand(string command, bool watched)
        {
            if (watched)
            {
                return command + " AND Watched = 1";
            }

            return command + " AND Watched = 0";

        }

        public static List<string> GetFilmsOlderThan(int year, bool watched = false)
        {
            List<string> movies = new List<string>();
            string query = "SELECT Name FROM Films WHERE Year > @Year";

            query = updateComand(query, watched);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Year", year);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    movies.Add(reader["Name"].ToString());             
                }
            }

            return movies;
        }

        public static List<string> GetFilmsByGenre(string genreName, bool watched = false)
        {
            List<string> movies = new List<string>();
            string query = @"SELECT m.Name 
                         FROM Films m 
                         JOIN FilmGenres mg ON m.FilmId = mg.FilmId 
                         JOIN Genres g ON mg.GenreId = g.GenreId 
                         WHERE g.GenreId = @GenreId";

            query = updateComand(query, watched);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GenreId", genreName);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    movies.Add(reader["Name"].ToString());
                }
            }

            return movies;
        }

        public static List<string> GetFilmsAboveRating(double inputRating, bool watched = false)
        {
            List<string> movies = new List<string>();
            string query = "SELECT Name FROM Films WHERE Rate > @Rate";

            query = updateComand(query, watched);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Rate", inputRating);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    movies.Add(reader["Name"].ToString());
                }
            }

            return movies;
        }
    }
}
