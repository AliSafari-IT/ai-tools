import { redactForExport } from './redaction'

interface ReportMetrics {
    totalEvents: number
    errorCount: number
    fatalCount: number
    warningCount: number
    infoCount: number
    uniqueClusters: number
    firstSeen: string
    lastSeen: string
    durationHours: number
}

interface TopIssue {
    clusterId?: string
    title: string
    severity: string
    count: number
    firstSeen: string
    lastSeen: string
    evidence: string[]
    suggestedActions: string[]
}

interface RootCauseHypothesis {
    issue: string
    likelyCause: string
    supportingEvidence: string[]
    confidence: string
}

interface FixTask {
    priority: string
    task: string
    whatToChange: string
    riskImpact: string
    howToVerify: string
}

interface TimelineHighlight {
    timestamp: string
    event: string
    eventCount: number
}

interface ReportOutput {
    executiveSummary: string
    metrics: ReportMetrics
    topIssues: TopIssue[]
    rootCauseHypotheses: RootCauseHypothesis[]
    recommendedFixPlan: FixTask[]
    observabilityGaps: string[]
    timelineHighlights: TimelineHighlight[]
    provider: string
}

interface ReportDetail {
    id: string
    scope: string
    createdAt: string
    output: ReportOutput
}

export function exportSummary(report: ReportDetail): string {
    return redactForExport(report.output.executiveSummary)
}

export function exportMarkdown(report: ReportDetail): string {
    const lines: string[] = []

    lines.push(`# Incident Report - ${report.scope}`)
    lines.push(``)
    lines.push(`**Created:** ${new Date(report.createdAt).toLocaleString()}`)
    lines.push(`**Provider:** ${report.output.provider}`)
    lines.push(``)

    lines.push(`## Executive Summary`)
    lines.push(``)
    lines.push(report.output.executiveSummary)
    lines.push(``)

    lines.push(`## Key Metrics`)
    lines.push(``)
    lines.push(`- **Total Events:** ${report.output.metrics.totalEvents}`)
    lines.push(`- **Errors:** ${report.output.metrics.errorCount}`)
    lines.push(`- **Fatal:** ${report.output.metrics.fatalCount}`)
    lines.push(`- **Warnings:** ${report.output.metrics.warningCount}`)
    lines.push(`- **Info:** ${report.output.metrics.infoCount}`)
    lines.push(`- **Unique Clusters:** ${report.output.metrics.uniqueClusters}`)
    lines.push(`- **Duration:** ${report.output.metrics.durationHours.toFixed(1)} hours`)
    lines.push(`- **Time Range:** ${new Date(report.output.metrics.firstSeen).toLocaleString()} to ${new Date(report.output.metrics.lastSeen).toLocaleString()}`)
    lines.push(``)

    if (report.output.topIssues.length > 0) {
        lines.push(`## Top Issues`)
        lines.push(``)
        report.output.topIssues.forEach((issue, idx) => {
            lines.push(`### ${idx + 1}. ${issue.title}`)
            lines.push(``)
            lines.push(`- **Severity:** ${issue.severity}`)
            lines.push(`- **Count:** ${issue.count}`)
            lines.push(`- **First Seen:** ${new Date(issue.firstSeen).toLocaleString()}`)
            lines.push(`- **Last Seen:** ${new Date(issue.lastSeen).toLocaleString()}`)
            
            if (issue.evidence.length > 0) {
                lines.push(``)
                lines.push(`**Evidence:**`)
                issue.evidence.forEach(e => lines.push(`- ${e}`))
            }

            if (issue.suggestedActions.length > 0) {
                lines.push(``)
                lines.push(`**Suggested Actions:**`)
                issue.suggestedActions.forEach(a => lines.push(`- ${a}`))
            }
            lines.push(``)
        })
    }

    if (report.output.rootCauseHypotheses.length > 0) {
        lines.push(`## Root Cause Analysis`)
        lines.push(``)
        report.output.rootCauseHypotheses.forEach((hyp, idx) => {
            lines.push(`### ${hyp.issue}`)
            lines.push(``)
            lines.push(`**Likely Cause:** ${hyp.likelyCause}`)
            lines.push(``)
            lines.push(`**Confidence:** ${hyp.confidence}`)
            lines.push(``)
            lines.push(`**Supporting Evidence:**`)
            hyp.supportingEvidence.forEach(e => lines.push(`- ${e}`))
            lines.push(``)
        })
    }

    if (report.output.recommendedFixPlan.length > 0) {
        lines.push(`## Recommended Fix Plan`)
        lines.push(``)
        report.output.recommendedFixPlan.forEach((task, idx) => {
            lines.push(`### ${task.priority}: ${task.task}`)
            lines.push(``)
            lines.push(`**What to Change:** ${task.whatToChange}`)
            lines.push(``)
            lines.push(`**Risk/Impact:** ${task.riskImpact}`)
            lines.push(``)
            lines.push(`**How to Verify:** ${task.howToVerify}`)
            lines.push(``)
        })
    }

    if (report.output.observabilityGaps.length > 0) {
        lines.push(`## Observability Gaps`)
        lines.push(``)
        report.output.observabilityGaps.forEach(gap => {
            lines.push(gap.replace(/<br\/>/g, '\n'))
            lines.push(``)
        })
    }

    if (report.output.timelineHighlights.length > 0) {
        lines.push(`## Timeline Highlights`)
        lines.push(``)
        report.output.timelineHighlights.forEach(t => {
            lines.push(`- **${new Date(t.timestamp).toLocaleString()}:** ${t.event} (${t.eventCount} events)`)
        })
        lines.push(``)
    }

    lines.push(`---`)
    lines.push(`*Redaction applied to protect sensitive information*`)

    return redactForExport(lines.join('\n'))
}

