using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionIngest.Data;

var builder = Host.CreateApplicationBuilder(args);

var connectionString =
    builder.Configuration.GetSection("ConnectionStrings")["DefaultConnection"]
    ?? "Data Source=transactions.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

using var host = builder.Build();

Console.WriteLine("Transaction ingest application initialized successfully.");