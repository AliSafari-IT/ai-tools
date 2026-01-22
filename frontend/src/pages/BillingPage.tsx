import React, { useEffect, useState } from 'react';
import { api } from '../lib/api';
import styles from './PagesCommon.module.css';

interface BillingPlanInfo {
  planName: string;
  maxUsers: number;
  maxStorageBytes: number;
  maxUploadSessionsPerMonth: number;
  currentUsers: number;
  currentStorageBytes: number;
  currentMonthUploads: number;
  enablesAiReports: boolean;
  enablesSemanticClustering: boolean;
}

export const BillingPage: React.FC = () => {
  const [planInfo, setPlanInfo] = useState<BillingPlanInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    loadPlanInfo();
  }, []);

  const loadPlanInfo = async () => {
    try {
      const response = await api.get('/billing/current');
      setPlanInfo(response.data);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  const getUsagePercentage = (current: number, max: number) => {
    return Math.min(100, (current / max) * 100);
  };

  if (loading) return <div className={styles.container}>Loading...</div>;
  if (error) return <div className={styles.container}>Error: {error}</div>;
  if (!planInfo) return <div className={styles.container}>No billing information available</div>;

  return (
    <div className={styles.container}>
      <h1>Billing & Usage</h1>
      <p className={styles.subtitle}>Current plan and resource usage</p>

      <div className={styles.card}>
        <h2>Current Plan: {planInfo.planName}</h2>
        
        <div className={styles.section}>
          <h3>Features</h3>
          <ul>
            <li>AI-Powered Reports: {planInfo.enablesAiReports ? '✓ Enabled' : '✗ Disabled'}</li>
            <li>Semantic Clustering: {planInfo.enablesSemanticClustering ? '✓ Enabled' : '✗ Disabled'}</li>
          </ul>
        </div>

        <div className={styles.section}>
          <h3>Usage</h3>
          
          <div className={styles.usageItem}>
            <div className={styles.usageLabel}>
              <span>Users</span>
              <span>{planInfo.currentUsers} / {planInfo.maxUsers}</span>
            </div>
            <div className={styles.progressBar}>
              <div 
                className={styles.progressFill}
                style={{ width: `${getUsagePercentage(planInfo.currentUsers, planInfo.maxUsers)}%` }}
              />
            </div>
          </div>

          <div className={styles.usageItem}>
            <div className={styles.usageLabel}>
              <span>Storage</span>
              <span>{formatBytes(planInfo.currentStorageBytes)} / {formatBytes(planInfo.maxStorageBytes)}</span>
            </div>
            <div className={styles.progressBar}>
              <div 
                className={styles.progressFill}
                style={{ width: `${getUsagePercentage(planInfo.currentStorageBytes, planInfo.maxStorageBytes)}%` }}
              />
            </div>
          </div>

          <div className={styles.usageItem}>
            <div className={styles.usageLabel}>
              <span>Uploads This Month</span>
              <span>{planInfo.currentMonthUploads} / {planInfo.maxUploadSessionsPerMonth}</span>
            </div>
            <div className={styles.progressBar}>
              <div 
                className={styles.progressFill}
                style={{ width: `${getUsagePercentage(planInfo.currentMonthUploads, planInfo.maxUploadSessionsPerMonth)}%` }}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
