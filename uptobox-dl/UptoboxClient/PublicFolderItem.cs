using System.Text.Json.Serialization;

namespace UptoboxDl.UptoboxClient;

public class PublicFolderItem
{
    [JsonPropertyName("file_name")] public string FileName { get; set; }
    [JsonPropertyName("file_code")] public string FileCode { get; set; }
}