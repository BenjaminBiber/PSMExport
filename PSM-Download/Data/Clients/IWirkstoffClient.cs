using PSM_Download.Data.Dto;

namespace PSM_Download.Data.Clients;

public interface IWirkstoffClient
{
    Task<IReadOnlyList<WirkstoffDto>> GetAllAsync(CancellationToken cancellationToken);
}
