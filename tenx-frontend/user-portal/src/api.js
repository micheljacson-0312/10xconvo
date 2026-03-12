import axios from 'axios'
import { create } from 'zustand'

const BASE = import.meta.env.VITE_API_URL || '/api'
export const api = axios.create({ baseURL: BASE })

api.interceptors.request.use(cfg => {
  const t = localStorage.getItem('accessToken')
  if (t) cfg.headers.Authorization = `Bearer ${t}`
  return cfg
})
api.interceptors.response.use(r => r, async err => {
  if (err.response?.status === 401) {
    const rt = localStorage.getItem('refreshToken')
    if (rt) {
      try {
        const { data } = await axios.post(`${BASE}/auth/refresh`, { refreshToken: rt })
        localStorage.setItem('accessToken', data.data.accessToken)
        localStorage.setItem('refreshToken', data.data.refreshToken)
        err.config.headers.Authorization = `Bearer ${data.data.accessToken}`
        return api(err.config)
      } catch { localStorage.clear(); window.location.href = '/login' }
    }
  }
  return Promise.reject(err)
})

// ── API CALLS ─────────────────────────────────────────────────────────────────
export const authApi = {
  step1:  email => api.post('/auth/login/step1', { email }),
  step2:  body  => api.post('/auth/login/step2', body),
  logout: rt    => api.post('/auth/logout', { refreshToken: rt }),
  externalLogin:   body => api.post('/auth/external-login', body),
  getLinkedProviders: () => api.get('/auth/external-logins'),
  linkProvider:    body => api.post('/auth/external-logins/link', body),
  unlinkProvider:  prov => api.delete(`/auth/external-logins/${prov}`),
}

// ── CREDITS (chars + minutes balance) ─────────────────────────────────────
export const creditsApi = {
  getBalance:           ()           => api.get('/credits/balance'),
  getHistory:           (p, ps)      => api.get('/credits/history', { params: { page: p, pageSize: ps } }),
  getPricing:           ()           => api.get('/credits/pricing'),
  purchase:             body         => api.post('/credits/purchase', body),
  purchasePayFast:      body         => api.post('/credits/purchase/payfast', body),
  purchaseEasyPaisa:    body         => api.post('/credits/purchase/easypaisa', body),
  purchaseEasyPaisaOtc: body         => api.post('/credits/purchase/easypaisa/otc', body),
  verify:               paymentId    => api.get(`/credits/verify/${paymentId}`),
}

// ── INVOICES ──────────────────────────────────────────────────────────────
export const invoiceApi = {
  list:     (p, ps)    => api.get('/invoices', { params: { page: p, pageSize: ps } }),
  download: id         => `${BASE}/invoices/${id}/download`,  // returns URL — open in new tab
}
export const userApi = {
  // Public — no auth needed
  getConsultants: (p, ps, s) => api.get('/user/consultants', { params: { page: p, pageSize: ps, search: s } }),
  getConsultant:  id         => api.get(`/user/consultants/${id}`),
  getConsultantBySlug: slug  => api.get(`/user/consultants/by-slug/${slug}`),

  // Auth required
  connect:           consultantId => api.post(`/user/consultants/${consultantId}/connect`),
  getProfile:        ()           => api.get('/user/profile'),
  updateProfile:     body         => api.put('/user/profile', body),
  getConversations:  (p, ps)      => api.get('/user/messages', { params: { page: p, pageSize: ps } }),
  getMessages:       (id, p, ps)  => api.get(`/user/messages/${id}`, { params: { page: p, pageSize: ps } }),
  sendMessage:       (id, body)   => api.post(`/user/messages/${id}`, body),
  markRead:          id           => api.put(`/user/messages/${id}/read`),
}

// ── AUTH STORE ────────────────────────────────────────────────────────────────
export const useAuthStore = create((set, get) => ({
  user:         JSON.parse(localStorage.getItem('user') || 'null'),
  accessToken:  localStorage.getItem('accessToken') || null,
  refreshToken: localStorage.getItem('refreshToken') || null,
  loading: false,
  step1Data: null,

  step1: async (email) => {
    set({ loading: true })
    try {
      const { data } = await authApi.step1(email)
      set({ step1Data: data.data, loading: false }); return data.data
    } catch (e) { set({ loading: false }); throw new Error(e.response?.data?.message || 'Email not found') }
  },

  step2: async (body) => {
    set({ loading: true })
    try {
      const { data } = await authApi.step2(body)
      const { accessToken, refreshToken, user } = data.data
      localStorage.setItem('accessToken', accessToken)
      localStorage.setItem('refreshToken', refreshToken)
      localStorage.setItem('user', JSON.stringify(user))
      set({ accessToken, refreshToken, user, loading: false }); return user
    } catch (e) { set({ loading: false }); throw new Error(e.response?.data?.message || 'Login failed') }
  },

  logout: async () => {
    try { await authApi.logout(get().refreshToken) } catch {}
    localStorage.clear()
    set({ user: null, accessToken: null, refreshToken: null })
  },

  isLoggedIn: () => !!get().accessToken,
}))

// ── USER NOTIFICATIONS ────────────────────────────────────────────────────────
export const notifApi = {
  getAll:   (unreadOnly) => api.get('/user/notifications', { params: { unreadOnly, page:1, pageSize:30 } }),
  markRead: id           => api.put(`/user/notifications/${id}/read`),
  markAll:  ()           => api.put('/user/notifications/read-all'),
}

// ── REVIEWS ───────────────────────────────────────────────────────────────────
export const reviewApi = {
  getAll:  (consultantId, p, ps) => api.get(`/user/consultants/${consultantId}/reviews`, { params:{page:p,pageSize:ps} }),
  create:  (consultantId, body)  => api.post(`/user/consultants/${consultantId}/reviews`, body),
  update:  (consultantId, id, b) => api.put(`/user/consultants/${consultantId}/reviews/${id}`, b),
  delete:  (consultantId, id)    => api.delete(`/user/consultants/${consultantId}/reviews/${id}`),
}

// ── PUBLIC AVAILABILITY ───────────────────────────────────────────────────────
export const availApi = {
  get: (consultantUserId) => api.get(`/user/consultants/${consultantUserId}/availability`),
}

// ── FORGOT PASSWORD ───────────────────────────────────────────────────────────
export const forgotApi = {
  request: (email)                          => api.post('/auth/forgot-password',    { email }),
  verify:  (email, token)                   => api.post('/auth/verify-reset-token', { email, token }),
  reset:   (email, resetSessionToken, pwd)  => api.post('/auth/reset-password',     { email, resetSessionToken, newPassword: pwd }),
}
