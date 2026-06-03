// ═══════════════════════════════════════════════════════════════════════════
//  CONSULTANT DIRECT PAGE — /c/:slug
//  Each consultant gets a unique shareable link like:
//    https://user.10xdigitalventures.com/c/ali-khan
//
//  This page shows:
//  - Consultant's full profile (avatar, name, bio, rate, specialization)
//  - "Connect" button (if logged in)
//  - "Sign in with Google / Facebook" buttons (if not logged in)
//  - Standard email login option
//
//  Consultants share this link on their social media, business cards, etc.
//  When a visitor clicks it, they see the consultant's profile and can
//  sign in with one click via Google/Facebook, then connect immediately.
// ═══════════════════════════════════════════════════════════════════════════

import React, { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { toast } from 'react-hot-toast'
import { Loader, Clock, MapPin, ChevronRight, MessageSquare } from 'lucide-react'
import { userApi, authApi } from '../api'
import { useAuthStore } from '../api'

export function ConsultantDirectPage() {
  const { slug } = useParams()
  const navigate = useNavigate()
  const { isLoggedIn, user, accessToken } = useAuthStore()

  const [consultant, setConsultant] = useState(null)
  const [loading, setLoading]       = useState(true)
  const [notFound, setNotFound]     = useState(false)
  const [connecting, setConnecting] = useState(false)
  const [socialLoading, setSocialLoading] = useState(null)

  // Load consultant by slug
  useEffect(() => {
    setLoading(true)
    userApi.getConsultantBySlug(slug)
      .then(r => setConsultant(r.data.data))
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false))
  }, [slug])

  // ── Connect ─────────────────────────────────────────────────────────────
  const handleConnect = async () => {
    if (!isLoggedIn()) { return } // shouldn't happen, buttons are conditional
    setConnecting(true)
    try {
      await userApi.connect(consultant.userId)
      toast.success('Connection request sent!')
    } catch (err) {
      toast.error(err.response?.data?.message || 'Already connected or pending')
    } finally { setConnecting(false) }
  }

  // ── Social Login → auto-connect ─────────────────────────────────────────
  const handleSocialLogin = async (providerName) => {
    setSocialLoading(providerName)
    try {
      const { signInWithGoogle, signInWithFacebook } = await import('../firebaseAuth.js')
      const fn = providerName === 'Google' ? signInWithGoogle : signInWithFacebook
      const claims = await fn()
      const { data } = await authApi.externalLogin(claims)
      const { accessToken: at, refreshToken: rt, user: u } = data.data
      localStorage.setItem('accessToken', at)
      localStorage.setItem('refreshToken', rt)
      localStorage.setItem('user', JSON.stringify(u))
      useAuthStore.setState({ accessToken: at, refreshToken: rt, user: u })
      toast.success(`Welcome, ${u.userName}!`)
      // Auto-connect after login
      try {
        await userApi.connect(consultant.userId)
        toast.success('Connected! You can now chat.')
        navigate('/messages')
      } catch {
        toast.success('Signed in! You can connect from this page.')
      }
    } catch (err) {
      const msg = err?.response?.data?.message || err?.message || 'Login failed'
      if (!msg.includes('popup-closed')) toast.error(msg)
    } finally { setSocialLoading(null) }
  }

  // ── Loading / Not Found ──────────────────────────────────────────────────
  if (loading) return (
    <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'var(--bg1)' }}>
      <Loader size={32} className="spin" style={{ color: 'var(--accent)' }} />
    </div>
  )

  if (notFound) return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: 'var(--bg1)', gap: 16 }}>
      <h2 style={{ fontFamily: 'var(--font-head)', fontSize: 28, fontWeight: 800 }}>Consultant Not Found</h2>
      <p style={{ color: 'var(--muted)' }}>The link <strong>/c/{slug}</strong> doesn't match any consultant.</p>
      <button className="btn btn-primary" onClick={() => navigate('/')}>Browse All Consultants</button>
    </div>
  )

  const c = consultant

  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg1)' }}>
      {/* ── Header Bar ── */}
      <div style={{ background: 'var(--bg2)', borderBottom: '1px solid var(--border)', padding: '12px 24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ cursor: 'pointer', fontFamily: 'var(--font-head)', fontWeight: 800, fontSize: 18, color: 'var(--text)' }} onClick={() => navigate('/')}>
          10X <span style={{ color: 'var(--accent)' }}>Convo</span>
        </div>
        {isLoggedIn() ? (
          <span style={{ fontSize: 13, color: 'var(--muted)' }}>Signed in as <strong style={{ color: 'var(--text)' }}>{user?.userName}</strong></span>
        ) : (
          <button className="btn btn-ghost" onClick={() => navigate('/login')} style={{ fontSize: 13 }}>Sign In</button>
        )}
      </div>

      {/* ── Profile Card ── */}
      <div style={{ maxWidth: 560, margin: '40px auto', padding: '0 20px' }}>
        <div style={{ background: 'var(--bg2)', borderRadius: 16, border: '1px solid var(--border)', overflow: 'hidden' }}>
          {/* Gradient banner */}
          <div style={{ height: 100, background: 'linear-gradient(135deg, var(--accent), var(--accent2))' }} />

          {/* Avatar + Name */}
          <div style={{ padding: '0 28px 28px', marginTop: -44 }}>
            <div style={{ width: 88, height: 88, borderRadius: '50%', background: 'var(--bg2)', border: '4px solid var(--bg2)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: 'var(--font-head)', fontWeight: 800, fontSize: 36, color: 'var(--accent)', boxShadow: '0 4px 20px rgba(0,0,0,0.1)' }}>
              {c.avatarUrl ? <img src={c.avatarUrl} alt="" style={{ width: '100%', height: '100%', borderRadius: '50%', objectFit: 'cover' }} /> : c.userName?.charAt(0)?.toUpperCase()}
            </div>

            <div style={{ marginTop: 14 }}>
              <h1 style={{ fontFamily: 'var(--font-head)', fontSize: 26, fontWeight: 800, marginBottom: 4 }}>
                {c.userName}
                {c.isOnline && <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: '50%', background: '#22c55e', marginLeft: 10, verticalAlign: 'middle' }} />}
              </h1>
              {c.specialization && <p style={{ color: 'var(--accent)', fontWeight: 600, fontSize: 14, marginBottom: 12 }}>{c.specialization}</p>}
              {c.bio && <p style={{ color: 'var(--muted)', lineHeight: 1.7, fontSize: 14, marginBottom: 16 }}>{c.bio}</p>}
            </div>

            {/* Stats */}
            <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', marginBottom: 20 }}>
              {c.experience && (
                <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13, color: 'var(--muted)' }}>
                  <Clock size={14} /> {c.experience}
                </div>
              )}
              {c.hourlyRate && (
                <div style={{ fontSize: 16, fontFamily: 'var(--font-head)', fontWeight: 800, color: 'var(--accent)' }}>
                  PKR {Number(c.hourlyRate).toLocaleString()} <span style={{ fontSize: 12, fontWeight: 400, color: 'var(--muted)' }}>/hr</span>
                </div>
              )}
            </div>

            {/* ── CTA Section ── */}
            {isLoggedIn() ? (
              <div style={{ display: 'flex', gap: 10 }}>
                <button className="btn btn-primary" onClick={handleConnect} disabled={connecting}
                  style={{ flex: 1, justifyContent: 'center', padding: 14, fontSize: 15, borderRadius: 12 }}>
                  {connecting ? <Loader size={16} className="spin" /> : <><MessageSquare size={16} /> Connect & Chat</>}
                </button>
              </div>
            ) : (
              <>
                <p style={{ fontSize: 13, color: 'var(--muted)', marginBottom: 14, textAlign: 'center' }}>
                  Sign in to connect with <strong>{c.userName}</strong>
                </p>

                {/* Social Login Buttons */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                  <button onClick={() => handleSocialLogin('Google')} disabled={!!socialLoading}
                    style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10, padding: '12px 0', borderRadius: 12, border: '1px solid var(--border)', background: '#fff', cursor: 'pointer', fontSize: 14, fontWeight: 600, color: '#1f2937', transition: 'all 0.15s', boxShadow: '0 1px 3px rgba(0,0,0,0.06)' }}>
                    {socialLoading === 'Google' ? <Loader size={16} className="spin" /> : (
                      <>
                        <svg width="18" height="18" viewBox="0 0 48 48"><path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/><path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/><path fill="#FBBC05" d="M10.53 28.59A14.5 14.5 0 019.5 24c0-1.59.28-3.14.76-4.59l-7.98-6.19A23.9 23.9 0 000 24c0 3.77.9 7.35 2.56 10.54l7.97-5.95z"/><path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 5.95C6.51 42.62 14.62 48 24 48z"/></svg>
                        Continue with Google
                      </>
                    )}
                  </button>

                  <button onClick={() => handleSocialLogin('Facebook')} disabled={!!socialLoading}
                    style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10, padding: '12px 0', borderRadius: 12, border: 'none', background: '#1877F2', cursor: 'pointer', fontSize: 14, fontWeight: 600, color: '#fff', transition: 'all 0.15s', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
                    {socialLoading === 'Facebook' ? <Loader size={16} className="spin" /> : (
                      <>
                        <svg width="18" height="18" viewBox="0 0 48 48"><path fill="#fff" d="M48 24C48 10.745 37.255 0 24 0S0 10.745 0 24c0 11.979 8.776 21.908 20.25 23.708v-16.77h-6.094V24h6.094v-5.288c0-6.014 3.583-9.337 9.065-9.337 2.625 0 5.372.469 5.372.469v5.906h-3.026c-2.981 0-3.911 1.85-3.911 3.75V24h6.656l-1.064 6.938H27.75v16.77C39.224 45.908 48 35.978 48 24z"/></svg>
                        Continue with Facebook
                      </>
                    )}
                  </button>

                  <div style={{ display: 'flex', alignItems: 'center', gap: 12, margin: '6px 0' }}>
                    <div style={{ flex: 1, height: 1, background: 'var(--border)' }} />
                    <span style={{ fontSize: 12, color: 'var(--muted)' }}>or</span>
                    <div style={{ flex: 1, height: 1, background: 'var(--border)' }} />
                  </div>

                  <button className="btn btn-ghost" onClick={() => navigate('/login')}
                    style={{ width: '100%', justifyContent: 'center', padding: 12, borderRadius: 12, fontSize: 14 }}>
                    Sign in with Email <ChevronRight size={14} />
                  </button>
                </div>
              </>
            )}

            {/* Shareable link */}
            <div style={{ marginTop: 20, padding: '10px 14px', background: 'var(--bg3)', borderRadius: 10, fontSize: 12, color: 'var(--muted)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {window.location.origin}/c/{slug}
              </span>
              <button onClick={() => { navigator.clipboard.writeText(`${window.location.origin}/c/${slug}`); toast.success('Link copied!') }}
                style={{ background: 'var(--accent)', color: '#fff', border: 'none', borderRadius: 6, padding: '4px 10px', fontSize: 11, cursor: 'pointer', fontWeight: 600, whiteSpace: 'nowrap', marginLeft: 10 }}>
                Copy
              </button>
            </div>
          </div>
        </div>

        {/* Back to directory */}
        <div style={{ textAlign: 'center', marginTop: 20 }}>
          <button className="btn btn-ghost" onClick={() => navigate('/')} style={{ fontSize: 13 }}>
            ← Browse All Consultants
          </button>
        </div>
      </div>
    </div>
  )
}
