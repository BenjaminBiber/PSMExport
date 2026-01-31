using PSM_Download.Data.Dto;

namespace PSM_Download.Data.Clients;

public interface IKodelistenClient
{
    Task<IReadOnlyList<KodelisteFeldnameDto>> GetFeldnameMappingsAsync(CancellationToken cancellationToken);
}
