// Models/Dtos.cs
namespace BankingApi.Models;

// ── Requests ──────────────────────────────────────────────────────────────

public record LoginRequest(
    string AccountNumber,
    string Password);

public record TransferRequest(
    string  FromAccountNumber,
    string  ToAccountNumber,
    decimal Amount);

// ── Responses ─────────────────────────────────────────────────────────────

public record AccountDto(
    int     Id,
    string  AccountNumber,
    string  OwnerName,
    decimal Balance);

public record TransactionDto(
    int      Id,
    string?  FromAccount,
    string?  ToAccount,
    decimal  Amount,
    string   TransactionType,
    string   Status,
    string?  Notes,
    DateTime CreatedAt);

public record LoginResponse(
    bool       Success,
    string     Message,
    AccountDto? Account);

public record TransferResponse(
    bool   Success,
    string Message,
    string LogLevel);

public record ApiError(string Message);
