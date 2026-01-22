using System.Text.Json;
using System.Text.RegularExpressions;

namespace LogCopilot.Infrastructure.Utils;

public static class JsonExtractor
{
    public static (bool success, string json, string error) ExtractJson(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return (false, string.Empty, "Empty response");
        }

        var trimmed = rawResponse.Trim();

        if (TryParseJson(trimmed, out var directJson))
        {
            return (true, directJson, string.Empty);
        }

        var withoutFences = StripCodeFences(trimmed);
        if (TryParseJson(withoutFences, out var fencedJson))
        {
            return (true, fencedJson, string.Empty);
        }

        var extracted = ExtractFirstJsonObject(trimmed);
        if (!string.IsNullOrEmpty(extracted) && TryParseJson(extracted, out var extractedJson))
        {
            return (true, extractedJson, string.Empty);
        }

        return (false, string.Empty, "No valid JSON found in response");
    }

    private static bool TryParseJson(string input, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(input);
            normalized = JsonSerializer.Serialize(doc.RootElement);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string StripCodeFences(string input)
    {
        var pattern = @"^```(?:json)?\s*\n?(.*?)\n?```$";
        var match = Regex.Match(input, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : input;
    }

    private static string ExtractFirstJsonObject(string input)
    {
        var firstBrace = input.IndexOf('{');
        if (firstBrace == -1) return string.Empty;

        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = firstBrace; i < input.Length; i++)
        {
            var c = input[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '{') depth++;
            if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return input.Substring(firstBrace, i - firstBrace + 1);
                }
            }
        }

        return string.Empty;
    }
}
