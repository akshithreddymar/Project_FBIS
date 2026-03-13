using Microsoft.EntityFrameworkCore;
using TransactionIngest.Data;
using TransactionIngest.Models;

namespace TransactionIngest.Services;

public class TransactionIngestionService
{
    private readonly AppDbContext _dbContext;
    private readonly ITransactionSnapshotReader _snapshotReader;

    public TransactionIngestionService(
        AppDbContext dbContext,
        ITransactionSnapshotReader snapshotReader)
    {
        _dbContext = dbContext;
        _snapshotReader = snapshotReader;
    }

    public async Task<int> ProcessSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshotItems = await _snapshotReader.ReadLast24HoursSnapshotAsync(cancellationToken);

        var snapshotTransactionIds = snapshotItems
            .Select(x => x.TransactionId)
            .ToHashSet();

        var existingTransactionIds = await _dbContext.Transactions
            .Where(t => snapshotTransactionIds.Contains(t.TransactionId))
            .Select(t => t.TransactionId)
            .ToListAsync(cancellationToken);

        var existingTransactionIdSet = existingTransactionIds.ToHashSet();

        var utcNow = DateTime.UtcNow;
        var insertedCount = 0;

        foreach (var item in snapshotItems)
        {
            if (existingTransactionIdSet.Contains(item.TransactionId))
            {
                continue;
            }

            var transaction = new Transaction
            {
                TransactionId = item.TransactionId,
                CardLast4 = GetLast4(item.CardNumber),
                LocationCode = item.LocationCode,
                ProductName = item.ProductName,
                Amount = item.Amount,
                TransactionTimeUtc = item.Timestamp,
                Status = Enums.TransactionStatus.Active,
                CreatedAtUtc = utcNow,
                UpdatedAtUtc = utcNow,
                LastSeenInSnapshotUtc = utcNow
            };

            var audit = new TransactionAudit
            {
                TransactionId = item.TransactionId,
                ActionType = "Created",
                ChangedAtUtc = utcNow,
                ChangedFields = "InitialInsert",
                OldValues = "{}",
                NewValues =
                    $"{{\"TransactionId\":{item.TransactionId},\"Amount\":{item.Amount},\"LocationCode\":\"{item.LocationCode}\",\"ProductName\":\"{item.ProductName}\"}}"
            };

            _dbContext.Transactions.Add(transaction);
            _dbContext.TransactionAudits.Add(audit);

            insertedCount++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return insertedCount;
    }

    private static string GetLast4(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return string.Empty;
        }

        return cardNumber.Length <= 4
            ? cardNumber
            : cardNumber[^4..];
    }
}