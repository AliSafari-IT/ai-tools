import React, { useState, useEffect } from 'react';
import pageStyles from './SessionsPage.module.css';
import styles from './PagesCommon.module.css';
import { api } from '../lib/api';

interface Session {
  id: string;
  name: string;
  status: string;
  createdAt: string;
  fileCount: number;
  logEventCount: number;
  ingestionJobCount: number;
}

export const SessionsPage: React.FC = () => {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(
    localStorage.getItem('activeSessionId')
  );
  const [message, setMessage] = useState('');

  useEffect(() => {
    loadSessions();
  }, []);

  const loadSessions = async () => {
    try {
      const response = await api.get('/sessions');
      setSessions(response.data);
    } catch (error) {
      setMessage('Error loading sessions');
    } finally {
      setLoading(false);
    }
  };

  const handleSetActive = (sessionId: string) => {
    setActiveSessionId(sessionId);
    localStorage.setItem('activeSessionId', sessionId);
    setMessage(`✓ Active session set to ${sessionId}`);
  };

  const handleClearActive = () => {
    setActiveSessionId(null);
    localStorage.removeItem('activeSessionId');
    setMessage('✓ Active session cleared');
  };

  const handleDeleteSession = async (sessionId: string) => {
    if (!window.confirm('Delete this session and all associated data?')) return;

    try {
      await api.delete(`/sessions/${sessionId}`);
      setSessions(sessions.filter(s => s.id !== sessionId));
      if (activeSessionId === sessionId) {
        handleClearActive();
      }
      setMessage('✓ Session deleted');
    } catch (error) {
      setMessage('Error: ' + (error as Error).message);
    }
  };

  if (loading) {
    return <div className={styles.container}>Loading sessions...</div>;
  }

  return (
    <div className={pageStyles.container}>
      <h1>Upload Sessions</h1>
      <p className={pageStyles.subtitle}>Manage datasets and filter exploration by session</p>

      {activeSessionId && (
        <div className={pageStyles.activeSession}>
          <span>Active Session: <span className={styles.cellMono} title={activeSessionId}>{sessions.find(s => s.id === activeSessionId)?.name || 'Unknown'}</span></span>
          <button onClick={handleClearActive} className={styles.secondaryBtn}>
            Clear
          </button>
        </div>
      )}

      {sessions.length === 0 ? (
        <div className={styles.empty}>No sessions found</div>
      ) : (
        <div className={styles.tableCard}>
          <div className={styles.tableWrapper}>
            <table className={`${styles.table} ${styles.tableResponsive}`}>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th>Files</th>
                  <th>Log Events</th>
                  <th>Jobs</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {sessions.map(session => (
                  <tr key={session.id} className={activeSessionId === session.id ? pageStyles.rowActive : ''}>
                    <td data-label="Name">{session.name}</td>
                    <td data-label="Status" className={styles.cellNowrap}>
                      <span className={`${styles.statusBadge} ${styles[`status${session.status}`]}`}>
                        {session.status}
                      </span>
                    </td>
                    <td data-label="Created" className={styles.cellNowrap}>{new Date(session.createdAt).toLocaleDateString()}</td>
                    <td data-label="Files" className={styles.cellNowrap}>{session.fileCount}</td>
                    <td data-label="Log Events" className={styles.cellNowrap}>{session.logEventCount}</td>
                    <td data-label="Jobs" className={styles.cellNowrap}>{session.ingestionJobCount}</td>
                    <td data-label="Actions" className={styles.cellActions}>
                      <div className={styles.actionsRow}>
                        <button
                          onClick={() => handleSetActive(session.id)}
                          className={styles.secondaryBtn}
                        >
                          Set Active
                        </button>
                        <button
                          onClick={() => handleDeleteSession(session.id)}
                          className={styles.dangerBtn}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {message && (
        <div className={`${styles.card} ${message.startsWith('✓') ? styles.alert : styles.error}`}>
          {message}
        </div>
      )}
    </div>
  );
};
