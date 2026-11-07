using System;
using System.IO;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;
using IdCardManager.Infrastructure.Persistence;
using NUnit.Framework;

namespace IdCardManager.Tests
{
    public class TransactionRepositoryTests
    {
        [Test]
        public void Insert_Then_Query()
        {
            var path = Path.Combine(Path.GetTempPath(), "idcard-test-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                var factory = new SqliteConnectionFactory(path);
                var repo = new TransactionRepository(factory);
                repo.Insert(new TradeRecord
                {
                    TradeNo = Guid.NewGuid().ToString("N"),
                    TradeType = TradeType.Store,
                    CardSerialNo = "C001",
                    AmountFen = 500,
                    BalanceAfterFen = 1500,
                    OperatorName = "t",
                    CreatedAt = DateTime.Now,
                    Success = true
                });

                var list = repo.Query("C001");
                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(500, list[0].AmountFen);
                Assert.IsTrue(list[0].Success);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
