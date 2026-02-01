using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using PSM_Download.Data.Clients;
using PSM_Download.Data.Dto;
using PSM_Download.Data.Models;

namespace PSM_Download.Data.Services;

public sealed class PsmExportService(
    IMittelClient mittelClient,
    IWirkstoffClient wirkstoffClient,
    IWirkstoffGehaltClient wirkstoffGehaltClient,
    IAwgClient awgClient,
    IAwgSchadorgClient awgSchadorgClient,
    IKodeClient kodeClient,
    IMemoryCache cache,
    CsvBuilder csvBuilder) : IPsmExportService
{
    public async Task<IReadOnlyList<MittelAggregate>> LoadAggregatedAsync(
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(new ExportProgress("Lade Mittel", 0, 0));
        var mittelDtos = await mittelClient.GetAllAsync(cancellationToken);

        progress?.Report(new ExportProgress("Lade Wirkstoffe", 0, 0));
        var wirkstoffDtos = await wirkstoffClient.GetAllAsync(cancellationToken);
        var wirkstoffGehaltDtos = await wirkstoffGehaltClient.GetAllAsync(cancellationToken);

        progress?.Report(new ExportProgress("Lade AWG-Daten", 0, 0));
        var awgDtos = await awgClient.GetAllAsync(cancellationToken);
        var awgSchadorgDtos = await awgSchadorgClient.GetAllAsync(cancellationToken);

        var wirkstoffLookup = wirkstoffDtos
            .Where(dto => !string.IsNullOrWhiteSpace(dto.WirkNr))
            .ToDictionary(dto => dto.WirkNr!, dto => dto.Wirkstoffname ?? dto.WirkNr!, StringComparer.OrdinalIgnoreCase);

        var wirkstoffByKennr = wirkstoffGehaltDtos
            .Where(dto => !string.IsNullOrWhiteSpace(dto.Kennr))
            .GroupBy(dto => dto.Kennr!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => new WirkstoffInfo
                    {
                        Wirkstoff = ResolveWirkstoffName(wirkstoffLookup, item),
                        Gehalt = ResolveWirkstoffGehalt(item),
                        Einheit = item.GehaltEinheit
                    })
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var awgByKennr = awgDtos
            .Where(dto => !string.IsNullOrWhiteSpace(dto.Kennr))
            .GroupBy(dto => dto.Kennr!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var schadorgByAwgId = awgSchadorgDtos
            .Where(dto => !string.IsNullOrWhiteSpace(dto.AwgId))
            .GroupBy(dto => dto.AwgId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var aggregates = new ConcurrentBag<MittelAggregate>();
        using var limiter = new SemaphoreSlim(8);
        var total = mittelDtos.Count(dto => !string.IsNullOrWhiteSpace(dto.Kennr));
        var completed = 0;
        progress?.Report(new ExportProgress("Lade Daten", 0, total));

        var tasks = mittelDtos.Select(async mittel =>
        {
            if (string.IsNullOrWhiteSpace(mittel.Kennr))
            {
                return;
            }

            await limiter.WaitAsync(cancellationToken);
            try
            {
                var kennr = mittel.Kennr.Trim();
                awgByKennr.TryGetValue(kennr, out var awgs);
                var schadorgCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (awgs is not null)
                {
                    foreach (var awg in awgs)
                    {
                        if (string.IsNullOrWhiteSpace(awg.AwgId))
                        {
                            continue;
                        }

                        if (!schadorgByAwgId.TryGetValue(awg.AwgId.Trim(), out var schadorgs))
                        {
                            continue;
                        }

                        foreach (var schadorg in schadorgs)
                        {
                            if (!string.IsNullOrWhiteSpace(schadorg.Schadorg))
                            {
                                schadorgCodes.Add(schadorg.Schadorg.Trim());
                            }
                        }
                    }
                }

                var schadorgInfos = await DecodeSchadorgAsync(schadorgCodes, cancellationToken);
                wirkstoffByKennr.TryGetValue(kennr, out var wirkstoffe);

                aggregates.Add(new MittelAggregate
                {
                    Kennr = kennr,
                    Name = ResolveMittelName(mittel),
                    ZulassungVon = ParseDate(ResolveMittelZulassungVon(mittel)),
                    ZulassungBis = ParseDate(ResolveMittelZulassungBis(mittel)),
                    Wirkstoffe = wirkstoffe ?? [],
                    Schadorganismen = schadorgInfos
                });
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(mittel.Kennr))
                {
                    var current = Interlocked.Increment(ref completed);
                    progress?.Report(new ExportProgress("Lade Daten", current, total));
                }
                limiter.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);

        return aggregates.OrderBy(item => item.Kennr, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<string> BuildCsvAsync(IReadOnlyList<string> selectedColumnIds, CancellationToken cancellationToken)
    {
        var aggregates = await LoadAggregatedAsync(null, cancellationToken);
        return csvBuilder.BuildCsv(aggregates, selectedColumnIds);
    }

    private async Task<List<SchadorgInfo>> DecodeSchadorgAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken)
    {
        var list = new List<SchadorgInfo>();
        foreach (var code in codes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase))
        {
            var text = await GetKodeTextAsync(code, cancellationToken);
            list.Add(new SchadorgInfo
            {
                Kode = code,
                Text = text
            });
        }

        return list;
    }

    private async Task<string?> GetKodeTextAsync(string kode, CancellationToken cancellationToken)
    {
        var cacheKey = $"psm:kode:{kode}";
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            var result = await kodeClient.GetKodeAsync(kode, cancellationToken);
            var german = result.FirstOrDefault(item => string.Equals(item.Sprache, "DE", StringComparison.OrdinalIgnoreCase));
            return german?.KodeText ?? result.FirstOrDefault()?.KodeText;
        });
    }

    private static string ResolveWirkstoffName(
        IReadOnlyDictionary<string, string> lookup,
        WirkstoffGehaltDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.WirkNr) && lookup.TryGetValue(item.WirkNr, out var name))
        {
            return name;
        }

        return item.WirkNr ?? "Unbekannt";
    }

    private static string? ResolveWirkstoffGehalt(WirkstoffGehaltDto item)
    {
        if (item.GehaltReinGrundstruktur.HasValue)
        {
            return item.GehaltReinGrundstruktur.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (item.GehaltRein.HasValue)
        {
            return item.GehaltRein.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        }

        return null;
    }

    private static string? ResolveMittelName(MittelDto mittel)
    {
        if (!string.IsNullOrWhiteSpace(mittel.Mittelname))
        {
            return mittel.Mittelname;
        }

        return GetExtensionValue(mittel, "bezeichnung", "name", "mittel_name", "mittel");
    }

    private static string? ResolveMittelZulassungVon(MittelDto mittel)
        => mittel.ZulassungVon ?? GetExtensionValue(mittel, "zul_von", "zulassung_von_dt", "zulassung_beginn");

    private static string? ResolveMittelZulassungBis(MittelDto mittel)
        => mittel.ZulassungBis ?? GetExtensionValue(mittel, "zul_bis", "zulassung_bis_dt", "zulassung_ende");

    private static string? GetExtensionValue(MittelDto mittel, params string[] keys)
    {
        if (mittel.ExtensionData is null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (mittel.ExtensionData.TryGetValue(key, out var value) && value.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                return value.ToString();
            }
        }

        foreach (var key in keys)
        {
            var match = mittel.ExtensionData.FirstOrDefault(pair =>
                pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(match.Key) && match.Value.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                return match.Value.ToString();
            }
        }

        return null;
    }

    private static DateOnly? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        var formats = new[]
        {
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "dd.MM.yyyy",
            "yyyyMMdd"
        };

        if (DateTime.TryParseExact(trimmed, formats, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var exactParsed))
        {
            return DateOnly.FromDateTime(exactParsed);
        }

        if (DateTime.TryParseExact(trimmed, formats, System.Globalization.CultureInfo.GetCultureInfo("de-DE"),
                System.Globalization.DateTimeStyles.AssumeLocal, out exactParsed))
        {
            return DateOnly.FromDateTime(exactParsed);
        }

        if (DateTime.TryParse(trimmed, out var parsed))
        {
            return DateOnly.FromDateTime(parsed);
        }

        return null;
    }
}
