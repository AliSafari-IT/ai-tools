import React, { useState, useRef } from 'react';
import styles from './UploadPage.module.css';
import { api } from '../lib/api';
import { UploadProgressBar } from '../components/UploadProgressBar';
import { useUploadProgress } from '../contexts/UploadProgressContext';

interface UploadJob {
  jobId: string;
  fileName: string;
  status: 'pending' | 'running' | 'completed' | 'cancelled' | 'failed';
  progress: number;
  processedLines: number;
  totalLines: number;
  errorMessage?: string;
}

export const UploadPage: React.FC = () => {
  const [jobs, setJobs] = useState<UploadJob[]>([]);
  const [sessionId, setSessionId] = useState<string>('');
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const pollIntervalRef = useRef<number | NodeJS.Timeout>();
  const { progress, startUpload, setUploadProgress, completeUpload, startProcessing, setProcessingProgress, completeProcessing, updateProcessingStats, setError, reset } = useUploadProgress();

  const handleDragOver = (event: React.DragEvent<HTMLLabelElement>) => {
    event.preventDefault();
    event.stopPropagation();
  };

  const handleDrop = async (event: React.DragEvent<HTMLLabelElement>) => {
    event.preventDefault();
    event.stopPropagation();
    
    const files = event.dataTransfer.files;
    if (!files || files.length === 0) return;

    // Process files the same way as file input
    await processFiles(files);
  };

  const processFiles = async (files: FileList) => {
    setIsUploading(true);
    const fileName = files.length === 1 ? files[0].name : `${files.length} files`;
    
    const formData = new FormData();
    let totalSize = 0;
    for (let i = 0; i < files.length; i++) {
      formData.append('files', files[i]);
      totalSize += files[i].size;
    }

    try {
      startUpload('', fileName);
      
      const response = await api.post('/logs/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
          const percentCompleted = Math.round(
            (progressEvent.loaded * 100) / (progressEvent.total || totalSize)
          );
          setUploadProgress(percentCompleted);
        },
      });

      const data = response.data;
      setSessionId(data.sessionId);
      completeUpload();

      const newJobs: UploadJob[] = [];
      for (let i = 0; i < files.length; i++) {
        newJobs.push({
          jobId: '',
          fileName: files[i].name,
          status: 'pending',
          progress: 0,
          processedLines: 0,
          totalLines: 0
        });
      }
      setJobs(newJobs);
      startProcessing();

      startPolling(data.sessionId);
    } catch (error) {
      const errorMsg = (error as Error).message;
      setError(errorMsg);
      alert('Error uploading files: ' + errorMsg);
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.currentTarget.files;
    if (!files || files.length === 0) return;

    await processFiles(files);
  };

  const startPolling = (sessionId: string) => {
    if (pollIntervalRef.current) {
      clearInterval(pollIntervalRef.current);
    }

    pollIntervalRef.current = setInterval(async () => {
      try {
        const response = await api.get(`/logs/status/${sessionId}`);
        const data = response.data;
        
        if (!data || !data.jobs) {
          console.warn('No jobs in response');
          return;
        }
        
        const updatedJobs = data.jobs.map((job: any) => ({
          jobId: job.id,
          fileName: job.uploadedFile?.originalFileName || 'Unknown',
          status: job.status.toLowerCase(),
          progress: job.totalLines > 0 ? (job.processedLines / job.totalLines) * 100 : 0,
          processedLines: job.processedLines,
          totalLines: job.totalLines,
          errorMessage: job.errorMessage
        }));
        setJobs(updatedJobs);

        const totalProcessed = updatedJobs.reduce((sum: number, j: UploadJob) => sum + j.processedLines, 0);
        const totalLines = updatedJobs.reduce((sum: number, j: UploadJob) => sum + j.totalLines, 0);
        updateProcessingStats(totalProcessed, totalLines);

        if (updatedJobs.length > 0 && totalLines > 0) {
          const avgProgress = (totalProcessed / totalLines) * 100;
          setProcessingProgress(avgProgress);
        }

        if (data.isComplete && pollIntervalRef.current) {
          clearInterval(pollIntervalRef.current);
          setIsUploading(false);
          completeProcessing();
        }
      } catch (error) {
        console.error('Error polling status:', error);
      }
    }, 1000);
  };

  const handleCancel = async (jobId: string) => {
    try {
      await api.post(`/logs/cancel/${jobId}`);
      
      setJobs(jobs.map(job =>
        job.jobId === jobId ? { ...job, status: 'cancelled' } : job
      ));
    } catch (error) {
      alert('Error cancelling job: ' + (error as Error).message);
    }
  };

  const handleStartNewUpload = () => {
    reset();
    setJobs([]);
    setSessionId('');
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <div className={styles.container}>
      <h1>Upload Logs</h1>
      <p className={styles.subtitle}>Upload log files for analysis and clustering</p>

      <UploadProgressBar />

      {progress.phase === 'completed' && (
        <div className={styles.nextSteps}>
          <div className={styles.successBanner}>
            <span className={styles.successIcon}>✓</span>
            <span>Upload complete. Logs ingested successfully.</span>
          </div>
          <div className={styles.actionsGrid}>
            <a href="/explore/logs" className={styles.actionBtn}>
              <span className={styles.actionIcon}>📋</span>
              <span>View Logs</span>
            </a>
            <a href="/explore/clusters" className={styles.actionBtn}>
              <span className={styles.actionIcon}>🔍</span>
              <span>View Clusters</span>
            </a>
            <a href="/explore/traces" className={styles.actionBtn}>
              <span className={styles.actionIcon}>🔗</span>
              <span>View Traces</span>
            </a>
            <a href="/reports" className={styles.actionBtn}>
              <span className={styles.actionIcon}>📊</span>
              <span>Generate Report</span>
            </a>
            <button onClick={handleStartNewUpload} className={styles.actionBtnPrimary}>
              <span className={styles.actionIcon}>⬆️</span>
              <span>Start New Upload</span>
            </button>
          </div>
        </div>
      )}

      <div className={styles.uploadSection}>
        <div className={styles.uploadBox}>
          <input
            ref={fileInputRef}
            id="file-input"
            type="file"
            multiple
            onChange={handleFileSelect}
            disabled={isUploading}
            className={styles.fileInput}
            accept=".json,.txt,.log"
          />
          <label 
            htmlFor="file-input" 
            className={styles.uploadLabel}
            onDragOver={handleDragOver}
            onDrop={handleDrop}
          >
            <span className={styles.icon}>📁</span>
            <span className={styles.text}>
              {isUploading ? 'Uploading...' : 'Click to select files or drag and drop'}
            </span>
            <span className={styles.hint}>Supported: JSON, TXT, LOG</span>
          </label>
        </div>
      </div>

      {sessionId && (
        <div className={styles.jobsSection}>
          <h2>Upload Progress</h2>
          {jobs.length === 0 ? (
            <p className={styles.empty}>No uploads</p>
          ) : (
            <div className={styles.jobsList}>
              {jobs.map((job, index) => (
                <div key={index} className={`${styles.jobCard} ${styles[job.status]}`}>
                  <div className={styles.jobHeader}>
                    <span className={styles.fileName}>{job.fileName}</span>
                    <span className={`${styles.status} ${styles[job.status]}`}>
                      {job.status.toUpperCase()}
                    </span>
                  </div>

                  <div className={styles.progressContainer}>
                    <div className={styles.progressBar}>
                      <div
                        className={styles.progressFill}
                        style={{ width: `${job.progress}%` }}
                      />
                    </div>
                    <span className={styles.progressText}>
                      {job.processedLines} / {job.totalLines} lines
                    </span>
                  </div>

                  {job.status === 'running' && (
                    <button
                      onClick={() => handleCancel(job.jobId)}
                      className={styles.cancelBtn}
                    >
                      Cancel Upload
                    </button>
                  )}

                  {job.errorMessage && (
                    <div className={styles.error}>{job.errorMessage}</div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
