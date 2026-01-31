using PSM_Download.Data.Dto;

namespace PSM_Download.Data.Clients;

public interface IAwgClient
{
    Task<IReadOnlyList<AwgDto>> GetAllAsync(CancellationToken cancellationToken);
}
