// =============================================================
//  Logging/BankingLogger.cs
//
//  Central logging helper.
//  Every call stamps four properties that Site24x7 will parse:
//    • Timestamp  – added automatically by Serilog
//    • LogLevel   – "Info" | "Warning" | "Error"
//    • ClassName  – C# class that raised the log
//    • ErrorDesc  – human-readable description of what happened
// =============================================================
using Serilog;

namespace BankingApi.Logging;

public static class BankingLogger
{
    // ── INFO ─────────────────────────────────────────────────────────────
    public static void Info(string className, string errorDesc)
    {
        Log.ForContext("ClassName", className)
           .ForContext("LogLevel",  "Info")
           .ForContext("ErrorDesc", errorDesc)
           .Information("[INFO] {ClassName} | {ErrorDesc}", className, errorDesc);
    }

    // ── WARNING ───────────────────────────────────────────────────────────
    public static void Warning(string className, string errorDesc)
    {
        Log.ForContext("ClassName", className)
           .ForContext("LogLevel",  "Warning")
           .ForContext("ErrorDesc", errorDesc)
           .Warning("[WARNING] {ClassName} | {ErrorDesc}", className, errorDesc);
    }

    // ── ERROR ─────────────────────────────────────────────────────────────
    public static void Error(string className, string errorDesc, Exception? ex = null)
    {
        var logger = Log.ForContext("ClassName", className)
                        .ForContext("LogLevel",  "Error")
                        .ForContext("ErrorDesc", errorDesc);

        if (ex is not null)
            logger.Error(ex, "[ERROR] {ClassName} | {ErrorDesc}", className, errorDesc);
        else
            logger.Error("[ERROR] {ClassName} | {ErrorDesc}", className, errorDesc);
    }
}
