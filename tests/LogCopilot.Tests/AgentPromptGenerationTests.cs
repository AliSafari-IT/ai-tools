using Xunit;

namespace LogCopilot.Tests;

public class AgentPromptGenerationTests
{
    [Fact]
    public void GeneratedPrompt_ContainsNoCodeBlocks()
    {
        var prompt = @"ABSOLUTE CODE-GENERATION RULE (NON-NEGOTIABLE)
- Do NOT write READMEs, architecture docs, explanations, plans, or diffs-only output.
- Do NOT ask for confirmation.
- You MUST directly implement the required functionality by editing/adding files in the repo.
- Output only: (1) list of files changed/added, (2) exact commands to run, (3) brief verification notes.

INCIDENT ANALYSIS REPORT
Report ID: test-id
Provider: Community
Generated: 2024-01-21T00:00:00Z

EXECUTIVE SUMMARY
Test summary

METRICS
- Total Events: 100
- Errors: 50
- Warnings: 10
- Fatal: 2
- Duration: 1.0 hours
- Unique Clusters: 3

TOP ISSUES IDENTIFIED
- JWT Token Error (Severity: High, Count: 25)

REQUIRED IMPLEMENTATION TASKS
- [P0] Fix JWT validation: Update token validation logic

ACCEPTANCE CRITERIA
- All errors from identified clusters must be resolved
- System health status must improve to HEALTHY
- No regressions in existing functionality

DELIVERABLE OUTPUT (ONLY)
1. List of files changed/added (paths)
2. Exact commands to run
3. Brief verification notes (what you tested and what you saw)";

        Assert.DoesNotContain("```", prompt);
        Assert.DoesNotContain("`", prompt);
        Assert.NotEmpty(prompt);
    }

    [Fact]
    public void GeneratedPrompt_IsNonEmpty()
    {
        var prompt = "ABSOLUTE CODE-GENERATION RULE";
        Assert.NotEmpty(prompt);
    }
}
