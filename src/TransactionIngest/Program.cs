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

using var host = builder.Build();

using var scope = host.Services.CreateScope();
var reader = scope.ServiceProvider.GetRequiredService<ITransactionSnapshotReader>();

var snapshot = await reader.ReadLast24HoursSnapshotAsync();

Console.WriteLine($"Loaded {snapshot.Count} transactions from mock snapshot.");