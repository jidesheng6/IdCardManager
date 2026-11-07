using IdCardManager.Domain.Entities;

namespace IdCardManager.Domain
{
    /// <summary>东升加油站固定参数（不再提供参数设置页）</summary>
    public static class StationDefaults
    {
        public const string AppTitle = "东升加油站用户卡管理（简易版）";

        public static AppSettings Create(bool useFakeCard = false)
        {
            return new AppSettings
            {
                TerminalNo = "00000001",
                UserKey = "68<879<<56<=====",
                StationNo = "00000001",
                AreaNo = "32",
                OperatorName = "本地操作员",
                LrCenterPath = @"F:\LrSingle\LRCenter",
                UserCardNad = 0,
                AuthCardNad = 1,
                ReaderType = 2,
                ReaderPort = 1,
                ReaderPollMs = 1500,
                UseFakeCard = useFakeCard
            };
        }

        public static AppSettings Merge(AppSettings stored)
        {
            var fixedSettings = Create(stored != null && stored.UseFakeCard);
            return fixedSettings;
        }
    }
}
