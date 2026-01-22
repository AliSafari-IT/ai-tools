using Xunit;
using LogCopilot.Infrastructure.Utils;

namespace LogCopilot.Tests;

public class JsonExtractorTests
{
    [Fact]
    public void ExtractJson_PureJson_ReturnsSuccess()
    {
        var input = "{\"name\":\"test\",\"value\":123}";
        var (success, json, error) = JsonExtractor.ExtractJson(input);

        Assert.True(success);
        Assert.NotEmpty(json);
        Assert.Empty(error);
    }

    [Fact]
    public void ExtractJson_JsonWithCodeFences_ReturnsSuccess()
    {
        var input = "```json\n{\"name\":\"test\",\"value\":123}\n```";
        var (success, json, error) = JsonExtractor.ExtractJson(input);

        Assert.True(success);
        Assert.NotEmpty(json);
        Assert.Empty(error);
    }

    [Fact]
    public void ExtractJson_JsonWithPlainCodeFences_ReturnsSuccess()
    {
        var input = "```\n{\"name\":\"test\",\"value\":123}\n```";
        var (success, json, error) = JsonExtractor.ExtractJson(input);

        Assert.True(success);
        Assert.NotEmpty(json);
        Assert.Empty(error);
    }

    [Fact]
    public void ExtractJson_JsonWithLeadingText_ReturnsSuccess()
    {
        var input = "Here is the result:\n{\"name\":\"test\",\"value\":123}";
        var (success, json, error) = JsonExtractor.ExtractJson(input);

        Assert.True(success);
        Assert.NotEmpty(json);
        Assert.Empty(error);
    }

    [Fact]
    public void ExtractJson_JsonWithTrailingText_ReturnsSuccess()
    {
        var input = "{\"name\":\"test\",\"value\":123}\nThat's the result.";
        var (success, json, error) = JsonExtractor.ExtractJson(input);

        Assert.True(success);
        Assert.NotEmpty(json);
        Assert.Empty(error);
    }

    [Fact]
    public void ExtractJson_EmptyString_ReturnsFailure()
    {
        var (success, json, error) = JsonExtractor.ExtractJson("");

        Assert.False(success);
        Assert.Empty(json);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void ExtractJson_NoJson_ReturnsFailure()
    {
        var input = "This is just plain text with no JSON";
        var (success, json, error) = JsonExtractor.ExtractJson(input);

        Assert.False(success);
        Assert.Empty(json);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void ExtractJson_NestedJson_ReturnsSuccess()
    {
        var input = "{\"outer\":{\"inner\":{\"value\":\"nested\"}}}";
        var (success, json, error) = JsonExtractor.ExtractJson(input);

        Assert.True(success);
        Assert.NotEmpty(json);
        Assert.Empty(error);
    }

    [Fact]
    public void ExtractJson_JsonWithEscapedQuotes_ReturnsSuccess()
    {
        var input = "{\"message\":\"He said \\\"hello\\\"\"}";
        var (success, json, error) = JsonExtractor.ExtractJson(input);

        Assert.True(success);
        Assert.NotEmpty(json);
        Assert.Empty(error);
    }
}
