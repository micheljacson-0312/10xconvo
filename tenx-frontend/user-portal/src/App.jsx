import { ReviewsPage, ConsultantAvailabilityPage, UserNotificationsPage, ForgotPasswordPage } from './pages/PartBPages'
import { ConsultantDirectPage } from './pages/ConsultantDirectPage'
import BillingPage from './pages/BillingPage'
import React, { useState, useEffect, useCallback, useRef } from 'react'
import { Routes, Route, Navigate, useNavigate, useLocation, useParams } from 'react-router-dom'
import { Toaster, toast } from 'react-hot-toast'
import * as signalR from '@microsoft/signalr'
import { userApi, useAuthStore } from './api'
import {
  Search, MessageSquare, User, LogOut, Send,
  ChevronRight, Loader, Check, CheckCheck, X, Star, CreditCard
} from 'lucide-react'

// ─────────────────────────────────────────────────────────────────────────────
// NAVBAR
// ─────────────────────────────────────────────────────────────────────────────
function Navbar() {
  const navigate  = useNavigate()
  const location  = useLocation()
  const { user, logout, isLoggedIn } = useAuthStore()

  const handleLogout = async () => { await logout(); navigate('/') }

  return (
    <nav className="navbar">
      <div className="navbar-logo" onClick={() => navigate('/')} style={{ cursor: 'pointer' }}>
        10X <span>Convo</span>
      </div>
      <div className="navbar-links">
        <button className={`nav-link ${location.pathname === '/' ? 'active' : ''}`} onClick={() => navigate('/')}>
          Find Consultants
        </button>
        {isLoggedIn() && (
          <>
            <button className={`nav-link ${location.pathname.startsWith('/messages') ? 'active' : ''}`} onClick={() => navigate('/messages')}>
              Messages
            </button>
            <button className={`nav-link ${location.pathname === '/billing' ? 'active' : ''}`} onClick={() => navigate('/billing')}>
              <CreditCard size={14} style={{ display: 'inline', marginRight: 4 }} />
              Billing
            </button>
            <button className={`nav-link ${location.pathname === '/profile' ? 'active' : ''}`} onClick={() => navigate('/profile')}>
              <User size={14} style={{ display: 'inline', marginRight: 4 }} />
              {user?.userName?.split(' ')[0]}
            </button>
            <button className="nav-link" onClick={handleLogout} style={{ color: '#ef4444' }}>
              <LogOut size={13} style={{ display: 'inline', marginRight: 4 }} /> Logout
            </button>
          </>
        )}
        {!isLoggedIn() && (
          <button className="btn btn-primary" onClick={() => navigate('/login')} style={{ fontSize: 13, padding: '7px 16px' }}>
            Sign In
          </button>
        )}
      </div>
    </nav>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// HOME — Consultant Directory
// ─────────────────────────────────────────────────────────────────────────────
function HomePage() {
  const [consultants, setConsultants] = useState([])
  const [total, setTotal]             = useState(0)
  const [page, setPage]               = useState(1)
  const [search, setSearch]           = useState('')
  const [query, setQuery]             = useState('')
  const [loading, setLoading]         = useState(true)
  const [selected, setSelected]       = useState(null)
  const [connecting, setConnecting]   = useState(null)
  const { isLoggedIn } = useAuthStore()
  const navigate = useNavigate()

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await userApi.getConsultants(page, 12, query)
      setConsultants(data.data?.items || [])
      setTotal(data.data?.totalRecords || 0)
    } catch { } finally { setLoading(false) }
  }, [page, query])

  useEffect(() => { load() }, [load])

  const handleConnect = async (consultantId, e) => {
    e.stopPropagation()
    if (!isLoggedIn()) { navigate('/login'); return }
    setConnecting(consultantId)
    try {
      await userApi.connect(consultantId)
      toast.success('Connection request sent!')
      setConsultants(cs => cs.map(c => c.id === consultantId ? { ...c, connectionStatus: 'pending' } : c))
    } catch (err) {
      toast.error(err.response?.data?.message || 'Already connected or pending')
    } finally { setConnecting(null) }
  }

  return (
    <div style={{ minHeight: '100vh' }}>
      <Navbar />
      <div style={{ maxWidth: 1100, margin: '0 auto', padding: '0 20px' }}>
        {/* ── HERO ── */}
        <div className="hero">
          <h1>Find Expert <span>Consultants</span><br />for Your Business</h1>
          <p>Connect with verified professionals across strategy, finance, marketing, tech and more.</p>
          <div className="search-wrap">
            <input
              className="search-input"
              placeholder="Search by name, specialization…"
              value={search}
              onChange={e => setSearch(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && setQuery(search)}
            />
            <button className="search-btn" onClick={() => setQuery(search)}>
              <Search size={16} />
            </button>
          </div>
        </div>

        {/* ── STATS ── */}
        <div style={{ display: 'flex', justifyContent: 'center', gap: 40, marginBottom: 40, color: 'var(--muted)', fontSize: 13 }}>
          <span><strong style={{ color: 'var(--text)', fontFamily: 'var(--font-head)', fontSize: 20 }}>{total}</strong> Consultants</span>
          <span><strong style={{ color: 'var(--text)', fontFamily: 'var(--font-head)', fontSize: 20 }}>3</strong> Portals</span>
          <span><strong style={{ color: 'var(--text)', fontFamily: 'var(--font-head)', fontSize: 20 }}>∞</strong> Possibilities</span>
        </div>

        {/* ── GRID ── */}
        {loading ? (
          <div style={{ textAlign: 'center', padding: 60, color: 'var(--muted)' }}><Loader size={24} className="spin" style={{ margin: '0 auto' }} /></div>
        ) : consultants.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 60, color: 'var(--muted)' }}>No consultants found for "{query}"</div>
        ) : (
          <div className="consultant-grid">
            {consultants.map(c => (
              <div key={c.id} className="consultant-card" onClick={() => setSelected(c)}>
                <div className="card-avatar-wrap">
                  <div className="card-avatar">{c.userName?.charAt(0)?.toUpperCase()}</div>
                  {c.isOnline ? <span className="online-ring" /> : <span className="offline-ring" />}
                </div>
                <div className="card-name">{c.userName}</div>
                <div className="card-spec">{c.specialization || 'Consultant'}</div>
                <div className="card-bio">{c.bio || 'Expert consultant available for consultation.'}</div>
                {c.experience && (
                  <div style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 10 }}>📅 {c.experience}</div>
                )}
                <div className="card-meta">
                  <div className="card-rate">
                    {c.hourlyRate ? <>PKR {Number(c.hourlyRate).toLocaleString()} <span>/hr</span></> : <span>Rate on request</span>}
                  </div>
                  {c.connectionStatus === 'accepted' ? (
                    <button className="connect-btn connected" onClick={e => { e.stopPropagation(); navigate('/messages') }}>
                      Chat ↗
                    </button>
                  ) : c.connectionStatus === 'pending' ? (
                    <button className="connect-btn pending" disabled>Pending…</button>
                  ) : (
                    <button
                      className="connect-btn"
                      disabled={connecting === c.id}
                      onClick={e => handleConnect(c.id, e)}
                    >
                      {connecting === c.id ? <Loader size={11} className="spin" /> : 'Connect ↗'}
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}

        {/* ── PAGINATION ── */}
        {total > 12 && (
          <div style={{ display: 'flex', justifyContent: 'center', gap: 8, marginTop: 40 }}>
            {Array.from({ length: Math.ceil(total / 12) }, (_, i) => i + 1).slice(0, 7).map(p => (
              <button key={p} onClick={() => setPage(p)}
                style={{ width: 36, height: 36, borderRadius: 8, border: '1px solid var(--border)', background: p === page ? 'var(--accent)' : 'var(--bg2)', color: p === page ? '#fff' : 'var(--text)', cursor: 'pointer', fontFamily: 'var(--font-head)', fontWeight: 600 }}>
                {p}
              </button>
            ))}
          </div>
        )}
        <div style={{ height: 60 }} />
      </div>

      {/* ── CONSULTANT DETAIL MODAL ── */}
      {selected && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setSelected(null)}>
          <div className="modal">
            <div className="modal-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                <div style={{ width: 52, height: 52, borderRadius: '50%', background: 'linear-gradient(135deg, var(--accent), var(--accent2))', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: 'var(--font-head)', fontWeight: 800, fontSize: 22, color: '#fff' }}>
                  {selected.userName?.charAt(0)?.toUpperCase()}
                </div>
                <div>
                  <h3>{selected.userName}</h3>
                  <span style={{ fontSize: 13, color: 'var(--accent)', fontWeight: 600 }}>{selected.specialization}</span>
                </div>
              </div>
              <button onClick={() => setSelected(null)} style={{ background: 'var(--bg3)', border: '1px solid var(--border)', borderRadius: 8, padding: 6, cursor: 'pointer' }}><X size={14} /></button>
            </div>
            <div className="modal-body">
              <div style={{ display: 'flex', gap: 12, marginBottom: 16 }}>
                <span className={`badge ${selected.isOnline ? 'badge-green' : 'badge-blue'}`}>
                  {selected.isOnline ? '🟢 Online' : '⚫ Offline'}
                </span>
                {selected.timezone && <span className="badge badge-blue">🕐 {selected.timezone}</span>}
              </div>
              {selected.bio && <p style={{ color: 'var(--muted)', lineHeight: 1.7, marginBottom: 16 }}>{selected.bio}</p>}
              {selected.experience && <p style={{ fontSize: 13, marginBottom: 8 }}><strong>Experience:</strong> {selected.experience}</p>}
              {selected.hourlyRate && (
                <p style={{ fontSize: 18, fontFamily: 'var(--font-head)', fontWeight: 800, color: 'var(--accent)', marginBottom: 16 }}>
                  PKR {Number(selected.hourlyRate).toLocaleString()} / hr
                </p>
              )}
            </div>
            <div className="modal-footer">
              <button className="btn btn-ghost" onClick={() => setSelected(null)}>Close</button>
              {selected.connectionStatus === 'accepted' ? (
                <button className="btn btn-primary" onClick={() => { setSelected(null); navigate('/messages') }}>Open Chat ↗</button>
              ) : selected.connectionStatus === 'pending' ? (
                <button className="btn" disabled style={{ background: '#f59e0b', color: '#fff' }}>Request Pending…</button>
              ) : (
                <button className="btn btn-primary" disabled={connecting === selected.id}
                  onClick={async e => { await handleConnect(selected.id, e); setSelected(s => ({ ...s, connectionStatus: 'pending' })) }}>
                  {connecting === selected.id ? <Loader size={14} className="spin" /> : 'Send Connect Request'}
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// LOGIN PAGE
// ─────────────────────────────────────────────────────────────────────────────
function LoginPage() {
  const navigate = useNavigate()
  const { step1, step2, loading, step1Data } = useAuthStore()
  const [phase, setPhase] = useState(1)
  const [email, setEmail] = useState('')
  const [pass, setPass]   = useState('')
  const [locId, setLoc]   = useState('')
  const [fyId, setFY]     = useState('')
  const [conn, setConn]   = useState('Production')
  const [socialLoading, setSocialLoading] = useState(null)

  const doStep1 = async e => {
    e.preventDefault()
    try {
      const d = await step1(email)
      const cur = d.fiscalYears?.find(f => f.isCurrent); if (cur) setFY(cur.id)
      if (d.locations?.length === 1) setLoc(d.locations[0].id)
      setPhase(2)
    } catch (err) { toast.error(err.message) }
  }

  const doStep2 = async e => {
    e.preventDefault()
    if (!locId || !fyId) { toast.error('Select location & fiscal year'); return }
    try {
      await step2({ email, password: pass, locationId: locId, fiscalYearId: fyId, connection: conn, rememberMe: false })
      toast.success('Welcome back!'); navigate('/')
    } catch (err) { toast.error(err.message) }
  }

  // ── Social Login (Firebase) ─────────────────────────────────────────────
  const handleSocialLogin = async (providerFn, providerName) => {
    setSocialLoading(providerName)
    try {
      const { signInWithGoogle, signInWithFacebook } = await import('./firebaseAuth.js')
      const fn = providerName === 'Google' ? signInWithGoogle : signInWithFacebook
      const claims = await fn()
      const { data } = await (await import('./api.js')).authApi.externalLogin(claims)
      const { accessToken, refreshToken, user } = data.data
      localStorage.setItem('accessToken', accessToken)
      localStorage.setItem('refreshToken', refreshToken)
      localStorage.setItem('user', JSON.stringify(user))
      useAuthStore.setState({ accessToken, refreshToken, user })
      toast.success(`Welcome, ${user.userName}!`)
      navigate('/')
    } catch (err) {
      const msg = err?.response?.data?.message || err?.message || 'Social login failed'
      if (!msg.includes('popup-closed')) toast.error(msg)
    } finally { setSocialLoading(null) }
  }

  return (
    <>
      <Navbar />
      <div className="login-page" style={{ marginTop: -60, paddingTop: 60 }}>
        <div className="login-box">
          <div className="login-logo">Sign <span style={{ color: 'var(--accent)' }}>In</span></div>
          <p className="login-sub">{phase === 1 ? 'Enter your email to continue' : `Welcome, ${step1Data?.userName}`}</p>

          {phase === 1 && (
            <form onSubmit={doStep1}>
              <div className="form-group">
                <label>Email</label>
                <input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="you@company.com" required autoFocus />
              </div>
              <button className="btn btn-primary" type="submit" disabled={loading} style={{ width: '100%', justifyContent: 'center', padding: 12 }}>
                {loading ? <Loader size={14} className="spin" /> : <>Continue <ChevronRight size={14} /></>}
              </button>

              {/* ── Social Login Divider ── */}
              <div style={{ display: 'flex', alignItems: 'center', gap: 12, margin: '20px 0 16px' }}>
                <div style={{ flex: 1, height: 1, background: 'var(--border)' }} />
                <span style={{ fontSize: 12, color: 'var(--muted)', whiteSpace: 'nowrap' }}>or continue with</span>
                <div style={{ flex: 1, height: 1, background: 'var(--border)' }} />
              </div>

              <div style={{ display: 'flex', gap: 10 }}>
                <button type="button" onClick={() => handleSocialLogin(null, 'Google')} disabled={!!socialLoading}
                  style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8, padding: '10px 0', borderRadius: 10, border: '1px solid var(--border)', background: 'var(--bg2)', cursor: 'pointer', fontSize: 13, fontWeight: 600, color: 'var(--text)' }}>
                  {socialLoading === 'Google' ? <Loader size={14} className="spin" /> : <><svg width="16" height="16" viewBox="0 0 48 48"><path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/><path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/><path fill="#FBBC05" d="M10.53 28.59A14.5 14.5 0 019.5 24c0-1.59.28-3.14.76-4.59l-7.98-6.19A23.9 23.9 0 000 24c0 3.77.9 7.35 2.56 10.54l7.97-5.95z"/><path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 5.95C6.51 42.62 14.62 48 24 48z"/></svg> Google</>}
                </button>
                <button type="button" onClick={() => handleSocialLogin(null, 'Facebook')} disabled={!!socialLoading}
                  style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8, padding: '10px 0', borderRadius: 10, border: '1px solid var(--border)', background: 'var(--bg2)', cursor: 'pointer', fontSize: 13, fontWeight: 600, color: 'var(--text)' }}>
                  {socialLoading === 'Facebook' ? <Loader size={14} className="spin" /> : <><svg width="16" height="16" viewBox="0 0 48 48"><path fill="#1877F2" d="M48 24C48 10.745 37.255 0 24 0S0 10.745 0 24c0 11.979 8.776 21.908 20.25 23.708v-16.77h-6.094V24h6.094v-5.288c0-6.014 3.583-9.337 9.065-9.337 2.625 0 5.372.469 5.372.469v5.906h-3.026c-2.981 0-3.911 1.85-3.911 3.75V24h6.656l-1.064 6.938H27.75v16.77C39.224 45.908 48 35.978 48 24z"/></svg> Facebook</>}
                </button>
              </div>
            </form>
          )}

          {phase === 2 && (
            <form onSubmit={doStep2}>
              <div className="form-group">
                <label>Password</label>
                <input type="password" value={pass} onChange={e => setPass(e.target.value)} required autoFocus />
                <div style={{ textAlign: 'right', marginTop: 4 }}>
                  <button onClick={() => navigate('/forgot-password')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 12, color: 'var(--accent)' }}>
                    Forgot password?
                  </button>
                </div>
              </div>
              <div className="form-group">
                <label>Location</label>
                <select value={locId} onChange={e => setLoc(e.target.value)} required>
                  <option value="">Select…</option>
                  {step1Data?.locations?.map(l => <option key={l.id} value={l.id}>{l.locationName}</option>)}
                </select>
              </div>
              <div className="grid-2">
                <div className="form-group">
                  <label>Fiscal Year</label>
                  <select value={fyId} onChange={e => setFY(e.target.value)} required>
                    <option value="">Select…</option>
                    {step1Data?.fiscalYears?.map(f => <option key={f.id} value={f.id}>{f.name}{f.isCurrent ? ' ★' : ''}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label>Connection</label>
                  <select value={conn} onChange={e => setConn(e.target.value)}>
                    {step1Data?.connections?.map(c => <option key={c}>{c}</option>)}
                  </select>
                </div>
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                <button type="button" className="btn btn-ghost" onClick={() => setPhase(1)} style={{ flex: 1, justifyContent: 'center' }}>Back</button>
                <button type="submit" className="btn btn-primary" disabled={loading} style={{ flex: 2, justifyContent: 'center', padding: 12 }}>
                  {loading ? <Loader size={14} className="spin" /> : 'Sign In'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// PROFILE PAGE
// ─────────────────────────────────────────────────────────────────────────────
function ProfilePage() {
  const { user } = useAuthStore()
  const [profile, setProfile] = useState(null)
  const [form, setForm]       = useState({})
  const [saving, setSaving]   = useState(false)

  useEffect(() => {
    userApi.getProfile().then(r => { setProfile(r.data.data); setForm(r.data.data || {}) }).catch(() => {})
  }, [])

  const save = async () => {
    setSaving(true)
    try { await userApi.updateProfile(form); toast.success('Profile updated!') }
    catch { toast.error('Save failed') } finally { setSaving(false) }
  }

  return (
    <>
      <Navbar />
      <div className="page">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
          <h2 style={{ fontFamily: 'var(--font-head)', fontSize: 22, fontWeight: 800 }}>My Profile</h2>
          <button className="btn btn-primary" onClick={save} disabled={saving}>
            {saving ? <Loader size={13} className="spin" /> : 'Save'}
          </button>
        </div>

        <div className="card" style={{ maxWidth: 540 }}>
          {[
            ['bio',         'Bio',          'Tell consultants about yourself…', 'textarea'],
            ['companyName', 'Company Name', 'Your company'],
            ['industry',    'Industry',     'e.g. Retail, Tech, Healthcare'],
            ['cityName',    'City',         'e.g. Karachi'],
          ].map(([k, l, p, t]) => (
            <div key={k} className="form-group">
              <label>{l}</label>
              {t === 'textarea'
                ? <textarea rows={3} value={form[k] || ''} onChange={e => setForm(f => ({ ...f, [k]: e.target.value }))} placeholder={p} />
                : <input value={form[k] || ''} onChange={e => setForm(f => ({ ...f, [k]: e.target.value }))} placeholder={p} />
              }
            </div>
          ))}
        </div>
      </div>
    </>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// MESSAGES PAGE — with SignalR real-time
// ─────────────────────────────────────────────────────────────────────────────
function MessagesPage() {
  const { id: paramId } = useParams()
  const navigate = useNavigate()
  const { user, accessToken } = useAuthStore()
  const [conversations, setConvs]   = useState([])
  const [activeId, setActiveId]     = useState(paramId || null)
  const [messages, setMsgs]         = useState([])
  const [text, setText]             = useState('')
  const [isTyping, setIsTyping]     = useState(false)
  const [sending, setSending]       = useState(false)
  const connRef  = useRef(null)
  const bottomRef = useRef(null)

  const loadConvs = useCallback(async () => {
    try { const { data } = await userApi.getConversations(1, 50); setConvs(data.data?.items || []) }
    catch {}
  }, [])

  useEffect(() => { loadConvs() }, [loadConvs])

  useEffect(() => {
    if (!activeId) return
    userApi.getMessages(activeId, 1, 50)
      .then(r => { setMsgs((r.data.data?.items || []).reverse()); userApi.markRead(activeId).catch(() => {}) })
      .catch(() => {})
  }, [activeId])

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }) }, [messages])

  // SignalR
  useEffect(() => {
    if (!accessToken) return
    const conn = new signalR.HubConnectionBuilder()
.withUrl('/hubs/chat', {
  accessTokenFactory: () => accessToken
})
      .withAutomaticReconnect().configureLogging(signalR.LogLevel.Error).build()

    conn.on('ReceiveMessage', msg => {
      if (msg.conversationId === activeId) setMsgs(p => [...p, msg])
      loadConvs()
    })
    conn.on('TypingStarted',   ({ conversationId }) => { if (conversationId === activeId) setIsTyping(true) })
    conn.on('TypingStopped',   ({ conversationId }) => { if (conversationId === activeId) setIsTyping(false) })
    conn.on('ConnectionAccepted', () => { toast('Your request was accepted! You can now chat.', { icon: '🎉' }); loadConvs() })

    conn.start().then(() => {
      connRef.current = conn
      if (activeId) conn.invoke('JoinConversation', activeId).catch(() => {})
    }).catch(() => {})

    return () => conn.stop()
  }, [accessToken])

  useEffect(() => {
    if (activeId && connRef.current?.state === signalR.HubConnectionState.Connected)
      connRef.current.invoke('JoinConversation', activeId).catch(() => {})
  }, [activeId])

  const send = async () => {
    if (!text.trim() || !activeId || sending) return
    setSending(true)
    try {
      if (connRef.current?.state === signalR.HubConnectionState.Connected)
        await connRef.current.invoke('SendMessage', activeId, text.trim())
      else { const { data } = await userApi.sendMessage(activeId, { body: text.trim() }); setMsgs(p => [...p, data.data]) }
      setText('')
    } catch { toast.error('Send failed') } finally { setSending(false) }
  }

  const activeConv = conversations.find(c => c.id === activeId)

  return (
    <>
      <Navbar />
      <div className="chat-layout">
        {/* Conversation list */}
        <div className="chat-list">
          <div className="chat-list-header">Messages</div>
          <div className="chat-list-items">
            {conversations.length === 0 && (
              <div style={{ padding: 30, textAlign: 'center', color: 'var(--muted)', fontSize: 13 }}>
                Connect with a consultant to start chatting!
              </div>
            )}
            {conversations.map(c => (
              <div key={c.id}
                className={`chat-item ${c.id === activeId ? 'active' : ''}`}
                onClick={() => { setActiveId(c.id); navigate(`/messages/${c.id}`) }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <div className="chat-item-name">{c.consultantName}</div>
                  {c.unreadCount > 0 && <span className="chat-unread">{c.unreadCount}</span>}
                </div>
                <div className="chat-item-last">{c.lastMessage || 'Say hello!'}</div>
              </div>
            ))}
          </div>
        </div>

        {/* Chat window */}
        <div className="chat-window">
          {!activeId ? (
            <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--muted)', flexDirection: 'column', gap: 12 }}>
              <MessageSquare size={52} style={{ opacity: 0.15 }} />
              <p>Select a conversation</p>
            </div>
          ) : (
            <>
              <div className="chat-header">
                <div style={{ width: 38, height: 38, borderRadius: '50%', background: 'linear-gradient(135deg, var(--accent), var(--accent2))', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: 'var(--font-head)', fontWeight: 800, color: '#fff' }}>
                  {activeConv?.consultantName?.charAt(0)?.toUpperCase()}
                </div>
                <div>
                  <div style={{ fontWeight: 700, fontSize: 14 }}>{activeConv?.consultantName}</div>
                  <div style={{ fontSize: 11, color: 'var(--muted)' }}>Consultant</div>
                </div>
              </div>

              <div className="chat-messages">
                {messages.map(m => {
                  const mine = m.senderId === user?.id
                  return (
                    <div key={m.messageId || m.id} className={`msg ${mine ? 'msg-sent' : 'msg-recv'}`}>
                      {m.body}
                      <div className="msg-time">
                        {new Date(m.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        {mine && (m.isRead ? <CheckCheck size={10} style={{ marginLeft: 4 }} /> : <Check size={10} style={{ marginLeft: 4 }} />)}
                      </div>
                    </div>
                  )
                })}
                {isTyping && <div className="typing-indicator">Consultant is typing…</div>}
                <div ref={bottomRef} />
              </div>

              <div className="chat-input-bar">
                <textarea className="chat-input" placeholder="Type a message…" value={text} rows={1}
                  onChange={e => { setText(e.target.value); if (connRef.current?.state === signalR.HubConnectionState.Connected) connRef.current.invoke('StartTyping', activeId).catch(() => {}) }}
                  onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send() } }}
                />
                <button className="send-btn" onClick={send} disabled={sending || !text.trim()}>
                  {sending ? <Loader size={14} className="spin" /> : <Send size={14} />}
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// APP ROUTER
// ─────────────────────────────────────────────────────────────────────────────
export default function App() {
  const isLoggedIn = useAuthStore(s => s.isLoggedIn())

  return (
    <>
      <Toaster position="top-right" toastOptions={{
        style: { background: '#fff', color: '#0f172a', border: '1px solid #e2e8f0', boxShadow: '0 4px 12px rgba(0,0,0,0.08)' }
      }} />
      <Routes>
        <Route path="/"             element={<HomePage />} />
        <Route path="/c/:slug"      element={<ConsultantDirectPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/login"        element={<LoginPage />} />
        <Route path="/profile"      element={isLoggedIn ? <ProfilePage /> : <Navigate to="/login" />} />
        <Route path="/billing"      element={isLoggedIn ? <BillingPage /> : <Navigate to="/login" />} />
        <Route path="/messages"     element={isLoggedIn ? <MessagesPage /> : <Navigate to="/login" />} />
        <Route path="/notifications" element={isLoggedIn ? <UserNotificationsPage /> : <Navigate to="/login" />} />
        <Route path="/consultants/:consultantId/reviews" element={<ReviewsPage />} />
        <Route path="/consultants/:consultantId/availability" element={<ConsultantAvailabilityPage />} />
        <Route path="/messages/:id" element={isLoggedIn ? <MessagesPage /> : <Navigate to="/login" />} />
        <Route path="/notifications" element={isLoggedIn ? <UserNotificationsPage /> : <Navigate to="/login" />} />
        <Route path="/consultants/:consultantId/reviews" element={<ReviewsPage />} />
        <Route path="/consultants/:consultantId/availability" element={<ConsultantAvailabilityPage />} />
        <Route path="*"             element={<Navigate to="/" />} />
      </Routes>
    </>
  )
}
