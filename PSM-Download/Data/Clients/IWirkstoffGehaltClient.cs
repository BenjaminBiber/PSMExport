using PSM_Download.Data.Dto;

namespace PSM_Download.Data.Clients;

public interface IWirkstoffGehaltClient
{
    Task<IReadOnlyList<WirkstoffGehaltDto>> GetAllAsync(CancellationToken cancellationToken);
}
