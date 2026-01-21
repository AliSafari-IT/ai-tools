using System.Text.RegularExpressions;
using LogCopilot.Infrastructure.Parsers.Models;

namespace LogCopilot.Infrastructure.Parsers;

public class NginxErrorLogParser : ILogParser
{
    private static readonly Regex ErrorLogRegex = new(
        @"^(?<timestamp>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$",
        RegexOptions.Compiled
    );

    public async Task<List<ParsedLogEvent>> ParseAsync(Stream stream)
    {
        var events = new List<ParsedLogEvent>();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var match = ErrorLogRegex.Match(line);
                if (match.Success)
                {
                    var timestamp = ParseNginxErrorTimestamp(match.Groups["timestamp"].Value);
                    var level = MapNginxLevel(match.Groups["level"].Value);
                    var message = match.Groups["message"].Value;

                    var logEvent = new ParsedLogEvent
                    {
                        Timestamp = timestamp,
                        Level = level,
                        RenderedMessage = message
                    };

                    events.Add(logEvent);
                }
            }
            catch
            {
            }
        }

        return events;
    }

    private DateTime ParseNginxErrorTimestamp(string timestamp)
    {
        if (DateTime.TryParseExact(timestamp, "yyyy/MM/dd HH:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
        {
            return dt.ToUniversalTime();
        }
        return DateTime.UtcNow;
    }

    private string MapNginxLevel(string nginxLevel)
    {
        return nginxLevel.ToLower() switch
        {
            "emerg" => "Fatal",
            "alert" => "Fatal",
            "crit" => "Fatal",
            "error" => "Error",
            "warn" => "Warning",
            "notice" => "Information",
            "info" => "Information",
            "debug" => "Debug",
            _ => "Information"
        };
    }
}
