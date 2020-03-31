using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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

        private readonly string _fileCode;
        private readonly HttpClient _client;
        private readonly string _userToken;
        private readonly Uri _baseAddress;

        /// <summary>
        /// Wrapper around Uptobox API
        /// </summary>
        /// <param name="fileCode">Filecode of the file to download https://docs.uptobox.com/?javascript#what-is-a-file-code</param>
        /// <param name="userToken">Token of the Uptobox account https://docs.uptobox.com/?javascript#how-to-find-my-api-token</param>
        /// <param name="hostname">Uptobox domain, should be uptobox.com</param>
        /// <param name="useHttps">Set to false to use http instead of https</param>
        /// <param name="customHttpClient">Custom HttpClient to use</param>
        public Client(string fileCode, string userToken, string hostname = "uptobox.com", bool useHttps = true,
            HttpClient customHttpClient = null)
        {
            _userToken = userToken;
            _fileCode = fileCode;
            _client = customHttpClient ?? new HttpClient();

            var scheme = useHttps ? "https" : "http";
            _baseAddress = new Uri($"{scheme}://{hostname}");
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
            var uri = GetUri("link", $"token={_userToken}", $"file_code={_fileCode}",
                password == null ? string.Empty : $"password={password}");
            return await GetAsync<WaitingToken>(uri, cancellationToken).ConfigureAwait(false);
        }

        /// https://docs.uptobox.com/?javascript#get-the-download-link
        public async Task<Uri> GetDownloadLinkAsync(WaitingToken waitingToken,
            CancellationToken cancellationToken = default)
        {
            var uri = GetUri("link", $"token={_userToken}", $"file_code={_fileCode}",
                $"waitingToken={waitingToken.Token}");
            var data = await GetAsync(uri, cancellationToken).ConfigureAwait(false);

            return new Uri(data.GetProperty("dlLink").GetString());
        }

        private async Task<JsonElement> GetAsync(Uri uri, CancellationToken ct = default)
        {
            var response = await _client.GetAsync(uri, ct).ConfigureAwait(false);
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return DeserializeJsonElementFromStream(stream);
        }

        private async Task<T> GetAsync<T>(Uri uri, CancellationToken ct = default)
        {
            var elt = await GetAsync(uri, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(elt.GetRawText(), JsonSerializerOptions);
        }

        private static JsonElement DeserializeJsonElementFromStream(Stream stream)
        {
            using var sr = new StreamReader(stream);
            var strData = sr.ReadToEnd();
            var data = JsonSerializer.Deserialize<ResponseBase>(strData, JsonSerializerOptions);

            if (InvalidStatusCodes.Contains(data.StatusCode))
            {
                throw new ClientException(data.Data.GetString());
            }

            return data.Data;
        }

        /// <summary>
        /// Get Uptobox URI by passing a path and multiple query elements
        /// </summary>
        /// <param name="path">Path without domain part</param>
        /// <param name="queryElements">Elements with "key=value" to be passed as query string</param>
        /// <returns>Built uri with domain prepended and query elements added</returns>
        private Uri GetUri(string path, params string[] queryElements)
        {
            var builder = new UriBuilder(_baseAddress) {Path = $"api/{path}"};
            if (queryElements.Length == 0)
            {
                return builder.Uri;
            }

            var queryBuilder = new StringBuilder();
            queryBuilder.Append($"?{queryElements[0]}");
            for (var i = 1; i < queryElements.Length; i++)
            {
                queryBuilder.Append($"&{queryElements[i]}");
            }

            builder.Query = queryBuilder.ToString();
            return builder.Uri;
        }
    }

    internal class ResponseBase
    {
        [JsonPropertyName("statusCode")] public int StatusCode { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }
        [JsonPropertyName("data")] public JsonElement Data { get; set; }
    }
}