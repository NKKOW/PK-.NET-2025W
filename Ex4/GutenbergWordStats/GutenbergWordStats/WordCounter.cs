using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace GutenbergWordStats;

public static class WordCounter
{
    public static Dictionary<string, int> CountWordsLocal(string text)
    {
        var local = new Dictionary<string, int>(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(text))
            return local;

        var sb = new StringBuilder(32);

        void FlushToken()
        {
            if (sb.Length == 0) return;

            var token = sb.ToString().ToLowerInvariant();
            sb.Clear();
            
            bool ok = false;
            foreach (var ch in token)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    ok = true;
                    break;
                }
            }
            if (!ok) return;

            local.TryGetValue(token, out var c);
            local[token] = c + 1;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                continue;
            }
            
            if (ch == '\'' && sb.Length > 0)
            {
                // sprawdź następny znak
                if (i + 1 < text.Length && char.IsLetterOrDigit(text[i + 1]))
                {
                    sb.Append(ch);
                    continue;
                }
            }
            FlushToken();
        }

        FlushToken();
        return local;
    }
    
    public static void MergeIntoConcurrent(
        Dictionary<string, int> local,
        ConcurrentDictionary<string, int> global)
    {
        foreach (var kv in local)
        {
            global.AddOrUpdate(kv.Key, kv.Value, (_, old) => old + kv.Value);
        }
    }
}
