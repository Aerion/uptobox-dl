using System.Text.Json.Serialization;

namespace UptoboxDl.UptoboxClient;

public class PublicFolder
{
    [JsonPropertyName("list")] public PublicFolderItem[] Items { get; set; }
}
