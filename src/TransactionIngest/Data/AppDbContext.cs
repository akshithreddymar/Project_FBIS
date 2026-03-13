using Microsoft.EntityFrameworkCore;
using TransactionIngest.Models;

namespace TransactionIngest.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionAudit> TransactionAudits => Set<TransactionAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.HasIndex(t => t.TransactionId).IsUnique();

            entity.Property(t => t.LocationCode)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(t => t.ProductName)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(t => t.CardLast4)
                .HasMaxLength(4)
                .IsRequired();

            entity.Property(t => t.Amount)
                .HasPrecision(18, 2);

            entity.Property(t => t.Status)
                .IsRequired();

            entity.HasMany(t => t.Audits)
                .WithOne(a => a.Transaction)
                .HasForeignKey(a => a.TransactionId)
                .HasPrincipalKey(t => t.TransactionId);
        });

        modelBuilder.Entity<TransactionAudit>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.ActionType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(a => a.ChangedFields)
                .IsRequired();

            entity.Property(a => a.OldValues)
                .IsRequired();

            entity.Property(a => a.NewValues)
                .IsRequired();
        });
    }
}