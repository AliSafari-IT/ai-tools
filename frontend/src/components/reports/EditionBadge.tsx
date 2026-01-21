import React from 'react';
import styles from './EditionBadge.module.css';

interface EditionBadgeProps {
  edition: 'Community' | 'Pro';
}

export const EditionBadge: React.FC<EditionBadgeProps> = ({ edition }) => {
  return (
    <div className={`${styles.badge} ${styles[edition.toLowerCase()]}`}>
      {edition}
    </div>
  );
};
