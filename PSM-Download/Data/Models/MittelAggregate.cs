namespace PSM_Download.Data.Models;

public sealed class MittelAggregate
{
    public required string Kennr { get; init; }
    public string? Name { get; init; }
    public DateOnly? ZulassungVon { get; init; }
    public DateOnly? ZulassungBis { get; init; }
    public List<WirkstoffInfo> Wirkstoffe { get; init; } = [];
    public List<SchadorgInfo> Schadorganismen { get; init; } = [];
}
