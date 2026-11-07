using IdCardManager.Domain.Enums;

namespace IdCardManager.Domain.Rules
{
    public static class AuthCardRules
    {
        public static bool CanProceed(TradeType tradeType, AuthCardType authCardType)
        {
            if (tradeType != TradeType.Store && tradeType != TradeType.Withdraw)
            {
                return false;
            }

            return authCardType == AuthCardType.ValueCard;
        }

        public static string GetBlockMessage(TradeType tradeType, AuthCardType authCardType)
        {
            if (authCardType == AuthCardType.None)
            {
                return "请插入授权卡";
            }

            if (!CanProceed(tradeType, authCardType))
            {
                return "授权卡类型不匹配";
            }

            return string.Empty;
        }
    }
}
