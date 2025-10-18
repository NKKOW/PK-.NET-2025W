using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using TextAnalytics.Core;
using TextAnalytics.Services;

namespace TextAnalytics.App;

internal static class Program
{
    private static int Main(string[] args)
    {
        var services = new ServiceCollection()
            .AddSingleton<ILoggerService, ConsoleLogger>()
            .AddSingleton<IInputProvider, ConsoleInputProvider>() 
            .AddSingleton<TextAnalyzer>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILoggerService>();
        var analyzer = services.GetRequiredService<TextAnalyzer>();

        try
        {
            logger.LogInfo("Aplikacja uruchomiona.");
            
            string text;

            if (args.Length > 0)
            {
                var path = args[0];
                try
                {
                    var fileProvider = new FileInputProvider(path);
                    text = fileProvider.GetInput();
                    logger.LogInfo($"Wczytano tekst z pliku: {path}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Błąd podczas wczytywania pliku: {ex.Message}");
                    return 2;
                }
            }
            else
            {
                var inputProvider = services.GetRequiredService<IInputProvider>();
                var input = inputProvider.GetInput();
                
                if (!string.IsNullOrWhiteSpace(input) && File.Exists(input))
                {
                    try
                    {
                        var fileProvider = new FileInputProvider(input);
                        text = fileProvider.GetInput();
                        logger.LogInfo($"Wykryto ścieżkę pliku w wejściu i wczytano: {input}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Błąd podczas wczytywania pliku: {ex.Message}");
                        return 2;
                    }
                }
                else
                {
                    text = input;
                }
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogError("Pusta zawartość – brak tekstu do analizy.");
                return 3;
            }
            
            var stats = analyzer.Analyze(text);
            
            PrintStats(stats);
            
            var json = JsonConvert.SerializeObject(stats, Newtonsoft.Json.Formatting.Indented);
            var outFile = "results.json";
            File.WriteAllText(outFile, json);

            logger.LogSummary($"Wyniki zapisane do {outFile}");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError($"Nieoczekiwany błąd: {ex}");
            return 1;
        }
    }

    private static void PrintStats(TextStatistics s)
    {
        Console.WriteLine();
        Console.WriteLine("=== STATYSTYKI TEKSTU ===");
        Console.WriteLine($"Znaki (ze spacjami):    {s.CharactersWithSpaces}");
        Console.WriteLine($"Znaki (bez spacji):     {s.CharactersWithoutSpaces}");
        Console.WriteLine($"Litery:                  {s.Letters}");
        Console.WriteLine($"Cyfry:                   {s.Digits}");
        Console.WriteLine($"Interpunkcja:            {s.Punctuation}");
        Console.WriteLine($"Słowa:                   {s.WordCount}");
        Console.WriteLine($"Unikalne słowa:          {s.UniqueWordCount}");
        Console.WriteLine($"Najczęstsze słowo:       {ShowOrDash(s.MostCommonWord)}");
        Console.WriteLine($"Śr. długość słowa:       {s.AverageWordLength}");
        Console.WriteLine($"Najdłuższe słowo:        {ShowOrDash(s.LongestWord)}");
        Console.WriteLine($"Najkrótsze słowo:        {ShowOrDash(s.ShortestWord)}");
        Console.WriteLine($"Liczba zdań:             {s.SentenceCount}");
        Console.WriteLine($"Śr. słów na zdanie:      {s.AverageWordsPerSentence}");
        Console.WriteLine($"Najdłuższe zdanie:       {ShowOrDash(s.LongestSentence)}");
        Console.WriteLine("==========================");
        Console.WriteLine();
    }

    private static string ShowOrDash(string? v) =>
        string.IsNullOrWhiteSpace(v) ? "-" : v;
}