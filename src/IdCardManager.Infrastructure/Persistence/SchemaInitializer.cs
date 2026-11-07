using System.Data;

namespace IdCardManager.Infrastructure.Persistence
{
    public static class SchemaInitializer
    {
        public static void EnsureCreated(IDbConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS settings (
  Id INTEGER PRIMARY KEY CHECK (Id = 1),
  Json TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS trades (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  TradeNo TEXT NOT NULL UNIQUE,
  TradeType INTEGER NOT NULL,
  CardSerialNo TEXT NOT NULL,
  AmountFen INTEGER NOT NULL,
  BalanceAfterFen INTEGER NULL,
  OperatorName TEXT NOT NULL,
  CreatedAt TEXT NOT NULL,
  Success INTEGER NOT NULL,
  ErrorMessage TEXT NULL
);";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
