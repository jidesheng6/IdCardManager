using System;
using IdCardManager.Domain.Enums;

namespace IdCardManager.Domain.Entities
{
    public sealed class TradeRecord
    {
        public long Id { get; set; }
        public string TradeNo { get; set; }
        public TradeType TradeType { get; set; }
        public string CardSerialNo { get; set; }
        public int AmountFen { get; set; }
        public int? BalanceAfterFen { get; set; }
        public string OperatorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
