using System;
using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;
using IdCardManager.Domain.Rules;

namespace IdCardManager.Application.Services
{
    public sealed class WithdrawValueService
    {
        private readonly ICardFacade _card;
        private readonly IReaderMonitor _monitor;
        private readonly ITransactionRepository _trades;
        private readonly ISettingsRepository _settings;

        public WithdrawValueService(
            ICardFacade card,
            IReaderMonitor monitor,
            ITransactionRepository trades,
            ISettingsRepository settings)
        {
            _card = card;
            _monitor = monitor;
            _trades = trades;
            _settings = settings;
        }

        public StoreWithdrawResult Execute(int amountFen, string pin)
        {
            var status = _monitor.Current ?? _card.GetSlotStatus();
            if (status.ReaderState != ReaderConnectionState.Connected)
            {
                return Fail("读卡器未连接");
            }

            var block = AuthCardRules.GetBlockMessage(TradeType.Withdraw, status.AuthCardType);
            if (!string.IsNullOrEmpty(block))
            {
                return Fail(block);
            }

            if (string.IsNullOrWhiteSpace(pin))
            {
                return Fail("请输入用户卡密码");
            }

            var settings = _settings.Get();
            if (string.IsNullOrWhiteSpace(settings.UserKey))
            {
                return Fail("请先配置用户密钥");
            }

            var result = _card.Withdraw(amountFen, pin.Trim(), settings);
            _trades.Insert(new TradeRecord
            {
                TradeNo = Guid.NewGuid().ToString("N"),
                TradeType = TradeType.Withdraw,
                CardSerialNo = result.CardSerialNo ?? string.Empty,
                AmountFen = amountFen,
                BalanceAfterFen = result.BalanceAfterFen.HasValue ? (int?)result.BalanceAfterFen.Value : null,
                OperatorName = settings.OperatorName,
                CreatedAt = DateTime.Now,
                Success = result.Success,
                ErrorMessage = result.Success ? null : result.ErrorMessage
            });
            return result;
        }

        private static StoreWithdrawResult Fail(string message)
        {
            return new StoreWithdrawResult { Success = false, ErrorMessage = message };
        }
    }
}
