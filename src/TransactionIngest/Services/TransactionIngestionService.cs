using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TransactionIngest.Data;
using TransactionIngest.Enums;
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

    public async Task<SnapshotProcessResult> ProcessSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshotItems = await _snapshotReader.ReadLast24HoursSnapshotAsync(cancellationToken);

        var snapshotTransactionIds = snapshotItems
            .Select(x => x.TransactionId)
            .ToHashSet();

        var existingTransactions = await _dbContext.Transactions
            .Where(t => snapshotTransactionIds.Contains(t.TransactionId))
            .ToListAsync(cancellationToken);

        var existingByTransactionId = existingTransactions
            .ToDictionary(t => t.TransactionId, t => t);

        var utcNow = DateTime.UtcNow;
        var insertedCount = 0;
        var updatedCount = 0;

        foreach (var item in snapshotItems)
        {
            if (!existingByTransactionId.TryGetValue(item.TransactionId, out var existingTransaction))
            {
                var transaction = new Transaction
                {
                    TransactionId = item.TransactionId,
                    CardLast4 = GetLast4(item.CardNumber),
                    LocationCode = item.LocationCode,
                    ProductName = item.ProductName,
                    Amount = item.Amount,
                    TransactionTimeUtc = item.Timestamp,
                    Status = TransactionStatus.Active,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow,
                    LastSeenInSnapshotUtc = utcNow
                };

                var audit = new TransactionAudit
                {
                    TransactionId = item.TransactionId,
                    ActionType = "Created",
                    ChangedAtUtc = utcNow,
                    ChangedFields = JsonSerializer.Serialize(new[] { "InitialInsert" }),
                    OldValues = "{}",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        item.TransactionId,
                        CardLast4 = transaction.CardLast4,
                        item.LocationCode,
                        item.ProductName,
                        item.Amount,
                        TransactionTimeUtc = item.Timestamp
                    })
                };

                _dbContext.Transactions.Add(transaction);
                _dbContext.TransactionAudits.Add(audit);

                insertedCount++;
                continue;
            }

            var changedFields = new List<string>();
            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            var incomingCardLast4 = GetLast4(item.CardNumber);

            if (existingTransaction.CardLast4 != incomingCardLast4)
            {
                changedFields.Add(nameof(existingTransaction.CardLast4));
                oldValues[nameof(existingTransaction.CardLast4)] = existingTransaction.CardLast4;
                newValues[nameof(existingTransaction.CardLast4)] = incomingCardLast4;
                existingTransaction.CardLast4 = incomingCardLast4;
            }

            if (existingTransaction.LocationCode != item.LocationCode)
            {
                changedFields.Add(nameof(existingTransaction.LocationCode));
                oldValues[nameof(existingTransaction.LocationCode)] = existingTransaction.LocationCode;
                newValues[nameof(existingTransaction.LocationCode)] = item.LocationCode;
                existingTransaction.LocationCode = item.LocationCode;
            }

            if (existingTransaction.ProductName != item.ProductName)
            {
                changedFields.Add(nameof(existingTransaction.ProductName));
                oldValues[nameof(existingTransaction.ProductName)] = existingTransaction.ProductName;
                newValues[nameof(existingTransaction.ProductName)] = item.ProductName;
                existingTransaction.ProductName = item.ProductName;
            }

            if (existingTransaction.Amount != item.Amount)
            {
                changedFields.Add(nameof(existingTransaction.Amount));
                oldValues[nameof(existingTransaction.Amount)] = existingTransaction.Amount;
                newValues[nameof(existingTransaction.Amount)] = item.Amount;
                existingTransaction.Amount = item.Amount;
            }

            if (existingTransaction.TransactionTimeUtc != item.Timestamp)
            {
                changedFields.Add(nameof(existingTransaction.TransactionTimeUtc));
                oldValues[nameof(existingTransaction.TransactionTimeUtc)] = existingTransaction.TransactionTimeUtc;
                newValues[nameof(existingTransaction.TransactionTimeUtc)] = item.Timestamp;
                existingTransaction.TransactionTimeUtc = item.Timestamp;
            }

            existingTransaction.LastSeenInSnapshotUtc = utcNow;

            if (changedFields.Count == 0)
            {
                continue;
            }

            existingTransaction.UpdatedAtUtc = utcNow;

            var updateAudit = new TransactionAudit
            {
                TransactionId = item.TransactionId,
                ActionType = "Updated",
                ChangedAtUtc = utcNow,
                ChangedFields = JsonSerializer.Serialize(changedFields),
                OldValues = JsonSerializer.Serialize(oldValues),
                NewValues = JsonSerializer.Serialize(newValues)
            };

            _dbContext.TransactionAudits.Add(updateAudit);
            updatedCount++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SnapshotProcessResult(insertedCount, updatedCount);
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

public record SnapshotProcessResult(int InsertedCount, int UpdatedCount);