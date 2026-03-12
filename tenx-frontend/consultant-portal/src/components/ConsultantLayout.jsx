import React from 'react'
import { Outlet, useNavigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { LayoutDashboard, User, Users, UserPlus, MessageSquare, Calendar, Bell, LogOut } from 'lucide-react'
import { NotificationBell } from '../pages/NotificationsPage'
import { notifApi } from '../api'

const nav = [
  { section: 'Overview' },
  { label: 'Dashboard',     icon: LayoutDashboard, path: '/' },
  { section: 'Work' },
  { label: 'My Clients',    icon: Users,           path: '/clients' },
  { label: 'Requests',      icon: UserPlus,        path: '/requests' },
  { label: 'Messages',      icon: MessageSquare,   path: '/messages' },
  { section: 'Profile' },
  { label: 'My Profile',    icon: User,            path: '/profile' },
  { label: 'Availability',  icon: Calendar,        path: '/availability' },
  { section: 'Inbox' },
  { label: 'Notifications', icon: Bell,            path: '/notifications' },
]

export default function ConsultantLayout() {
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()

  const isActive = path => path === '/' ? location.pathname === '/' : location.pathname.startsWith(path)

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-logo">10X <span>Consultant</span></div>
        {nav.map((item, i) =>
          item.section ? (
            <div key={i} className="sidebar-section">{item.section}</div>
          ) : (
            <button key={item.path}
              className={`nav-item ${isActive(item.path) ? 'active' : ''}`}
              onClick={() => navigate(item.path)}>
              <item.icon size={15} />
              {item.label}
            </button>
          )
        )}
        <div style={{ marginTop: 'auto', padding: '0 8px' }}>
          <button className="nav-item" onClick={async () => { await logout(); navigate('/login') }}
            style={{ color: '#ff6b6b' }}>
            <LogOut size={15} /> Logout
          </button>
        </div>
      </aside>
      <div className="main">
        <header className="topbar">
          <span className="topbar-title">
            {nav.find(n => n.path && isActive(n.path))?.label || 'Consultant'}
          </span>
          <div className="topbar-right">
            <NotificationBell notifApi={notifApi} />
            <span style={{ color: 'var(--muted)', fontSize: 12 }}>{user?.email}</span>
            <div className="avatar-btn">{user?.userName?.charAt(0)?.toUpperCase() || 'C'}</div>
          </div>
        </header>
        <main className="page"><Outlet /></main>
      </div>
    </div>
  )
}
