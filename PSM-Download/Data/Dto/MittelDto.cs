using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSM_Download.Data.Dto;

public sealed class MittelDto
{
    [JsonPropertyName("kennr")]
    public string? Kennr { get; init; }

    [JsonPropertyName("mittelname")]
    public string? Mittelname { get; init; }

    [JsonPropertyName("zul_erstmalig_am")]
    public string? ZulassungVon { get; init; }

    [JsonPropertyName("zul_ende")]
    public string? ZulassungBis { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
