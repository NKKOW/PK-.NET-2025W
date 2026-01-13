using System;

namespace GutenbergWordStats;

public static class GutenbergCleaner
{
    public static string StripHeaderAndFooter(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        
        var startMarker = "*** START OF";
        var endMarker = "*** END OF";

        var startIndex = text.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (startIndex >= 0)
        {
            var startLineEnd = text.IndexOf('\n', startIndex);
            if (startLineEnd > 0 && startLineEnd < text.Length - 1)
                text = text[(startLineEnd + 1)..];
        }

        var endIndex = text.IndexOf(endMarker, StringComparison.OrdinalIgnoreCase);
        if (endIndex >= 0)
        {
            text = text[..endIndex];
        }

        return text;
    }
}