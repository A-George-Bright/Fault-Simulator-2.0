// Models/Account.cs
namespace BankingApi.Models;

public class Account
{
    public int      Id            { get; set; }
    public string   AccountNumber { get; set; } = string.Empty;
    public string   OwnerName     { get; set; } = string.Empty;
    public decimal  Balance       { get; set; }
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> OutgoingTransactions { get; set; } = [];
    public ICollection<Transaction> IncomingTransactions { get; set; } = [];
}
