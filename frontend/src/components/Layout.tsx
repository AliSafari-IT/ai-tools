import { useEffect, useState } from 'react'
import { Outlet, Link, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import styles from './Layout.module.css'

type NavItem = { to: string; label: string }

const NAV: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/upload', label: 'Upload' },
  { to: '/explore/logs', label: 'Logs' },
  { to: '/explore/clusters', label: 'Clusters' },
  { to: '/explore/traces', label: 'Traces' },
  { to: '/explore/sessions', label: 'Sessions' },
  { to: '/reports', label: 'Reports' },
  { to: '/admin/data', label: 'Data Management' }
]

export default function Layout() {
  const { user, organization, logout } = useAuth()
  const location = useLocation()
  const [navOpen, setNavOpen] = useState(false)
  const [theme, setTheme] = useState<'light' | 'dark'>('light')

  const isActive = (path: string) => location.pathname.startsWith(path)

  const linkClass = (path: string) =>
    `${styles.navLink} ${isActive(path) ? styles.navLinkActive : ''}`.trim()

  useEffect(() => {
    setNavOpen(false)
  }, [location.pathname])

  useEffect(() => {
    if (!navOpen) return
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setNavOpen(false)
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [navOpen])

  useEffect(() => {
    if (!navOpen) return
    const prev = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.body.style.overflow = prev
    }
  }, [navOpen])

  const handleThemeToggle = () => {
    setTheme(theme === 'light' ? 'dark' : 'light')
    // data theme attribute on html element
    document.documentElement.setAttribute('data-theme', theme === 'light' ? 'dark' : 'light')
  }

  return (
    <div className={styles.layout}>
      <header className={styles.topbar}>
        <button
          type="button"
          className={styles.menuBtn}
          onClick={() => setNavOpen((v) => !v)}
          aria-label={navOpen ? 'Close navigation' : 'Open navigation'}
          aria-expanded={navOpen}
          aria-controls="app-sidebar"
        >
          <span className={styles.menuIcon} aria-hidden="true">
            ☰
          </span>
        </button>

        <div className={styles.topbarBrand}>
          <img src="/logo.svg" alt="Log Copilot" width="28" height="28" />
          <div className={styles.topbarBrandText}>
            <div className={styles.topbarTitle}>Log Copilot</div>
            <div className={styles.topbarMeta}>
              <div className={styles.topbarOrg}>{organization?.name}</div>
              {/* theme toggle */}
              <button onClick={handleThemeToggle} className={styles.themeToggle}>
                {theme === 'light' ? '🌙' : '☀️'}
              </button>
            </div>
          </div>
        </div>

        <div className={styles.topbarActions}>
          <button onClick={logout} className={styles.topbarLogout}>
            Logout
          </button>
        </div>
      </header>

      <div className={styles.content}>
        {navOpen && (
          <button
            type="button"
            className={styles.backdrop}
            onClick={() => setNavOpen(false)}
            aria-label="Close navigation"
          />
        )}

        <nav
          id="app-sidebar"
          className={`${styles.sidebar} ${navOpen ? styles.sidebarOpen : ''}`}
          aria-label="Primary navigation"
        >
          <div className={styles.logo}>
            <div className={styles.logoContent}>
              <img src="/logo.svg" alt="Log Copilot" width="32" height="32" />
              <h1 className={styles.logoText}>Log Copilot</h1>
            </div>
            <p className={styles.orgName}>{organization?.name}</p>
          </div>

          <div className={styles.nav}>
            {NAV.map((item) => (
              <Link key={item.to} to={item.to} className={linkClass(item.to)}>
                {item.label}
              </Link>
            ))}
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
    </div>
  )
}
