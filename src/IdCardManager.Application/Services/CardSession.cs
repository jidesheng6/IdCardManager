using System;
using IdCardManager.Domain.Entities;

namespace IdCardManager.Application.Services
{
    /// <summary>
    /// 插卡验密后的会话：拔卡或换卡后失效，期间业务不再重复要密码
    /// </summary>
    public sealed class CardSession
    {
        private readonly object _sync = new object();

        public bool IsUnlocked { get; private set; }
        public string Pin { get; private set; }
        public string CardSerialNo { get; private set; }
        public uint? BalanceFen { get; private set; }
        public string OilLimitText { get; private set; }
        public string OilLimitCodes { get; private set; }

        public event Action Changed;

        public void Unlock(string pin, string serialNo, uint? balanceFen, string oilLimitText, string oilLimitCodes)
        {
            lock (_sync)
            {
                IsUnlocked = true;
                Pin = pin;
                CardSerialNo = serialNo ?? string.Empty;
                BalanceFen = balanceFen;
                OilLimitText = oilLimitText ?? string.Empty;
                OilLimitCodes = oilLimitCodes ?? string.Empty;
            }

            Changed?.Invoke();
        }

        public void UpdateBalance(uint? balanceFen)
        {
            lock (_sync)
            {
                if (!IsUnlocked) return;
                BalanceFen = balanceFen;
            }

            Changed?.Invoke();
        }

        public void SyncDevice(CardSlotStatus status)
        {
            var clear = false;
            lock (_sync)
            {
                if (!IsUnlocked)
                {
                    return;
                }

                if (status == null ||
                    status.ReaderState != Domain.Enums.ReaderConnectionState.Connected ||
                    !status.UserCardPresent)
                {
                    clear = true;
                }
                else if (!string.IsNullOrEmpty(CardSerialNo) &&
                         !string.IsNullOrEmpty(status.UserCardSerialNo) &&
                         !string.Equals(CardSerialNo, status.UserCardSerialNo, StringComparison.OrdinalIgnoreCase))
                {
                    clear = true;
                }
            }

            if (clear)
            {
                Lock();
            }
        }

        public void Lock()
        {
            lock (_sync)
            {
                IsUnlocked = false;
                Pin = null;
                CardSerialNo = null;
                BalanceFen = null;
                OilLimitText = null;
                OilLimitCodes = null;
            }

            Changed?.Invoke();
        }
    }
}
