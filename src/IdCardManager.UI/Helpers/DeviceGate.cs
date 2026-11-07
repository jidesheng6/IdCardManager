using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;
using IdCardManager.UI.Composition;

namespace IdCardManager.UI.Helpers
{
    /// <summary>
    /// 读卡器已连接 + 充值授权卡(ValueCard) + 用户卡 三者齐才允许业务输入。
    /// </summary>
    internal static class DeviceGate
    {
        public static bool IsReady(CardSlotStatus status)
        {
            return status != null
                   && status.ReaderState == ReaderConnectionState.Connected
                   && status.AuthCardType == AuthCardType.ValueCard
                   && status.UserCardPresent;
        }

        public static bool IsReady(AppComposition app) => IsReady(app.StatusHub.Current);

        public static string BlockMessage(CardSlotStatus status)
        {
            if (status == null || status.ReaderState != ReaderConnectionState.Connected)
            {
                return "请连接读卡器";
            }

            if (status.AuthCardType == AuthCardType.None)
            {
                return "请插入授权卡";
            }

            if (status.AuthCardType != AuthCardType.ValueCard)
            {
                return "授权卡类型不匹配";
            }

            if (!status.UserCardPresent)
            {
                return "请插入用户卡";
            }

            return null;
        }
    }
}
