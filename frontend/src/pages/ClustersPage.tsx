import { useEffect, useState } from 'react'
import styles from './PagesCommon.module.css'

interface IssueCluster {
    id: string
    type: string
    title: string
    firstSeen: string
    lastSeen: string
    severity: string
    status: string
    eventCount: number
}

export default function ClustersPage() {
    const [clusters, setClusters] = useState<IssueCluster[]>([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)

    useEffect(() => {
        fetchClusters()
    }, [])

    const fetchClusters = async () => {
        try {
            setLoading(true)
            setError(null)
            const token = localStorage.getItem('accessToken')
            const response = await fetch('http://localhost:5000/api/clusters?page=1&pageSize=50', {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            })

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`)
            }

            const data = await response.json()
            setClusters(data.items || [])
        } catch (err) {
            console.error('Error fetching clusters:', err)
            setError(err instanceof Error ? err.message : 'Failed to fetch clusters')
        } finally {
            setLoading(false)
        }
    }

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleString()
    }

    const getSeverityClass = (severity: string) => {
        switch (severity.toLowerCase()) {
            case 'critical': return styles.critical
            case 'high': return styles.high
            case 'medium': return styles.medium
            default: return styles.low
        }
    }

    return (
        <div className={styles.container}>
            <h1>Issue Clusters</h1>
            <p className={styles.subtitle}>Grouped errors and issues</p>

            {error && (
                <div className={styles.error}>
                    Error: {error}
                </div>
            )}

            <div className={styles.tableCard}>
                <table className={styles.table}>
                    <thead>
                        <tr>
                            <th>Title</th>
                            <th>Type</th>
                            <th>Severity</th>
                            <th>Count</th>
                            <th>Last Seen</th>
                        </tr>
                    </thead>
                    <tbody>
                        {loading ? (
                            <tr>
                                <td colSpan={5} className={styles.empty}>Loading clusters...</td>
                            </tr>
                        ) : clusters.length === 0 ? (
                            <tr>
                                <td colSpan={5} className={styles.empty}>
                                    {error ? 'Error loading clusters' : 'No warning/error events found in current dataset'}
                                </td>
                            </tr>
                        ) : (
                            clusters.map(cluster => (
                                <tr key={cluster.id}>
                                    <td>{cluster.title}</td>
                                    <td>{cluster.type}</td>
                                    <td>
                                        <span className={getSeverityClass(cluster.severity)}>
                                            {cluster.severity}
                                        </span>
                                    </td>
                                    <td>{cluster.eventCount}</td>
                                    <td>{formatDate(cluster.lastSeen)}</td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    )
}
