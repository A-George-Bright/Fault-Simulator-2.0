// =============================================================
//  Program.cs – Application entry point
// =============================================================
using BankingApi.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// ── Bootstrap Serilog ─────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File(
        formatter: new JsonFormatter(renderMessage: true),
        path: Path.Combine(Directory.GetCurrentDirectory(), "Logs", "banking-app-logs.json"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        shared: true
    )
    .CreateLogger();

try
{
    Log.Information("BankingApi starting up");

    var builder = WebApplication.CreateBuilder(args);

    // ✅ FIX: Force fixed port (NO MORE RANDOM PORTS)
    builder.WebHost.UseUrls("http://localhost:5000");

    // ── Use Serilog ────────────────────────────────────────────
    builder.Host.UseSerilog();

    // ── MySQL via EF Core ──────────────────────────────────────
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

    builder.Services.AddDbContext<BankingDbContext>(opt =>
        opt.UseMySql(
            connStr,
            new MySqlServerVersion(new Version(8, 0, 0)) // safer than AutoDetect
        ));

    // ── CORS ───────────────────────────────────────────────────
    var origins = builder.Configuration
                         .GetSection("Cors:AllowedOrigins")
                         .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("BankingPolicy", policy =>
        {
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    // ── Controllers + Swagger ─────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
        c.SwaggerDoc("v1", new() { Title = "Faulty Banking API", Version = "v1" }));

    var app = builder.Build();

    // ── Middleware pipeline ────────────────────────────────────
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

    // ✅ FIX: Use ONLY correct CORS policy
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