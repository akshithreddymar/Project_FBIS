using TransactionIngest.DTOs;

namespace TransactionIngest.Services;

public interface ITransactionSnapshotReader
{
    Task<IReadOnlyList<TransactionSnapshotItemDto>> ReadLast24HoursSnapshotAsync(CancellationToken cancellationToken = default);
}