using System.Text.Json.Serialization;

namespace PSM_Download.Data.Dto;

public sealed class AwgDto
{
    [JsonPropertyName("awg_id")]
    public string? AwgId { get; init; }

    [JsonPropertyName("kennr")]
    public string? Kennr { get; init; }
}
