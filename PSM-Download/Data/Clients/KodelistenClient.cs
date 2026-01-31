using Microsoft.Extensions.Options;
using PSM_Download.Data.Dto;
using PSM_Download.Data.Options;

namespace PSM_Download.Data.Clients;

public sealed class KodelistenClient(HttpClient httpClient, IOptions<PsmApiOptions> options) : IKodelistenClient
{
    public Task<IReadOnlyList<KodelisteFeldnameDto>> GetFeldnameMappingsAsync(CancellationToken cancellationToken)
        => OrdsClient.GetAllAsync<KodelisteFeldnameDto>(httpClient, "kodeliste_feldname", options.Value, cancellationToken);
}
