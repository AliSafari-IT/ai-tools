using LogCopilot.Infrastructure.Parsers.Models;

namespace LogCopilot.Infrastructure.Parsers;

public interface ILogParser
{
    Task<List<ParsedLogEvent>> ParseAsync(Stream stream);
}
