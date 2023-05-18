using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using UptoboxDl.UptoboxClient;

namespace UptoboxDl.Tests
{
    public class UptoboxClientTest
    {
        private const string Hostname = "mocked";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task GetWaitingToken()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"http://{Hostname}/api/link")
                .Respond("application/json",
                    @"{
    ""statusCode"": 16,
    ""message"": ""Waiting needed"",
    ""data"": {
        ""waiting"": 30,
        ""waitingToken"": ""waitingToken""
    }
}");
            var client = new Client("userToken", Hostname, false, mockHttp.ToHttpClient());

            var token = await client.GetWaitingTokenAsync("fileCode");
            Assert.AreEqual("waitingToken", token.Token);
            Assert.AreEqual(30, token.Delay);
        }

        [Test]
        public void GetWaitingTokenFailsOnBadFileCode()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"http://{Hostname}/api/link")
                .Respond("application/json",
                    @"{
    ""statusCode"": 7,
    ""message"": ""Invalid parameter"",
    ""data"": ""bad file code""
}");
            var client = new Client("userToken", Hostname, false, mockHttp.ToHttpClient());

            var ex = Assert.ThrowsAsync<ClientException>(async () => { await client.GetWaitingTokenAsync("fileCode"); });
            Assert.AreEqual(ex.Message, "bad file code");
        }

        [Test]
        public async Task GetDownloadLink()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"http://{Hostname}/api/link")
                .Respond("application/json",
                    @"{
    ""statusCode"": 0,
    ""message"": ""Success"",
    ""data"": {
        ""dlLink"": ""http://dllink/foo.bar"",
    }
}
");
            var client = new Client("userToken", Hostname, false, mockHttp.ToHttpClient());

            var downloadLink = await client.GetDownloadLinkAsync("fileCode", new WaitingToken() { Token = "ok" });
            Assert.AreEqual("http://dllink/foo.bar", downloadLink.ToString());
        }

        [Test]
        public async Task GetFolderFileCodes()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"http://{Hostname}/api/user/public?hash=myhash&folder=myfolder&limit=100&offset=0")
                .Respond("application/json",
                    @"{
    ""statusCode"": 0,
    ""message"": ""Success"",
    ""data"": {
        ""list"": [" + string.Join(",", Enumerable.Range(0, 100).Select(idx => @$"
            {{
                ""file_name"": ""name{idx}"",
                ""file_code"": ""code{idx}""
            }}")) + @"
        ]
    }
}
");
            mockHttp.When($"http://{Hostname}/api/user/public?hash=myhash&folder=myfolder&limit=100&offset=100")
                .Respond("application/json",
                    @"{
    ""statusCode"": 0,
    ""message"": ""Success"",
    ""data"": {
        ""list"": [" + string.Join(",", Enumerable.Range(100, 23).Select(idx => @$"
            {{
                ""file_name"": ""name{idx}"",
                ""file_code"": ""code{idx}""
            }}")) + @"
        ]
    }
}
");
            mockHttp.When($"http://{Hostname}/api/user/public?hash=myhash&folder=myfolder&limit=100&offset=200")
                .Respond("application/json",
                    @"{
    ""statusCode"": 0,
    ""message"": ""Success"",
    ""data"": {
        ""list"": []
    }
}
");
            var client = new Client("userToken", Hostname, false, mockHttp.ToHttpClient());

            var fileCodes = await client.GetFolderFileCodes("myfolder", "myhash");
            var expectedFileCodes = Enumerable.Range(0, 123).Select(idx => $"code{idx}");
            Assert.That(fileCodes, Is.EquivalentTo(expectedFileCodes));
        }
    }
}