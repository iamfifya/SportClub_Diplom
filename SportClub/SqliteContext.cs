using System;
using System.Data.Entity;
using System.Data.Common;
using System.Data.SQLite;

namespace SportClub
{
    /// <summary>
    /// Контекст Entity Framework для локальной базы данных SQLite (файл SportClub.db).
    /// Класс и имена свойств сохранены без изменений, чтобы не трогать код окон приложения.
    /// </summary>
    public class dkEntities : DbContext
    {
        // Имя файла локальной базы данных SQLite
        public const string DbFileName = "SportClub.db";

        public dkEntities()
            : base(BuildConnection(), true)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Первичные ключи с автоинкрементом (SQLite: INTEGER PRIMARY KEY AUTOINCREMENT)
            modelBuilder.Entity<AbonementsType>().HasKey(a => a.ID);
            modelBuilder.Entity<Experts>().HasKey(e => e.ID);
            modelBuilder.Entity<Users>().HasKey(u => u.ID);

            // Ограничения по длине строк соответствуют исходной структуре таблиц SQL Server
            modelBuilder.Entity<AbonementsType>().Property(a => a.Abonement).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<Experts>().Property(e => e.Expert).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<Experts>().Property(e => e.Photo).HasMaxLength(50);
            modelBuilder.Entity<Users>().Property(u => u.FullName).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<Users>().Property(u => u.Report).IsOptional();
            modelBuilder.Entity<Users>().Property(u => u.Photo).HasMaxLength(50);

            // Связь Users -> AbonementsType (обязательная): AbonementID
            modelBuilder.Entity<Users>()
                .HasRequired(u => u.AbonementsType)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.AbonementID);

            // Связь Users -> Experts (необязательная): ExpertID
            modelBuilder.Entity<Users>()
                .HasOptional(u => u.Experts)
                .WithMany(e => e.Users)
                .HasForeignKey(u => u.ExpertID);
        }

        /// <summary>
        /// Строит соединение с локальным файлом SQLite и при необходимости создаёт сам файл и таблицы.
        /// </summary>
        private static DbConnection BuildConnection()
        {
            var connection = new SQLiteConnection("Data Source=" + DbFileName + ";Version=3;Foreign Keys=True;");
            EnsureDatabase(connection);
            return connection;
        }

        /// <summary>
        /// Создаёт файл базы данных и структуру таблиц, если они ещё не существуют.
        /// </summary>
        private static void EnsureDatabase(SQLiteConnection connection)
        {
            bool createSchema = !System.IO.File.Exists(DbFileName);
            connection.Open();
            try
            {
                if (!createSchema)
                {
                    // Проверяем наличие таблиц (файл мог быть пустым)
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('AbonementsType','Experts','Users')";
                        long count = Convert.ToInt64(cmd.ExecuteScalar());
                        if (count == 3)
                            return;
                    }
                }

                using (var transaction = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText =
                            @"CREATE TABLE IF NOT EXISTS AbonementsType (
                                ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                                Abonement NVARCHAR(50) NOT NULL
                              );
                              CREATE TABLE IF NOT EXISTS Experts (
                                ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                                Expert NVARCHAR(50) NOT NULL,
                                Photo NVARCHAR(50)
                              );
                              CREATE TABLE IF NOT EXISTS Users (
                                ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                                FullName NVARCHAR(50) NOT NULL,
                                AbonementID INTEGER NOT NULL,
                                Report NVARCHAR,
                                ExpertID INTEGER,
                                PriceOfAbonement INTEGER NOT NULL,
                                Photo NVARCHAR(50),
                                CONSTRAINT FK_Users_AbonementsType FOREIGN KEY (AbonementID) REFERENCES AbonementsType(ID),
                                CONSTRAINT FK_Users_Experts FOREIGN KEY (ExpertID) REFERENCES Experts(ID)
                              );";
                        cmd.ExecuteNonQuery();
                    }

                    SeedData(connection);

                    transaction.Commit();
                }
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Заполняет справочники начальными данными, если таблицы пусты.
        /// </summary>
        private static void SeedData(SQLiteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM AbonementsType";
                if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
                {
                    cmd.CommandText =
                        @"INSERT INTO AbonementsType (Abonement) VALUES
                          ('Бассейн'),
                          ('Тренажёрный зал'),
                          ('Аэробика'),
                          ('Йога'),
                          ('Единоборства');";
                    cmd.ExecuteNonQuery();
                }

                cmd.CommandText = "SELECT COUNT(*) FROM Experts";
                if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
                {
                    cmd.CommandText =
                        @"INSERT INTO Experts (Expert, Photo) VALUES
                          ('Иванов Иван Иванович', 'image1.jpg'),
                          ('Петров Пётр Петрович', 'image2.jpg'),
                          ('Сидорова Анна Сергеевна', 'image3.jpg'),
                          ('Кузнецова Мария Дмитриевна', 'image4.jpg');";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
