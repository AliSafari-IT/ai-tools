using System.Text;
using System.Text.Json;
using LogCopilot.Application.DTOs;
using LogCopilot.Application.Interfaces;
using LogCopilot.Infrastructure.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IIncidentReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IIncidentReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateReport([FromBody] GenerateReportDto dto)
    {
        try
        {
            var result = await _reportService.GenerateReportAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return StatusCode(500, new { message = "Error generating report", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _reportService.GetReportsAsync(page, pageSize);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports");
            return StatusCode(500, new { message = "Error retrieving reports", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReportById(Guid id)
    {
        try
        {
            var result = await _reportService.GetReportByIdAsync(id);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report {ReportId}", id);
            return StatusCode(500, new { message = "Error retrieving report", error = ex.Message });
        }
    }

    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportReport(Guid id, [FromQuery] string format = "md", [FromQuery] string target = "claude")
    {
        try
        {
            var report = await _reportService.GetReportByIdAsync(id);
            var output = report.Output;

            var redactedOutput = new ReportOutput
            {
                ExecutiveSummary = RedactionUtility.RedactSensitiveData(output.ExecutiveSummary),
                Metrics = output.Metrics,
                TopIssues = output.TopIssues.Select(ti => new TopIssue
                {
                    ClusterId = ti.ClusterId,
                    Title = ti.Title,
                    Severity = ti.Severity,
                    Count = ti.Count,
                    FirstSeen = ti.FirstSeen,
                    LastSeen = ti.LastSeen,
                    Evidence = ti.Evidence.Select(e => RedactionUtility.RedactSensitiveData(e)).ToList(),
                    SuggestedActions = ti.SuggestedActions
                }).ToList(),
                RootCauseHypotheses = output.RootCauseHypotheses,
                RecommendedFixPlan = output.RecommendedFixPlan,
                ObservabilityGaps = output.ObservabilityGaps,
                TimelineHighlights = output.TimelineHighlights,
                Provider = output.Provider
            };

            return format.ToLower() switch
            {
                "md" => ExportMarkdown(id, redactedOutput),
                "json" => ExportJson(id, redactedOutput),
                "html" => ExportHtml(id, redactedOutput),
                "prompt" => ExportPrompt(id, redactedOutput, target),
                _ => BadRequest(new { message = "Invalid format. Supported: md, json, html, prompt" })
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report {ReportId}", id);
            return StatusCode(500, new { message = "Error exporting report", error = ex.Message });
        }
    }

    private IActionResult ExportMarkdown(Guid id, ReportOutput output)
    {
        var md = new StringBuilder();
        md.AppendLine($"# Incident Report {id}");
        md.AppendLine();
        md.AppendLine($"**Provider**: {output.Provider}");
        md.AppendLine($"**Generated**: {DateTime.UtcNow:O}");
        md.AppendLine();
        md.AppendLine("## Executive Summary");
        md.AppendLine(output.ExecutiveSummary);
        md.AppendLine();
        md.AppendLine("## Metrics");
        md.AppendLine($"- Total Events: {output.Metrics.TotalEvents}");
        md.AppendLine($"- Errors: {output.Metrics.ErrorCount}");
        md.AppendLine($"- Warnings: {output.Metrics.WarningCount}");
        md.AppendLine($"- Duration: {output.Metrics.DurationHours:F1} hours");
        md.AppendLine();
        md.AppendLine("## Top Issues");
        foreach (var issue in output.TopIssues)
        {
            md.AppendLine($"### {issue.Title}");
            md.AppendLine($"- Severity: {issue.Severity}");
            md.AppendLine($"- Count: {issue.Count}");
        }

        var content = Encoding.UTF8.GetBytes(md.ToString());
        return File(content, "text/markdown", $"incident-report-{id}.md");
    }

    private IActionResult ExportJson(Guid id, ReportOutput output)
    {
        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
        var content = Encoding.UTF8.GetBytes(json);
        return File(content, "application/json", $"incident-report-{id}.json");
    }

    private IActionResult ExportHtml(Guid id, ReportOutput output)
    {
        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Incident Report {id}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; max-width: 900px; margin: 0 auto; padding: 20px; color: #333; }}
        h1 {{ color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; }}
        h2 {{ color: #34495e; margin-top: 30px; }}
        .summary {{ background: #ecf0f1; padding: 15px; border-left: 4px solid #3498db; margin: 20px 0; }}
        .metrics {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin: 20px 0; }}
        .metric {{ background: #f8f9fa; padding: 15px; border-radius: 5px; }}
        .metric-value {{ font-size: 24px; font-weight: bold; color: #3498db; }}
        .metric-label {{ font-size: 12px; color: #7f8c8d; text-transform: uppercase; }}
        @media print {{ body {{ padding: 0; }} }}
    </style>
</head>
<body>
    <h1>Incident Report</h1>
    <p><strong>ID</strong>: {id}</p>
    <p><strong>Provider</strong>: {output.Provider}</p>
    <p><strong>Generated</strong>: {DateTime.UtcNow:O}</p>
    
    <h2>Executive Summary</h2>
    <div class='summary'>{output.ExecutiveSummary}</div>
    
    <h2>Metrics</h2>
    <div class='metrics'>
        <div class='metric'><div class='metric-value'>{output.Metrics.TotalEvents}</div><div class='metric-label'>Total Events</div></div>
        <div class='metric'><div class='metric-value'>{output.Metrics.ErrorCount}</div><div class='metric-label'>Errors</div></div>
        <div class='metric'><div class='metric-value'>{output.Metrics.WarningCount}</div><div class='metric-label'>Warnings</div></div>
        <div class='metric'><div class='metric-value'>{output.Metrics.DurationHours:F1}h</div><div class='metric-label'>Duration</div></div>
    </div>
    
    <h2>Top Issues</h2>
    {string.Join("", output.TopIssues.Select(ti => $"<h3>{ti.Title}</h3><p>Severity: {ti.Severity} | Count: {ti.Count}</p>"))}
</body>
</html>";

        var content = Encoding.UTF8.GetBytes(html);
        return File(content, "text/html", $"incident-report-{id}.html");
    }

    private IActionResult ExportPrompt(Guid id, ReportOutput output, string target)
    {
        var prompt = $@"ABSOLUTE CODE-GENERATION RULE (NON-NEGOTIABLE)
- Do NOT write READMEs, architecture docs, explanations, plans, or diffs-only output.
- Do NOT ask for confirmation.
- You MUST directly implement the required functionality by editing/adding files in the repo.
- Output only: (1) list of files changed/added, (2) exact commands to run, (3) brief verification notes.

INCIDENT ANALYSIS REPORT
Report ID: {id}
Provider: {output.Provider}
Generated: {DateTime.UtcNow:O}

EXECUTIVE SUMMARY
{output.ExecutiveSummary}

METRICS
- Total Events: {output.Metrics.TotalEvents}
- Errors: {output.Metrics.ErrorCount}
- Warnings: {output.Metrics.WarningCount}
- Fatal: {output.Metrics.FatalCount}
- Duration: {output.Metrics.DurationHours:F1} hours
- Unique Clusters: {output.Metrics.UniqueClusters}

TOP ISSUES IDENTIFIED
{string.Join("\n", output.TopIssues.Select(ti => $"- {ti.Title} (Severity: {ti.Severity}, Count: {ti.Count})"))}

REQUIRED IMPLEMENTATION TASKS
{string.Join("\n", output.RecommendedFixPlan.Select(ft => $"- [{ft.Priority}] {ft.Task}: {ft.WhatToChange}"))}

ACCEPTANCE CRITERIA
- All errors from identified clusters must be resolved
- System health status must improve to HEALTHY
- No regressions in existing functionality

DELIVERABLE OUTPUT (ONLY)
1. List of files changed/added (paths)
2. Exact commands to run
3. Brief verification notes (what you tested and what you saw)";

        var content = Encoding.UTF8.GetBytes(prompt);
        return File(content, "text/plain", $"incident-report-{id}-prompt.txt");
    }
}
