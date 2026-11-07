using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;

namespace IdCardManager.Application.Services
{
    public sealed class CardDataService
    {
        private readonly ICardFacade _card;
        private readonly IReaderMonitor _monitor;

        public CardDataService(ICardFacade card, IReaderMonitor monitor)
        {
            _card = card;
            _monitor = monitor;
        }

        public CardSnapshot Read(string pin)
        {
            var status = _monitor.Current ?? _card.GetSlotStatus();
            if (status.ReaderState != ReaderConnectionState.Connected)
            {
                return new CardSnapshot { ExtraFields = { ["错误"] = "读卡器未连接" } };
            }

            if (!status.UserCardPresent)
            {
                return new CardSnapshot { ExtraFields = { ["错误"] = "未检测到用户卡" } };
            }

            return _card.ReadUserCard(pin ?? string.Empty);
        }
    }
}
