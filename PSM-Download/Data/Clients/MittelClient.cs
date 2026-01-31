using Microsoft.Extensions.Options;
using PSM_Download.Data.Dto;
using PSM_Download.Data.Options;

namespace PSM_Download.Data.Clients;

public sealed class MittelClient(HttpClient httpClient, IOptions<PsmApiOptions> options) : IMittelClient
{
    public Task<IReadOnlyList<MittelDto>> GetAllAsync(CancellationToken cancellationToken)
        => OrdsClient.GetAllAsync<MittelDto>(httpClient, "mittel/", options.Value, cancellationToken);
}
