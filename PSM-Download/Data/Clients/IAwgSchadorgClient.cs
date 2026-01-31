using PSM_Download.Data.Dto;

namespace PSM_Download.Data.Clients;

public interface IAwgSchadorgClient
{
    Task<IReadOnlyList<AwgSchadorgDto>> GetAllAsync(CancellationToken cancellationToken);
}
