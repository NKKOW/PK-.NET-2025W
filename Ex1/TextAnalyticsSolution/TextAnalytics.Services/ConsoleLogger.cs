using System;

namespace TextAnalytics.Services
{
    public sealed class ConsoleLogger : ILoggerService
    {
        public void LogInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} - {message}");
            Console.ResetColor();
        }

        public void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {message}");
            Console.ResetColor();
        }

        public void LogSummary(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[SUMMARY] {message}");
            Console.ResetColor();
        }
    }
}
