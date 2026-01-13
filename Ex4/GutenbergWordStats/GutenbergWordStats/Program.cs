using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GutenbergWordStats;

public static class Program
{
    private static readonly HttpClient Http = new HttpClient();

    public static async Task Main(string[] args)
    {
        if (!Http.DefaultRequestHeaders.UserAgent.Any())
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("GutenbergWordStats/1.0");

        var books = new[]
        {
            new BookSource("Pride and Prejudice", "https://www.gutenberg.org/cache/epub/1342/pg1342.txt"),
            new BookSource("Frankenstein", "https://www.gutenberg.org/cache/epub/84/pg84.txt"),
        };

        // 1) Pobieranie (faza 1)
        var swDownload = Stopwatch.StartNew();

        var downloaded = await Task.WhenAll(
            books.Select(async b =>
            {
                var text = await Http.GetStringAsync(b.Url);
                return (Book: b, Text: text);
            })
        );

        swDownload.Stop();

        // 2) Przetwarzanie (faza 2)
        var swProcess = Stopwatch.StartNew();

        var global = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);

        await Task.WhenAll(
            downloaded.Select(item => Task.Run(() =>
            {
                var cleaned = GutenbergCleaner.StripHeaderAndFooter(item.Text);
                var local = WordCounter.CountWordsLocal(cleaned);
                WordCounter.MergeIntoConcurrent(local, global);
            }))
        );

        swProcess.Stop();

        // 3) Raport
        var top10 = global
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        Console.WriteLine("Najczęstsze słowa:");
        for (int i = 0; i < top10.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {top10[i].Key}: {top10[i].Value}");
        }

        Console.WriteLine();
        Console.WriteLine($"Czas pobierania: {swDownload.Elapsed.TotalSeconds:0.00} sekundy");
        Console.WriteLine($"Czas przetwarzania: {swProcess.Elapsed.TotalSeconds:0.00} sekundy");
    }
}
