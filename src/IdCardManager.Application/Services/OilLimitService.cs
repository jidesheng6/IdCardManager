using System.Collections.Generic;
using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Enums;

namespace IdCardManager.Application.Services
{
    public sealed class OilLimitService
    {
        private readonly ICardFacade _card;
        private readonly IReaderMonitor _monitor;
        private readonly ISettingsRepository _settings;

        public OilLimitService(ICardFacade card, IReaderMonitor monitor, ISettingsRepository settings)
        {
            _card = card;
            _monitor = monitor;
            _settings = settings;
        }

        public OilLimitResult Read(string pin)
        {
            var status = _monitor.Current ?? _card.GetSlotStatus();
            if (status.ReaderState != ReaderConnectionState.Connected)
            {
                return Fail("读卡器未连接");
            }

            if (!status.UserCardPresent)
            {
                return Fail("请插入用户卡");
            }

            if (string.IsNullOrWhiteSpace(pin))
            {
                return Fail("请输入用户卡密码");
            }

            return _card.ReadOilLimit(pin.Trim());
        }

        public OilLimitResult Update(IEnumerable<string> codes, string pin)
        {
            var status = _monitor.Current ?? _card.GetSlotStatus();
            if (status.ReaderState != ReaderConnectionState.Connected)
            {
                return Fail("读卡器未连接");
            }

            if (status.AuthCardType == AuthCardType.None)
            {
                return Fail("请插入授权卡");
            }

            if (status.AuthCardType != AuthCardType.ValueCard)
            {
                return Fail("授权卡类型不匹配");
            }

            if (!status.UserCardPresent)
            {
                return Fail("请插入用户卡");
            }

            if (string.IsNullOrWhiteSpace(pin))
            {
                return Fail("请输入用户卡密码");
            }

            var settings = _settings.Get();
            if (settings == null || string.IsNullOrWhiteSpace(settings.UserKey))
            {
                return Fail("请先配置用户密钥");
            }

            return _card.UpdateOilLimit(codes, pin.Trim(), settings);
        }

        private static OilLimitResult Fail(string message)
        {
            return new OilLimitResult { Success = false, ErrorMessage = message };
        }
    }
}
