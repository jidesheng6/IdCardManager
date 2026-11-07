using System;
using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;

namespace IdCardManager.Application.Services
{
    public sealed class ReaderStatusHub
    {
        private readonly IReaderMonitor _monitor;
        private readonly ICardFacade _card;

        public ReaderStatusHub(IReaderMonitor monitor, ICardFacade card)
        {
            _monitor = monitor;
            _card = card;
        }

        public event Action<CardSlotStatus> StatusChanged
        {
            add => _monitor.StatusChanged += value;
            remove => _monitor.StatusChanged -= value;
        }

        public CardSlotStatus Current => _monitor.Current;

        public bool IsBusy => _monitor.IsPaused;

        public void Start() => _monitor.Start();

        public void Stop() => _monitor.Stop();

        public void Pause() => _monitor.Pause();

        public void Resume() => _monitor.Resume();

        public void RefreshNow(bool ignorePause = false) => _monitor.RefreshNow(ignorePause);

        /// <summary>一键硬重连读卡器并立即刷新状态（忽略 Pause，供忙等窗口内调用）。</summary>
        public bool ReconnectReader()
        {
            var ok = _card.Reconnect();
            try
            {
                _monitor.RefreshNow(ignorePause: true);
            }
            catch
            {
                // ignore
            }

            return ok;
        }

        public void PublishBalance(uint? balanceFen) => _monitor.PublishBalance(balanceFen);

        public void PublishSession(uint? balanceFen, string oilLimitText) =>
            _monitor.PublishSession(balanceFen, oilLimitText);
    }
}
