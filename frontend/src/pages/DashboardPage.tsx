import { useRef, useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '@/lib/api'
import styles from './DashboardPage.module.css'
import { UploadProgressBar } from '@/components/UploadProgressBar'
import { useUploadProgress } from '@/contexts/UploadProgressContext'
import { CircularProgress } from '@asafarim/progress-bars'

export default function DashboardPage() {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const navigate = useNavigate()
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [uploadSuccess, setUploadSuccess] = useState<string | null>(null)
  const [sessionId, setSessionId] = useState<string | null>(null)
  const [processingStatus, setProcessingStatus] = useState<any>(null)
  const { progress, startUpload, setUploadProgress, completeUpload, startProcessing, setProcessingProgress, completeProcessing, updateProcessingStats, setError, reset } = useUploadProgress()

  const handleFileUpload = async (files: FileList | null) => {
    if (!files || files.length === 0) return

    setIsUploading(true)
    setUploadError(null)
    setUploadSuccess(null)

    const fileName = files.length === 1 ? files[0].name : `${files.length} files`
    let totalSize = 0
    for (let i = 0; i < files.length; i++) {
      totalSize += files[i].size
    }

    try {
      startUpload('', fileName)

      const formData = new FormData()
      for (let i = 0; i < files.length; i++) {
        formData.append('files', files[i])
      }

      const response = await api.post('/logs/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
          const percentCompleted = Math.round(
            (progressEvent.loaded * 100) / (progressEvent.total || totalSize)
          )
          setUploadProgress(percentCompleted)
        },
      })

      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }

      setSessionId(response.data.sessionId)
      completeUpload()
      startProcessing()
      setUploadSuccess(`Successfully uploaded ${response.data.count} file(s). Processing...`)
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : 'Upload failed'
      setError(errorMsg)
      setUploadError(errorMsg)
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
        <UploadProgressBar key={sessionId || 'initial'} />
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
            <p style={{ color: 'var(--asm-color-semantic-success)', marginBottom: '1rem' }}>
              {uploadSuccess}
            </p>
            {processingStatus && !processingStatus.isComplete && (
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '1.5rem', marginTop: '1rem' }}>
                <div>
                  <CircularProgress
                    value={processingStatus.totalEvents > 0 ? (processingStatus.totalEvents / (processingStatus.totalEvents + 1000)) * 100 : 0}
                    size={100}
                    thickness={6}
                    tone="success"
                    showLabel
                    formatValue={(v) => `${Math.round(v)}%`}
                  />
                </div>
                <div style={{ textAlign: 'left' }}>
                  <p style={{ fontSize: '0.9rem', color: 'var(--asm-color-text-muted)', margin: '0 0 0.5rem 0' }}>
                    Processing: {processingStatus.completedJobs}/{processingStatus.totalJobs} files
                  </p>
                  <p style={{ fontSize: '1rem', color: 'var(--asm-color-text)', fontWeight: '600', margin: '0' }}>
                    {processingStatus.totalEvents} events imported
                  </p>
                </div>
              </div>
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
