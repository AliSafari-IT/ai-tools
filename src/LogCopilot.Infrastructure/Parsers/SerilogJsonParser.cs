using System.Text.Json;
using LogCopilot.Infrastructure.Parsers.Models;

namespace LogCopilot.Infrastructure.Parsers;

public class SerilogJsonParser : ILogParser
{
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
                var jsonDoc = JsonDocument.Parse(line);
                var root = jsonDoc.RootElement;

                var logEvent = new ParsedLogEvent
                {
                    RawJson = line,
                    Timestamp = ParseTimestamp(root),
                    Level = root.TryGetProperty("Level", out var levelProp)
                        ? levelProp.GetString() ?? "Information"
                        : "Information",
                    MessageTemplate = root.TryGetProperty("MessageTemplate", out var mtProp)
                        ? mtProp.GetString()
                        : null,
                    RenderedMessage =
                        root.TryGetProperty("RenderedMessage", out var rmProp)
                            ? rmProp.GetString() ?? ""
                        : root.TryGetProperty("Message", out var msgProp)
                            ? msgProp.GetString() ?? ""
                        : "",
                    TraceId = GetPropertyString(root, "TraceId"),
                    SpanId = GetPropertyString(root, "SpanId"),
                    ParentSpanId = GetPropertyString(root, "ParentSpanId"),
                    RequestId = GetPropertyString(root, "RequestId"),
                    CorrelationId = GetPropertyString(root, "CorrelationId"),
                };

                if (root.TryGetProperty("Properties", out var props))
                {
                    logEvent.Properties = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        props.GetRawText()
                    );
                    ExtractTracingFromProperties(logEvent, props);
                }

                if (
                    root.TryGetProperty("Exception", out var ex)
                    && ex.ValueKind != JsonValueKind.Null
                )
                {
                    logEvent.ExceptionDetails = ParseException(ex.GetString());
                }

                ExtractHttpDetails(logEvent, root);

                events.Add(logEvent);
            }
            catch { }
        }

        return events;
    }

    private DateTime ParseTimestamp(JsonElement root)
    {
        if (root.TryGetProperty("Timestamp", out var ts))
        {
            if (ts.TryGetDateTime(out var dt))
                return dt.ToUniversalTime();
            if (DateTime.TryParse(ts.GetString(), out var parsed))
                return parsed.ToUniversalTime();
        }
        if (root.TryGetProperty("@t", out var at))
        {
            if (at.TryGetDateTime(out var dt))
                return dt.ToUniversalTime();
        }
        return DateTime.UtcNow;
    }

    private string? GetPropertyString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop))
            return prop.GetString();

        if (
            root.TryGetProperty("Properties", out var props)
            && props.TryGetProperty(propertyName, out var nested)
        )
            return nested.GetString();

        return null;
    }

    private void ExtractTracingFromProperties(ParsedLogEvent logEvent, JsonElement props)
    {
        logEvent.TraceId ??= GetFromProps(props, "TraceId");
        logEvent.SpanId ??= GetFromProps(props, "SpanId");
        logEvent.ParentSpanId ??= GetFromProps(props, "ParentSpanId");
        logEvent.RequestId ??= GetFromProps(props, "RequestId");
        logEvent.CorrelationId ??= GetFromProps(props, "CorrelationId");
    }

    private string? GetFromProps(JsonElement props, string name)
    {
        return props.TryGetProperty(name, out var val) ? val.GetString() : null;
    }

    private void ExtractHttpDetails(ParsedLogEvent logEvent, JsonElement root)
    {
        var method = GetPropertyString(root, "Method") ?? GetPropertyString(root, "HttpMethod");
        var path = GetPropertyString(root, "Path") ?? GetPropertyString(root, "RequestPath");
        var statusCode = GetPropertyInt(root, "StatusCode");

        if (method != null || path != null || statusCode != null)
        {
            logEvent.HttpDetails = new ParsedHttpDetails
            {
                Method = method,
                Path = path,
                QueryString = GetPropertyString(root, "QueryString"),
                StatusCode = statusCode,
                DurationMs =
                    GetPropertyDouble(root, "Elapsed")
                    ?? GetPropertyDouble(root, "ElapsedMilliseconds"),
                ClientIp =
                    GetPropertyString(root, "ClientIp")
                    ?? GetPropertyString(root, "RemoteIpAddress"),
                UserAgent = GetPropertyString(root, "UserAgent"),
            };
        }
    }

    private int? GetPropertyInt(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out var val))
            return val;
        if (
            root.TryGetProperty("Properties", out var props)
            && props.TryGetProperty(propertyName, out var nested)
            && nested.TryGetInt32(out var nval)
        )
            return nval;
        return null;
    }

    private double? GetPropertyDouble(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop) && prop.TryGetDouble(out var val))
            return val;
        if (
            root.TryGetProperty("Properties", out var props)
            && props.TryGetProperty(propertyName, out var nested)
            && nested.TryGetDouble(out var nval)
        )
            return nval;
        return null;
    }

    private ParsedExceptionDetails? ParseException(string? exceptionString)
    {
        if (string.IsNullOrWhiteSpace(exceptionString))
            return null;

        var lines = exceptionString.Split('\n');
        var firstLine = lines.FirstOrDefault() ?? "";
        var typeSeparatorIndex = firstLine.IndexOf(':');

        var type = typeSeparatorIndex > 0 ? firstLine[..typeSeparatorIndex].Trim() : "Exception";
        var message =
            typeSeparatorIndex > 0 && typeSeparatorIndex < firstLine.Length - 1
                ? firstLine[(typeSeparatorIndex + 1)..].Trim()
                : firstLine;

        return new ParsedExceptionDetails
        {
            Type = type,
            Message = message,
            StackTrace = string.Join("\n", lines.Skip(1)),
        };
    }
}
