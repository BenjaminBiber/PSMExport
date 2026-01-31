using Microsoft.Extensions.Options;
using PSM_Download.Data.Dto;
using PSM_Download.Data.Options;

namespace PSM_Download.Data.Clients;

public sealed class WirkstoffClient(HttpClient httpClient, IOptions<PsmApiOptions> options) : IWirkstoffClient
{
    public Task<IReadOnlyList<WirkstoffDto>> GetAllAsync(CancellationToken cancellationToken)
        => OrdsClient.GetAllAsync<WirkstoffDto>(httpClient, "wirkstoff/", options.Value, cancellationToken);
}
