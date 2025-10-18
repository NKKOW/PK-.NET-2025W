using System;

namespace TextAnalytics.Services;

public sealed class ConsoleInputProvider : IInputProvider
{
    public string GetInput()
    {
        Console.WriteLine("Wpisz (lub wklej) tekst do analizy i zatwierdź Enterem:");
        var line = Console.ReadLine();
        return line ?? string.Empty;
    }
}