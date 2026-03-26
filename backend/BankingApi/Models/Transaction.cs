// Models/Transaction.cs
namespace BankingApi.Models;

public class Transaction
{
    public int      Id              { get; set; }
    public int?     FromAccountId   { get; set; }
    public int?     ToAccountId     { get; set; }
    public decimal  Amount          { get; set; }
    public string   TransactionType { get; set; } = string.Empty;
    public string   Status          { get; set; } = string.Empty;
    public string?  Notes           { get; set; }
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;

    public Account? FromAccount { get; set; }
    public Account? ToAccount   { get; set; }
}
