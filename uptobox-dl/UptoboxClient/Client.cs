using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace UptoboxDl.UptoboxClient
{
    public class Client
    {
        private static readonly HashSet<int> InvalidStatusCodes = new HashSet<int>() {7};

        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
            {AllowTrailingCommas = true};

        private string _fileCode { get; }
        private HttpClient _client;
        private readonly string _userToken;

        /// <summary>
        /// Wrapper around Uptobox API
        /// </summary>
        /// <param name="fileCode">Filecode of the file to download https://docs.uptobox.com/?javascript#what-is-a-file-code</param>
        /// <param name="userToken">Token of the Uptobox account https://docs.uptobox.com/?javascript#how-to-find-my-api-token</param>
        /// <param name="hostname">Uptobox domain, should be uptobox.com</param>
        /// <param name="useHttps">Set to false to use http instead of https</param>
        /// <param name="customHttpClient">Specify a custom httpclient for requests to be handled</param>
        public Client(string fileCode, string userToken, string hostname = "uptobox.com", bool useHttps = true,
            HttpClient customHttpClient = null)
        {
            _userToken = userToken;
            _fileCode = fileCode;
            _client = customHttpClient ?? new HttpClient();
            var scheme = useHttps ? "https" : "http";
            _client.BaseAddress = new Uri($"{scheme}://{hostname}/api/");
        }

        /// https://docs.uptobox.com/?javascript#get-a-waiting-token
        public Task<WaitingToken> GetWaitingTokenAsync(CancellationToken cancellationToken = default)
        {
            return GetWaitingTokenAsync(null, cancellationToken);
        }

        /// https://docs.uptobox.com/?javascript#get-a-waiting-token
        public async Task<WaitingToken> GetWaitingTokenAsync(string password,
            CancellationToken cancellationToken = default)
        {
            var url = $"link?token={_userToken}&file_code={_fileCode}";
            if (password != null)
            {
                url += $"&password={password}";
            }

            return await GetAsync<WaitingToken>(url, cancellationToken).ConfigureAwait(false);
        }

        /// https://docs.uptobox.com/?javascript#get-the-download-link
        public async Task<Uri> GetDownloadLinkAsync(WaitingToken waitingToken,
            CancellationToken cancellationToken = default)
        {
            var url = $"link?token={_userToken}&file_code={_fileCode}&waitingToken={waitingToken.Token}";
            var data = await GetAsync(url, cancellationToken).ConfigureAwait(false);

            return new Uri(data.GetProperty("dlLink").GetString(), UriKind.Absolute);
        }

        private async Task<JsonElement> GetAsync(string url, CancellationToken ct = default, bool retryOnTimeout = true)
        {
            try
            {
                var response = await _client.GetAsync(url, ct).ConfigureAwait(false);
                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                return DeserializeJsonElementFromStream(stream);
            }
            catch (TimeoutException)
            {
                // Retry once on timeout error
                return await GetAsync(url, ct, false);
            }
        }

        private async Task<T> GetAsync<T>(string url, CancellationToken ct = default)
        {
            var elt = await GetAsync(url, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(elt.GetRawText(), JsonSerializerOptions);
        }

        private static JsonElement DeserializeJsonElementFromStream(Stream stream)
        {
            using var sr = new StreamReader(stream);
            var data = JsonSerializer.Deserialize<ResponseBase>(sr.ReadToEnd(), JsonSerializerOptions);

            if (InvalidStatusCodes.Contains(data.StatusCode))
            {
                throw new ClientException(data.Data.GetString());
            }

            return data.Data;
        }
    }

    internal class ResponseBase
    {
        [JsonPropertyName("statusCode")] public int StatusCode { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }
        [JsonPropertyName("data")] public JsonElement Data { get; set; }
    }
}