using System;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.IO;

namespace PolyclinicApp.Services
{
    public static class DatabaseBackupService
    {
        private const string DatabaseName = "Поликлиника";
        private const string EntityConnectionName = "ПоликлиникаEntities";

        public static void CreateBackup(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath))
                throw new Exception("Не указан путь для резервной копии.");

            string directory = Path.GetDirectoryName(backupFilePath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string providerConnectionString = GetProviderConnectionString();

            string sql =
                "BACKUP DATABASE [" + DatabaseName + "] " +
                "TO DISK = N'" + EscapeSql(backupFilePath) + "' " +
                "WITH INIT, FORMAT, NAME = N'Backup_" + DatabaseName + "', STATS = 10;";

            using (SqlConnection connection = new SqlConnection(providerConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = 0;
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void CreateSixBackups(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new Exception("Не указана папка для резервных копий.");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            for (int i = 1; i <= 6; i++)
            {
                string fileName =
                    "Поликлиника_backup_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmss") +
                    "_day_" + i + ".bak";

                string fullPath = Path.Combine(folderPath, fileName);

                CreateBackup(fullPath);

                System.Threading.Thread.Sleep(1000);
            }
        }

        public static void RestoreBackup(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath))
                throw new Exception("Не выбран файл резервной копии.");

            if (!File.Exists(backupFilePath))
                throw new Exception("Файл резервной копии не найден.");

            SqlConnection.ClearAllPools();

            string masterConnectionString = GetMasterConnectionString();

            using (SqlConnection connection = new SqlConnection(masterConnectionString))
            {
                connection.Open();

                try
                {
                    ExecuteNonQuery(connection,
                        "ALTER DATABASE [" + DatabaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");

                    ExecuteNonQuery(connection,
                        "RESTORE DATABASE [" + DatabaseName + "] " +
                        "FROM DISK = N'" + EscapeSql(backupFilePath) + "' " +
                        "WITH REPLACE, STATS = 10;");

                    ExecuteNonQuery(connection,
                        "ALTER DATABASE [" + DatabaseName + "] SET MULTI_USER;");
                }
                catch
                {
                    try
                    {
                        ExecuteNonQuery(connection,
                            "ALTER DATABASE [" + DatabaseName + "] SET MULTI_USER;");
                    }
                    catch
                    {
                    }

                    throw;
                }
            }

            SqlConnection.ClearAllPools();
        }

        private static void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = 0;
                command.ExecuteNonQuery();
            }
        }

        private static string GetProviderConnectionString()
        {
            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings[EntityConnectionName];

            if (settings == null)
                throw new Exception("В App.config не найдена строка подключения " + EntityConnectionName + ".");

            string connectionString = settings.ConnectionString;

            if (connectionString.TrimStart().StartsWith("metadata=", StringComparison.OrdinalIgnoreCase))
            {
                EntityConnectionStringBuilder builder =
                    new EntityConnectionStringBuilder(connectionString);

                return builder.ProviderConnectionString;
            }

            return connectionString;
        }

        private static string GetMasterConnectionString()
        {
            string providerConnectionString = GetProviderConnectionString();

            SqlConnectionStringBuilder builder =
                new SqlConnectionStringBuilder(providerConnectionString);

            builder.InitialCatalog = "master";

            return builder.ConnectionString;
        }

        private static string EscapeSql(string value)
        {
            return value.Replace("'", "''");
        }
    }
}