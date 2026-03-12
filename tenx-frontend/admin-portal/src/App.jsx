import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import { useAuthStore } from './store/authStore'
import AdminLayout from './components/AdminLayout'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import UsersPage from './pages/UsersPage'
import RolesPage from './pages/RolesPage'
import LocationsPage from './pages/LocationsPage'
import SettingsPage from './pages/SettingsPage'
import ErrorLogsPage from './pages/ErrorLogsPage'
import DataConstantsPage from './pages/DataConstantsPage'
import FiscalYearPage from './pages/FiscalYearPage'
import NotificationTemplatesPage from './pages/NotificationTemplatesPage'
import NotificationsPage from './pages/NotificationsPage'
import RoleModulesPage from './pages/RoleModulesPage'
import DocumentMovementsPage from './pages/DocumentMovementsPage'
import ReviewsPage from './pages/ReviewsPage'
import InvoicesPage from './pages/InvoicesPage'

function Guard({ children }) {
  const isLoggedIn = useAuthStore(s => s.isLoggedIn())
  return isLoggedIn ? children : <Navigate to="/login" replace />
}

export default function App() {
  return (
    <>
      <Toaster position="top-right" toastOptions={{
        style: { background: '#1a1a24', color: '#e8e8f0', border: '1px solid #2a2a3a' }
      }} />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<Guard><AdminLayout /></Guard>}>
          <Route index                  element={<DashboardPage />} />
          <Route path="users"           element={<UsersPage />} />
          <Route path="roles"           element={<RolesPage />} />
          <Route path="role-permissions" element={<RoleModulesPage />} />
          <Route path="locations"       element={<LocationsPage />} />
          <Route path="fiscal-years"    element={<FiscalYearPage />} />
          <Route path="settings"        element={<SettingsPage />} />
          <Route path="data"            element={<DataConstantsPage />} />
          <Route path="doc-movements"   element={<DocumentMovementsPage />} />
          <Route path="templates"       element={<NotificationTemplatesPage />} />
          <Route path="notifications"   element={<NotificationsPage />} />
          <Route path="reviews"         element={<ReviewsPage />} />
          <Route path="invoices"        element={<InvoicesPage />} />
          <Route path="errors"          element={<ErrorLogsPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  )
}
