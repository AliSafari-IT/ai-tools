import { useEffect, useState } from 'react'
import styles from './PagesCommon.module.css'

interface RequestTrace {
  id: string
  traceId: string | null
  correlationId: string | null
  startTime: string
  endTime: string
  durationMs: number
  eventCount: number
  errorCount: number
  warningCount: number
}

export default function TracesPage() {
  const [traces, setTraces] = useState<RequestTrace[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchTraces()
  }, [])

  const fetchTraces = async () => {
    try {
      setLoading(true)
      setError(null)
      const token = localStorage.getItem('accessToken')
      const response = await fetch('http://localhost:5000/api/traces?page=1&pageSize=50', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      })

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`)
      }

      const data = await response.json()
      setTraces(data.items || [])
    } catch (err) {
      console.error('Error fetching traces:', err)
      setError(err instanceof Error ? err.message : 'Failed to fetch traces')
    } finally {
      setLoading(false)
    }
  }

  const formatDate = (dateString: string) => new Date(dateString).toLocaleString()

  const formatDuration = (ms: number) => {
    if (ms < 1000) return `${ms.toFixed(0)}ms`
    return `${(ms / 1000).toFixed(2)}s`
  }

  return (
    <div className={styles.container}>
      <h1>Request Traces</h1>
      <p className={styles.subtitle}>View correlated log events</p>

      {error && (
        <div className={styles.error}>
          Error: {error}
        </div>
      )}

      <div className={styles.tableCard}>
        <div className={styles.tableWrapper}>
          <table className={`${styles.table} ${styles.tableResponsive}`}>
            <thead>
              <tr>
                <th>Trace ID</th>
                <th>Start Time</th>
                <th>Duration</th>
                <th>Events</th>
                <th>Errors</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className={styles.empty}>Loading traces...</td>
                </tr>
              ) : traces.length === 0 ? (
                <tr>
                  <td colSpan={5} className={styles.empty}>
                    {error ? 'Error loading traces' : 'No trace IDs found in current dataset'}
                  </td>
                </tr>
              ) : (
                traces.map(trace => (
                  <tr key={trace.id}>
                    <td data-label="Trace ID" className={styles.cellMono}>
                      {trace.traceId || trace.correlationId || '-'}
                    </td>
                    <td data-label="Start Time" className={styles.cellNowrap}>{formatDate(trace.startTime)}</td>
                    <td data-label="Duration" className={styles.cellNowrap}>{formatDuration(trace.durationMs)}</td>
                    <td data-label="Events" className={styles.cellNowrap}>{trace.eventCount}</td>
                    <td
                      data-label="Errors"
                      className={`${styles.cellNowrap} ${trace.errorCount > 0 ? styles.textError : ''}`}
                    >
                      {trace.errorCount}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
