using System.Text.RegularExpressions;
using LogCopilot.Infrastructure.Parsers.Models;

namespace LogCopilot.Infrastructure.Parsers;

public class SerilogPlainTextParser : ILogParser
{
    private static readonly Regex LogLinePattern = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2})\s+\[(?<level>\w+)\]\s+(?<message>.*)$",
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
                var match = LogLinePattern.Match(line);
                if (!match.Success)
                {
                    var detectedLevel = DetectLevelFromContent(line);
                    if (!string.IsNullOrEmpty(detectedLevel))
                    {
                        var logEvent = new ParsedLogEvent
                        {
                            RawJson = line,
                            Timestamp = DateTime.UtcNow,
                            Level = detectedLevel,
                            MessageTemplate = null,
                            RenderedMessage = line,
                            TraceId = null,
                            SpanId = null,
                            ParentSpanId = null,
                            RequestId = null,
                            CorrelationId = null
                        };
                        events.Add(logEvent);
                    }
                    continue;
                }

                var timestamp = match.Groups["timestamp"].Value;
                var level = match.Groups["level"].Value;
                var message = match.Groups["message"].Value;

                if (!DateTime.TryParse(timestamp, out var parsedTimestamp))
                    parsedTimestamp = DateTime.UtcNow;

                var normalizedLevel = NormalizeLevel(level, message);

                var logEvent2 = new ParsedLogEvent
                {
                    RawJson = line,
                    Timestamp = parsedTimestamp.ToUniversalTime(),
                    Level = normalizedLevel,
                    MessageTemplate = null,
                    RenderedMessage = message,
                    TraceId = null,
                    SpanId = null,
                    ParentSpanId = null,
                    RequestId = null,
                    CorrelationId = null
                };

                events.Add(logEvent2);
            }
            catch
            {
            }
        }

        return events;
    }

    private string NormalizeLevel(string level, string message)
    {
        var lowerLevel = level.ToLower();
        
        if (lowerLevel == "warn" || lowerLevel == "wrn") return "Warning";
        if (lowerLevel == "err" || lowerLevel == "error") return "Error";
        if (lowerLevel == "crit" || lowerLevel == "critical" || lowerLevel == "fatal" || lowerLevel == "ftl") return "Fatal";
        if (lowerLevel == "dbg" || lowerLevel == "debug") return "Debug";
        if (lowerLevel == "inf" || lowerLevel == "info" || lowerLevel == "information") return "Information";
        if (lowerLevel == "verbose" || lowerLevel == "vrb" || lowerLevel == "trace" || lowerLevel == "trc") return "Verbose";
        
        var detectedFromMessage = DetectLevelFromContent(message);
        if (!string.IsNullOrEmpty(detectedFromMessage))
            return detectedFromMessage;
        
        return "Information";
    }

    private string DetectLevelFromContent(string content)
    {
        var lower = content.ToLower();
        
        if (lower.Contains("fail:") || lower.Contains("failed") || lower.Contains("fatal") || lower.Contains("critical"))
            return "Fatal";
        if (lower.Contains("error") || lower.Contains("err:") || lower.Contains("exception"))
            return "Error";
        if (lower.Contains("warn:") || lower.Contains("warning"))
            return "Warning";
        
        return string.Empty;
    }
}
