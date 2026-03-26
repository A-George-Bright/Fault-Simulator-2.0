// Data/BankingDbContext.cs
using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Data;

public class BankingDbContext : DbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options)
        : base(options) { }

    public DbSet<Account>     Accounts     => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Account
        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("Accounts");
            e.HasKey(a => a.Id);
            e.Property(a => a.Balance).HasPrecision(18, 2);
            e.HasIndex(a => a.AccountNumber).IsUnique();
        });

        // Transaction
        modelBuilder.Entity<Transaction>(e =>
        {
            e.ToTable("Transactions");
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasPrecision(18, 2);

            e.HasOne(t => t.FromAccount)
             .WithMany(a => a.OutgoingTransactions)
             .HasForeignKey(t => t.FromAccountId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.ToAccount)
             .WithMany(a => a.IncomingTransactions)
             .HasForeignKey(t => t.ToAccountId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
