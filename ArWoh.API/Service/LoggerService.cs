using ArWoh.API.Interface;

namespace ArWoh.API.Service;

public class LoggerService : ILoggerService
{
    public override void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Logger Success] - {DateTime.UtcNow.AddHours(7)} - " + msg);
        Console.ResetColor();
    }

    public override void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Logger Error] - {DateTime.UtcNow.AddHours(7)} - " + msg);
        Console.ResetColor();
    }

    public override void Warn(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Logger Warn] - {DateTime.UtcNow.AddHours(7)} - " + msg);
        Console.ResetColor();
    }

    public override void Info(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[Logger Info] - {DateTime.UtcNow.AddHours(7)} - " + msg);
        Console.ResetColor();
    }
}