// Controllers/BankingController.cs
// =============================================================
//  Endpoint map
//  ─────────────────────────────────────────────────────────────
//  GET  /api/banking/accounts                → INFO
//  GET  /api/banking/accounts/{number}       → INFO | WARNING
//  POST /api/banking/login                   → INFO | WARNING
//  POST /api/banking/transfer                → INFO | WARNING
//  GET  /api/banking/transactions/{number}   → INFO
//  GET  /api/banking/chaos/null-reference    → ERROR
//  GET  /api/banking/chaos/db-timeout        → ERROR
//  GET  /api/banking/chaos/unhandled         → ERROR (bubbles up)
// =============================================================
using BankingApi.Data;
using BankingApi.Logging;
using BankingApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Controllers;

[ApiController]
[Route("api/banking")]
public class BankingController : ControllerBase
{
    private const string ClassName = nameof(BankingController);

    private readonly BankingDbContext _db;

    public BankingController(BankingDbContext db) => _db = db;

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/accounts
    //  Returns all accounts.  Logs → INFO
    // ─────────────────────────────────────────────────────────
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _db.Accounts
            .Select(a => new AccountDto(a.Id, a.AccountNumber, a.OwnerName, a.Balance))
            .ToListAsync();

        BankingLogger.Info(ClassName,
            $"All accounts retrieved successfully. Count={accounts.Count}.");

