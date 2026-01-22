import React, { createContext, useContext, useState, useCallback } from 'react'

export interface UploadProgress {
  sessionId: string | null
  phase: 'idle' | 'uploading' | 'processing' | 'completed' | 'error'
  uploadProgress: number
  processingProgress: number
  fileName: string | null
  errorMessage: string | null
  processedLines: number
  totalLines: number
  lastUpdate: Date | null
}

interface UploadProgressContextType {
  progress: UploadProgress
  setUploadProgress: (percentage: number) => void
  setProcessingProgress: (percentage: number) => void
  startUpload: (sessionId: string, fileName: string) => void
  completeUpload: () => void
  startProcessing: () => void
  completeProcessing: () => void
  updateProcessingStats: (processedLines: number, totalLines: number) => void
  setError: (message: string) => void
  reset: () => void
}

const UploadProgressContext = createContext<UploadProgressContextType | undefined>(undefined)

export const UploadProgressProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [progress, setProgress] = useState<UploadProgress>({
    sessionId: null,
    phase: 'idle',
    uploadProgress: 0,
    processingProgress: 0,
    fileName: null,
    errorMessage: null,
    processedLines: 0,
    totalLines: 0,
    lastUpdate: null,
  })

  const setUploadProgress = useCallback((percentage: number) => {
    setProgress(prev => ({
      ...prev,
      uploadProgress: Math.min(100, Math.max(0, percentage)),
    }))
  }, [])

  const setProcessingProgress = useCallback((percentage: number) => {
    setProgress(prev => ({
      ...prev,
      processingProgress: Math.min(100, Math.max(0, percentage)),
    }))
  }, [])

  const startUpload = useCallback((sessionId: string, fileName: string) => {
    setProgress(prev => ({
      ...prev,
      sessionId,
      fileName,
      phase: 'uploading',
      uploadProgress: 0,
      processingProgress: 0,
      errorMessage: null,
    }))
  }, [])

  const completeUpload = useCallback(() => {
    setProgress(prev => ({
      ...prev,
      uploadProgress: 100,
      phase: 'processing',
      processingProgress: 0,
    }))
  }, [])

  const startProcessing = useCallback(() => {
    setProgress(prev => ({
      ...prev,
      phase: 'processing',
      processingProgress: 0,
    }))
  }, [])

  const completeProcessing = useCallback(() => {
    setProgress(prev => ({
      ...prev,
      phase: 'completed',
      processingProgress: 100,
      lastUpdate: new Date(),
    }))
  }, [])

  const updateProcessingStats = useCallback((processedLines: number, totalLines: number) => {
    setProgress(prev => ({
      ...prev,
      processedLines,
      totalLines,
      lastUpdate: new Date(),
    }))
  }, [])

  const setError = useCallback((message: string) => {
    setProgress(prev => ({
      ...prev,
      phase: 'error',
      errorMessage: message,
    }))
  }, [])

  const reset = useCallback(() => {
    setProgress({
      sessionId: null,
      phase: 'idle',
      uploadProgress: 0,
      processingProgress: 0,
      fileName: null,
      errorMessage: null,
      processedLines: 0,
      totalLines: 0,
      lastUpdate: null,
    })
  }, [])

  return (
    <UploadProgressContext.Provider
      value={{
        progress,
        setUploadProgress,
        setProcessingProgress,
        startUpload,
        completeUpload,
        startProcessing,
        completeProcessing,
        updateProcessingStats,
        setError,
        reset,
      }}
    >
      {children}
    </UploadProgressContext.Provider>
  )
}

export const useUploadProgress = () => {
  const context = useContext(UploadProgressContext)
  if (!context) {
    throw new Error('useUploadProgress must be used within UploadProgressProvider')
  }
  return context
}
