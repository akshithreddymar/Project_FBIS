using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TransactionIngest.DTOs;

namespace TransactionIngest.Services;

public class MockTransactionSnapshotReader : ITransactionSnapshotReader
{
    private readonly IConfiguration _configuration;

    public MockTransactionSnapshotReader(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<TransactionSnapshotItemDto>> ReadLast24HoursSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var mockFeedPath = _configuration["Api:MockFeedPath"];

        if (string.IsNullOrWhiteSpace(mockFeedPath))
        {
            throw new InvalidOperationException("Mock feed path is not configured.");
        }

        var fullPath = Path.Combine(AppContext.BaseDirectory, mockFeedPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Mock transactions file not found at path: {fullPath}");
        }

        await using var stream = File.OpenRead(fullPath);

        var items = await JsonSerializer.DeserializeAsync<List<TransactionSnapshotItemDto>>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            },
            cancellationToken);

        return items ?? new List<TransactionSnapshotItemDto>();
    }
}