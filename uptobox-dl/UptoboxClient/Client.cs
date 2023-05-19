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

namespace UptoboxDl.UptoboxClient;

public class Client
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
    { AllowTrailingCommas = true };
    private readonly HttpClient _client;
    private readonly string _userToken;
    private readonly TextWriter _debugWriter;
    private readonly Uri _baseAddress;

    /// <summary>
    /// Wrapper around Uptobox API
    /// </summary>
    /// <param name="userToken">Token of the Uptobox account https://docs.uptobox.com/?javascript#how-to-find-my-api-token</param>
    /// <param name="hostname">Uptobox domain, should be uptobox.com</param>
    /// <param name="useHttps">Set to false to use http instead of https</param>
    /// <param name="customHttpClient">Custom HttpClient to use</param>
    /// <param name="debugWriter">Print to given writer debug information</param>
    public Client(string userToken, string hostname = "uptobox.com", bool useHttps = true,
        HttpClient customHttpClient = null, TextWriter debugWriter = null)
    {
        _userToken = userToken;
        _client = customHttpClient ?? new HttpClient();
        _debugWriter = debugWriter;

        var scheme = useHttps ? "https" : "http";
        _baseAddress = new Uri($"{scheme}://{hostname}");
    }

    /// https://docs.uptobox.com/?javascript#get-a-waiting-token
    /// <summary>
    /// Get waiting token for the filecode.
    /// Note: if the resulting <c>WaitingToken.Token</c> is null, it means that the caller must wait <c>WaitingToken.Delay</c>
    /// and then call this method again with the same parameters.
    /// Note: If the resulting <c>WaitingToken.Delay</c> is equal to zero, it means that token is already valid and the
    /// caller must call <c>GetDownloadLink</c>
    /// </summary>
    public async Task<WaitingToken> GetWaitingTokenAsync(string fileCode, string password = null,
        CancellationToken cancellationToken = default)
    {
        var uri = GetUri("link", $"token={_userToken}", $"file_code={fileCode}",
            password == null ? string.Empty : $"password={password}");
        return await GetAsync<WaitingToken>(uri, cancellationToken).ConfigureAwait(false);
    }

    /// https://docs.uptobox.com/?javascript#get-the-download-link
    public async Task<Uri> GetDownloadLinkAsync(string fileCode, WaitingToken waitingToken,
        CancellationToken cancellationToken = default)
    {
        var uri = GetUri("link", $"token={_userToken}", $"file_code={fileCode}",
            $"waitingToken={waitingToken.Token}");
        var data = await GetAsync(uri, cancellationToken).ConfigureAwait(false);

        return new Uri(data.GetProperty("dlLink").GetString());
    }

    /// <summary>
    /// Get the file codes of all the links in a public folder.
    /// The folder and hash params should be extracted from LinkParser.ParseFolderHashFromLink
    /// </summary>
    public async Task<IReadOnlyList<string>> GetFolderFileCodes(string folder, string hash)
    {
        const int limit = 100;
        var fileCodes = new List<string>();

        for (var offset = 0; ; offset += limit)
        {
            var uri = GetUri("user/public", $"folder={folder}", $"hash={hash}", $"limit={limit}", $"offset={offset}");
            var folderResponse = await GetAsync<PublicFolder>(uri);
            if (folderResponse.Items.Length == 0)
            {
                // No more items
                return fileCodes;
            }

            fileCodes.AddRange(folderResponse.Items.Select(item => item.FileCode));
        }
    }

    public async Task<FileCodeInfo> GetFileCodeInfoAsync(string fileCode)
    {
        var uri = GetUri("link/info", $"fileCodes={fileCode}");
        var fileInfos = await GetAsync<RemoteList<FileCodeInfo>>(uri);
        return fileInfos.Items[0];
    }

    private async Task<JsonElement> GetAsync(Uri uri, CancellationToken ct = default)
    {
        DebugWriteLine($"Getting uri: {uri}");
        var response = await _client.GetAsync(uri, ct).ConfigureAwait(false);
        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        return DeserializeJsonElementFromStream(stream);
    }

    private async Task<T> GetAsync<T>(Uri uri, CancellationToken ct = default)
    {
        var elt = await GetAsync(uri, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(elt.GetRawText(), JsonSerializerOptions);
    }

    private JsonElement DeserializeJsonElementFromStream(Stream stream)
    {
        using var sr = new StreamReader(stream);
        var strData = sr.ReadToEnd();
        DebugWriteLine($"Deserialized data: {strData}");
        var data = JsonSerializer.Deserialize<ResponseBase>(strData, JsonSerializerOptions);

        var statusCode = (StatusCode)data.StatusCode;
        if (statusCode != StatusCode.Success && statusCode != StatusCode.Waiting_needed && statusCode != StatusCode.Data_unchanged && statusCode != StatusCode.Permission_granted && statusCode != StatusCode.You_need_to_wait_before_requesting_a_new_download_link)
        {
            DebugWriteLine($"Status code: {statusCode} ({data.StatusCode})");
            throw new ClientException($"Error: {statusCode}");
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
        var builder = new UriBuilder(_baseAddress) { Path = $"api/{path}" };
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

    /// <summary>
    /// Write debug information to <see cref="_debugWriter"/> if it's defined.
    /// Should be replaced with a proper logger, but will do the work for now.
    /// </summary>
    private void DebugWriteLine(string str)
    {
        _debugWriter?.WriteLine(str);
    }
}

internal class ResponseBase
{
    [JsonPropertyName("statusCode")] public int StatusCode { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; }
    [JsonPropertyName("data")] public JsonElement Data { get; set; }
}