import React, { useState } from 'react';
import styles from './ExportDropdown.module.css';

interface ExportDropdownProps {
  reportId: string;
  onExport: (format: 'md' | 'json' | 'html' | 'prompt') => void;
}

export const ExportDropdown: React.FC<ExportDropdownProps> = ({ reportId, onExport }) => {
  const [isOpen, setIsOpen] = useState(false);

  const handleExport = (format: 'md' | 'json' | 'html' | 'prompt') => {
    onExport(format);
    setIsOpen(false);
  };

  return (
    <div className={styles.dropdown}>
      <button
        className={styles.trigger}
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
      >
        Export ▼
      </button>
      {isOpen && (
        <div className={styles.menu}>
          <button onClick={() => handleExport('md')} className={styles.item}>
            Download Markdown
          </button>
          <button onClick={() => handleExport('json')} className={styles.item}>
            Download JSON
          </button>
          <button onClick={() => handleExport('html')} className={styles.item}>
            Download HTML
          </button>
          <button onClick={() => handleExport('prompt')} className={styles.item}>
            Copy Agent Prompt
          </button>
        </div>
      )}
    </div>
  );
};
