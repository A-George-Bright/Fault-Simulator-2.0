// =============================================================
//  Program.cs  –  Application entry point
//  Serilog writes structured JSON logs to C:\Logs\banking-app-logs.json
//  Every log line includes: Timestamp, LogLevel, ClassName, ErrorDesc
// =============================================================
using BankingApi.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// ── Bootstrap Serilog before the host builds ─────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()       // picks up all ForContext() properties
    .Enrich.WithMachineName()
    .WriteTo.Console()             // helpful during local dev
    .WriteTo.File(
        formatter: new JsonFormatter(renderMessage: true),
        path: Path.Combine(Directory.GetCurrentDirectory(), "Logs", "banking-app-logs.json"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        shared: true    // safe for IIS worker-process restarts
    )
    .CreateLogger();

try
{
    Log.Information("BankingApi starting up");

    var builder = WebApplication.CreateBuilder(args);

    // ── Replace default logging with Serilog ─────────────────────────────
    builder.Host.UseSerilog();

    // ── MySQL via EF Core ─────────────────────────────────────────────────
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddDbContext<BankingDbContext>(opt =>
        opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

    // ── CORS ──────────────────────────────────────────────────────────────
    var origins = builder.Configuration
                         .GetSection("Cors:AllowedOrigins")
                         .Get<string[]>() ?? [];

    builder.Services.AddCors(o => o.AddPolicy("BankingPolicy", p =>
        p.WithOrigins(origins)
         .AllowAnyHeader()
         .AllowAnyMethod()));

    // ── MVC + Swagger ─────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
        c.SwaggerDoc("v1", new() { Title = "Faulty Banking API", Version = "v1" }));

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Banking API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("BankingPolicy");
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BankingApi terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
