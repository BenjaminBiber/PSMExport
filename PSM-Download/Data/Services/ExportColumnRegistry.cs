using PSM_Download.Data.Models;

namespace PSM_Download.Data.Services;

public sealed class ExportColumnRegistry
{
    private readonly List<ExportColumn> _columns =
    [
        new ExportColumn("kennr", "Kennr", mittel => mittel.Kennr),
        new ExportColumn("name", "Mittelname", mittel => mittel.Name),
        new ExportColumn("zulassung_von", "Zulassung von", mittel => mittel.ZulassungVon?.ToString("yyyy-MM-dd")),
        new ExportColumn("zulassung_bis", "Zulassung bis", mittel => mittel.ZulassungBis?.ToString("yyyy-MM-dd")),
        new ExportColumn("wirkstoffe", "Wirkstoffe", mittel =>
            string.Join("; ", mittel.Wirkstoffe.Select(w =>
            {
                var details = string.IsNullOrWhiteSpace(w.Gehalt)
                    ? w.Wirkstoff
                    : $"{w.Wirkstoff} {w.Gehalt} {w.Einheit}".Trim();
                return details;
            }))),
        new ExportColumn("schadorganismen", "Schadorganismen", mittel =>
            string.Join("; ", mittel.Schadorganismen.Select(s =>
            {
                if (string.IsNullOrWhiteSpace(s.Text))
                {
                    return s.Kode;
                }

                return $"{s.Kode} ({s.Text})";
            })))
    ];

    public IReadOnlyList<ExportColumn> Columns => _columns;

    public IReadOnlyList<string> DefaultColumnIds => _columns.Select(column => column.Id).ToList();

    public ExportColumn? GetById(string id)
        => _columns.FirstOrDefault(column => string.Equals(column.Id, id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ExportColumn> ResolveColumns(IEnumerable<string> ids)
    {
        var requested = ids
            .Select(id => GetById(id))
            .Where(column => column is not null)
            .Cast<ExportColumn>()
            .ToList();

        return requested.Count == 0 ? _columns : requested;
    }
}
