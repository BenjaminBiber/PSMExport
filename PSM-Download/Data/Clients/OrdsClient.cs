using System.Net.Http.Json;
using System.Text.Json;
using PSM_Download.Data.Api;
using PSM_Download.Data.Options;

namespace PSM_Download.Data.Clients;

public static class OrdsClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<IReadOnlyList<T>> GetAllAsync<T>(
        HttpClient client,
        string relativeUrl,
        PsmApiOptions options,
        CancellationToken cancellationToken)
    {
        var results = new List<T>();
        var offset = 0;
        var limit = options.PageSize <= 0 ? 1000 : options.PageSize;

        while (true)
        {
            var separator = relativeUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            var pageUrl = $"{relativeUrl}{separator}offset={offset}&limit={limit}";
            var response = await client.GetFromJsonAsync<OrdsResponse<T>>(pageUrl, SerializerOptions, cancellationToken);
            if (response is null)
            {
                break;
            }

            results.AddRange(response.Items);
            if (!response.HasMore || response.Items.Count == 0)
            {
                break;
            }

            offset = response.Offset + response.Limit;
        }

        return results;
    }
}
