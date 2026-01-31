using Microsoft.Extensions.Options;
using PSM_Download.Data.Dto;
using PSM_Download.Data.Options;

namespace PSM_Download.Data.Clients;

public sealed class AwgSchadorgClient(HttpClient httpClient, IOptions<PsmApiOptions> options) : IAwgSchadorgClient
{
    public Task<IReadOnlyList<AwgSchadorgDto>> GetAllAsync(CancellationToken cancellationToken)
        => OrdsClient.GetAllAsync<AwgSchadorgDto>(httpClient, "awg_schadorg", options.Value, cancellationToken);
}
