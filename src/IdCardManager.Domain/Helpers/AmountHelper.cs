using System;

namespace IdCardManager.Domain.Helpers
{
    public static class AmountHelper
    {
        public static int YuanToFen(decimal yuan)
        {
            if (yuan <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(yuan), "金额必须大于零");
            }

            return (int)Math.Round(yuan * 100m, MidpointRounding.AwayFromZero);
        }

        public static decimal FenToYuan(int fen)
        {
            return fen / 100m;
        }

        public static bool TryParseYuan(string text, out decimal yuan)
        {
            yuan = 0m;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (!decimal.TryParse(text.Trim(), out yuan))
            {
                return false;
            }

            return yuan > 0m;
        }

        public static string ValidateSingleAmount(decimal yuan)
        {
            if (yuan <= 0m) return "请输入金额";
            if (yuan > AmountLimits.MaxSingleYuan)
            {
                return "单次最多 " + AmountLimits.MaxSingleText + " 元";
            }

            // 最多两位小数
            if (decimal.Round(yuan, 2) != yuan)
            {
                return "金额最多两位小数";
            }

            return null;
        }

        public static decimal FenToYuan(uint fen)
        {
            return fen / 100m;
        }
    }
}
