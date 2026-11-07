namespace IdCardManager.Domain.Helpers
{
    public static class AmountLimits
    {
        /// <summary>单次充钱/扣钱最大金额（元）— 2000 万</summary>
        public const decimal MaxSingleYuan = 20000000m;

        /// <summary>卡内余额上限（元）</summary>
        public const decimal MaxBalanceYuan = 20000000m;

        public static string MaxSingleText => MaxSingleYuan.ToString("0.##");

        public static readonly decimal[] StoreQuickAmounts =
        {
            500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000,
            5500, 6000, 6500, 7000, 7500, 8000, 8500, 9000, 9500
        };

        public static readonly decimal[] WithdrawQuickAmounts =
        {
            10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100
        };
    }
}
