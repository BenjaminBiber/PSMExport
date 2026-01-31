using Microsoft.Extensions.Options;
using PSM_Download.Data.Dto;
using PSM_Download.Data.Options;

namespace PSM_Download.Data.Clients;

public sealed class WirkstoffGehaltClient(HttpClient httpClient, IOptions<PsmApiOptions> options) : IWirkstoffGehaltClient
{
    public Task<IReadOnlyList<WirkstoffGehaltDto>> GetAllAsync(CancellationToken cancellationToken)
        => OrdsClient.GetAllAsync<WirkstoffGehaltDto>(httpClient, "wirkstoff_gehalt/", options.Value, cancellationToken);
}
