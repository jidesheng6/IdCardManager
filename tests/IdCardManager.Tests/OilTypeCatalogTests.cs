using System.Linq;
using IdCardManager.Infrastructure.Card;
using NUnit.Framework;

namespace IdCardManager.Tests
{
    public class OilTypeCatalogTests
    {
        [Test]
        public void Encode_SelectedCodes_RoundTrips()
        {
            var bytes = OilTypeCatalog.Encode(new[] { "1020", "1040" }, 8);

            var text = OilTypeCatalog.Decode(bytes, out var codes);

            Assert.AreEqual("92号汽油/95号汽油", text);
            Assert.AreEqual("1020,1040", codes);
        }

        [Test]
        public void Encode_Empty_Means_Unlimited()
        {
            var bytes = OilTypeCatalog.Encode(new string[0], 8);

            Assert.IsTrue(bytes.All(b => b == 0xFF));
            Assert.AreEqual("不限", OilTypeCatalog.Decode(bytes, out var codes));
            Assert.AreEqual(string.Empty, codes);
        }

        [Test]
        public void Decode_Skips_Padding_Bytes()
        {
            var bytes = new byte[] { 0x10, 0x20, 0x10, 0x40, 0xFF, 0xFF, 0x00, 0x00 };

            var text = OilTypeCatalog.Decode(bytes, out var codes);

            Assert.AreEqual("92号汽油/95号汽油", text);
            Assert.AreEqual("1020,1040", codes);
        }

        [Test]
        public void Encode_Expands_When_Not_Enough_Room()
        {
            var bytes = OilTypeCatalog.Encode(new[] { "1020", "1040", "1090" }, 4);

            Assert.AreEqual(6, bytes.Length);
            Assert.AreEqual("92号汽油/95号汽油/98号汽油", OilTypeCatalog.Decode(bytes, out var codes));
            Assert.AreEqual("1020,1040,1090", codes);
        }

        [Test]
        public void Encode_Unknown_Code_Still_Writes_Bytes()
        {
            var bytes = OilTypeCatalog.Encode(new[] { "1060" }, 8);

            Assert.AreEqual("1060", OilTypeCatalog.Decode(bytes, out var codes));
            Assert.AreEqual("1060", codes);
        }
    }
}
