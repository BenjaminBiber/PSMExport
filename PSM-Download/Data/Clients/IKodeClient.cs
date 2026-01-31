using PSM_Download.Data.Dto;

namespace PSM_Download.Data.Clients;

public interface IKodeClient
{
    Task<IReadOnlyList<KodeDto>> GetKodeAsync(string kode, CancellationToken cancellationToken);
}
