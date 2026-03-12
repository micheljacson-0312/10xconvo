// ── CONSULTANT NOTIFICATIONS PAGE ────────────────────────────────────────────
import React, { useState, useEffect } from 'react'
import { toast } from 'react-hot-toast'
import { Bell, Check, CheckCheck, Clock, Trash2 } from 'lucide-react'
import { notifApi } from '../api'

function timeAgo(d) {
  const diff = Date.now() - new Date(d).getTime()
  const mins = Math.floor(diff / 60000)
  if (mins < 1)  return 'just now'
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24)  return `${hrs}h ago`
  return `${Math.floor(hrs / 24)}d ago`
}

export function NotificationsPage() {
  const [items, setItems]       = useState([])
  const [unread, setUnread]     = useState(0)
  const [loading, setLoading]   = useState(true)
  const [unreadOnly, setFilter] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const { data } = await notifApi.getAll(unreadOnly)
      setItems(data.data?.items || [])
      setUnread(data.data?.unreadCount || 0)
    } catch { toast.error('Failed to load') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [unreadOnly])

  const markRead = async (id) => {
    try { await notifApi.markRead(id); load() }
    catch { toast.error('Failed') }
  }

  const markAll = async () => {
    try { await notifApi.markAll(); toast.success('All marked as read'); load() }
    catch { toast.error('Failed') }
  }

  return (
    <>
      <div className="page-header">
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <h2>Notifications</h2>
          {unread > 0 && (
            <span style={{
              background: 'var(--accent)', color: '#fff', borderRadius: 10,
              padding: '1px 8px', fontSize: 12, fontWeight: 700,
            }}>{unread}</span>
          )}
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className={`btn ${unreadOnly ? 'btn-primary' : 'btn-ghost'}`}
            style={{ fontSize: 12 }} onClick={() => setFilter(f => !f)}>
            {unreadOnly ? 'Unread Only' : 'All'}
          </button>
          {unread > 0 && (
            <button className="btn btn-ghost" style={{ fontSize: 12 }} onClick={markAll}>
              <CheckCheck size={13} /> Mark All Read
            </button>
          )}
        </div>
      </div>

      {loading ? (
        <div style={{ padding: 40, textAlign: 'center', color: 'var(--muted)' }}>Loading…</div>
      ) : items.length === 0 ? (
        <div style={{ textAlign: 'center', padding: 60 }}>
          <Bell size={32} color="var(--border)" style={{ marginBottom: 12 }} />
          <p style={{ color: 'var(--muted)' }}>{unreadOnly ? 'No unread notifications' : 'No notifications yet'}</p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {items.map(n => (
            <div key={n.id}
              style={{
                display: 'flex', gap: 12, padding: '14px 16px',
                background: n.isRead ? 'var(--bg2)' : 'var(--bg3)',
                borderRadius: 10, border: `1px solid ${n.isRead ? 'var(--border)' : 'var(--accent)'}`,
                opacity: n.isRead ? 0.7 : 1, transition: 'all 0.15s',
              }}>
              <div style={{
                width: 8, height: 8, borderRadius: '50%', marginTop: 6, flexShrink: 0,
                background: n.isRead ? 'var(--border)' : 'var(--accent)',
              }} />
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 600, fontSize: 14 }}>{n.title}</div>
                <div style={{ color: 'var(--muted)', fontSize: 13, marginTop: 3 }}>{n.body}</div>
                <div style={{ color: 'var(--muted)', fontSize: 11, marginTop: 6, display: 'flex', alignItems: 'center', gap: 4 }}>
                  <Clock size={10} /> {timeAgo(n.createdOn)}
                </div>
              </div>
              {!n.isRead && (
                <button className="btn-icon" title="Mark read" onClick={() => markRead(n.id)}>
                  <Check size={13} color="var(--green)" />
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </>
  )
}

// ── NOTIFICATION BELL WIDGET (embed in layout header) ────────────────────────
export function NotificationBell({ notifApi: api }) {
  const [open, setOpen]   = useState(false)
  const [items, setItems] = useState([])
  const [unread, setUnread] = useState(0)

  const load = async () => {
    try {
      const { data } = await api.getAll(false)
      setItems((data.data?.items || []).slice(0, 8))
      setUnread(data.data?.unreadCount || 0)
    } catch {}
  }

  useEffect(() => {
    load()
    const t = setInterval(load, 30000) // poll every 30s
    return () => clearInterval(t)
  }, [])

  const markRead = async (id) => {
    try { await api.markRead(id); load() } catch {}
  }

  const markAll = async () => {
    try { await api.markAll(); load() } catch {}
  }

  return (
    <div style={{ position: 'relative' }}>
      <button onClick={() => setOpen(o => !o)}
        style={{
          position: 'relative', background: 'none', border: 'none',
          cursor: 'pointer', padding: 6, borderRadius: 8,
          color: unread > 0 ? 'var(--accent)' : 'var(--muted)',
        }}>
        <Bell size={18} />
        {unread > 0 && (
          <span style={{
            position: 'absolute', top: 0, right: 0,
            background: 'var(--accent2)', color: '#fff',
            borderRadius: '50%', width: 16, height: 16,
            fontSize: 9, fontWeight: 700,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>{unread > 9 ? '9+' : unread}</span>
        )}
      </button>

      {open && (
        <>
          <div style={{ position:'fixed', inset:0, zIndex:99 }} onClick={() => setOpen(false)} />
          <div style={{
            position: 'absolute', right: 0, top: 36, zIndex: 100,
            width: 320, maxHeight: 400, overflowY: 'auto',
            background: 'var(--bg2)', border: '1px solid var(--border)',
            borderRadius: 12, boxShadow: '0 8px 32px rgba(0,0,0,0.4)',
          }}>
            <div style={{
              padding: '12px 16px', borderBottom: '1px solid var(--border)',
              display: 'flex', justifyContent: 'space-between', alignItems: 'center',
            }}>
              <span style={{ fontWeight: 600, fontSize: 13 }}>Notifications {unread > 0 && `(${unread})`}</span>
              {unread > 0 && (
                <button onClick={markAll} style={{ background:'none', border:'none', cursor:'pointer',
                  color:'var(--accent)', fontSize:11 }}>Mark all read</button>
              )}
            </div>
            {items.length === 0 ? (
              <div style={{ padding: 24, textAlign: 'center', color: 'var(--muted)', fontSize: 13 }}>
                No notifications
              </div>
            ) : (
              items.map(n => (
                <div key={n.id}
                  onClick={() => { if (!n.isRead) markRead(n.id) }}
                  style={{
                    padding: '10px 16px', cursor: 'pointer',
                    background: n.isRead ? 'none' : 'rgba(108,99,255,0.06)',
                    borderBottom: '1px solid var(--border)',
                    display: 'flex', gap: 10, alignItems: 'flex-start',
                  }}>
                  <div style={{
                    width: 6, height: 6, borderRadius: '50%', marginTop: 5, flexShrink: 0,
                    background: n.isRead ? 'transparent' : 'var(--accent)',
                  }} />
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontWeight: n.isRead ? 400 : 600, fontSize: 13, overflow:'hidden', textOverflow:'ellipsis', whiteSpace:'nowrap' }}>
                      {n.title}
                    </div>
                    <div style={{ color: 'var(--muted)', fontSize: 12, marginTop: 2, overflow:'hidden', textOverflow:'ellipsis', whiteSpace:'nowrap' }}>
                      {n.body}
                    </div>
                    <div style={{ color: 'var(--muted)', fontSize: 11, marginTop: 3 }}>
                      {timeAgo(n.createdOn)}
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </>
      )}
    </div>
  )
}

export default NotificationsPage
