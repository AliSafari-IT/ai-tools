import { useRef, useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '@/lib/api'
import styles from './DashboardPage.module.css'

export default function DashboardPage() {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const navigate = useNavigate()
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [uploadSuccess, setUploadSuccess] = useState<string | null>(null)
  const [sessionId, setSessionId] = useState<string | null>(null)
  const [processingStatus, setProcessingStatus] = useState<any>(null)

  const handleFileUpload = async (files: FileList | null) => {
    if (!files || files.length === 0) return

    setIsUploading(true)
    setUploadError(null)
    setUploadSuccess(null)

    try {
      const formData = new FormData()
      for (let i = 0; i < files.length; i++) {
        formData.append('files', files[i])
      }

      const response = await api.post('/logs/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      })

      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }

      setSessionId(response.data.sessionId)
      setUploadSuccess(`Successfully uploaded ${response.data.count} file(s). Processing...`)
    } catch (error) {
      setUploadError(error instanceof Error ? error.message : 'Upload failed')
    } finally {
      setIsUploading(false)
    }
  }

  const handleDropzoneClick = () => {
    fileInputRef.current?.click()
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    handleFileUpload(e.dataTransfer.files)
  }

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    handleFileUpload(e.target.files)
  }

  useEffect(() => {
    if (!sessionId) return

    const checkStatus = async () => {
      try {
        const response = await api.get(`/logs/status/${sessionId}`)
        setProcessingStatus(response.data)

        if (response.data.isComplete) {
          setUploadSuccess(`Processing complete! ${response.data.totalEvents} log events imported.`)
          setTimeout(() => {
            navigate('/explore/logs')
          }, 2000)
        }
      } catch (error) {
        console.error('Failed to check status:', error)
      }
    }

    checkStatus()
    const interval = setInterval(checkStatus, 2000)

    return () => clearInterval(interval)
  }, [sessionId, navigate])

  return (
    <div className={styles.container}>
      <h1>Dashboard</h1>
      <p className={styles.subtitle}>Upload and manage your log files</p>

      <div className={styles.uploadCard}>
        <h2>Upload Logs</h2>
        <div
          className={styles.dropzone}
          onClick={handleDropzoneClick}
          onDragOver={handleDragOver}
          onDrop={handleDrop}
        >
          <p>Drag and drop log files here, or click to browse</p>
          <p className={styles.hint}>Supported: Serilog JSON, Nginx Access/Error logs</p>
          <input
            ref={fileInputRef}
            type="file"
            accept=".log,.json,.txt"
            multiple
            style={{ display: 'none' }}
            onChange={handleFileInputChange}
            disabled={isUploading}
          />
        </div>
        {isUploading && <p style={{ marginTop: '1rem', textAlign: 'center' }}>Uploading...</p>}
        {uploadSuccess && (
          <div style={{ marginTop: '1rem', textAlign: 'center' }}>
            <p style={{ color: 'var(--asm-color-semantic-success)', marginBottom: '0.5rem' }}>
              {uploadSuccess}
            </p>
            {processingStatus && !processingStatus.isComplete && (
              <p style={{ fontSize: '0.9rem', color: 'var(--asm-color-text-secondary)' }}>
                Processing: {processingStatus.completedJobs}/{processingStatus.totalJobs} files
                {processingStatus.totalEvents > 0 && ` • ${processingStatus.totalEvents} events imported`}
              </p>
            )}
          </div>
        )}
        {uploadError && (
          <p style={{ marginTop: '1rem', textAlign: 'center', color: 'var(--asm-color-semantic-error)' }}>
            Error: {uploadError}
          </p>
        )}
      </div>

      <div className={styles.stats}>
        <div className={styles.stat}>
          <h3>Total Sessions</h3>
          <p className={styles.statValue}>0</p>
        </div>
        <div className={styles.stat}>
          <h3>Log Events</h3>
          <p className={styles.statValue}>0</p>
        </div>
        <div className={styles.stat}>
          <h3>Active Clusters</h3>
          <p className={styles.statValue}>0</p>
        </div>
        <div className={styles.stat}>
          <h3>Reports</h3>
          <p className={styles.statValue}>0</p>
        </div>
      </div>
    </div>
  )
}
