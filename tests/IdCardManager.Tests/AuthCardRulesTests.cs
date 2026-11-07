using IdCardManager.Domain.Enums;
using IdCardManager.Domain.Rules;
using NUnit.Framework;

namespace IdCardManager.Tests
{
    public class AuthCardRulesTests
    {
        [Test]
        public void Store_Requires_ValueCard()
        {
            Assert.IsFalse(AuthCardRules.CanProceed(TradeType.Store, AuthCardType.MotherCard));
            Assert.IsFalse(AuthCardRules.CanProceed(TradeType.Store, AuthCardType.None));
            Assert.IsTrue(AuthCardRules.CanProceed(TradeType.Store, AuthCardType.ValueCard));
        }

        [Test]
        public void Withdraw_Requires_ValueCard()
        {
            Assert.IsTrue(AuthCardRules.CanProceed(TradeType.Withdraw, AuthCardType.ValueCard));
            Assert.IsFalse(AuthCardRules.CanProceed(TradeType.Withdraw, AuthCardType.UserMotherCard));
        }

        [Test]
        public void Block_Message_When_Missing()
        {
            Assert.AreEqual("请插入授权卡", AuthCardRules.GetBlockMessage(TradeType.Store, AuthCardType.None));
            Assert.AreEqual("授权卡类型不匹配", AuthCardRules.GetBlockMessage(TradeType.Store, AuthCardType.MotherCard));
        }
    }
}
