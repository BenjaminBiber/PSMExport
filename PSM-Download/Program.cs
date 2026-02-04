using System.Collections;
using System.Reflection;
using System.Text;
using BenjaminBiber.PSM_Api;
using BenjaminBiber.PSM_Api.Data.Clients;
using BenjaminBiber.PSM_Api.Data.Options;
using BenjaminBiber.PSM_Api.Data.Services;
using Microsoft.Extensions.Configuration;
using PSM_Download.Components;
using PSM_Download.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ExportColumnRegistry>();
builder.Services.AddSingleton<CsvBuilder>();
builder.Services.AddSingleton<ApiCsvBuilder>();
builder.Services.AddScoped<IPsmExportService, PsmExportService>();
builder.Services.AddPsmApiClients(options =>
    builder.Configuration.GetSection("PsmApi").Bind(options));

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
    return Results.File(encoding.GetBytes(csv), "text/csv; charset=utf-8", "psm-export.csv");
});

app.MapGet("/api-export", async (string? method, IPsmApiClient apiClient, ApiCsvBuilder csvBuilder, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(method))
    {
        return Results.BadRequest("Method is required.");
    }

    var methodInfo = typeof(IPsmApiClient).GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
    if (!IsGetAllMethod(methodInfo))
    {
        return Results.BadRequest("Unknown or unsupported method.");
    }

    var task = methodInfo!.Invoke(apiClient, new object?[] { ct }) as Task;
    if (task is null)
    {
        return Results.BadRequest("Invalid method result.");
    }

    await task.ConfigureAwait(false);
    var resultProperty = task.GetType().GetProperty("Result");
    var result = resultProperty?.GetValue(task);
    var csv = csvBuilder.BuildCsv(result as IEnumerable);
    var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    var fileName = $"{ApiCsvBuilder.ToFileName(method)}.csv";

    return Results.File(encoding.GetBytes(csv), "text/csv; charset=utf-8", fileName);
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

static bool IsGetAllMethod(MethodInfo? methodInfo)
{
    if (methodInfo is null)
    {
        return false;
    }

    if (!methodInfo.Name.StartsWith("GetAll", StringComparison.Ordinal) ||
        !methodInfo.Name.EndsWith("Async", StringComparison.Ordinal) ||
        string.Equals(methodInfo.Name, "GetAllAsync", StringComparison.Ordinal))
    {
        return false;
    }

    var parameters = methodInfo.GetParameters();
    return parameters.Length == 1 && parameters[0].ParameterType == typeof(CancellationToken);
}
