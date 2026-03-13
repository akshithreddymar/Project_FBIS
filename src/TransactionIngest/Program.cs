using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionIngest.Data;
using TransactionIngest.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

var connectionString =
    builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? "Data Source=transactions.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<ITransactionSnapshotReader, MockTransactionSnapshotReader>();
builder.Services.AddScoped<TransactionIngestionService>();

using var host = builder.Build();

using var scope = host.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.MigrateAsync();

var ingestionService = scope.ServiceProvider.GetRequiredService<TransactionIngestionService>();
var insertedCount = await ingestionService.ProcessSnapshotAsync();

Console.WriteLine($"Inserted {insertedCount} new transactions.");