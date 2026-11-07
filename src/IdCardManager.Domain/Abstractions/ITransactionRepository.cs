using System.Collections.Generic;
using IdCardManager.Domain.Entities;

namespace IdCardManager.Domain.Abstractions
{
    public interface ITransactionRepository
    {
        void Insert(TradeRecord record);
        IReadOnlyList<TradeRecord> Query(string cardSerialNo = null, int take = 200);
    }
}
