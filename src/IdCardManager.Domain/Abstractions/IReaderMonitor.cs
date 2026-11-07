using System;
using IdCardManager.Domain.Entities;

namespace IdCardManager.Domain.Abstractions
{
    public interface IReaderMonitor : IDisposable
    {
        event Action<CardSlotStatus> StatusChanged;
        CardSlotStatus Current { get; }
        bool IsPaused { get; }
        void Start();
        void Stop();
        void Pause();
        void Resume();
        void RefreshNow(bool ignorePause = false);
        void PublishBalance(uint? balanceFen);
        void PublishSession(uint? balanceFen, string oilLimitText);
    }
}
