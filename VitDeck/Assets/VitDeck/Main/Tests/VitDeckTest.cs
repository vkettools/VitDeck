using NUnit.Framework;
using VitDeck.Utility;

namespace VitDeck.Main.Tests
{
    public class VitDeckTest
    {
        [Test]
        public void TestVitDeckVersion()
        {
            string version = VitDeck.GetVersion();
            Assert.That(VersionUtility.IsSemanticVersioning(version), Is.True);
        }
    }
}