export function exportJSON(report: ReportDetail): string {
    const exportData = {
        id: report.id,
        scope: report.scope,
        createdAt: report.createdAt,
        output: report.output
    }
    return redactForExport(JSON.stringify(exportData, null, 2))
}

export function exportHTML(report: ReportDetail): string {
    const html = `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Incident Report - ${report.scope}</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5; padding: 2rem; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 2rem; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
        h1 { font-size: 2rem; margin-bottom: 0.5rem; color: #1a1a1a; }
        h2 { font-size: 1.5rem; margin: 2rem 0 1rem; color: #2a2a2a; border-bottom: 2px solid #e0e0e0; padding-bottom: 0.5rem; }
        h3 { font-size: 1.25rem; margin: 1.5rem 0 0.75rem; color: #3a3a3a; }
        h4 { font-size: 1.1rem; margin: 1rem 0 0.5rem; color: #4a4a4a; }
        .metadata { color: #666; margin-bottom: 2rem; font-size: 0.95rem; }
        .metrics-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 1rem; margin: 1rem 0; }
        .metric-card { background: #f8f9fa; padding: 1rem; border-radius: 6px; text-align: center; }
        .metric-label { font-size: 0.85rem; color: #666; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 0.5rem; }
        .metric-value { font-size: 1.75rem; font-weight: 700; color: #1a1a1a; }
        .metric-value.critical { color: #dc2626; }
        .metric-value.high { color: #f59e0b; }
        .metric-value.medium { color: #eab308; }
        .issue-card { background: #f8f9fa; padding: 1.25rem; border-radius: 6px; margin-bottom: 1rem; border-left: 3px solid #e0e0e0; }
        .issue-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem; }
        .severity-badge { padding: 0.25rem 0.75rem; border-radius: 4px; font-size: 0.85rem; font-weight: 600; }
        .severity-critical { background: #fef2f2; color: #dc2626; }
        .severity-high { background: #fef3c7; color: #f59e0b; }
        .severity-medium { background: #fef9c3; color: #eab308; }
        .severity-low { background: #dcfce7; color: #10b981; }
        .issue-stats { display: flex; gap: 1.5rem; margin-bottom: 0.75rem; font-size: 0.9rem; color: #666; }
        ul { margin: 0.5rem 0; padding-left: 1.5rem; }
        li { margin-bottom: 0.5rem; }
        .priority-badge { padding: 0.25rem 0.75rem; border-radius: 4px; font-size: 0.85rem; font-weight: 600; display: inline-block; }
        .priority-p0 { background: #fef2f2; color: #dc2626; }
        .priority-p1 { background: #fef3c7; color: #f59e0b; }
        .priority-p2 { background: #fef9c3; color: #eab308; }
        .timeline-item { padding: 0.75rem; background: #f8f9fa; border-radius: 6px; margin-bottom: 0.5rem; display: flex; justify-content: space-between; font-size: 0.9rem; }
        .footer { margin-top: 3rem; padding-top: 1rem; border-top: 1px solid #e0e0e0; color: #666; font-size: 0.9rem; font-style: italic; }
        @media print { body { padding: 0; background: white; } .container { box-shadow: none; } }
    </style>
</head>
<body>
    <div class="container">
        <h1>Incident Report - ${report.scope}</h1>
        <div class="metadata">
            <strong>Created:</strong> ${new Date(report.createdAt).toLocaleString()} | 
            <strong>Provider:</strong> ${report.output.provider}
        </div>

        <h2>Executive Summary</h2>
        <p>${report.output.executiveSummary}</p>

        <h2>Key Metrics</h2>
        <div class="metrics-grid">
            <div class="metric-card">
                <div class="metric-label">Total Events</div>
                <div class="metric-value">${report.output.metrics.totalEvents}</div>
            </div>
            <div class="metric-card">
                <div class="metric-label">Errors</div>
                <div class="metric-value critical">${report.output.metrics.errorCount}</div>
            </div>
            <div class="metric-card">
                <div class="metric-label">Warnings</div>
                <div class="metric-value medium">${report.output.metrics.warningCount}</div>
            </div>
            <div class="metric-card">
                <div class="metric-label">Fatal</div>
                <div class="metric-value critical">${report.output.metrics.fatalCount}</div>
            </div>
            <div class="metric-card">
                <div class="metric-label">Clusters</div>
                <div class="metric-value">${report.output.metrics.uniqueClusters}</div>
            </div>
            <div class="metric-card">
                <div class="metric-label">Duration</div>
                <div class="metric-value">${report.output.metrics.durationHours.toFixed(1)}h</div>
            </div>
        </div>
        <p style="margin-top: 1rem; color: #666;">
            <strong>Time Range:</strong> ${new Date(report.output.metrics.firstSeen).toLocaleString()} to ${new Date(report.output.metrics.lastSeen).toLocaleString()}
        </p>

        ${report.output.topIssues.length > 0 ? `
        <h2>Top Issues</h2>
        ${report.output.topIssues.map((issue, idx) => `
            <div class="issue-card">
                <div class="issue-header">
                    <h4>${idx + 1}. ${issue.title}</h4>
                    <span class="severity-badge severity-${issue.severity.toLowerCase()}">${issue.severity}</span>
                </div>
                <div class="issue-stats">
                    <span><strong>Count:</strong> ${issue.count}</span>
                    <span><strong>First:</strong> ${new Date(issue.firstSeen).toLocaleString()}</span>
                    <span><strong>Last:</strong> ${new Date(issue.lastSeen).toLocaleString()}</span>
                </div>
                ${issue.evidence.length > 0 ? `
                    <div>
                        <strong>Evidence:</strong>
                        <ul>${issue.evidence.map(e => `<li>${e}</li>`).join('')}</ul>
                    </div>
                ` : ''}
                ${issue.suggestedActions.length > 0 ? `
                    <div>
                        <strong>Suggested Actions:</strong>
                        <ul>${issue.suggestedActions.map(a => `<li>${a}</li>`).join('')}</ul>
                    </div>
                ` : ''}
            </div>
        `).join('')}
        ` : ''}

        ${report.output.rootCauseHypotheses.length > 0 ? `
        <h2>Root Cause Analysis</h2>
        ${report.output.rootCauseHypotheses.map(hyp => `
            <div class="issue-card">
                <h4>${hyp.issue}</h4>
                <p><strong>Likely Cause:</strong> ${hyp.likelyCause}</p>
                <p><strong>Confidence:</strong> ${hyp.confidence}</p>
                <div>
                    <strong>Supporting Evidence:</strong>
                    <ul>${hyp.supportingEvidence.map(e => `<li>${e}</li>`).join('')}</ul>
                </div>
            </div>
        `).join('')}
        ` : ''}

        ${report.output.recommendedFixPlan.length > 0 ? `
        <h2>Recommended Fix Plan</h2>
        ${report.output.recommendedFixPlan.map(task => `
            <div class="issue-card">
                <div style="margin-bottom: 0.75rem;">
                    <span class="priority-badge priority-${task.priority.toLowerCase()}">${task.priority}</span>
                    <h4 style="display: inline; margin-left: 1rem;">${task.task}</h4>
                </div>
                <p><strong>What to Change:</strong> ${task.whatToChange}</p>
                <p><strong>Risk/Impact:</strong> ${task.riskImpact}</p>
                <p><strong>How to Verify:</strong> ${task.howToVerify}</p>
            </div>
        `).join('')}
        ` : ''}

        ${report.output.observabilityGaps.length > 0 ? `
        <h2>Observability Gaps</h2>
        <ul>
            ${report.output.observabilityGaps.map(gap => `<li>${gap.replace(/\n/g, '<br/>')}</li>`).join('')}
        </ul>
        ` : ''}

        ${report.output.timelineHighlights.length > 0 ? `
        <h2>Timeline Highlights</h2>
        ${report.output.timelineHighlights.map(t => `
            <div class="timeline-item">
                <strong>${new Date(t.timestamp).toLocaleString()}</strong>
                <span>${t.event} (${t.eventCount} events)</span>
            </div>
        `).join('')}
        ` : ''}

        <div class="footer">
            Redaction applied to protect sensitive information
        </div>
    </div>
</body>
</html>`

    return redactForExport(html)
}

