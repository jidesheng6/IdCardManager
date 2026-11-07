using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;

namespace IdCardManager.Infrastructure.Card
{
    public sealed class FakeCardFacade : ICardFacade
    {
        private bool _open = true;
        private uint _balance = 100000; // 1000.00 元
        private string _pin = "123456";

        public bool Open()
        {
            _open = true;
            return true;
        }

        public void Close()
        {
            _open = false;
        }

        public bool Reconnect()
        {
            Close();
            return Open();
        }

        public CardSlotStatus GetSlotStatus()
        {
            return new CardSlotStatus
            {
                ReaderState = _open ? ReaderConnectionState.Connected : ReaderConnectionState.Disconnected,
                AuthCardType = AuthCardType.ValueCard,
                UserCardPresent = true,
                UserCardSerialNo = "FAKE0001",
                Message = _open ? "模拟读卡器已连接" : "读卡器未连接"
            };
        }

        public CardSnapshot ReadUserCard(string pin)
        {
            return new CardSnapshot
            {
                AppSerialNo = "FAKE0001",
                BalanceFen = _balance,
                EnablementDate = "2020-01-01",
                ExpirationDate = "2030-12-31",
                HolderName = "测试用户",
                CertificateNo = "000000",
                OilTypeCode = "1020",
                OilTypeHex = "1020",
                DetectedCardType = "OilCard",
                CardStep = "Saled",
                IsGreyCard = "否",
                PubAppHex = "00",
                CommonHex = "00",
                PrivateHex = "00",
                SensitiveHex = "1020"
            };
        }

        public uint? GetBalance(string pin) => _balance;

        public CardUnlockResult Unlock(string pin)
        {
            if (pin != _pin)
            {
                return new CardUnlockResult { Success = false, ErrorMessage = "密码错误" };
            }

            return new CardUnlockResult
            {
                Success = true,
                CardSerialNo = "FAKE0001",
                BalanceFen = _balance,
                OilLimitText = "92号汽油",
                OilLimitCodes = "1020"
            };
        }

        public StoreWithdrawResult ResetPin(string newPin, AppSettings settings)
        {
            _pin = newPin;
            return new StoreWithdrawResult { Success = true, CardSerialNo = "FAKE0001" };
        }

        public StoreWithdrawResult Store(int amountFen, string pin, AppSettings settings)
        {
            _balance += (uint)amountFen;
            return new StoreWithdrawResult
            {
                Success = true,
                CardSerialNo = "FAKE0001",
                BalanceAfterFen = _balance
            };
        }

        public StoreWithdrawResult Withdraw(int amountFen, string pin, AppSettings settings)
        {
            if (_balance < amountFen)
            {
                return new StoreWithdrawResult { Success = false, ErrorMessage = "余额不足", CardSerialNo = "FAKE0001" };
            }

            _balance -= (uint)amountFen;
            return new StoreWithdrawResult
            {
                Success = true,
                CardSerialNo = "FAKE0001",
                BalanceAfterFen = _balance
            };
        }
    }
}
