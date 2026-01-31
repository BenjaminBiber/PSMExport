using System.Text.Json.Serialization;

namespace PSM_Download.Data.Api;

public sealed class OrdsResponse<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; init; } = [];

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }
}
