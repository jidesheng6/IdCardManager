using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;

namespace IdCardManager.Infrastructure.Persistence
{
    public sealed class TransactionRepository : ITransactionRepository
    {
        private readonly SqliteConnectionFactory _factory;

        public TransactionRepository(SqliteConnectionFactory factory)
        {
            _factory = factory;
        }

        public void Insert(TradeRecord record)
        {
            using (var conn = _factory.Create())
            {
                SchemaInitializer.EnsureCreated(conn);
                conn.Execute(@"
INSERT INTO trades(TradeNo, TradeType, CardSerialNo, AmountFen, BalanceAfterFen, OperatorName, CreatedAt, Success, ErrorMessage)
VALUES(@TradeNo, @TradeType, @CardSerialNo, @AmountFen, @BalanceAfterFen, @OperatorName, @CreatedAt, @Success, @ErrorMessage);",
                    new
                    {
                        record.TradeNo,
                        TradeType = (int)record.TradeType,
                        record.CardSerialNo,
                        record.AmountFen,
                        record.BalanceAfterFen,
                        record.OperatorName,
                        CreatedAt = record.CreatedAt.ToString("o"),
                        Success = record.Success ? 1 : 0,
                        record.ErrorMessage
                    });
            }
        }

        public IReadOnlyList<TradeRecord> Query(string cardSerialNo = null, int take = 200)
        {
            using (var conn = _factory.Create())
            {
                SchemaInitializer.EnsureCreated(conn);
                var sql = @"
SELECT Id, TradeNo, TradeType, CardSerialNo, AmountFen, BalanceAfterFen, OperatorName, CreatedAt, Success, ErrorMessage
FROM trades
WHERE (@card IS NULL OR @card = '' OR CardSerialNo = @card)
ORDER BY Id DESC
LIMIT @take;";
                var rows = conn.Query<TradeRow>(sql, new { card = cardSerialNo, take }).ToList();
                return rows.Select(r => new TradeRecord
                {
                    Id = r.Id,
                    TradeNo = r.TradeNo,
                    TradeType = (TradeType)r.TradeType,
                    CardSerialNo = r.CardSerialNo,
                    AmountFen = r.AmountFen,
                    BalanceAfterFen = r.BalanceAfterFen,
                    OperatorName = r.OperatorName,
                    CreatedAt = DateTime.Parse(r.CreatedAt),
                    Success = r.Success == 1,
                    ErrorMessage = r.ErrorMessage
                }).ToList();
            }
        }

        private sealed class TradeRow
        {
            public long Id { get; set; }
            public string TradeNo { get; set; }
            public int TradeType { get; set; }
            public string CardSerialNo { get; set; }
            public int AmountFen { get; set; }
            public int? BalanceAfterFen { get; set; }
            public string OperatorName { get; set; }
            public string CreatedAt { get; set; }
            public int Success { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}
