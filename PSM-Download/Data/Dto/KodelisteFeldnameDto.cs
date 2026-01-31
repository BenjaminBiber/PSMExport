using System.Text.Json.Serialization;

namespace PSM_Download.Data.Dto;

public sealed class KodelisteFeldnameDto
{
    [JsonPropertyName("tabellenname")]
    public string? Tabellenname { get; init; }

    [JsonPropertyName("feldname")]
    public string? Feldname { get; init; }

    [JsonPropertyName("kodeliste")]
    public int? Kodeliste { get; init; }
}
