import React, { useState, useEffect, useCallback, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { toast } from 'react-hot-toast'
import { useAuthStore } from '../store/authStore'
import { consultantApi } from '../api'
import * as signalR from '@microsoft/signalr'
import {
  ChevronRight, Loader, User, Users, MessageSquare,
  Send, Check, CheckCheck, X, Bell
} from 'lucide-react'

// ═══════════════════════════════════════════════════════════════════════
// LOGIN PAGE
// ═══════════════════════════════════════════════════════════════════════
export function LoginPage() {
  const navigate = useNavigate()
  const { step1, step2, loading } = useAuthStore()
  const [phase, setPhase]   = useState(1)
  const [step1Data, setD]   = useState(null)
  const [email, setEmail]   = useState('')
  const [password, setPass] = useState('')
  const [locationId, setLoc]   = useState('')
  const [fiscalYearId, setFY]  = useState('')
  const [connection, setConn]  = useState('Production')

  const doStep1 = async e => {
    e.preventDefault()
    try {
      const d = await step1(email); setD(d)
      const cur = d.fiscalYears?.find(f => f.isCurrent)
      if (cur) setFY(cur.id)
      if (d.locations?.length === 1) setLoc(d.locations[0].id)
      setPhase(2)
    } catch (err) { toast.error(err.message) }
  }

  const doStep2 = async e => {
    e.preventDefault()
    if (!locationId || !fiscalYearId) { toast.error('Select location & fiscal year'); return }
    try {
      const user = await step2({ email, password, locationId, fiscalYearId, connection, rememberMe: false })
      if (!user.roleName?.toLowerCase().includes('consultant')) {
        toast.error('This portal is for consultants only')
        useAuthStore.getState().logout(); return
      }
      toast.success('Welcome back!'); navigate('/')
    } catch (err) { toast.error(err.message) }
  }

  return (
    <div className="login-page">
      <div className="login-box">
        <div className="login-logo">10X <span style={{ color: 'var(--accent)' }}>Consultant</span></div>
        <p className="login-sub">{phase === 1 ? 'Sign in to your consultant portal' : `Hi, ${step1Data?.userName}`}</p>

        {phase === 1 && (
          <form onSubmit={doStep1}>
            <div className="form-group">
              <label>Email</label>
              <input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="you@company.com" required autoFocus />
            </div>
            <button className="btn btn-primary" type="submit" disabled={loading} style={{ width: '100%', justifyContent: 'center', padding: 11 }}>
              {loading ? <Loader size={14} className="spin" /> : <>Continue <ChevronRight size={14} /></>}
            </button>
          </form>
        )}

        {phase === 2 && (
          <form onSubmit={doStep2}>
            <div className="form-group">
              <label>Password</label>
              <input type="password" value={password} onChange={e => setPass(e.target.value)} placeholder="••••••••" required autoFocus />
            </div>
            <div className="form-group">
              <label>Location</label>
              <select value={locationId} onChange={e => setLoc(e.target.value)} required>
                <option value="">Select…</option>
                {step1Data?.locations?.map(l => <option key={l.id} value={l.id}>{l.locationName}</option>)}
              </select>
            </div>
            <div className="grid-2">
              <div className="form-group">
                <label>Fiscal Year</label>
                <select value={fiscalYearId} onChange={e => setFY(e.target.value)} required>
                  <option value="">Select…</option>
                  {step1Data?.fiscalYears?.map(f => <option key={f.id} value={f.id}>{f.name}{f.isCurrent ? ' ★' : ''}</option>)}
                </select>
              </div>
              <div className="form-group">
                <label>Connection</label>
                <select value={connection} onChange={e => setConn(e.target.value)}>
                  {step1Data?.connections?.map(c => <option key={c}>{c}</option>)}
                </select>
              </div>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <button type="button" className="btn btn-ghost" onClick={() => setPhase(1)} style={{ flex: 1, justifyContent: 'center' }}>Back</button>
              <button type="submit" className="btn btn-primary" disabled={loading} style={{ flex: 2, justifyContent: 'center', padding: 11 }}>
                {loading ? <Loader size={14} className="spin" /> : 'Sign In'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  )
}

// ═══════════════════════════════════════════════════════════════════════
// DASHBOARD
// ═══════════════════════════════════════════════════════════════════════
export function DashboardPage() {
  const { user } = useAuthStore()
  const [stats, setStats] = useState({ clients: 0, pending: 0, unread: 0 })
  const navigate = useNavigate()

  useEffect(() => {
    Promise.all([
      consultantApi.getClients(1, 1, ''),
      consultantApi.getRequests(),
      consultantApi.getConversations(1, 50),
    ]).then(([c, r, m]) => setStats({
      clients: c.data.data?.totalRecords || 0,
      pending: r.data.data?.length || 0,
      unread:  (m.data.data?.items || []).reduce((s, c) => s + (c.unreadCount || 0), 0),
    })).catch(() => {})
  }, [])

  return (
    <>
      <div className="page-header">
        <h2>Welcome, {user?.userName?.split(' ')[0]} 👋</h2>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16, marginBottom: 20 }}>
        {[
          { label: 'Total Clients', val: stats.clients, color: 'var(--accent)',  icon: Users,         path: '/clients' },
          { label: 'Pending Requests', val: stats.pending, color: '#f7c948',  icon: Bell,  path: '/requests' },
          { label: 'Unread Messages', val: stats.unread,  color: 'var(--blue)', icon: MessageSquare, path: '/messages' },
        ].map(s => (
          <div key={s.label} className="card" style={{ cursor: 'pointer' }} onClick={() => navigate(s.path)}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
              <span style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: 1, color: 'var(--muted)' }}>{s.label}</span>
              <s.icon size={16} style={{ color: s.color }} />
            </div>
            <div style={{ fontFamily: 'var(--font-head)', fontSize: 32, fontWeight: 800, color: s.color }}>{s.val}</div>
          </div>
        ))}
      </div>

      <div className="card">
        <p style={{ color: 'var(--muted)', fontSize: 13 }}>
          Use the sidebar to manage your profile, view clients, handle connection requests, and chat with clients in real-time.
        </p>
      </div>
    </>
  )
}

