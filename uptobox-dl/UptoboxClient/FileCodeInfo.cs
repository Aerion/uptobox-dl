using System.Text.Json.Serialization;

namespace UptoboxDl.UptoboxClient;

public class FileCodeInfo
{
    [JsonPropertyName("file_name")] public string FileName { get; set; }
    [JsonPropertyName("file_code")] public string FileCode { get; set; }
    [JsonPropertyName("file_size")] public long FileSize { get; set; }
}
