using System.Collections.Generic;

namespace IdCardManager.Domain.Entities
{
    public sealed class CardSnapshot
    {
        // 公共应用文件 SP_PUBLIC_APP_FILE
        public string IssuerId { get; set; }
        public string AppTypeId { get; set; }
        public string AppVer { get; set; }
        public string AppSerialNo { get; set; }
        public string EnablementDate { get; set; }
        public string ExpirationDate { get; set; }
        public string InstructionSetVer { get; set; }
        public string Fci { get; set; }

        // 常用信息文件 SP_COMMONLY_INFO_FILE
        public string UseDefaultPassword { get; set; }
        public string EmployeeNo { get; set; }
        public string EmployeePasswordHex { get; set; }

        // 私有数据文件 SP_PRIVATE_DATA_FILE
        public string CardTypeCode { get; set; }
        public string EmployeeId { get; set; }
        public string HolderName { get; set; }
        public string CertificateNo { get; set; }
        public string CertificateType { get; set; }

        // 敏感信息文件 SP_SENSITIVITY_INFO_FILE
        public string OilTypeCode { get; set; }
        public string OilTypeHex { get; set; }
        public string AreaMode { get; set; }
        public string AreaLimit { get; set; }
        public string AreaLimitHex { get; set; }
        public string PerTimeAmount { get; set; }
        public string PerTimeAmountHex { get; set; }
        public string PerDayTimes { get; set; }
        public string PerDayAmount { get; set; }
        public string PerDayAmountHex { get; set; }
        public string PlateNumber { get; set; }
        public string PriceIndex { get; set; }

        // 其它
        public uint? BalanceFen { get; set; }
        public string IsGreyCard { get; set; }
        public string CardStep { get; set; }
        public string DetectedCardType { get; set; }
        public Dictionary<string, string> ExtraFields { get; set; } = new Dictionary<string, string>();

        public string PubAppHex { get; set; }
        public string CommonHex { get; set; }
        public string PrivateHex { get; set; }
        public string SensitiveHex { get; set; }
    }
}
