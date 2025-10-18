using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace TextAnalytics.Core;

public sealed class TextAnalyzer
{
    
    private static readonly Regex WordRegex = new(@"\b[\p{L}\p{N}']+\b", RegexOptions.Multiline | RegexOptions.CultureInvariant);
    // Zdania zakończone . ! ?
    private static readonly Regex SentenceSplitRegex = new(@"[\.!?]+", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    public TextStatistics Analyze(string text)
    {
        text ??= string.Empty;
        if (text.Length == 0)
            return TextStatistics.Empty;

        var charsWithSpaces = CountCharacters(text);
        var charsWithoutSpaces = CountCharacters(text, includeSpaces: false);
        var letters = CountLetters(text);
        var digits = CountDigits(text);
        var punctuation = CountPunctuation(text);

        var words = TokenizeWords(text);
        var wordCount = words.Count;
        var uniqueWordCount = words
            .Select(w => w.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Count();

        var mostCommonWord = words.Count == 0
            ? string.Empty
            : words.GroupBy(w => w, StringComparer.OrdinalIgnoreCase)
                   .OrderByDescending(g => g.Count())
                   .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                   .First().Key;

        var averageWordLength = words.Count == 0 ? 0d : words.Average(w => (double)w.Length);
        var longestWord = words.OrderByDescending(w => w.Length).ThenBy(w => w, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty;
        var shortestWord = words.OrderBy(w => w.Length).ThenBy(w => w, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty;

        var sentences = SplitSentences(text);
        var sentenceCount = sentences.Count;
        var averageWordsPerSentence = sentenceCount == 0 ? 0d
            : sentences.Select(s => TokenizeWords(s).Count).DefaultIfEmpty(0).Average();

        var longestSentence = sentences
            .OrderByDescending(s => TokenizeWords(s).Count)
            .ThenBy(s => s, StringComparer.Ordinal)
            .FirstOrDefault() ?? string.Empty;

        return new TextStatistics(
            charsWithSpaces,
            charsWithoutSpaces,
            letters,
            digits,
            punctuation,
            wordCount,
            uniqueWordCount,
            mostCommonWord,
            Math.Round(averageWordLength, 2),
            longestWord,
            shortestWord,
            sentenceCount,
            Math.Round(averageWordsPerSentence, 2),
            longestSentence
        );
    }

    public int CountCharacters(string text, bool includeSpaces = true)
    {
        text ??= string.Empty;
        return includeSpaces ? text.Length : text.Count(c => !char.IsWhiteSpace(c));
    }

    public int CountLetters(string text) => (text ?? string.Empty).Count(char.IsLetter);

    public int CountDigits(string text) => (text ?? string.Empty).Count(char.IsDigit);

    public int CountPunctuation(string text) => (text ?? string.Empty).Count(char.IsPunctuation);

    public int CountWords(string text) => TokenizeWords(text).Count;

    public int CountUniqueWords(string text) =>
        TokenizeWords(text).Select(w => w.ToLowerInvariant()).Distinct().Count();

    public string FindMostCommonWord(string text)
    {
        var words = TokenizeWords(text);
        return words.Count == 0
            ? string.Empty
            : words.GroupBy(w => w, StringComparer.OrdinalIgnoreCase)
                   .OrderByDescending(g => g.Count())
                   .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase).First().Key;
    }

    public double AverageWordLength(string text)
    {
        var words = TokenizeWords(text);
        return words.Count == 0 ? 0 : words.Average(w => (double)w.Length);
    }

    public string MaxWord(string text) =>
        TokenizeWords(text).OrderByDescending(w => w.Length).FirstOrDefault() ?? string.Empty;

    public string MinWord(string text) =>
        TokenizeWords(text).OrderBy(w => w.Length).FirstOrDefault() ?? string.Empty;

    public int CountSentences(string text) => SplitSentences(text).Count;

    public double AverageWordsPerSentence(string text)
    {
        var sentences = SplitSentences(text);
        if (sentences.Count == 0) return 0;
        return sentences.Select(s => TokenizeWords(s).Count).DefaultIfEmpty(0).Average();
    }

    public string LongestSentence(string text)
    {
        var sentences = SplitSentences(text);
        return sentences.Count == 0
            ? string.Empty
            : sentences.OrderByDescending(s => TokenizeWords(s).Count).First();
    }
    
    private static List<string> TokenizeWords(string text)
    {
        text ??= string.Empty;
        return WordRegex.Matches(text).Select(m => m.Value).ToList();
    }

    private static List<string> SplitSentences(string text)
    {
        text ??= string.Empty;
        return SentenceSplitRegex
            .Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}