// ═══════════════════════════════════════════════════════════════════════
// PROFILE PAGE
// ═══════════════════════════════════════════════════════════════════════
export function ProfilePage() {
  const [profile, setProfile] = useState(null)
  const [saving, setSaving]   = useState(false)
  const [form, setForm]       = useState({})

  useEffect(() => {
    consultantApi.getProfile()
      .then(r => { setProfile(r.data.data); setForm(r.data.data || {}) })
      .catch(() => toast.error('Failed to load profile'))
  }, [])

  const save = async () => {
    setSaving(true)
    try {
      await consultantApi.updateProfile(form)
      toast.success('Profile updated!')
    } catch { toast.error('Save failed') }
    finally { setSaving(false) }
  }

  const F = ({ name, label, type = 'text', ...p }) => (
    <div className="form-group">
      <label>{label}</label>
      <input type={type} value={form[name] || ''} onChange={e => setForm(f => ({ ...f, [name]: e.target.value }))} {...p} />
    </div>
  )

  return (
    <>
      <div className="page-header">
        <h2>My Profile</h2>
        <button className="btn btn-primary" onClick={save} disabled={saving}>
          {saving ? <Loader size={13} className="spin" /> : 'Save Changes'}
        </button>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div className="card">
          <h3 style={{ fontFamily: 'var(--font-head)', marginBottom: 16, fontSize: 14 }}>Basic Info</h3>
          <div className="form-group">
            <label>Bio / Tagline</label>
            <textarea rows={3} value={form.bio || ''} onChange={e => setForm(f => ({ ...f, bio: e.target.value }))} placeholder="Strategic consulting services for..." />
          </div>
          <F name="specialization" label="Specialization" placeholder="Business Strategy, Finance..." />
          <F name="experience" label="Experience" placeholder="10+ years" />
          <F name="hourlyRate" label="Hourly Rate (PKR)" type="number" placeholder="5000" />
          <F name="timezone" label="Timezone" placeholder="PKT (UTC+5)" />
        </div>

        <div className="card">
          <h3 style={{ fontFamily: 'var(--font-head)', marginBottom: 16, fontSize: 14 }}>Visibility</h3>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 16, padding: 14, background: 'var(--bg3)', borderRadius: 8 }}>
            <label className="toggle">
              <input type="checkbox" checked={form.isPublic ?? true} onChange={e => setForm(f => ({ ...f, isPublic: e.target.checked }))} />
              <span className="toggle-slider" />
            </label>
            <div>
              <div style={{ fontWeight: 600, fontSize: 13 }}>Public Profile</div>
              <div style={{ fontSize: 11, color: 'var(--muted)' }}>Visible in consultant directory</div>
            </div>
          </div>

          {/* Preview card */}
          <div style={{ padding: 16, background: 'var(--bg3)', borderRadius: 10, border: '1px solid var(--border)' }}>
            <div style={{ fontSize: 11, color: 'var(--muted)', marginBottom: 10, textTransform: 'uppercase', letterSpacing: 1 }}>Card Preview</div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 10 }}>
              <div style={{ width: 48, height: 48, borderRadius: '50%', background: 'var(--accent)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: 'var(--font-head)', fontWeight: 800, fontSize: 18, color: '#000', position: 'relative' }}>
                C
                {form.isPublic && <span style={{ position: 'absolute', bottom: 0, right: 0, width: 12, height: 12, borderRadius: '50%', background: 'var(--muted)', border: '2px solid var(--bg3)' }} />}
              </div>
              <div>
                <div style={{ fontWeight: 600, fontSize: 14 }}>Your Name</div>
                <div style={{ fontSize: 12, color: 'var(--muted)' }}>{form.specialization || 'Specialization'}</div>
              </div>
            </div>
            <p style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 10 }}>{form.bio || 'Your bio will appear here...'}</p>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ fontSize: 11, color: 'var(--accent)' }}>PKR {form.hourlyRate || '—'}/hr</span>
              <span style={{ fontSize: 11, padding: '3px 10px', borderRadius: 99, background: 'var(--accent)22', color: 'var(--accent)' }}>Connect ↗</span>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}

