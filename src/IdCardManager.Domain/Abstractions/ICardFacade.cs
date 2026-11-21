using System.Collections.Generic;
using IdCardManager.Domain.Entities;

namespace IdCardManager.Domain.Abstractions
{
    public interface ICardFacade
    {
        bool Open();
        void Close();
        /// <summary>硬重连：关闭并销毁读卡器实例后重新打开（等同拔插 USB 的软件侧恢复）。</summary>
        bool Reconnect();
        CardSlotStatus GetSlotStatus();
        CardSnapshot ReadUserCard(string pin);
        uint? GetBalance(string pin);
        CardUnlockResult Unlock(string pin);
        /// <summary>用户 PIN 无法从卡内读出；可用授权卡+系统密钥重置为新密码</summary>
        StoreWithdrawResult ResetPin(string newPin, AppSettings settings);
        /// <summary>读取当前卡的限加油品（需要用户卡密码）</summary>
        OilLimitResult ReadOilLimit(string pin);
        /// <summary>修改当前卡的限加油品；codes 为空表示不限</summary>
        OilLimitResult UpdateOilLimit(IEnumerable<string> codes, string pin, AppSettings settings);
        StoreWithdrawResult Store(int amountFen, string pin, AppSettings settings);
        StoreWithdrawResult Withdraw(int amountFen, string pin, AppSettings settings);
    }

    public sealed class OilLimitResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string CardSerialNo { get; set; }
        public string OilLimitText { get; set; }
        public string OilLimitCodes { get; set; }
    }

    public sealed class StoreWithdrawResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public uint? BalanceAfterFen { get; set; }
        public string CardSerialNo { get; set; }
    }

    public sealed class CardUnlockResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string CardSerialNo { get; set; }
        public uint? BalanceFen { get; set; }
        public string OilLimitText { get; set; }
        public string OilLimitCodes { get; set; }
    }
}
