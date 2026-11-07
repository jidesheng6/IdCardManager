using IdCardManager.Domain.Enums;

namespace IdCardManager.Domain.Entities
{
    public sealed class CardSlotStatus
    {
        public ReaderConnectionState ReaderState { get; set; }
        public AuthCardType AuthCardType { get; set; }
        public bool UserCardPresent { get; set; }
        public string UserCardSerialNo { get; set; }
        public uint? BalanceFen { get; set; }
        public string OilLimitText { get; set; }
        public string Message { get; set; }

        public CardSlotStatus Clone()
        {
            return new CardSlotStatus
            {
                ReaderState = ReaderState,
                AuthCardType = AuthCardType,
                UserCardPresent = UserCardPresent,
                UserCardSerialNo = UserCardSerialNo,
                BalanceFen = BalanceFen,
                OilLimitText = OilLimitText,
                Message = Message
            };
        }
    }
}
