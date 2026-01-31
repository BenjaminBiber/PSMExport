namespace PSM_Download.Data.Models;

public sealed class SchadorgInfo
{
    public required string Kode { get; init; }
    public string? Text { get; init; }
    public List<string> Gruppen { get; init; } = [];
}
