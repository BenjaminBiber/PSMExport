using PSM_Download.Data.Dto;

namespace PSM_Download.Data.Clients;

public interface IMittelClient
{
    Task<IReadOnlyList<MittelDto>> GetAllAsync(CancellationToken cancellationToken);
}
