using System.Text.Json.Serialization;

namespace PSM_Download.Data.Dto;

public sealed class AwgSchadorgDto
{
    [JsonPropertyName("awg_id")]
    public string? AwgId { get; init; }

    [JsonPropertyName("schadorg")]
    public string? Schadorg { get; init; }
}
