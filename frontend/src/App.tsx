import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import { UploadProgressProvider } from './contexts/UploadProgressContext'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import DashboardPage from './pages/DashboardPage'
import LogsPage from './pages/LogsPage'
import ClustersPage from './pages/ClustersPage'
import TracesPage from './pages/TracesPage'
import ReportsPage from './pages/ReportsPage'
import { DataManagementPage } from './pages/DataManagementPage'
import { SessionsPage } from './pages/SessionsPage'
import { UploadPage } from './pages/UploadPage'
import Layout from './components/Layout'

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const { user, isLoading } = useAuth()
  if (isLoading) return <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>Loading...</div>
  return user ? <>{children}</> : <Navigate to="/login" />
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <UploadProgressProvider>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route
              path="/"
              element={
                <PrivateRoute>
                  <Layout />
                </PrivateRoute>
              }
            >
              <Route index element={<Navigate to="/dashboard" replace />} />
              <Route path="dashboard" element={<DashboardPage />} />
              <Route path="explore/logs" element={<LogsPage />} />
              <Route path="explore/clusters" element={<ClustersPage />} />
              <Route path="explore/traces" element={<TracesPage />} />
              <Route path="explore/sessions" element={<SessionsPage />} />
              <Route path="reports" element={<ReportsPage />} />
              <Route path="admin/data" element={<DataManagementPage />} />
              <Route path="upload" element={<UploadPage />} />
            </Route>
          </Routes>
        </UploadProgressProvider>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
