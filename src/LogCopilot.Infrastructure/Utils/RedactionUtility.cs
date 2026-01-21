using System.Text.RegularExpressions;

namespace LogCopilot.Infrastructure.Utils;

public static class RedactionUtility
{
    public static string RedactSensitiveData(string text, bool redactEmails = true)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var redacted = text;

        redacted = RedactJwtTokens(redacted);
        redacted = RedactApiKeys(redacted);
        redacted = RedactPasswords(redacted);
        redacted = RedactConnectionStrings(redacted);
        
        if (redactEmails)
            redacted = RedactEmails(redacted);

        return redacted;
    }

    private static string RedactJwtTokens(string text)
    {
        text = Regex.Replace(
            text,
            @"Bearer\s+[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+",
            "Bearer [REDACTED_JWT]",
            RegexOptions.IgnoreCase
        );

        text = Regex.Replace(
            text,
            @"\b[A-Za-z0-9\-_]{20,}\.[A-Za-z0-9\-_]{20,}\.[A-Za-z0-9\-_]{20,}\b",
            "[REDACTED_JWT]"
        );

        return text;
    }

    private static string RedactApiKeys(string text)
    {
        text = Regex.Replace(text, @"sk-[A-Za-z0-9]{20,}(?=\s|$)", "[REDACTED_OPENAI_KEY]");
        text = Regex.Replace(text, @"sk-proj-[A-Za-z0-9_-]{20,}(?=\s|$)", "[REDACTED_OPENAI_KEY]");
        
        text = Regex.Replace(
            text,
            @"(api[_-]?key|apikey)\s*[:=]\s*([A-Za-z0-9_-]+)(?=\s|$)",
            "$1=[REDACTED_API_KEY]",
            RegexOptions.IgnoreCase
        );

        return text;
    }

    private static string RedactPasswords(string text)
    {
        text = Regex.Replace(
            text,
            @"(password|pwd|secret|token)[""']?\s*[:=]\s*[""']?([^""'\s,};]+)",
            "$1=[REDACTED]",
            RegexOptions.IgnoreCase
        );

        return text;
    }

    private static string RedactConnectionStrings(string text)
    {
        text = Regex.Replace(
            text,
            @"(Server|Data Source|Initial Catalog|User ID|Password|Uid|Pwd)\s*=\s*[^;]+",
            "$1=[REDACTED]",
            RegexOptions.IgnoreCase
        );

        return text;
    }

    private static string RedactEmails(string text)
    {
        text = Regex.Replace(
            text,
            @"[\w._%+-]+@[\w.-]+\.\w{2,}",
            "[REDACTED_EMAIL]",
            RegexOptions.IgnoreCase
        );

        return text;
    }
}
