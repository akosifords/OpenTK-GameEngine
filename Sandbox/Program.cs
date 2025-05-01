// Entry point for the Sandbox application

using Makina.Engine;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting Sandbox Application...");

        var app = new Application();
        app.Run();

        Console.WriteLine("Sandbox Application Finished.");
    }
}