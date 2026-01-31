using PSM_Download.Data.Models;

namespace PSM_Download.Data.Services;

public sealed record ExportColumn(
    string Id,
    string Label,
    Func<MittelAggregate, string?> ValueSelector);
