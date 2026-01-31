using System.Text.Json.Serialization;

namespace PSM_Download.Data.Dto;

public sealed class WirkstoffGehaltDto
{
    [JsonPropertyName("kennr")]
    public string? Kennr { get; init; }

    [JsonPropertyName("wirknr")]
    public string? WirkNr { get; init; }

    [JsonPropertyName("gehalt_rein")]
    public decimal? GehaltRein { get; init; }

    [JsonPropertyName("gehalt_rein_grundstruktur")]
    public decimal? GehaltReinGrundstruktur { get; init; }

    [JsonPropertyName("gehalt_einheit")]
    public string? GehaltEinheit { get; init; }
}
