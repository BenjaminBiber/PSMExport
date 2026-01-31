using System.Text;
using Microsoft.Extensions.Options;
using PSM_Download.Components;
using PSM_Download.Data.Clients;
using PSM_Download.Data.Options;
using PSM_Download.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<PsmApiOptions>(builder.Configuration.GetSection("PsmApi"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ExportColumnRegistry>();
builder.Services.AddSingleton<CsvBuilder>();
builder.Services.AddScoped<IPsmExportService, PsmExportService>();

builder.Services.AddHttpClient<IMittelClient, MittelClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PsmApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddHttpClient<IWirkstoffClient, WirkstoffClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PsmApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddHttpClient<IWirkstoffGehaltClient, WirkstoffGehaltClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PsmApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddHttpClient<IAwgClient, AwgClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PsmApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddHttpClient<IAwgSchadorgClient, AwgSchadorgClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PsmApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddHttpClient<IKodeClient, KodeClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PsmApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddHttpClient<IKodelistenClient, KodelistenClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PsmApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapGet("/export", async (string? columns, IPsmExportService exportService, ExportColumnRegistry registry, CancellationToken ct) =>
{
    var columnIds = ParseColumnSelection(columns, registry.DefaultColumnIds);
    var csv = await exportService.BuildCsvAsync(columnIds, ct);
    var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    return Results.File(encoding.GetBytes(csv), "text/csv", "psm-export.csv");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static IReadOnlyList<string> ParseColumnSelection(string? raw, IReadOnlyList<string> defaultColumns)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return defaultColumns;
    }

    return raw
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}
