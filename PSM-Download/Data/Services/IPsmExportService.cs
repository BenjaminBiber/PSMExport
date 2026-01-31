using PSM_Download.Data.Models;

namespace PSM_Download.Data.Services;

public interface IPsmExportService
{
    Task<IReadOnlyList<MittelAggregate>> LoadAggregatedAsync(
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken);
    Task<string> BuildCsvAsync(IReadOnlyList<string> selectedColumnIds, CancellationToken cancellationToken);
}

public sealed record ExportProgress(int Completed, int Total);
