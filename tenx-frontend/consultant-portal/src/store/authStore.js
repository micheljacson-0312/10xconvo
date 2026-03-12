import { create } from 'zustand'
import { authApi } from '../api'

export const useAuthStore = create((set, get) => ({
  user:         JSON.parse(localStorage.getItem('user') || 'null'),
  accessToken:  localStorage.getItem('accessToken') || null,
  refreshToken: localStorage.getItem('refreshToken') || null,
  loading: false,
  error: null,
  step1Data: null,

  step1: async (email) => {
    set({ loading: true, error: null })
    try {
      const { data } = await authApi.step1(email)
      set({ step1Data: data.data, loading: false })
      return data.data
    } catch (e) {
      const msg = e.response?.data?.message || 'Email not found'
      set({ error: msg, loading: false }); throw new Error(msg)
    }
  },

  step2: async (body) => {
    set({ loading: true, error: null })
    try {
      const { data } = await authApi.step2(body)
      const { accessToken, refreshToken, user } = data.data
      localStorage.setItem('accessToken',  accessToken)
      localStorage.setItem('refreshToken', refreshToken)
      localStorage.setItem('user', JSON.stringify(user))
      set({ accessToken, refreshToken, user, loading: false })
      return user
    } catch (e) {
      const msg = e.response?.data?.message || 'Login failed'
      set({ error: msg, loading: false }); throw new Error(msg)
    }
  },

  logout: async () => {
    try { await authApi.logout(get().refreshToken) } catch {}
    localStorage.clear()
    set({ user: null, accessToken: null, refreshToken: null })
  },

  isLoggedIn: () => !!get().accessToken,
}))
