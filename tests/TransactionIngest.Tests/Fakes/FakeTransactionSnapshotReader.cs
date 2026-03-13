using TransactionIngest.DTOs;
using TransactionIngest.Services;

namespace TransactionIngest.Tests.Fakes;

public class FakeTransactionSnapshotReader : ITransactionSnapshotReader
{
    private readonly IReadOnlyList<TransactionSnapshotItemDto> _items;

    public FakeTransactionSnapshotReader(IReadOnlyList<TransactionSnapshotItemDto> items)
    {
        _items = items;
    }

    public Task<IReadOnlyList<TransactionSnapshotItemDto>> ReadLast24HoursSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items);
    }
}