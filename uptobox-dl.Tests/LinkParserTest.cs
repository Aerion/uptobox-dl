using NUnit.Framework;
using UptoboxDl.UptoboxClient;

namespace UptoboxDl.Tests
{
    public class LinkParserTest
    {
        [TestCase("https://uptobox.com/user_public?hash=foobar&folder=barbar", ExpectedResult = LinkParser.LinkType.Folder)]
        [TestCase("https://uptobox.com/foobar", ExpectedResult = LinkParser.LinkType.DirectLink)]
        [TestCase("foobar", ExpectedResult = LinkParser.LinkType.Unknown)]
        public LinkParser.LinkType Test_GetLinkType(string link)
        {
            return LinkParser.GetLinkType(link);
        }

        [TestCase("https://uptobox.com/m5f0ce9h197j", ExpectedResult = "m5f0ce9h197j")]
        [TestCase("https://uptobox.com/m5f0ce9h197j?randomparam=true", ExpectedResult = "m5f0ce9h197j")]
        public string Test_ParseFileCodeFromDirectLink(string link)
        {
            return LinkParser.ParseFileCodeFromDirectLink(link);
        }

        [Test]
        public void Test_ParseFolderHashFromLink()
        {
            var link = "https://uptobox.com/user_public?hash=myhash&folder=myfolder";
            var (folder, hash) = LinkParser.ParseFolderHashFromLink(link);

            Assert.That(folder, Is.EqualTo("myfolder"));
            Assert.That(hash, Is.EqualTo("myhash"));
        }
    }
}