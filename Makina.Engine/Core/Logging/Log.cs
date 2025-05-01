using NLog;

namespace Makina.Engine.Core.Logging;

/// <summary>
/// Static wrapper class for the NLog logging system.
/// Provides easy access to logging methods throughout the engine.
/// </summary>
public static class Log
{
    private static readonly Logger LoggerInstance = LogManager.GetCurrentClassLogger();

    // Optional: Initialize NLog explicitly if needed (e.g., load config from specific path)
    // static Log()
    // {
    //     // NLog.LogManager.LoadConfiguration("nlog.config"); 
    // }

    public static void Trace(string message) => LoggerInstance.Trace(message);
    public static void Debug(string message) => LoggerInstance.Debug(message);
    public static void Info(string message) => LoggerInstance.Info(message);
    public static void Warn(string message) => LoggerInstance.Warn(message);
    public static void Error(string message) => LoggerInstance.Error(message);
    public static void Fatal(string message) => LoggerInstance.Fatal(message);

    public static void Trace(Exception exception, string message = "") => LoggerInstance.Trace(exception, message);
    public static void Debug(Exception exception, string message = "") => LoggerInstance.Debug(exception, message);
    public static void Info(Exception exception, string message = "") => LoggerInstance.Info(exception, message);
    public static void Warn(Exception exception, string message = "") => LoggerInstance.Warn(exception, message);
    public static void Error(Exception exception, string message = "") => LoggerInstance.Error(exception, message);
    public static void Fatal(Exception exception, string message = "") => LoggerInstance.Fatal(exception, message);
} 