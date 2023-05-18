using System.Text.Json.Serialization;

namespace UptoboxDl.UptoboxClient;

public class WaitingToken
{
    [JsonPropertyName("waiting")] public int Delay { get; set; }
    [JsonPropertyName("waitingToken")] public string Token { get; set; }
}