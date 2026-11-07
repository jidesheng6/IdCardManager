using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace IdCardManager.Infrastructure.Persistence
{
    public sealed class SqliteConnectionFactory
    {
        private readonly string _dbPath;

        public SqliteConnectionFactory(string dbPath = null)
        {
            if (!string.IsNullOrWhiteSpace(dbPath))
            {
                _dbPath = dbPath;
            }
            else
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "IdCardManager");
                Directory.CreateDirectory(dir);
                _dbPath = Path.Combine(dir, "idcard.db");
            }
        }

        public string DbPath => _dbPath;

        public IDbConnection Create()
        {
            var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            conn.Open();
            return conn;
        }
    }
}
