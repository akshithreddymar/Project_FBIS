using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TransactionIngest.Data;
using TransactionIngest.DTOs;
using TransactionIngest.Enums;
using TransactionIngest.Models;
using TransactionIngest.Services;
using TransactionIngest.Tests.Fakes;
using Xunit;

namespace TransactionIngest.Tests;

public class TransactionIngestionServiceTests
{
    [Fact]
    public async Task ProcessSnapshotAsync_Inserts_New_Transactions()
    {
        var dbContext = CreateDbContext();
        var snapshot = new List<TransactionSnapshotItemDto>
        {
            new()
            {
                TransactionId = 1001,
                CardNumber = "4111111111111111",
                LocationCode = "STO-01",
                ProductName = "Mouse",
                Amount = 19.99m,
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                TransactionId = 1002,
                CardNumber = "4000000000000002",
                LocationCode = "STO-02",
                ProductName = "Cable",
                Amount = 25.00m,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };

        var reader = new FakeTransactionSnapshotReader(snapshot);
        var service = new TransactionIngestionService(dbContext, reader);

        var result = await service.ProcessSnapshotAsync();

        result.InsertedCount.Should().Be(2);
        result.UpdatedCount.Should().Be(0);
        result.RevokedCount.Should().Be(0);

        dbContext.Transactions.Should().HaveCount(2);
        dbContext.TransactionAudits.Should().HaveCount(2);
    }



[Fact]
public async Task ProcessSnapshotAsync_Updates_Existing_Transaction_When_Tracked_Fields_Change()
{
    var dbContext = CreateDbContext();
    var transactionTime = DateTime.UtcNow.AddHours(-2);
    var auditTime = DateTime.UtcNow.AddHours(-2);

    dbContext.Transactions.Add(new Transaction
    {
        TransactionId = 1001,
        CardLast4 = "1111",
        LocationCode = "STO-01",
        ProductName = "Mouse",
        Amount = 19.99m,
        TransactionTimeUtc = transactionTime,
        Status = TransactionStatus.Active,
        CreatedAtUtc = auditTime,
        UpdatedAtUtc = auditTime,
        LastSeenInSnapshotUtc = auditTime
    });

    await dbContext.SaveChangesAsync();

    var snapshot = new List<TransactionSnapshotItemDto>
    {
        new()
        {
            TransactionId = 1001,
            CardNumber = "4111111111111111",
            LocationCode = "STO-01",
            ProductName = "Wireless Mouse",
            Amount = 24.99m,
            Timestamp = transactionTime
        }
    };

    var reader = new FakeTransactionSnapshotReader(snapshot);
    var service = new TransactionIngestionService(dbContext, reader);

    var result = await service.ProcessSnapshotAsync();

    result.InsertedCount.Should().Be(0);
    result.UpdatedCount.Should().Be(1);
    result.RevokedCount.Should().Be(0);

    var updatedTransaction = await dbContext.Transactions.SingleAsync(t => t.TransactionId == 1001);
    updatedTransaction.ProductName.Should().Be("Wireless Mouse");
    updatedTransaction.Amount.Should().Be(24.99m);

    dbContext.TransactionAudits.Count(a => a.ActionType == "Updated").Should().Be(1);
}






[Fact]
public async Task ProcessSnapshotAsync_Revokes_Missing_InWindow_Transaction()
{
    var dbContext = CreateDbContext();
    var transactionTime = DateTime.UtcNow.AddHours(-2);
    var auditTime = DateTime.UtcNow.AddHours(-2);

    dbContext.Transactions.AddRange(
        new Transaction
        {
            TransactionId = 1001,
            CardLast4 = "1111",
            LocationCode = "STO-01",
            ProductName = "Mouse",
            Amount = 19.99m,
            TransactionTimeUtc = transactionTime,
            Status = TransactionStatus.Active,
            CreatedAtUtc = auditTime,
            UpdatedAtUtc = auditTime,
            LastSeenInSnapshotUtc = auditTime
        },
        new Transaction
        {
            TransactionId = 1002,
            CardLast4 = "0002",
            LocationCode = "STO-02",
            ProductName = "Cable",
            Amount = 25.00m,
            TransactionTimeUtc = transactionTime,
            Status = TransactionStatus.Active,
            CreatedAtUtc = auditTime,
            UpdatedAtUtc = auditTime,
            LastSeenInSnapshotUtc = auditTime
        });

    await dbContext.SaveChangesAsync();

    var snapshot = new List<TransactionSnapshotItemDto>
    {
        new()
        {
            TransactionId = 1001,
            CardNumber = "4111111111111111",
            LocationCode = "STO-01",
            ProductName = "Mouse",
            Amount = 19.99m,
            Timestamp = transactionTime
        }
    };

    var reader = new FakeTransactionSnapshotReader(snapshot);
    var service = new TransactionIngestionService(dbContext, reader);

    var result = await service.ProcessSnapshotAsync();

    result.InsertedCount.Should().Be(0);
    result.UpdatedCount.Should().Be(0);
    result.RevokedCount.Should().Be(1);

    var revokedTransaction = await dbContext.Transactions.SingleAsync(t => t.TransactionId == 1002);
    revokedTransaction.Status.Should().Be(TransactionStatus.Revoked);

    dbContext.TransactionAudits.Count(a => a.ActionType == "Revoked").Should().Be(1);
}

// xyz




    [Fact]
    public async Task ProcessSnapshotAsync_Is_Idempotent_For_Unchanged_Input()
    {
        var dbContext = CreateDbContext();

        var snapshot = new List<TransactionSnapshotItemDto>
        {
            new()
            {
                TransactionId = 1001,
                CardNumber = "4111111111111111",
                LocationCode = "STO-01",
                ProductName = "Mouse",
                Amount = 19.99m,
                Timestamp = DateTime.UtcNow.AddHours(-2)
            }
        };

        var reader = new FakeTransactionSnapshotReader(snapshot);
        var service = new TransactionIngestionService(dbContext, reader);

        var firstResult = await service.ProcessSnapshotAsync();
        var secondResult = await service.ProcessSnapshotAsync();

        firstResult.InsertedCount.Should().Be(1);
        firstResult.UpdatedCount.Should().Be(0);
        firstResult.RevokedCount.Should().Be(0);

        secondResult.InsertedCount.Should().Be(0);
        secondResult.UpdatedCount.Should().Be(0);
        secondResult.RevokedCount.Should().Be(0);

        dbContext.Transactions.Should().HaveCount(1);
        dbContext.TransactionAudits.Should().HaveCount(1);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}