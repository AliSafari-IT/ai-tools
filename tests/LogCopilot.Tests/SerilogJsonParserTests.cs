using System.Text;
using LogCopilot.Infrastructure.Parsers;
using Xunit;

namespace LogCopilot.Tests;

public class SerilogJsonParserTests
{
    [Fact]
    public async Task ParseAsync_ParsesValidSerilogJson()
    {
        var json = "{\"Timestamp\":\"2024-01-20T12:00:00Z\",\"Level\":\"Error\",\"MessageTemplate\":\"Test message\",\"RenderedMessage\":\"Test message\"}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        var parser = new SerilogJsonParser();
        var events = await parser.ParseAsync(stream);

        Assert.Single(events);
        Assert.Equal("Error", events[0].Level);
        Assert.Equal("Test message", events[0].RenderedMessage);
    }

    [Fact]
    public async Task ParseAsync_SkipsInvalidLines()
    {
        var content = "invalid json\n{\"Timestamp\":\"2024-01-20T12:00:00Z\",\"Level\":\"Info\",\"Message\":\"Valid\"}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        var parser = new SerilogJsonParser();
        var events = await parser.ParseAsync(stream);

        Assert.Single(events);
    }

    [Fact]
    public async Task ParseAsync_ExtractsTraceId()
    {
        var json = "{\"Timestamp\":\"2024-01-20T12:00:00Z\",\"Level\":\"Info\",\"Message\":\"Test\",\"TraceId\":\"abc123\"}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        var parser = new SerilogJsonParser();
        var events = await parser.ParseAsync(stream);

        Assert.Single(events);
        Assert.Equal("abc123", events[0].TraceId);
    }
}
