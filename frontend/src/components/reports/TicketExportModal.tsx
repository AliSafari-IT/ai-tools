import React, { useState } from 'react';
import styles from './TicketExportModal.module.css';

interface TicketExportModalProps {
  reportId: string;
  onClose: () => void;
}

export const TicketExportModal: React.FC<TicketExportModalProps> = ({ reportId, onClose }) => {
  const [target, setTarget] = useState<'github' | 'jira'>('github');
  const [ticketContent, setTicketContent] = useState('');
  const [loading, setLoading] = useState(false);

  const handleGenerate = async () => {
    setLoading(true);
    try {
      const response = await fetch(`/api/reports/${reportId}/ticket?target=${target}`);
      if (response.ok) {
        const text = await response.text();
        setTicketContent(text);
      }
    } catch (error) {
      console.error('Error generating ticket:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCopy = () => {
    navigator.clipboard.writeText(ticketContent);
  };

  const handleDownload = () => {
    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(ticketContent));
    element.setAttribute('download', `ticket-${reportId}.md`);
    element.style.display = 'none';
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
  };

  return (
    <div className={styles.modal}>
      <div className={styles.content}>
        <div className={styles.header}>
          <h2>Create Ticket</h2>
          <button onClick={onClose} className={styles.closeBtn}>×</button>
        </div>

        {!ticketContent ? (
          <div className={styles.form}>
            <label>
              Target Platform:
              <select value={target} onChange={(e) => setTarget(e.target.value as 'github' | 'jira')}>
                <option value="github">GitHub</option>
                <option value="jira">Jira</option>
              </select>
            </label>
            <button onClick={handleGenerate} disabled={loading} className={styles.generateBtn}>
              {loading ? 'Generating...' : 'Generate Ticket'}
            </button>
          </div>
        ) : (
          <div className={styles.preview}>
            <pre>{ticketContent}</pre>
            <div className={styles.actions}>
              <button onClick={handleCopy} className={styles.copyBtn}>Copy to Clipboard</button>
              <button onClick={handleDownload} className={styles.downloadBtn}>Download .md</button>
              <button onClick={() => setTicketContent('')} className={styles.backBtn}>Back</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
