import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import styles from './PagesCommon.module.css'
import ReportActionsBar from '../components/reports/ReportActionsBar'
import AgentPromptBuilderModal from '../components/reports/AgentPromptBuilderModal'

interface IncidentReport {
    id: string
    scope: string
    createdAt: string
    summary: string
}

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

export default function ReportsPage() {
    const navigate = useNavigate()
    const [reports, setReports] = useState<IncidentReport[]>([])
    const [loading, setLoading] = useState(true)
    const [generating, setGenerating] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [selectedReport, setSelectedReport] = useState<ReportDetail | null>(null)
    const [showModal, setShowModal] = useState(false)
    const [loadingDetail, setLoadingDetail] = useState(false)
    const [showPromptBuilder, setShowPromptBuilder] = useState(false)

    useEffect(() => {
        fetchReports()
    }, [])

    const fetchReports = async () => {
        try {
            setLoading(true)
            setError(null)
            const token = localStorage.getItem('accessToken')
            const response = await fetch('http://localhost:5000/api/reports?page=1&pageSize=50', {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            })

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`)
            }

            const data = await response.json()
            setReports(data.items || [])
        } catch (err) {
            console.error('Error fetching reports:', err)
            setError(err instanceof Error ? err.message : 'Failed to fetch reports')
        } finally {
            setLoading(false)
        }
    }

    const generateReport = async () => {
        try {
            setGenerating(true)
            setError(null)
            const token = localStorage.getItem('accessToken')
            
            const now = new Date()
            const oneHourAgo = new Date(now.getTime() - 60 * 60 * 1000)
            
            const response = await fetch('http://localhost:5000/api/reports/generate', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    scope: 'TimeRange',
                    timeRangeStart: oneHourAgo.toISOString(),
                    timeRangeEnd: now.toISOString()
                })
            })

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`)
            }

            await fetchReports()
        } catch (err) {
            console.error('Error generating report:', err)
            setError(err instanceof Error ? err.message : 'Failed to generate report')
        } finally {
            setGenerating(false)
        }
    }

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleString()
    }

    const truncateSummary = (summary: string, maxLength: number = 100) => {
        if (summary.length <= maxLength) return summary
        return summary.substring(0, maxLength) + '...'
    }

    const handleViewReport = async (report: IncidentReport) => {
        try {
            setLoadingDetail(true)
            const token = localStorage.getItem('accessToken')
            const response = await fetch(`http://localhost:5000/api/reports/${report.id}`, {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            })

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`)
            }

            const detail = await response.json()
            setSelectedReport(detail)
            setShowModal(true)
        } catch (err) {
            console.error('Error fetching report detail:', err)
            setError(err instanceof Error ? err.message : 'Failed to fetch report detail')
        } finally {
            setLoadingDetail(false)
        }
    }

    const closeModal = () => {
        setShowModal(false)
        setSelectedReport(null)
    }

    const handleClusterClick = (clusterId: string) => {
        navigate('/explore/clusters')
        closeModal()
    }

    const handleOpenPromptBuilder = () => {
        setShowPromptBuilder(true)
    }

    const closePromptBuilder = () => {
        setShowPromptBuilder(false)
    }

    const getSeverityClass = (severity: string) => {
        switch (severity.toLowerCase()) {
            case 'critical': return styles.critical
            case 'high': return styles.high
            case 'medium': return styles.medium
            case 'low': return styles.low
            default: return ''
        }
    }

    const getPriorityClass = (priority: string) => {
        if (priority === 'P0') return styles.critical
        if (priority === 'P1') return styles.high
        if (priority === 'P2') return styles.medium
        return styles.low
    }

    return (
        <div className={styles.container}>
            <h1>Incident Reports</h1>
            <p className={styles.subtitle}>AI-generated incident analysis</p>

            <button 
                className={styles.primaryBtn} 
                onClick={generateReport}
                disabled={generating}
            >
                {generating ? 'Generating...' : 'Generate New Report'}
            </button>

            {error && (
                <div className={styles.error}>
                    Error: {error}
                </div>
            )}

            <div className={styles.tableCard}>
                <table className={styles.table}>
                    <thead>
                        <tr>
                            <th>Scope</th>
                            <th>Summary</th>
                            <th>Created</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {loading ? (
                            <tr>
                                <td colSpan={4} className={styles.empty}>Loading reports...</td>
                            </tr>
                        ) : reports.length === 0 ? (
                            <tr>
                                <td colSpan={4} className={styles.empty}>
                                    {error ? 'Error loading reports' : 'No reports found. Click "Generate New Report" to create one.'}
                                </td>
                            </tr>
                        ) : (
                            reports.map(report => (
                                <tr key={report.id}>
                                    <td>{report.scope}</td>
                                    <td>{truncateSummary(report.summary)}</td>
                                    <td>{formatDate(report.createdAt)}</td>
                                    <td>
                                        <button 
                                            className={styles.secondaryBtn}
                                            onClick={() => handleViewReport(report)}
                                        >
                                            View
                                        </button>
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>

            {showModal && selectedReport && (
                <div className={styles.modal} onClick={closeModal}>
                    <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
                        <div className={styles.modalHeader}>
                            <h2>Incident Report - {selectedReport.scope}</h2>
                            <button className={styles.closeBtn} onClick={closeModal}>&times;</button>
                        </div>
                        <div className={styles.modalBody}>
                            <p><strong>Created:</strong> {formatDate(selectedReport.createdAt)} | <strong>Provider:</strong> {selectedReport.output.provider}</p>
                            
                            <ReportActionsBar 
                                report={selectedReport}
                                onOpenPromptBuilder={handleOpenPromptBuilder}
                            />
                            
                            <div className={styles.reportSection}>
                                <h3>Executive Summary</h3>
                                <p>{selectedReport.output.executiveSummary}</p>
                            </div>

                            <div className={styles.reportSection}>
                                <h3>Key Metrics</h3>
                                <div className={styles.metricsGrid}>
                                    <div className={styles.metricCard}>
                                        <div className={styles.metricLabel}>Total Events</div>
                                        <div className={styles.metricValue}>{selectedReport.output.metrics.totalEvents}</div>
                                    </div>
                                    <div className={styles.metricCard}>
                                        <div className={styles.metricLabel}>Errors</div>
                                        <div className={`${styles.metricValue} ${styles.critical}`}>{selectedReport.output.metrics.errorCount}</div>
                                    </div>
                                    <div className={styles.metricCard}>
                                        <div className={styles.metricLabel}>Warnings</div>
                                        <div className={`${styles.metricValue} ${styles.medium}`}>{selectedReport.output.metrics.warningCount}</div>
                                    </div>
                                    <div className={styles.metricCard}>
                                        <div className={styles.metricLabel}>Fatal</div>
                                        <div className={`${styles.metricValue} ${styles.critical}`}>{selectedReport.output.metrics.fatalCount}</div>
                                    </div>
                                    <div className={styles.metricCard}>
                                        <div className={styles.metricLabel}>Clusters</div>
                                        <div className={styles.metricValue}>{selectedReport.output.metrics.uniqueClusters}</div>
                                    </div>
                                    <div className={styles.metricCard}>
                                        <div className={styles.metricLabel}>Duration</div>
                                        <div className={styles.metricValue}>{selectedReport.output.metrics.durationHours.toFixed(1)}h</div>
                                    </div>
                                </div>
                                <p className={styles.timeRange}>
                                    <strong>Time Range:</strong> {new Date(selectedReport.output.metrics.firstSeen).toLocaleString()} to {new Date(selectedReport.output.metrics.lastSeen).toLocaleString()}
                                </p>
                            </div>

                            {selectedReport.output.topIssues.length > 0 && (
                                <div className={styles.reportSection}>
                                    <h3>Top Issues</h3>
                                    {selectedReport.output.topIssues.map((issue, idx) => (
                                        <div key={idx} className={styles.issueCard}>
                                            <div className={styles.issueHeader}>
                                                <h4>{issue.title}</h4>
                                                <span className={getSeverityClass(issue.severity)}>{issue.severity}</span>
                                            </div>
                                            <div className={styles.issueStats}>
                                                <span><strong>Count:</strong> {issue.count}</span>
                                                <span><strong>First:</strong> {new Date(issue.firstSeen).toLocaleString()}</span>
                                                <span><strong>Last:</strong> {new Date(issue.lastSeen).toLocaleString()}</span>
                                            </div>
                                            {issue.evidence.length > 0 && (
                                                <div className={styles.evidence}>
                                                    <strong>Evidence:</strong>
                                                    <ul>
                                                        {issue.evidence.map((e, i) => <li key={i}>{e}</li>)}
                                                    </ul>
                                                </div>
                                            )}
                                            {issue.suggestedActions.length > 0 && (
                                                <div className={styles.actions}>
                                                    <strong>Suggested Actions:</strong>
                                                    <ul>
                                                        {issue.suggestedActions.map((a, i) => <li key={i}>{a}</li>)}
                                                    </ul>
                                                </div>
                                            )}
                                            {issue.clusterId && (
                                                <button 
                                                    className={styles.secondaryBtn}
                                                    onClick={() => handleClusterClick(issue.clusterId!)}
                                                    style={{marginTop: '0.5rem'}}
                                                >
                                                    View Cluster Details
                                                </button>
                                            )}
                                        </div>
                                    ))}
                                </div>
                            )}

                            {selectedReport.output.rootCauseHypotheses.length > 0 && (
                                <div className={styles.reportSection}>
                                    <h3>Root Cause Analysis</h3>
                                    {selectedReport.output.rootCauseHypotheses.map((hyp, idx) => (
                                        <div key={idx} className={styles.hypothesisCard}>
                                            <h4>{hyp.issue}</h4>
                                            <p><strong>Likely Cause:</strong> {hyp.likelyCause}</p>
                                            <p><strong>Confidence:</strong> <span className={hyp.confidence === 'High' ? styles.high : styles.medium}>{hyp.confidence}</span></p>
                                            <div className={styles.evidence}>
                                                <strong>Supporting Evidence:</strong>
                                                <ul>
                                                    {hyp.supportingEvidence.map((e, i) => <li key={i}>{e}</li>)}
                                                </ul>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}

                            {selectedReport.output.recommendedFixPlan.length > 0 && (
                                <div className={styles.reportSection}>
                                    <h3>Recommended Fix Plan</h3>
                                    {selectedReport.output.recommendedFixPlan.map((task, idx) => (
                                        <div key={idx} className={styles.fixTaskCard}>
                                            <div className={styles.taskHeader}>
                                                <span className={getPriorityClass(task.priority)}>{task.priority}</span>
                                                <h4>{task.task}</h4>
                                            </div>
                                            <p><strong>What to Change:</strong> {task.whatToChange}</p>
                                            <p><strong>Risk/Impact:</strong> {task.riskImpact}</p>
                                            <p><strong>How to Verify:</strong> {task.howToVerify}</p>
                                        </div>
                                    ))}
                                </div>
                            )}

                            {selectedReport.output.observabilityGaps.length > 0 && (
                                <div className={styles.reportSection}>
                                    <h3>Observability Gaps</h3>
                                    <ul className={styles.gapsList}>
                                        {selectedReport.output.observabilityGaps.map((gap, idx) => (
                                            <li key={idx} dangerouslySetInnerHTML={{ __html: gap.replace(/\n/g, '<br/>') }} />
                                        ))}
                                    </ul>
                                </div>
                            )}

                            {selectedReport.output.timelineHighlights.length > 0 && (
                                <div className={styles.reportSection}>
                                    <h3>Timeline Highlights</h3>
                                    <div className={styles.timeline}>
                                        {selectedReport.output.timelineHighlights.map((t, idx) => (
                                            <div key={idx} className={styles.timelineItem}>
                                                <strong>{new Date(t.timestamp).toLocaleString()}</strong>
                                                <span>{t.event} ({t.eventCount} events)</span>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            )}

            {showPromptBuilder && selectedReport && (
                <AgentPromptBuilderModal
                    report={selectedReport}
                    onClose={closePromptBuilder}
                />
            )}
        </div>
    )
}
