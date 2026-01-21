import { useState } from 'react'
import styles from './ReportActions.module.css'
import { 
    exportSummary, 
    exportMarkdown, 
    exportJSON, 
    exportHTML,
    copyToClipboard,
    downloadFile
} from '../../utils/reportExport'

interface ReportDetail {
    id: string
    scope: string
    createdAt: string
    output: any
}

interface Props {
    report: ReportDetail
    onOpenPromptBuilder: () => void
}

export default function ReportActionsBar({ report, onOpenPromptBuilder }: Props) {
    const [copying, setCopying] = useState<string | null>(null)
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null)

    const showToast = (message: string, type: 'success' | 'error' = 'success') => {
        setToast({ message, type })
        setTimeout(() => setToast(null), 3000)
    }

    const handleCopySummary = async () => {
        try {
            setCopying('summary')
            const content = exportSummary(report)
            await copyToClipboard(content)
            showToast('Summary copied to clipboard')
        } catch (err) {
            showToast('Failed to copy summary', 'error')
        } finally {
            setCopying(null)
        }
    }

    const handleCopyMarkdown = async () => {
        try {
            setCopying('markdown')
            const content = exportMarkdown(report)
            await copyToClipboard(content)
            showToast('Report copied as Markdown')
        } catch (err) {
            showToast('Failed to copy Markdown', 'error')
        } finally {
            setCopying(null)
        }
    }

    const handleCopyJSON = async () => {
        try {
            setCopying('json')
            const content = exportJSON(report)
            await copyToClipboard(content)
            showToast('Report copied as JSON')
        } catch (err) {
            showToast('Failed to copy JSON', 'error')
        } finally {
            setCopying(null)
        }
    }

    const handleDownloadMarkdown = () => {
        try {
            const content = exportMarkdown(report)
            const filename = `incident-report-${report.id.substring(0, 8)}-${Date.now()}.md`
            downloadFile(content, filename, 'text/markdown')
            showToast('Report downloaded as Markdown')
        } catch (err) {
            showToast('Failed to download Markdown', 'error')
        }
    }

    const handleDownloadJSON = () => {
        try {
            const content = exportJSON(report)
            const filename = `incident-report-${report.id.substring(0, 8)}-${Date.now()}.json`
            downloadFile(content, filename, 'application/json')
            showToast('Report downloaded as JSON')
        } catch (err) {
            showToast('Failed to download JSON', 'error')
        }
    }

    const handleDownloadHTML = () => {
        try {
            const content = exportHTML(report)
            const filename = `incident-report-${report.id.substring(0, 8)}-${Date.now()}.html`
            downloadFile(content, filename, 'text/html')
            showToast('Report downloaded as HTML')
        } catch (err) {
            showToast('Failed to download HTML', 'error')
        }
    }

    const handleOpenInNewTab = () => {
        try {
            const content = exportHTML(report)
            const blob = new Blob([content], { type: 'text/html' })
            const url = URL.createObjectURL(blob)
            window.open(url, '_blank')
            showToast('Report opened in new tab')
        } catch (err) {
            showToast('Failed to open in new tab', 'error')
        }
    }

    const handlePrint = () => {
        try {
            const content = exportHTML(report)
            const iframe = document.createElement('iframe')
            iframe.style.display = 'none'
            document.body.appendChild(iframe)
            const doc = iframe.contentWindow?.document
            if (doc) {
                doc.open()
                doc.write(content)
                doc.close()
                iframe.contentWindow?.print()
                setTimeout(() => document.body.removeChild(iframe), 1000)
                showToast('Print dialog opened')
            }
        } catch (err) {
            showToast('Failed to print', 'error')
        }
    }

    return (
        <div className={styles.actionsBar}>
            <div className={styles.actionGroup}>
                <h4>Clipboard</h4>
                <button 
                    onClick={handleCopySummary}
                    disabled={copying === 'summary'}
                    className={styles.actionBtn}
                >
                    {copying === 'summary' ? 'Copying...' : 'Copy Summary'}
                </button>
                <button 
                    onClick={handleCopyMarkdown}
                    disabled={copying === 'markdown'}
                    className={styles.actionBtn}
                >
                    {copying === 'markdown' ? 'Copying...' : 'Copy Markdown'}
                </button>
                <button 
                    onClick={handleCopyJSON}
                    disabled={copying === 'json'}
                    className={styles.actionBtn}
                >
                    {copying === 'json' ? 'Copying...' : 'Copy JSON'}
                </button>
            </div>

            <div className={styles.actionGroup}>
                <h4>Download</h4>
                <button onClick={handleDownloadMarkdown} className={styles.actionBtn}>
                    Download .md
                </button>
                <button onClick={handleDownloadJSON} className={styles.actionBtn}>
                    Download .json
                </button>
                <button onClick={handleDownloadHTML} className={styles.actionBtn}>
                    Download .html
                </button>
            </div>

            <div className={styles.actionGroup}>
                <h4>Tools</h4>
                <button onClick={onOpenPromptBuilder} className={`${styles.actionBtn} ${styles.primary}`}>
                    AI Agent Prompt...
                </button>
                <button onClick={handleOpenInNewTab} className={styles.actionBtn}>
                    Open in New Tab
                </button>
                <button onClick={handlePrint} className={styles.actionBtn}>
                    Print
                </button>
            </div>

            {toast && (
                <div className={`${styles.toast} ${styles[toast.type]}`}>
                    {toast.message}
                </div>
            )}
        </div>
    )
}
