using System.Text.RegularExpressions;
using LogCopilot.Infrastructure.Parsers.Models;

namespace LogCopilot.Infrastructure.Parsers;

public class NginxAccessLogParser : ILogParser
{
    private static readonly Regex CombinedLogRegex = new(
        @"^(?<ip>\S+) \S+ \S+ \[(?<timestamp>[^\]]+)\] ""(?<method>\S+) (?<path>\S+) \S+"" (?<status>\d{3}) (?<size>\d+) ""[^""]*"" ""(?<useragent>[^""]*)""",
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
                var match = CombinedLogRegex.Match(line);
                if (match.Success)
                {
                    var timestamp = ParseNginxTimestamp(match.Groups["timestamp"].Value);
                    var statusCode = int.Parse(match.Groups["status"].Value);
                    var method = match.Groups["method"].Value;
                    var path = match.Groups["path"].Value;

                    var logEvent = new ParsedLogEvent
                    {
                        Timestamp = timestamp,
                        Level = statusCode >= 500 ? "Error" : statusCode >= 400 ? "Warning" : "Information",
                        RenderedMessage = $"{method} {path} - {statusCode}",
                        HttpDetails = new ParsedHttpDetails
                        {
                            Method = method,
                            Path = path,
                            StatusCode = statusCode,
                            ClientIp = match.Groups["ip"].Value,
                            UserAgent = match.Groups["useragent"].Value
                        }
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

    private DateTime ParseNginxTimestamp(string timestamp)
    {
        if (DateTime.TryParseExact(timestamp, "dd/MMM/yyyy:HH:mm:ss zzz", 
            System.Globalization.CultureInfo.InvariantCulture, 
            System.Globalization.DateTimeStyles.None, out var dt))
        {
            return dt.ToUniversalTime();
        }
        return DateTime.UtcNow;
    }
}