// ═══════════════════════════════════════════════════════════════════════
// CLIENTS PAGE
// ═══════════════════════════════════════════════════════════════════════
export function ClientsPage() {
  const [clients, setClients] = useState([])
  const [total, setTotal]     = useState(0)
  const [page, setPage]       = useState(1)
  const [search, setSearch]   = useState('')
  const navigate = useNavigate()

  const load = useCallback(async () => {
    try {
      const { data } = await consultantApi.getClients(page, 10, search)
      setClients(data.data?.items || []); setTotal(data.data?.totalRecords || 0)
    } catch { toast.error('Failed to load clients') }
  }, [page, search])

  useEffect(() => { load() }, [load])

  return (
    <>
      <div className="page-header">
        <h2>My Clients</h2>
        <span style={{ color: 'var(--muted)', fontSize: 13 }}>{total} total</span>
      </div>

      <div className="card">
        <div style={{ display: 'flex', gap: 8, marginBottom: 16, padding: '8px 12px', background: 'var(--bg3)', border: '1px solid var(--border)', borderRadius: 8 }}>
          <input placeholder="Search clients…" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }}
            style={{ background: 'none', border: 'none', padding: 0, flex: 1 }} />
        </div>

        {clients.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 60, color: 'var(--muted)' }}>
            <Users size={40} style={{ margin: '0 auto 12px', opacity: 0.3 }} />
            <p>No clients yet. Accept connection requests to add clients.</p>
          </div>
        ) : (
          <div style={{ display: 'grid', gap: 10 }}>
            {clients.map(c => (
              <div key={c.id} style={{ display: 'flex', alignItems: 'center', gap: 14, padding: 14, background: 'var(--bg3)', borderRadius: 10, border: '1px solid var(--border)' }}>
                <div style={{ width: 40, height: 40, borderRadius: '50%', background: 'var(--blue)33', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: 'var(--font-head)', fontWeight: 800, color: 'var(--blue)' }}>
                  {c.customerName?.charAt(0)?.toUpperCase()}
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600, fontSize: 14 }}>{c.customerName}</div>
                  <div style={{ fontSize: 12, color: 'var(--muted)' }}>{c.customerEmail}</div>
                </div>
                <div style={{ fontSize: 11, color: 'var(--muted)' }}>
                  Since {new Date(c.acceptedAt).toLocaleDateString()}
                </div>
                <button className="btn btn-sm" style={{ background: 'var(--accent)22', color: 'var(--accent)', border: 'none' }}
                  onClick={() => navigate(`/messages/${c.conversationId}`)}>
                  <MessageSquare size={12} /> Chat
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </>
  )
}

