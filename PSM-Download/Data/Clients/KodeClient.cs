using System.Net;
using Microsoft.Extensions.Options;
using PSM_Download.Data.Dto;
using PSM_Download.Data.Options;

namespace PSM_Download.Data.Clients;

public sealed class KodeClient(HttpClient httpClient, IOptions<PsmApiOptions> options) : IKodeClient
{
    public Task<IReadOnlyList<KodeDto>> GetKodeAsync(string kode, CancellationToken cancellationToken)
    {
        var encodedKode = WebUtility.UrlEncode(kode);
        return OrdsClient.GetAllAsync<KodeDto>(
            httpClient,
            $"kode?kode={encodedKode}",
            options.Value,
            cancellationToken);
    }
}
