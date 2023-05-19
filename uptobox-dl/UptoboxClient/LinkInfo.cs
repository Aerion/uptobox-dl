using System.Text.Json.Serialization;

namespace UptoboxDl.UptoboxClient;

public class RemoteList<T>
{
    [JsonPropertyName("list")] public T[] Items { get; set; }
}
