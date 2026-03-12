import React from 'react'
import { Outlet, useNavigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import {
  LayoutDashboard, Users, Shield, MapPin, Settings, AlertTriangle,
  Database, LogOut, CalendarDays, Bell, FileText, Star,
  FileDigit, Radio, ShieldCheck, Receipt
} from 'lucide-react'

const nav = [
  { section: 'Overview' },
  { label: 'Dashboard',        icon: LayoutDashboard, path: '/' },
  { section: 'Users & Access' },
  { label: 'Registrations',    icon: Users,           path: '/users' },
  { label: 'Roles',            icon: Shield,          path: '/roles' },
  { label: 'Role Permissions', icon: ShieldCheck,     path: '/role-permissions' },
  { section: 'Billing' },
  { label: 'Invoices & Purchases', icon: Receipt,     path: '/invoices' },
  { section: 'Setup' },
  { label: 'Locations',        icon: MapPin,          path: '/locations' },
  { label: 'Fiscal Years',     icon: CalendarDays,    path: '/fiscal-years' },
  { label: 'Settings',         icon: Settings,        path: '/settings' },
  { section: 'Data' },
  { label: 'Data Constants',   icon: Database,        path: '/data' },
  { label: 'Doc Movements',    icon: FileDigit,       path: '/doc-movements' },
  { label: 'Reviews',          icon: Star,            path: '/reviews' },
  { section: 'Notifications' },
  { label: 'Templates',        icon: FileText,        path: '/templates' },
  { label: 'Send / History',   icon: Radio,           path: '/notifications' },
  { section: 'System' },
  { label: 'Error Logs',       icon: AlertTriangle,   path: '/errors' },
]

export default function AdminLayout() {
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()

  const handleLogout = async () => { await logout(); navigate('/login') }

  const isActive = (path) => path === '/' ? location.pathname === '/' : location.pathname.startsWith(path)

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-logo">10X <span>Admin</span></div>
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
          <button className="nav-item" onClick={handleLogout} style={{ color: '#ff6b6b' }}>
            <LogOut size={15} /> Logout
          </button>
        </div>
      </aside>
      <div className="main">
        <header className="topbar">
          <span className="topbar-title">
            {nav.find(n => n.path && isActive(n.path))?.label || 'Admin'}
          </span>
          <div className="topbar-right">
            <span style={{ color: 'var(--muted)', fontSize: 12 }}>{user?.email}</span>
            <div className="avatar-btn">{user?.userName?.charAt(0)?.toUpperCase() || 'A'}</div>
          </div>
        </header>
        <main className="page"><Outlet /></main>
      </div>
    </div>
  )
}