// ═══════════════════════════════════════════════════════════════════════
// REQUESTS PAGE
// ═══════════════════════════════════════════════════════════════════════
export function RequestsPage() {
  const [requests, setRequests] = useState([])
  const [loading, setLoading]   = useState(false)

  const load = async () => {
    setLoading(true)
    try { const { data } = await consultantApi.getRequests(); setRequests(data.data || []) }
    catch { toast.error('Failed') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [])

  const handle = async (id, action) => {
    try {
      if (action === 'accept') await consultantApi.acceptRequest(id)
      else await consultantApi.rejectRequest(id)
      toast.success(action === 'accept' ? 'Request accepted! Conversation started.' : 'Request rejected')
      load()
    } catch { toast.error('Failed') }
  }

  return (
    <>
      <div className="page-header">
        <h2>Connection Requests</h2>
        <span className="badge badge-blue">{requests.length} pending</span>
      </div>

      <div className="card">
        {loading && <p style={{ color: 'var(--muted)', textAlign: 'center', padding: 40 }}>Loading…</p>}
        {!loading && requests.length === 0 && (
          <div style={{ textAlign: 'center', padding: 60, color: 'var(--muted)' }}>
            <Bell size={40} style={{ margin: '0 auto 12px', opacity: 0.3 }} />
            <p>No pending requests</p>
          </div>
        )}
        <div style={{ display: 'grid', gap: 10 }}>
          {requests.map(r => (
            <div key={r.id} style={{ display: 'flex', alignItems: 'center', gap: 14, padding: 16, background: 'var(--bg3)', borderRadius: 10, border: '1px solid var(--border)' }}>
              <div style={{ width: 44, height: 44, borderRadius: '50%', background: 'var(--blue)22', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: 'var(--font-head)', fontWeight: 800, fontSize: 18, color: 'var(--blue)' }}>
                {r.customerName?.charAt(0)?.toUpperCase()}
              </div>
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 600, fontSize: 14, marginBottom: 2 }}>{r.customerName}</div>
                <div style={{ fontSize: 12, color: 'var(--muted)' }}>{r.customerEmail}</div>
                <div style={{ fontSize: 11, color: 'var(--muted)', marginTop: 4 }}>
                  Requested {new Date(r.requestedAt).toLocaleDateString()}
                </div>
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                <button className="btn btn-sm" style={{ background: 'var(--accent2)22', color: 'var(--accent2)', border: 'none' }}
                  onClick={() => handle(r.id, 'reject')}><X size={13} /> Reject</button>
                <button className="btn btn-sm btn-primary" onClick={() => handle(r.id, 'accept')}>
                  <Check size={13} /> Accept
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </>
  )
}

// ═══════════════════════════════════════════════════════════════════════
// MESSAGING PAGE — with SignalR real-time
// ═══════════════════════════════════════════════════════════════════════
export function MessagingPage() {
  const { id: paramConvId } = useParams()
  const navigate = useNavigate()
  const { user, accessToken } = useAuthStore()

  const [conversations, setConversations] = useState([])
  const [activeConv, setActiveConv]       = useState(paramConvId || null)
  const [messages, setMessages]           = useState([])
  const [msgText, setMsgText]             = useState('')
  const [isTyping, setIsTyping]           = useState(false)
  const [sending, setSending]             = useState(false)

  const connectionRef = useRef(null)
  const bottomRef     = useRef(null)
  const typingTimer   = useRef(null)

  // ── Load conversations ────────────────────────────────────────────────
  const loadConversations = useCallback(async () => {
    try {
      const { data } = await consultantApi.getConversations(1, 50)
      setConversations(data.data?.items || [])
    } catch {}
  }, [])

  useEffect(() => { loadConversations() }, [loadConversations])

  // ── Load messages for active conv ─────────────────────────────────────
  const loadMessages = useCallback(async (convId) => {
    if (!convId) return
    try {
      const { data } = await consultantApi.getMessages(convId, 1, 50)
      setMessages((data.data?.items || []).reverse())
      await consultantApi.markRead(convId)
    } catch {}
  }, [])

  useEffect(() => {
    if (activeConv) loadMessages(activeConv)
  }, [activeConv, loadMessages])

  // ── Scroll to bottom ──────────────────────────────────────────────────
  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }) }, [messages])

  // ── SignalR connection ────────────────────────────────────────────────
  useEffect(() => {
    if (!accessToken) return

   const conn = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/chat', {
    accessTokenFactory: () => accessToken
  })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Error)
  .build()

    conn.on('ReceiveMessage', msg => {
      if (msg.conversationId === activeConv) {
        setMessages(prev => [...prev, msg])
        consultantApi.markRead(msg.conversationId).catch(() => {})
      }
      loadConversations()
    })

    conn.on('TypingStarted', ({ conversationId }) => {
      if (conversationId === activeConv) setIsTyping(true)
    })
    conn.on('TypingStopped', ({ conversationId }) => {
      if (conversationId === activeConv) setIsTyping(false)
    })
    conn.on('NewConnectionRequest', () => {
      toast('New connection request!', { icon: '🔔' })
    })

    conn.start()
      .then(() => {
        connectionRef.current = conn
        if (activeConv) conn.invoke('JoinConversation', activeConv).catch(() => {})
      })
      .catch(() => {})

    return () => { conn.stop() }
  }, [accessToken])

  // ── Join new room when conv changes ───────────────────────────────────
  useEffect(() => {
    if (activeConv && connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      connectionRef.current.invoke('JoinConversation', activeConv).catch(() => {})
      connectionRef.current.invoke('MarkRead', activeConv).catch(() => {})
    }
  }, [activeConv])

  // ── Send message ──────────────────────────────────────────────────────
  const sendMessage = async () => {
    if (!msgText.trim() || !activeConv || sending) return
    setSending(true)
    try {
      // Try SignalR first, fallback to REST
      if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
        await connectionRef.current.invoke('SendMessage', activeConv, msgText.trim())
      } else {
        const { data } = await consultantApi.sendMessage(activeConv, { body: msgText.trim() })
        setMessages(prev => [...prev, data.data])
      }
      setMsgText('')
      stopTyping()
    } catch { toast.error('Send failed') }
    finally { setSending(false) }
  }

  // ── Typing indicators ─────────────────────────────────────────────────
  const startTyping = () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected)
      connectionRef.current.invoke('StartTyping', activeConv).catch(() => {})
    clearTimeout(typingTimer.current)
    typingTimer.current = setTimeout(stopTyping, 2000)
  }

  const stopTyping = () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected)
      connectionRef.current.invoke('StopTyping', activeConv).catch(() => {})
  }

  const activeConvData = conversations.find(c => c.id === activeConv)

  return (
    <div style={{ height: '100%', display: 'flex', gap: 0, overflow: 'hidden', margin: -20 }}>
      {/* ── CONVERSATION LIST ── */}
      <div className="chat-list">
        <div className="chat-list-header">Messages</div>
        <div className="chat-list-items">
          {conversations.length === 0 && (
            <div style={{ padding: 30, textAlign: 'center', color: 'var(--muted)', fontSize: 13 }}>
              No conversations yet.<br />Accept requests to start chatting.
            </div>
          )}
          {conversations.map(c => (
            <div key={c.id}
              className={`chat-item ${c.id === activeConv ? 'active' : ''}`}
              onClick={() => { setActiveConv(c.id); navigate(`/messages/${c.id}`) }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div className="chat-item-name">{c.customerName}</div>
                {c.unreadCount > 0 && <span className="chat-unread">{c.unreadCount}</span>}
              </div>
              <div className="chat-item-last">{c.lastMessage || 'Start a conversation'}</div>
              {c.lastMessageAt && (
                <div style={{ fontSize: 10, color: 'var(--muted)', marginTop: 3 }}>
                  {new Date(c.lastMessageAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </div>
              )}
            </div>
          ))}
        </div>
      </div>

      {/* ── CHAT WINDOW ── */}
      <div className="chat-window">
        {!activeConv ? (
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--muted)', flexDirection: 'column', gap: 10 }}>
            <MessageSquare size={48} style={{ opacity: 0.2 }} />
            <p>Select a conversation to start chatting</p>
          </div>
        ) : (
          <>
            <div className="chat-header">
              <div style={{ width: 36, height: 36, borderRadius: '50%', background: 'var(--blue)22', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 800, color: 'var(--blue)', fontFamily: 'var(--font-head)' }}>
                {activeConvData?.customerName?.charAt(0)?.toUpperCase()}
              </div>
              <div>
                <div style={{ fontWeight: 600, fontSize: 14 }}>{activeConvData?.customerName}</div>
                <div style={{ fontSize: 11, color: 'var(--muted)' }}>{activeConvData?.customerEmail}</div>
              </div>
            </div>

            <div className="chat-messages">
              {messages.map(m => {
                const isMine = m.senderId === user?.id
                return (
                  <div key={m.messageId || m.id}>
                    <div className={`msg ${isMine ? 'msg-sent' : 'msg-recv'}`}>
                      {m.body}
                      <div className="msg-time">
                        {new Date(m.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        {isMine && (m.isRead ? <CheckCheck size={10} style={{ marginLeft: 4 }} /> : <Check size={10} style={{ marginLeft: 4 }} />)}
                      </div>
                    </div>
                  </div>
                )
              })}
              {isTyping && <div className="typing-indicator">Customer is typing…</div>}
              <div ref={bottomRef} />
            </div>

            <div className="chat-input-bar">
              <textarea
                className="chat-input"
                placeholder="Type a message…"
                value={msgText}
                rows={1}
                onChange={e => { setMsgText(e.target.value); startTyping() }}
                onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage() } }}
              />
              <button className="send-btn" onClick={sendMessage} disabled={sending || !msgText.trim()}>
                {sending ? <Loader size={14} className="spin" /> : <Send size={14} />}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}

// ─────────────────────────────────────────────────────────
// EXPORT ALL PAGES
// ─────────────────────────────────────────────────────────

export const AllPages = {
  LoginPage,
  DashboardPage,
  ProfilePage,
  ClientsPage,
  RequestsPage,
  MessagingPage
}