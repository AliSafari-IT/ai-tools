import React, { useState } from 'react';
import styles from './DataManagementPage.module.css';
import { api } from '../lib/api';

type PurgeScope = 'all' | 'timeRange' | 'uploadSession';

interface PurgePreview {
  logEventsCount: number;
  httpDetailsCount: number;
  exceptionDetailsCount: number;
  traceEventsCount: number;
  requestTracesAffectedCount: number;
  orphanedTracesCount: number;
  clusterEventsCount: number;
  issueClustersAffectedCount: number;
  orphanedClustersCount: number;
  reportsAffectedCount: number;
}

export const DataManagementPage: React.FC = () => {
  const [scope, setScope] = useState<PurgeScope>('timeRange');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [uploadSessionId, setUploadSessionId] = useState('');
  const [preview, setPreview] = useState<PurgePreview | null>(null);
  const [confirmToken, setConfirmToken] = useState('');
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');

  const handlePreview = async () => {
    setLoading(true);
    try {
      const body: any = { scope };
      if (scope === 'timeRange') {
        body.startUtc = new Date(startDate).toISOString();
        body.endUtc = new Date(endDate).toISOString();
      } else if (scope === 'uploadSession') {
        body.uploadSessionId = uploadSessionId;
      }

      const response = await api.post('/data/purge/preview', body);
      setPreview(response.data);
      setMessage('');
    } catch (error) {
      setMessage('Error: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const handleExecute = async () => {
    if (confirmToken !== 'DELETE') {
      setMessage('Confirmation token must be "DELETE"');
      return;
    }

    setLoading(true);
    try {
      const body: any = {
        purgeRequest: { scope },
        confirmToken
      };
      if (scope === 'timeRange') {
        body.purgeRequest.startUtc = new Date(startDate).toISOString();
        body.purgeRequest.endUtc = new Date(endDate).toISOString();
      } else if (scope === 'uploadSession') {
        body.purgeRequest.uploadSessionId = uploadSessionId;
      }

      const response = await api.post('/data/purge/execute', body);
      setMessage(`✓ ${response.data.message}`);
      setPreview(null);
      setConfirmToken('');
    } catch (error) {
      setMessage('Error: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <h1>Data Management</h1>
      <p className={styles.subtitle}>Admin-only data purge and cleanup tools</p>

      <div className={styles.section}>
        <h2>Purge Scope</h2>
        <div className={styles.scopeOptions}>
          <label>
            <input
              type="radio"
              value="all"
              checked={scope === 'all'}
              onChange={(e) => setScope(e.target.value as PurgeScope)}
            />
            All Data
          </label>
          <label>
            <input
              type="radio"
              value="timeRange"
              checked={scope === 'timeRange'}
              onChange={(e) => setScope(e.target.value as PurgeScope)}
            />
            Time Range
          </label>
          <label>
            <input
              type="radio"
              value="uploadSession"
              checked={scope === 'uploadSession'}
              onChange={(e) => setScope(e.target.value as PurgeScope)}
            />
            Upload Session
          </label>
        </div>

        {scope === 'timeRange' && (
          <div className={styles.inputs}>
            <input
              type="datetime-local"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              placeholder="Start Date"
            />
            <input
              type="datetime-local"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              placeholder="End Date"
            />
          </div>
        )}

        {scope === 'uploadSession' && (
          <div className={styles.inputs}>
            <input
              type="text"
              value={uploadSessionId}
              onChange={(e) => setUploadSessionId(e.target.value)}
              placeholder="Upload Session ID"
            />
          </div>
        )}
      </div>

      <button
        onClick={handlePreview}
        disabled={loading}
        className={styles.previewBtn}
      >
        Preview Purge
      </button>

      {preview && (
        <div className={styles.preview}>
          <h3>Purge Preview</h3>
          <div className={styles.stats}>
            <div className={styles.stat}>
              <span>Log Events</span>
              <strong>{preview.logEventsCount}</strong>
            </div>
            <div className={styles.stat}>
              <span>HTTP Details</span>
              <strong>{preview.httpDetailsCount}</strong>
            </div>
            <div className={styles.stat}>
              <span>Exception Details</span>
              <strong>{preview.exceptionDetailsCount}</strong>
            </div>
            <div className={styles.stat}>
              <span>Trace Events</span>
              <strong>{preview.traceEventsCount}</strong>
            </div>
            <div className={styles.stat}>
              <span>Orphaned Traces</span>
              <strong>{preview.orphanedTracesCount}</strong>
            </div>
            <div className={styles.stat}>
              <span>Cluster Events</span>
              <strong>{preview.clusterEventsCount}</strong>
            </div>
            <div className={styles.stat}>
              <span>Orphaned Clusters</span>
              <strong>{preview.orphanedClustersCount}</strong>
            </div>
            <div className={styles.stat}>
              <span>Reports Affected</span>
              <strong>{preview.reportsAffectedCount}</strong>
            </div>
          </div>

          <div className={styles.confirmation}>
            <p>Type "DELETE" to confirm purge:</p>
            <input
              type="text"
              value={confirmToken}
              onChange={(e) => setConfirmToken(e.target.value)}
              placeholder="Type DELETE"
              className={styles.confirmInput}
            />
            <button
              onClick={handleExecute}
              disabled={loading || confirmToken !== 'DELETE'}
              className={styles.executeBtn}
            >
              Execute Purge
            </button>
          </div>
        </div>
      )}

      {message && (
        <div className={`${styles.message} ${message.startsWith('✓') ? styles.success : styles.error}`}>
          {message}
        </div>
      )}
    </div>
  );
};
