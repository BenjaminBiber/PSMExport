using Microsoft.Extensions.Options;
using PSM_Download.Data.Dto;
using PSM_Download.Data.Options;

namespace PSM_Download.Data.Clients;

public sealed class AwgClient(HttpClient httpClient, IOptions<PsmApiOptions> options) : IAwgClient
{
    public Task<IReadOnlyList<AwgDto>> GetAllAsync(CancellationToken cancellationToken)
        => OrdsClient.GetAllAsync<AwgDto>(httpClient, "awg", options.Value, cancellationToken);
}
