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
            var client = new Client("fileCode", "userToken", Hostname, false, mockHttp.ToHttpClient());

            var token = await client.GetWaitingTokenAsync();
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
            var client = new Client("fileCode", "userToken", Hostname, false, mockHttp.ToHttpClient());

            var ex = Assert.ThrowsAsync<ClientException>(async () => { await client.GetWaitingTokenAsync(); });
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
            var client = new Client("fileCode", "userToken", Hostname, false, mockHttp.ToHttpClient());

            var downloadLink = await client.GetDownloadLinkAsync(new WaitingToken() {Token = "ok"});
            Assert.AreEqual("http://dllink/foo.bar", downloadLink.ToString());
        }
    }
}