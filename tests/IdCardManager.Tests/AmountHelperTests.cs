using IdCardManager.Domain.Helpers;
using NUnit.Framework;

namespace IdCardManager.Tests
{
    public class AmountHelperTests
    {
        [Test]
        public void YuanToFen_Works()
        {
            Assert.AreEqual(1000, AmountHelper.YuanToFen(10m));
            Assert.AreEqual(101, AmountHelper.YuanToFen(1.01m));
        }

        [Test]
        public void Reject_NonPositive()
        {
            Assert.IsFalse(AmountHelper.TryParseYuan("0", out _));
            Assert.IsFalse(AmountHelper.TryParseYuan("-1", out _));
            Assert.IsTrue(AmountHelper.TryParseYuan("1.5", out var y));
            Assert.AreEqual(1.5m, y);
        }
    }
}
