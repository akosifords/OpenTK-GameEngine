// Entry point for the Sandbox application

using Makina.Engine;
using Sandbox;
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting Sandbox Application...");

        var app = new SandboxApp();
        
        try
        {
             app.Run();
        }
        catch (Exception ex)
        {
             // Catch top-level exceptions that might occur outside the main loop's try/catch
             Console.WriteLine($"Unhandled exception in Sandbox: {ex}");
             // Optionally log using NLog if configured for Sandbox too
        }
        finally
        {
             Console.WriteLine("Sandbox Application Finished.");
             // NLog shutdown is handled in Application.Shutdown()
        }
    }
}