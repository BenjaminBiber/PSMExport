using System.Text.Json.Serialization;

namespace PSM_Download.Data.Dto;

public sealed class WirkstoffDto
{
    [JsonPropertyName("wirknr")]
    public string? WirkNr { get; init; }

    [JsonPropertyName("wirkstoffname")]
    public string? Wirkstoffname { get; init; }
}
