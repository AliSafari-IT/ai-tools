import { Outlet, Link, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import styles from './Layout.module.css'

export default function Layout() {
  const { user, organization, logout } = useAuth()
  const location = useLocation()

  const isActive = (path: string) => location.pathname.startsWith(path)

  return (
    <div className={styles.layout}>
      <nav className={styles.sidebar}>
        <div className={styles.logo}>
          <h1>Log Copilot</h1>
          <p>{organization?.name}</p>
        </div>
        <div className={styles.nav}>
          <Link
            to="/dashboard"
            className={isActive('/dashboard') ? styles.active : ''}
          >
            Dashboard
          </Link>
          <Link
            to="/explore/logs"
            className={isActive('/explore/logs') ? styles.active : ''}
          >
            Logs
          </Link>
          <Link
            to="/explore/clusters"
            className={isActive('/explore/clusters') ? styles.active : ''}
          >
            Clusters
          </Link>
          <Link
            to="/explore/traces"
            className={isActive('/explore/traces') ? styles.active : ''}
          >
            Traces
          </Link>
          <Link
            to="/reports"
            className={isActive('/reports') ? styles.active : ''}
          >
            Reports
          </Link>
        </div>
        <div className={styles.user}>
          <div className={styles.userInfo}>
            <p className={styles.userName}>
              {user?.firstName} {user?.lastName}
            </p>
            <p className={styles.userEmail}>{user?.email}</p>
          </div>
          <button onClick={logout} className={styles.logoutBtn}>
            Logout
          </button>
        </div>
      </nav>
      <main className={styles.main}>
        <Outlet />
      </main>
    </div>
  )
}
