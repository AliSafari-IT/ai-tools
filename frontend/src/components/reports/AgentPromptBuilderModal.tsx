import { useState } from 'react'
import styles from './ReportActions.module.css'
import modalStyles from '../../pages/PagesCommon.module.css'
import { generateAgentPrompt, copyToClipboard, downloadFile } from '../../utils/reportExport'

interface ReportDetail {
    id: string
    scope: string
    createdAt: string
    output: any
}

interface Props {
    report: ReportDetail
    onClose: () => void
}

export default function AgentPromptBuilderModal({ report, onClose }: Props) {
    const [targetAgent, setTargetAgent] = useState<'sonnet' | 'windsurf' | 'generic'>('sonnet')
    const [targetRepo, setTargetRepo] = useState('')
    const [targetStack, setTargetStack] = useState('.NET + React + PostgreSQL')
    const [includeClusterEvidence, setIncludeClusterEvidence] = useState(true)
    const [includeRawLogs, setIncludeRawLogs] = useState(false)
    const [includeFixPlan, setIncludeFixPlan] = useState(true)
    const [includeObservability, setIncludeObservability] = useState(true)
    const [redactEmails, setRedactEmails] = useState(true)
    const [verbosity, setVerbosity] = useState<'concise' | 'standard' | 'detailed'>('standard')
    const [generatedPrompt, setGeneratedPrompt] = useState<string | null>(null)
    const [copying, setCopying] = useState(false)
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null)

    const showToast = (message: string, type: 'success' | 'error' = 'success') => {
        setToast({ message, type })
        setTimeout(() => setToast(null), 3000)
    }

    const handleGenerate = () => {
        const prompt = generateAgentPrompt(report, {
            targetAgent,
            targetRepo,
            targetStack,
            includeClusterEvidence,
            includeRawLogs,
            includeFixPlan,
            includeObservability,
            redactEmails,
            verbosity
        })
        setGeneratedPrompt(prompt)
        showToast('Prompt generated')
    }

    const handleCopy = async () => {
        if (!generatedPrompt) return
        try {
            setCopying(true)
            await copyToClipboard(generatedPrompt)
            showToast('Prompt copied to clipboard')
        } catch (err) {
            showToast('Failed to copy prompt', 'error')
        } finally {
            setCopying(false)
        }
    }

    const handleDownload = () => {
        if (!generatedPrompt) return
        try {
            const filename = `ai-agent-prompt-${report.id.substring(0, 8)}-${Date.now()}.txt`
            downloadFile(generatedPrompt, filename, 'text/plain')
            showToast('Prompt downloaded')
        } catch (err) {
            showToast('Failed to download prompt', 'error')
        }
    }

    return (
        <div className={modalStyles.modal} onClick={onClose}>
            <div className={styles.promptModal} onClick={(e) => e.stopPropagation()}>
                <div className={styles.promptModalHeader}>
                    <h2>AI Agent Prompt Builder</h2>
                </div>
                <div className={styles.promptModalBody}>
                    <div className={styles.formGroup}>
                        <label>Target Agent</label>
                        <select value={targetAgent} onChange={(e) => setTargetAgent(e.target.value as any)}>
                            <option value="sonnet">Claude Sonnet</option>
                            <option value="windsurf">Windsurf</option>
                            <option value="generic">Generic</option>
                        </select>
                    </div>

                    <div className={styles.formGroup}>
                        <label>Target Repository Path (optional)</label>
                        <input
                            type="text"
                            value={targetRepo}
                            onChange={(e) => setTargetRepo(e.target.value)}
                            placeholder="e.g., /path/to/repo or ProjectName"
                        />
                    </div>

                    <div className={styles.formGroup}>
                        <label>Target Stack</label>
                        <input
                            type="text"
                            value={targetStack}
                            onChange={(e) => setTargetStack(e.target.value)}
                            placeholder="e.g., .NET + React + PostgreSQL"
                        />
                    </div>

                    <div className={styles.formGroup}>
                        <label>Verbosity</label>
                        <select value={verbosity} onChange={(e) => setVerbosity(e.target.value as any)}>
                            <option value="concise">Concise</option>
                            <option value="standard">Standard</option>
                            <option value="detailed">Detailed</option>
                        </select>
                    </div>

                    <div className={styles.formGroup}>
                        <label>Options</label>
                        <div className={styles.toggleGroup}>
                            <div className={styles.toggleItem}>
                                <input
                                    type="checkbox"
                                    id="includeEvidence"
                                    checked={includeClusterEvidence}
                                    onChange={(e) => setIncludeClusterEvidence(e.target.checked)}
                                />
                                <label htmlFor="includeEvidence">Include cluster evidence lines</label>
                            </div>
                            <div className={styles.toggleItem}>
                                <input
                                    type="checkbox"
                                    id="includeRawLogs"
                                    checked={includeRawLogs}
                                    onChange={(e) => setIncludeRawLogs(e.target.checked)}
                                />
                                <label htmlFor="includeRawLogs">Include raw log samples</label>
                            </div>
                            <div className={styles.toggleItem}>
                                <input
                                    type="checkbox"
                                    id="includeFixPlan"
                                    checked={includeFixPlan}
                                    onChange={(e) => setIncludeFixPlan(e.target.checked)}
                                />
                                <label htmlFor="includeFixPlan">Include fix plan</label>
                            </div>
                            <div className={styles.toggleItem}>
                                <input
                                    type="checkbox"
                                    id="includeObservability"
                                    checked={includeObservability}
                                    onChange={(e) => setIncludeObservability(e.target.checked)}
                                />
                                <label htmlFor="includeObservability">Include observability improvements</label>
                            </div>
                            <div className={styles.toggleItem}>
                                <input
                                    type="checkbox"
                                    id="redactEmails"
                                    checked={redactEmails}
                                    onChange={(e) => setRedactEmails(e.target.checked)}
                                />
                                <label htmlFor="redactEmails">Redact email addresses</label>
                            </div>
                        </div>
                    </div>

                    {generatedPrompt && (
                        <div className={styles.formGroup}>
                            <label>Generated Prompt Preview</label>
                            <div className={styles.promptPreview}>
                                {generatedPrompt.substring(0, 500)}
                                {generatedPrompt.length > 500 && '...\n\n[Preview truncated - full prompt will be copied/downloaded]'}
                            </div>
                        </div>
                    )}

                    <div className={styles.promptActions}>
                        <button onClick={onClose} className={modalStyles.secondaryBtn}>
                            Cancel
                        </button>
                        <button onClick={handleGenerate} className={modalStyles.secondaryBtn}>
                            {generatedPrompt ? 'Regenerate' : 'Generate Prompt'}
                        </button>
                        {generatedPrompt && (
                            <>
                                <button 
                                    onClick={handleCopy} 
                                    disabled={copying}
                                    className={modalStyles.secondaryBtn}
                                >
                                    {copying ? 'Copying...' : 'Copy to Clipboard'}
                                </button>
                                <button onClick={handleDownload} className={modalStyles.secondaryBtn}>
                                    Download .txt
                                </button>
                            </>
                        )}
                    </div>
                </div>
            </div>

            {toast && (
                <div className={`${styles.toast} ${styles[toast.type]}`}>
                    {toast.message}
                </div>
            )}
        </div>
    )
}
