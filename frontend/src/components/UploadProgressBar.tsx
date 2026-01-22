import React from 'react'
import { useUploadProgress } from '../contexts/UploadProgressContext'
import styles from './UploadProgressBar.module.css'

export const UploadProgressBar: React.FC = () => {
  const { progress } = useUploadProgress()

  if (progress.phase === 'idle') {
    return null
  }

  const formatTimestamp = (date: Date | null) => {
    if (!date) return 'N/A'
    return date.toLocaleTimeString()
  }

  return (
    <div className={styles.container}>
      {progress.phase === 'uploading' && (
        <div className={styles.progressSection}>
          <div className={styles.label}>
            <span>Uploading {progress.fileName}</span>
            <span className={styles.percentage}>{Math.round(progress.uploadProgress)}%</span>
          </div>
          <div className={styles.progressBarWrapper}>
            <div className={styles.progressBarFill} style={{ width: `${progress.uploadProgress}%` }} />
          </div>
        </div>
      )}

      {(progress.phase === 'processing' || progress.phase === 'completed') && (
        <div className={styles.progressSection}>
          <div className={styles.label}>
            <span>{progress.phase === 'completed' ? 'Processing complete' : 'Processing logs'}</span>
            <span className={styles.percentage}>{Math.round(progress.processingProgress)}%</span>
          </div>
          <div className={styles.progressBarWrapper}>
            <div className={styles.progressBarFill} style={{ width: `${progress.processingProgress}%` }} />
          </div>
          <div className={styles.diagnostics}>
            <span>Status: {progress.phase}</span>
            <span>Lines: {progress.processedLines.toLocaleString()} / {progress.totalLines.toLocaleString()}</span>
            <span>Last update: {formatTimestamp(progress.lastUpdate)}</span>
          </div>
        </div>
      )}

      {progress.phase === 'error' && (
        <div className={styles.error}>
          Error: {progress.errorMessage}
        </div>
      )}
    </div>
  )
}
