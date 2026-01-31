using System.Text.Json.Serialization;

namespace PSM_Download.Data.Dto;

public sealed class KodeDto
{
    [JsonPropertyName("kode")]
    public string? Kode { get; init; }

    [JsonPropertyName("sprache")]
    public string? Sprache { get; init; }

    [JsonPropertyName("kodetext")]
    public string? KodeText { get; init; }
}
