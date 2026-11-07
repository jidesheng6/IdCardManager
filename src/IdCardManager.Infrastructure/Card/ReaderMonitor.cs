using System;
using System.Threading;
using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;

namespace IdCardManager.Infrastructure.Card
{
    public sealed class ReaderMonitor : IReaderMonitor
    {
        private readonly ICardFacade _facade;
        private readonly int _intervalMs;
        private Timer _timer;
        private readonly object _sync = new object();
        private int _paused;
        private int _refreshing;
        private uint? _cachedBalanceFen;
        private string _cachedOilLimitText;

        public ReaderMonitor(ICardFacade facade, int intervalMs = 1200)
        {
            _facade = facade;
            _intervalMs = Math.Max(800, intervalMs);
            Current = new CardSlotStatus
            {
                ReaderState = ReaderConnectionState.Disconnected,
                AuthCardType = AuthCardType.None,
                Message = "读卡器未连接"
            };
        }

        public event Action<CardSlotStatus> StatusChanged;

        public CardSlotStatus Current { get; private set; }

        public bool IsPaused => Volatile.Read(ref _paused) > 0;

        public void Start()
        {
            RefreshNow();
            _timer = new Timer(_ => SafeRefresh(), null, _intervalMs, _intervalMs);
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Pause()
        {
            Interlocked.Increment(ref _paused);
        }

        public void Resume()
        {
            if (Interlocked.Decrement(ref _paused) < 0)
            {
                Interlocked.Exchange(ref _paused, 0);
            }
        }

        public void PublishBalance(uint? balanceFen)
        {
            PublishSession(balanceFen, _cachedOilLimitText);
        }

        public void PublishSession(uint? balanceFen, string oilLimitText)
        {
            _cachedBalanceFen = balanceFen;
            if (oilLimitText != null)
            {
                _cachedOilLimitText = oilLimitText;
            }

            CardSlotStatus snapshot;
            lock (_sync)
            {
                var next = (Current ?? new CardSlotStatus()).Clone();
                next.BalanceFen = balanceFen;
                next.OilLimitText = _cachedOilLimitText;
                Current = next;
                snapshot = next;
            }

            StatusChanged?.Invoke(snapshot);
        }

        public void RefreshNow(bool ignorePause = false)
        {
            if (!ignorePause && IsPaused)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _refreshing, 1, 0) != 0)
            {
                return;
            }

            try
            {
                CardSlotStatus next;
                try
                {
                    next = _facade.GetSlotStatus();
                }
                catch
                {
                    next = new CardSlotStatus
                    {
                        ReaderState = ReaderConnectionState.Disconnected,
                        AuthCardType = AuthCardType.None,
                        Message = "读卡器未连接"
                    };
                }

                // 轮询不读余额/油品，保留会话缓存
                next.BalanceFen = _cachedBalanceFen;
                next.OilLimitText = _cachedOilLimitText;
                if (!next.UserCardPresent)
                {
                    _cachedBalanceFen = null;
                    _cachedOilLimitText = null;
                    next.BalanceFen = null;
                    next.OilLimitText = null;
                }

                lock (_sync)
                {
                    Current = next;
                }

                StatusChanged?.Invoke(next);
            }
            finally
            {
                Interlocked.Exchange(ref _refreshing, 0);
            }
        }

        private void SafeRefresh()
        {
            try
            {
                RefreshNow();
            }
            catch
            {
                // 定时轮询异常不向外抛
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
