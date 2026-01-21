import { useState, useEffect } from 'react'
import { api } from '@/lib/api'
import styles from './PagesCommon.module.css'

interface LogEvent {
  id: string
  timestamp: string
  level: string
  message: string
  traceId: string | null
  source: string
}

export default function LogsPage() {
  const [logs, setLogs] = useState<LogEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [level, setLevel] = useState('All Levels')
  const [skip, setSkip] = useState(0)
  const [total, setTotal] = useState(0)

  const take = 50

  useEffect(() => {
    const fetchLogs = async () => {
      setLoading(true)
      setError(null)
      try {
        console.log('Fetching logs with params:', { skip, take, search, level })
        const response = await api.get('/logs', {
          params: {
            skip,
            take,
            search: search || undefined,
            level: level !== 'All Levels' ? level : undefined,
          },
        })
        console.log('Logs response:', response.data)
        setLogs(response.data.logs)
        setTotal(response.data.total)
      } catch (err) {
        console.error('Failed to fetch logs:', err)
        setError(err instanceof Error ? err.message : 'Failed to fetch logs')
      } finally {
        setLoading(false)
      }
    }

    fetchLogs()
  }, [skip, search, level])

  const handleSearch = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearch(e.target.value)
    setSkip(0)
  }

  const handleLevelChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setLevel(e.target.value)
    setSkip(0)
  }

  const formatTimestamp = (timestamp: string) => {
    return new Date(timestamp).toLocaleString()
  }

  return (
    <div className={styles.container}>
      <h1>Log Explorer</h1>
      <p className={styles.subtitle}>Search and filter log events</p>

      <div className={styles.filters}>
        <input
          type="text"
          placeholder="Search logs..."
          className={styles.searchInput}
          value={search}
          onChange={handleSearch}
        />
        <select className={styles.select} value={level} onChange={handleLevelChange}>
          <option>All Levels</option>
          <option>Error</option>
          <option>Warning</option>
          <option>Information</option>
          <option>Debug</option>
          <option>Verbose</option>
          <option>Fatal</option>
        </select>
      </div>

      <div className={styles.tableCard}>
        {loading ? (
          <p style={{ padding: '1rem', textAlign: 'center' }}>Loading logs...</p>
        ) : error ? (
          <p style={{ padding: '1rem', textAlign: 'center', color: 'var(--asm-color-semantic-error)' }}>
            Error: {error}
          </p>
        ) : (
          <>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Timestamp</th>
                  <th>Level</th>
                  <th>Message</th>
                  <th>Source</th>
                  <th>Trace ID</th>
                </tr>
              </thead>
              <tbody>
                {logs.length === 0 ? (
                  <tr>
                    <td colSpan={5} className={styles.empty}>
                      No logs found
                    </td>
                  </tr>
                ) : (
                  logs.map((log) => (
                    <tr key={log.id}>
                      <td>{formatTimestamp(log.timestamp)}</td>
                      <td>{log.level}</td>
                      <td>{log.message}</td>
                      <td>{log.source}</td>
                      <td>{log.traceId || '-'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
            {logs.length > 0 && (
              <div style={{ padding: '1rem', textAlign: 'center', fontSize: '0.9rem', color: 'var(--asm-color-text-secondary)' }}>
                Showing {skip + 1}-{Math.min(skip + take, total)} of {total} logs
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
