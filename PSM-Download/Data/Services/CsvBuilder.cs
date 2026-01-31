using System.Text;
using PSM_Download.Data.Models;

namespace PSM_Download.Data.Services;

public sealed class CsvBuilder(ExportColumnRegistry columnRegistry)
{
    private const char Separator = ';';

    public string BuildCsv(IReadOnlyList<MittelAggregate> data, IReadOnlyList<string> selectedColumnIds)
    {
        var columns = columnRegistry.ResolveColumns(selectedColumnIds);
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(Separator, columns.Select(column => Escape(column.Label))));

        foreach (var mittel in data)
        {
            var row = columns
                .Select(column => Escape(column.ValueSelector(mittel)))
                .ToArray();
            builder.AppendLine(string.Join(Separator, row));
        }

        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var mustQuote = value.Contains(Separator, StringComparison.Ordinal)
                        || value.Contains('"', StringComparison.Ordinal)
                        || value.Contains('\n', StringComparison.Ordinal)
                        || value.Contains('\r', StringComparison.Ordinal);

        var escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return mustQuote ? $"\"{escaped}\"" : escaped;
    }
}