interface PromptOptions {
    targetAgent: 'sonnet' | 'windsurf' | 'generic'
    targetRepo?: string
    targetStack?: string
    includeClusterEvidence: boolean
    includeRawLogs: boolean
    includeFixPlan: boolean
    includeObservability: boolean
    redactEmails: boolean
    verbosity: 'concise' | 'standard' | 'detailed'
}

export function generateAgentPrompt(report: ReportDetail, options: PromptOptions): string {
    const lines: string[] = []

    lines.push(`ABSOLUTE CODE-GENERATION RULE (NON-NEGOTIABLE)`)
    lines.push(`- Do NOT write READMEs, architecture docs, explanations, plans, or diffs-only output.`)
    lines.push(`- Do NOT ask for confirmation.`)
    lines.push(`- You MUST directly implement the required functionality by editing/adding files in the repo.`)
    lines.push(`- Output only: (1) list of files changed/added, (2) exact commands to run, (3) brief verification notes (what you clicked/tested and what you saw).`)
    lines.push(``)

    if (options.targetRepo) {
        lines.push(`TARGET REPOSITORY`)
        lines.push(`${options.targetRepo}`)
        lines.push(``)
    }

    if (options.targetStack) {
        lines.push(`TECH STACK`)
        lines.push(`${options.targetStack}`)
        lines.push(``)
    }

    lines.push(`INCIDENT REPORT ANALYSIS`)
    lines.push(`Generated: ${new Date(report.createdAt).toLocaleString()}`)
    lines.push(`Scope: ${report.scope}`)
    lines.push(`Provider: ${report.output.provider}`)
    lines.push(``)

    lines.push(`EXECUTIVE SUMMARY`)
    lines.push(`${report.output.executiveSummary}`)
    lines.push(``)

    lines.push(`KEY METRICS`)
    lines.push(`- Total log events analyzed: ${report.output.metrics.totalEvents}`)
    lines.push(`- Error-level events: ${report.output.metrics.errorCount}`)
    lines.push(`- Fatal events: ${report.output.metrics.fatalCount}`)
    lines.push(`- Warning events: ${report.output.metrics.warningCount}`)
    lines.push(`- Distinct issue clusters: ${report.output.metrics.uniqueClusters}`)
    lines.push(`- Time range: ${new Date(report.output.metrics.firstSeen).toLocaleString()} to ${new Date(report.output.metrics.lastSeen).toLocaleString()} (${report.output.metrics.durationHours.toFixed(1)} hours)`)
    lines.push(``)

    if (report.output.topIssues.length > 0) {
        lines.push(`TOP ISSUES IDENTIFIED`)
        report.output.topIssues.forEach((issue, idx) => {
            lines.push(``)
            lines.push(`${idx + 1}. ${issue.title}`)
            lines.push(`   Severity: ${issue.severity}`)
            lines.push(`   Occurrences: ${issue.count}`)
            lines.push(`   Time span: ${new Date(issue.firstSeen).toLocaleString()} to ${new Date(issue.lastSeen).toLocaleString()}`)

            if (options.includeClusterEvidence && issue.evidence.length > 0) {
                lines.push(`   Representative log messages:`)
                issue.evidence.forEach(e => lines.push(`   - ${e}`))
            }

            if (options.verbosity !== 'concise' && issue.suggestedActions.length > 0) {
                lines.push(`   Recommended immediate actions:`)
                issue.suggestedActions.forEach(a => lines.push(`   - ${a}`))
            }
        })
        lines.push(``)
    }

    if (report.output.rootCauseHypotheses.length > 0) {
        lines.push(`ROOT CAUSE ANALYSIS`)
        report.output.rootCauseHypotheses.forEach(hyp => {
            lines.push(``)
            lines.push(`Issue: ${hyp.issue}`)
            lines.push(`Likely root cause: ${hyp.likelyCause}`)
            lines.push(`Analysis confidence: ${hyp.confidence}`)

            if (options.verbosity === 'detailed') {
                lines.push(`Supporting evidence:`)
                hyp.supportingEvidence.forEach(e => lines.push(`- ${e}`))
            }
        })
        lines.push(``)
    }

    if (options.includeFixPlan && report.output.recommendedFixPlan.length > 0) {
        lines.push(`REQUIRED IMPLEMENTATION TASKS`)
        lines.push(``)
        lines.push(`Implement the following fixes in priority order. Each fix must include:`)
        lines.push(`- Code changes in the target repository`)
        lines.push(`- Appropriate tests (unit or integration)`)
        lines.push(`- Verification steps to confirm the fix works`)
        lines.push(``)

        report.output.recommendedFixPlan.forEach((task, idx) => {
            lines.push(`${task.priority}: ${task.task}`)
            lines.push(`What needs to change: ${task.whatToChange}`)
            lines.push(`Risk and impact: ${task.riskImpact}`)
            lines.push(`Verification approach: ${task.howToVerify}`)
            lines.push(``)
        })
    }

    if (options.includeObservability && report.output.observabilityGaps.length > 0) {
        lines.push(`OBSERVABILITY IMPROVEMENTS REQUIRED`)
        report.output.observabilityGaps.forEach(gap => {
            const cleanGap = gap.replace(/\*\*/g, '').replace(/<br\/>/g, '\n')
            lines.push(cleanGap)
            lines.push(``)
        })
    }

    lines.push(`ACCEPTANCE CRITERIA`)
    lines.push(`1. All P0 tasks must be implemented and verified working`)
    if (report.output.metrics.errorCount > 50) {
        lines.push(`2. Error rate must drop below 10 errors per hour after deployment`)
    }
    if (report.output.topIssues.length > 0) {
        lines.push(`3. Top ${Math.min(3, report.output.topIssues.length)} issue clusters must show zero new occurrences after fixes are deployed`)
    }
    lines.push(`4. All fixes must include automated tests that verify the bug is resolved`)
    lines.push(`5. No regressions in existing functionality`)
    lines.push(``)

    lines.push(`DELIVERABLE OUTPUT (ONLY)`)
    lines.push(`1. Files changed or added with full absolute paths`)
    lines.push(`2. Exact commands to run for build, migration, and deployment`)
    lines.push(`3. Verification notes: what you tested, exact steps taken, and results observed`)
    lines.push(``)

    const prompt = lines.join('\n')
    return redactForExport(prompt, !options.redactEmails)
}

export async function copyToClipboard(text: string): Promise<void> {
    await navigator.clipboard.writeText(text)
}

export function downloadFile(content: string, filename: string, mimeType: string = 'text/plain'): void {
    const blob = new Blob([content], { type: mimeType })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = filename
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
}