        return Ok(accounts);
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/accounts/{accountNumber}
    //  Returns one account.  Logs → INFO | WARNING
    // ─────────────────────────────────────────────────────────
    [HttpGet("accounts/{accountNumber}")]
    public async Task<IActionResult> GetAccount(string accountNumber)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account is null)
        {
            BankingLogger.Warning(ClassName,
                $"Balance check failed. AccountNumber='{accountNumber}' does not exist in the database.");

            return NotFound(new ApiError($"Account '{accountNumber}' not found."));
        }

        BankingLogger.Info(ClassName,
            $"Balance check successful. AccountNumber='{account.AccountNumber}', " +
            $"Owner='{account.OwnerName}', Balance={account.Balance:C}.");

        return Ok(new AccountDto(account.Id, account.AccountNumber, account.OwnerName, account.Balance));
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/transactions/{accountNumber}
    //  Returns last 20 transactions for an account. Logs → INFO
    // ─────────────────────────────────────────────────────────
    [HttpGet("transactions/{accountNumber}")]
    public async Task<IActionResult> GetTransactions(string accountNumber)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account is null)
        {
            BankingLogger.Warning(ClassName,
                $"Transaction history requested for unknown account '{accountNumber}'.");
            return NotFound(new ApiError($"Account '{accountNumber}' not found."));
        }

        var txns = await _db.Transactions
            .Where(t => t.FromAccountId == account.Id || t.ToAccountId == account.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .Select(t => new TransactionDto(
                t.Id,
                t.FromAccount != null ? t.FromAccount.AccountNumber : null,
                t.ToAccount != null ? t.ToAccount.AccountNumber : null,
                t.Amount,
                t.TransactionType,
                t.Status,
                t.Notes,
                t.CreatedAt))
            .ToListAsync();

        BankingLogger.Info(ClassName,
            $"Transaction history retrieved. AccountNumber='{accountNumber}', Records={txns.Count}.");

        return Ok(txns);
    }

    // ─────────────────────────────────────────────────────────
    //  POST /api/banking/login
    //  Demo password for every account: password123
    //  Logs → INFO (success) | WARNING (bad credentials)
    // ─────────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        const string DemoPassword = "password123";

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);

        if (account is null)
        {
            BankingLogger.Warning(ClassName,
                $"Login failed. AccountNumber='{request.AccountNumber}' not found. " +
                "Possible invalid account or brute-force attempt.");

            return Unauthorized(new LoginResponse(false, "Invalid credentials.", null));
        }

        if (request.Password != DemoPassword)
        {
            BankingLogger.Warning(ClassName,
                $"Login failed. AccountNumber='{request.AccountNumber}' provided an incorrect password. " +
                "Failed attempt has been recorded.");

            return Unauthorized(new LoginResponse(false, "Invalid credentials.", null));
        }

        BankingLogger.Info(ClassName,
            $"Login successful. AccountNumber='{account.AccountNumber}', " +
            $"Owner='{account.OwnerName}', Timestamp={DateTime.UtcNow:O}.");

        return Ok(new LoginResponse(
            true,
            "Login successful.",
            new AccountDto(account.Id, account.AccountNumber, account.OwnerName, account.Balance)));
    }

    // ─────────────────────────────────────────────────────────
    //  POST /api/banking/transfer
    //  Logs → INFO (success) | WARNING (insufficient funds)
    //       → ERROR (unexpected DB/system failure)
    // ─────────────────────────────────────────────────────────
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        if (request.Amount <= 0)
        {
            BankingLogger.Warning(ClassName,
                $"Transfer rejected. Invalid amount={request.Amount}. " +
                $"FromAccount='{request.FromAccountNumber}'.");

            return BadRequest(new TransferResponse(false, "Amount must be greater than zero.", "Warning"));
        }

        var from = await _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == request.FromAccountNumber);

        var to = await _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccountNumber);

        if (from is null || to is null)
        {
            BankingLogger.Warning(ClassName,
                $"Transfer rejected. One or both accounts not found. " +
                $"From='{request.FromAccountNumber}', To='{request.ToAccountNumber}'.");

            return NotFound(new TransferResponse(false, "One or both accounts not found.", "Warning"));
        }

        // ── INSUFFICIENT FUNDS → WARNING ──────────────────────────────────
        if (from.Balance < request.Amount)
        {
            BankingLogger.Warning(ClassName,
                $"Insufficient funds. AccountNumber='{from.AccountNumber}', " +
                $"Owner='{from.OwnerName}', " +
                $"AvailableBalance={from.Balance:C}, " +
                $"RequestedAmount={request.Amount:C}. Transfer blocked.");

            await _db.Transactions.AddAsync(new Transaction
            {
                FromAccountId = from.Id,
                ToAccountId = to.Id,
                Amount = request.Amount,
                TransactionType = "TRANSFER",
                Status = "WARNING",
                Notes = $"Insufficient funds. Available={from.Balance:C}, Requested={request.Amount:C}"
            });
            await _db.SaveChangesAsync();

            return Ok(new TransferResponse(false, "Insufficient funds.", "Warning"));
        }

        // ── SUCCESS → INFO ────────────────────────────────────────────────
        from.Balance -= request.Amount;
        to.Balance += request.Amount;

        await _db.Transactions.AddAsync(new Transaction
        {
            FromAccountId = from.Id,
            ToAccountId = to.Id,
            Amount = request.Amount,
            TransactionType = "TRANSFER",
            Status = "SUCCESS",
            Notes = $"Transfer of {request.Amount:C} from {from.AccountNumber} to {to.AccountNumber}."
        });

        await _db.SaveChangesAsync();

        BankingLogger.Info(ClassName,
            $"Transfer successful. " +
            $"From='{from.AccountNumber}' ({from.OwnerName}), " +
            $"To='{to.AccountNumber}' ({to.OwnerName}), " +
            $"Amount={request.Amount:C}, " +
            $"NewFromBalance={from.Balance:C}.");

        return Ok(new TransferResponse(true, $"Transfer of {request.Amount:C} completed.", "Info"));
    }

    // =============================================================
    //  CHAOS ENDPOINTS – Intentional fault generators
    // =============================================================

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/null-reference
    //  Forces a NullReferenceException.  Logs → ERROR
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/null-reference")]
    public IActionResult ChaosNullReference()
    {
        try
        {
            string? payload = null;
            _ = payload!.Length;   // deliberate NullReferenceException
            return Ok();
        }
        catch (NullReferenceException ex)
        {
            BankingLogger.Error(ClassName,
                "CHAOS: NullReferenceException in payment processing pipeline. " +
                "The payment object was null – possible race condition or missing initialisation. " +
                $"Source: {ex.Source}",
                ex);

            return StatusCode(500, new ApiError("Internal server error (null reference)."));
        }
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/db-timeout
    //  Simulates a MySQL connection timeout.  Logs → ERROR
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/db-timeout")]
    public IActionResult ChaosDbTimeout()
    {
        try
        {
            throw new TimeoutException(
                "A connection attempt failed because the connected MySQL host did not " +
                "respond after 30 000 ms.  Connection pool exhausted. " +
                "Server=localhost;Port=3306;Database=FaultyBankingDB");
        }
        catch (TimeoutException ex)
        {
            BankingLogger.Error(ClassName,
                "CHAOS: MySQL connection timeout. Server is unresponsive or the connection " +
                $"pool is exhausted. Transaction rolled back automatically. Detail: {ex.Message}",
                ex);

            return StatusCode(503, new ApiError("Database unavailable – connection timeout."));
        }
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/unhandled
    //  Lets an exception bubble up uncaught → ASP.NET 500.
    //  Serilog captures it automatically.  Logs → ERROR
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/unhandled")]
    public IActionResult ChaosUnhandled()
    {
        BankingLogger.Error(ClassName,
            "CHAOS: About to throw an unhandled InvalidOperationException. " +
            "This simulates a catastrophic application fault with no recovery path.");

        throw new InvalidOperationException(
            "CHAOS_FAULT: Critical ledger invariant violated – " +
            "debit and credit totals are inconsistent. Manual reconciliation required.");
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/slow-transaction
    //  Simulates a slow transaction/trace. Logs → WARNING
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/slow-transaction")]
    public async Task<IActionResult> ChaosSlowTransaction()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(3500); // Simulate blocking/slow execution
        watch.Stop();

        BankingLogger.Warning(ClassName,
            $"Transaction Trace: Slow controller execution detected. " +
            $"TransactionName='ProcessPayment', ExecutionTime={watch.ElapsedMilliseconds}ms, " +
            $"Threshold=2000ms, InefficientCodeLoop=true.");

        return Ok(new ApiError($"Transaction processed slowly ({watch.ElapsedMilliseconds}ms)."));
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/slow-query
    //  Simulates a database bottleneck. Logs → WARNING
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/slow-query")]
    public async Task<IActionResult> ChaosSlowQuery()
    {
        // Forces the database to literally hang for 4 seconds.
        // Site24x7 APM will intercept this and put it in the "Database Operations" tab.
        await _db.Database.ExecuteSqlRawAsync("SELECT SLEEP(4);");

        return Ok(new ApiError("Real slow database query executed."));
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/external-api
    //  Simulates a failed third-party integration. Logs → ERROR
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/external-api")]
    public async Task<IActionResult> ChaosExternalApi()
    {
        using var client = new HttpClient();

        // httpstat.us is a free service for testing HTTP responses. 
        // This simulates a 3rd party API taking 5 seconds and returning a 504 Gateway Timeout.
        // Site24x7 will log this in the "External APIs" tab.
        var response = await client.GetAsync("https://httpstat.us/504?sleep=5000");

        return StatusCode((int)response.StatusCode, new ApiError("Real external API failure triggered."));
    }
    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/runtime-metrics
    //  Simulates JVM/CLR runtime metrics (Memory/CPU). Logs → INFO
    // ─────────────────────────────────────────────────────────
    // Add this static list to the top of your controller class to hold memory globally
    private static readonly List<byte[]> _memoryLeak = new List<byte[]>();

    [HttpGet("chaos/runtime-metrics")]
    public IActionResult ChaosRuntimeMetrics()
    {
        // Actually allocates ~50MB of memory into the heap that the Garbage Collector cannot clean up.
        // Click this a few times to see the Memory graph spike in Site24x7 Infrastructure.
        _memoryLeak.Add(new byte[1024 * 1024 * 50]);

        var memoryMb = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
        return Ok(new ApiError($"Real memory allocated. Current process memory: {memoryMb} MB"));
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/deep-stack
    //  Simulates a complex stack trace for debugging. Logs → ERROR
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/deep-stack")]
    public IActionResult ChaosDeepStack()
    {
        try
        {
            TriggerLevel1();
            return Ok(); // Will never hit
        }
        catch (Exception ex)
        {
            BankingLogger.Error(ClassName,
                $"Code-level Bug / Dependency Failure. ExceptionType='{ex.GetType().Name}', " +
                $"Message='{ex.Message}', ClassName='PaymentProcessor', MethodName='ExecuteTransfer'.", ex);

            return StatusCode(500, new ApiError("Internal server error due to dependency failure."));
        }
    }

    // Helper methods to build a deep stack trace
    private void TriggerLevel1() => TriggerLevel2();
    private void TriggerLevel2() => TriggerLevel3();
    private void TriggerLevel3() => throw new DllNotFoundException("Library incompatibility: 'lib-crypto-v2.dll' not found.");

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/throughput
    //  Simulates load balancing issues/traffic spikes. Logs → WARNING
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/throughput")]
    public IActionResult ChaosThroughput()
    {
        BankingLogger.Warning(ClassName,
            "Throughput Alert: Traffic spike detected causing request drops. " +
            "RequestsPerMinute=12500, ErrorRate=14.5%, AvgResponseTime=1250ms, " +
            "PeakLoad=true, SuccessfulTransactions=10687.");

        return Ok(new ApiError("Throughput warning logged."));
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/business-event
    //  Simulates Custom Logs/Business KPIs. Logs → INFO
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/business-event")]
    public IActionResult ChaosBusinessEvent()
    {
        BankingLogger.Info(ClassName,
            $"Business KPI Tracked. EventName='HighValueTransfer_Flagged', " +
            $"UserID='USR-88219', TransactionID='TX-{Guid.NewGuid().ToString().Substring(0, 8)}', " +
            $"EventTime='{DateTime.UtcNow:O}', Status='RequiresManualReview'.");

        return Ok(new ApiError("Custom business event logged."));
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/http-request
    //  Matches "HTTP Requests" table row. Logs → WARNING / INFO
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/http-request")]
    public IActionResult ChaosHttpRequest()
    {
        // Simulating an IIS/Kestrel HTTP request log for a 404
        BankingLogger.Warning(ClassName,
            "HTTP Request Log: Endpoint not found. " +
            "HTTPMethod='POST', UrlEndpoint='/api/banking/legacy-transfer', " +
            "ClientIP='192.168.1.105', ResponseTime=12ms, StatusCode=404.");

        return StatusCode(404, new ApiError("Simulated 404 HTTP Request logged."));
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/banking/chaos/transaction-trace
    //  Matches "Transaction Traces" table row explicitly. Logs → INFO
    // ─────────────────────────────────────────────────────────
    [HttpGet("chaos/transaction-trace")]
    public IActionResult ChaosTransactionTrace()
    {
        BankingLogger.Info(ClassName,
            "Transaction Trace Execution Path: " +
            "TransactionName='CalculateInterestAccrual', " +
            "ExecutionTime=850ms, " +
            "SlowMethodCalls='AccountRepo.GetHistory(), TaxService.Calculate()', " +
            "CodeLevelTrace='Controller -> Service -> Repository -> ExternalAPI', " +
            "RequestParameters='{ accountType: \"savings\", months: 12 }'.");

        return Ok(new ApiError("Transaction trace logged."));
    }
}
