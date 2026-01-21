using LogCopilot.Infrastructure.Utils;
using Xunit;

namespace LogCopilot.Tests;

public class RedactionUtilityTests
{
    [Fact]
    public void RedactSensitiveData_RedactsJwtTokens()
    {
        var input =
            "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = RedactionUtility.RedactSensitiveData(input);

        Assert.Contains("[REDACTED_JWT]", result);
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", result);
    }

    [Fact]
    public void RedactSensitiveData_RedactsOpenAIKeys()
    {
        var input = "API Key: sk-1234567890abcdefghijklmnopqrstuvwxyz";
        var result = RedactionUtility.RedactSensitiveData(input);

        Assert.Contains("[REDACTED_OPENAI_KEY]", result);
        Assert.DoesNotContain("sk-1234567890abcdefghijklmnopqrstuvwxyz", result);
    }

    [Fact]
    public void RedactSensitiveData_RedactsOpenAIProjectKeys()
    {
        var input = "API Key: sk-proj-abcdefghijklmnopqrstuvwxyz1234567890";
        var result = RedactionUtility.RedactSensitiveData(input);

        Assert.Contains("[REDACTED_OPENAI_KEY]", result);
        Assert.DoesNotContain("sk-proj-", result);
    }

    [Fact]
    public void RedactSensitiveData_RedactsPasswords()
    {
        var input = "password=MySecret123";
        var result = RedactionUtility.RedactSensitiveData(input);

        Assert.Contains("password=[REDACTED]", result);
        Assert.DoesNotContain("MySecret123", result);
    }

    [Fact]
    public void RedactSensitiveData_RedactsConnectionStrings()
    {
        var input = "Server=localhost;Database=mydb;User ID=admin;Password=secret123;";
        var result = RedactionUtility.RedactSensitiveData(input);

        Assert.Contains("Server=[REDACTED]", result);
        Assert.Contains("User ID=[REDACTED]", result);
        Assert.Contains("Password=[REDACTED]", result);
        Assert.DoesNotContain("localhost", result);
        Assert.DoesNotContain("admin", result);
        Assert.DoesNotContain("secret123", result);
    }

    [Fact]
    public void RedactSensitiveData_RedactsEmails_WhenEnabled()
    {
        var input = "Contact: user@example.com for support";
        var result = RedactionUtility.RedactSensitiveData(input, redactEmails: true);

        Assert.Contains("[REDACTED_EMAIL]", result);
        Assert.DoesNotContain("user@example.com", result);
    }

    [Fact]
    public void RedactSensitiveData_PreservesEmails_WhenDisabled()
    {
        var input = "Contact: user@example.com for support";
        var result = RedactionUtility.RedactSensitiveData(input, redactEmails: false);

        Assert.Contains("user@example.com", result);
        Assert.DoesNotContain("[REDACTED_EMAIL]", result);
    }

    [Fact]
    public void RedactSensitiveData_HandlesNullInput()
    {
        string? input = null;
        var result = RedactionUtility.RedactSensitiveData(input!);

        Assert.Null(result);
    }

    [Fact]
    public void RedactSensitiveData_HandlesEmptyInput()
    {
        var input = "";
        var result = RedactionUtility.RedactSensitiveData(input);

        Assert.Equal("", result);
    }

}
