namespace IdCardManager.Domain.Entities
{
    public sealed class AppSettings
    {
        public string TerminalNo { get; set; } = "00000001";
        public string UserKey { get; set; } = string.Empty;
        public string StationNo { get; set; } = string.Empty;
        public string AreaNo { get; set; } = string.Empty;
        public string OperatorName { get; set; } = "本地操作员";
        public string LrCenterPath { get; set; } = @"F:\LrSingle\LRCenter";
        public int UserCardNad { get; set; } = 0;
        public int AuthCardNad { get; set; } = 1;
        public int ReaderType { get; set; } = 2;
        public int ReaderPort { get; set; } = 1;
        public int ReaderPollMs { get; set; } = 1500;
        public bool UseFakeCard { get; set; }
    }
}
