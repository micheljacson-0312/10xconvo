import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import { useAuthStore } from './store/authStore'
import ConsultantLayout from './components/ConsultantLayout'
import { AllPages } from './pages/AllPages'
import { AvailabilityPage, ConsultantNotificationsPage } from './pages/PartBPages'
import { NotificationsPage } from './pages/NotificationsPage'

const { LoginPage, DashboardPage, ProfilePage, ClientsPage, RequestsPage, MessagingPage } = AllPages || {}

function Guard({ children }) {
  const isLoggedIn = useAuthStore(s => s.isLoggedIn())
  return isLoggedIn ? children : <Navigate to="/login" replace />
}

export default function App() {
  return (
    <>
      <Toaster
        position="top-right"
        toastOptions={{
          style: {
            background: '#1a1a24',
            color: '#e8e8f0',
            border: '1px solid #2a2a3a'
          }
        }}
      />

      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route path="/" element={<Guard><ConsultantLayout /></Guard>}>
          <Route index element={<DashboardPage />} />
          <Route path="profile" element={<ProfilePage />} />
          <Route path="clients" element={<ClientsPage />} />
          <Route path="requests" element={<RequestsPage />} />
          <Route path="messages" element={<MessagingPage />} />
          <Route path="messages/:id" element={<MessagingPage />} />

          <Route path="availability" element={<AvailabilityPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  )
}