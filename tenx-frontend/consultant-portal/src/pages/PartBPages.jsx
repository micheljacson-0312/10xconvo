// ═══════════════════════════════════════════════════════════════════════════
//  CONSULTANT PORTAL — PART B PAGES
//  AvailabilityPage, NotificationsPage
// ═══════════════════════════════════════════════════════════════════════════
import React, { useState, useEffect, useCallback } from 'react'
import { toast } from 'react-hot-toast'
import { Plus, Trash2, Save, Bell, Check, CheckCheck, Clock } from 'lucide-react'
import { api } from '../api'

// ── API helpers ───────────────────────────────────────────────────────────────
const availApi = {
  get:   ()     => api.get('/consultant/availability'),
  save:  slots  => api.put('/consultant/availability', slots),
  clear: ()     => api.delete('/consultant/availability'),
}
const notifApi = {
  get:        (unreadOnly, page) => api.get('/consultant/notifications', { params: { unreadOnly, page, pageSize: 20 } }),
  markRead:   id                 => api.put(`/consultant/notifications/${id}/read`),
  markAllRead: ()                => api.put('/consultant/notifications/read-all'),
}

const DAYS = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday']
const DEFAULT_SLOT = { startTime: '09:00', endTime: '17:00', isAvailable: true }

// ── 1. AVAILABILITY SCHEDULE ─────────────────────────────────────────────────
export function AvailabilityPage() {
  const [schedule, setSchedule] = useState(
    DAYS.map((day, i) => ({ day, dayNumber: i, slots: [], isWorkDay: false }))
  )
  const [loading, setLoading]   = useState(true)
  const [saving, setSaving]     = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const r = await availApi.get()
      const data = r.data.data
      setSchedule(DAYS.map((day, i) => {
        const found = data.find(d => d.dayNumber === i)
        return {
          day, dayNumber: i,
          slots: found?.slots?.map(s => ({ startTime: s.startTime, endTime: s.endTime, isAvailable: s.isAvailable })) || [],
          isWorkDay: found?.isWorkDay || false
        }
      }))
    } catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [])

  useEffect(() => { load() }, [load])

  const toggleDay = (i) => {
    setSchedule(sc => sc.map((d, idx) => idx !== i ? d : {
      ...d,
      isWorkDay: !d.isWorkDay,
      slots: !d.isWorkDay && d.slots.length === 0 ? [{ ...DEFAULT_SLOT }] : d.slots
    }))
  }

  const addSlot = (i) => {
    setSchedule(sc => sc.map((d, idx) => idx !== i ? d : {
      ...d, slots: [...d.slots, { ...DEFAULT_SLOT }]
    }))
  }

  const removeSlot = (dayIdx, slotIdx) => {
    setSchedule(sc => sc.map((d, i) => i !== dayIdx ? d : {
      ...d,
      slots: d.slots.filter((_, si) => si !== slotIdx),
      isWorkDay: d.slots.length <= 1 ? false : d.isWorkDay
    }))
  }

  const updateSlot = (dayIdx, slotIdx, field, val) => {
    setSchedule(sc => sc.map((d, i) => i !== dayIdx ? d : {
      ...d,
      slots: d.slots.map((s, si) => si !== slotIdx ? s : { ...s, [field]: val })
    }))
  }

  const save = async () => {
    const flat = []
    for (const d of schedule) {
      for (const s of d.slots) {
        flat.push({ dayOfWeek: d.dayNumber, startTime: s.startTime, endTime: s.endTime, isAvailable: d.isWorkDay && s.isAvailable })
      }
    }
    setSaving(true)
    try {
      await availApi.save(flat)
      toast.success('Schedule saved!')
    } catch (e) { toast.error(e.response?.data?.message || 'Save failed') }
    finally { setSaving(false) }
  }

  const clearAll = async () => {
    if (!confirm('Clear entire schedule?')) return
    try { await availApi.clear(); await load(); toast.success('Schedule cleared') }
    catch { toast.error('Failed') }
  }

  if (loading) return <div className="spinner" style={{ marginTop: 80 }} />

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Availability Schedule</h2>
          <p style={{ color: 'var(--muted)', fontSize: 13, margin: '4px 0 0' }}>Set your weekly working hours</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="btn btn-ghost" onClick={clearAll} style={{ fontSize: 13 }}>Clear All</button>
          <button className="btn btn-primary" onClick={save} disabled={saving}>
            <Save size={14} /> {saving ? 'Saving...' : 'Save Schedule'}
          </button>
        </div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {schedule.map((d, i) => (
          <div key={d.day} className="card" style={{ padding: '14px 20px', display: 'flex', alignItems: 'flex-start', gap: 16 }}>
            {/* Day toggle */}
            <div style={{ minWidth: 140, display: 'flex', alignItems: 'center', gap: 10, paddingTop: 2 }}>
              <label className="toggle-switch">
                <input type="checkbox" checked={d.isWorkDay} onChange={() => toggleDay(i)} />
                <span className="toggle-slider" />
              </label>
              <span style={{ fontWeight: 600, fontSize: 14, color: d.isWorkDay ? 'var(--text)' : 'var(--muted)' }}>{d.day}</span>
            </div>

            {/* Slots */}
            <div style={{ flex: 1 }}>
              {!d.isWorkDay ? (
                <span style={{ fontSize: 13, color: 'var(--muted)' }}>Not available</span>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  {d.slots.map((slot, si) => (
                    <div key={si} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 6, background: 'var(--surface)', padding: '6px 12px', borderRadius: 8, fontSize: 13 }}>
                        <Clock size={13} style={{ color: 'var(--muted)' }} />
                        <input type="time" value={slot.startTime}
                          onChange={e => updateSlot(i, si, 'startTime', e.target.value)}
                          style={{ background: 'transparent', border: 'none', outline: 'none', fontSize: 13, color: 'var(--text)', cursor: 'pointer' }} />
                        <span style={{ color: 'var(--muted)' }}>—</span>
                        <input type="time" value={slot.endTime}
                          onChange={e => updateSlot(i, si, 'endTime', e.target.value)}
                          style={{ background: 'transparent', border: 'none', outline: 'none', fontSize: 13, color: 'var(--text)', cursor: 'pointer' }} />
                      </div>
                      <button onClick={() => removeSlot(i, si)}
                        style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--muted)', padding: 4, borderRadius: 4 }}>
                        <Trash2 size={13} />
                      </button>
                    </div>
                  ))}
                  <button onClick={() => addSlot(i)}
                    style={{ alignSelf: 'flex-start', display: 'flex', alignItems: 'center', gap: 5, fontSize: 12, color: 'var(--accent)', background: 'var(--accent)11', border: '1px dashed var(--accent)', borderRadius: 6, padding: '4px 10px', cursor: 'pointer' }}>
                    <Plus size={12} /> Add slot
                  </button>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Toggle switch CSS */}
      <style>{`
        .toggle-switch { position: relative; display: inline-block; width: 36px; height: 20px; cursor: pointer; }
        .toggle-switch input { opacity: 0; width: 0; height: 0; }
        .toggle-slider { position: absolute; inset: 0; background: var(--border); border-radius: 20px; transition: .2s; }
        .toggle-slider::before { content: ''; position: absolute; width: 14px; height: 14px; left: 3px; top: 3px; background: white; border-radius: 50%; transition: .2s; }
        .toggle-switch input:checked + .toggle-slider { background: var(--accent); }
        .toggle-switch input:checked + .toggle-slider::before { transform: translateX(16px); }
      `}</style>
    </>
  )
}

// ── 2. NOTIFICATIONS BELL ────────────────────────────────────────────────────
export function ConsultantNotificationsPage() {
  const [items, setItems]     = useState([])
  const [page, setPage]       = useState(1)
  const [total, setTotal]     = useState(0)
  const [unread, setUnread]   = useState(0)
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const r = await notifApi.get(unreadOnly, page)
      setItems(r.data.data.items)
      setTotal(r.data.data.totalRecords)
      setUnread(r.data.data.unreadCount)
    } catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [unreadOnly, page])

  useEffect(() => { load() }, [load])

  const markRead = async (id) => {
    try { await notifApi.markRead(id); load() }
    catch { toast.error('Failed') }
  }

  const markAll = async () => {
    try { await notifApi.markAllRead(); toast.success('All marked as read'); load() }
    catch { toast.error('Failed') }
  }

  return (
    <>
      <div className="page-header">
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <h2>Notifications</h2>
          {unread > 0 && (
            <span style={{ background: 'var(--accent)', color: 'white', fontSize: 11, fontWeight: 700, padding: '2px 8px', borderRadius: 20 }}>{unread} unread</span>
          )}
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <label style={{ display: 'flex', gap: 6, alignItems: 'center', fontSize: 13, cursor: 'pointer', color: 'var(--muted)' }}>
            <input type="checkbox" checked={unreadOnly} onChange={e => { setUnreadOnly(e.target.checked); setPage(1) }} />
            Unread only
          </label>
          {unread > 0 && (
            <button className="btn btn-ghost" onClick={markAll} style={{ fontSize: 12 }}>
              <CheckCheck size={13} /> Mark all read
            </button>
          )}
        </div>
      </div>

      <div className="card" style={{ padding: 0 }}>
        {loading ? <div className="spinner" style={{ margin: 40 }} /> : (
          <>
            {items.map(n => (
              <div key={n.id}
                style={{ padding: '14px 20px', borderBottom: '1px solid var(--border)', display: 'flex', gap: 14, alignItems: 'flex-start',
                  background: n.isRead ? 'transparent' : 'var(--accent)08', cursor: n.isRead ? 'default' : 'pointer' }}
                onClick={() => !n.isRead && markRead(n.id)}>
                <div style={{ width: 36, height: 36, borderRadius: 10, background: 'var(--accent)22', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                  <Bell size={16} style={{ color: 'var(--accent)' }} />
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 8 }}>
                    <strong style={{ fontSize: 14, display: 'block' }}>{n.title}</strong>
                    <span style={{ fontSize: 11, color: 'var(--muted)', whiteSpace: 'nowrap' }}>
                      {new Date(n.createdOn).toLocaleDateString()}
                    </span>
                  </div>
                  <p style={{ fontSize: 13, color: 'var(--muted)', margin: '4px 0 0', lineHeight: 1.5 }}>{n.body}</p>
                  {n.url && <a href={n.url} style={{ fontSize: 12, color: 'var(--accent)', marginTop: 4, display: 'inline-block' }}>View →</a>}
                </div>
                {!n.isRead && (
                  <div style={{ width: 8, height: 8, borderRadius: '50%', background: 'var(--accent)', marginTop: 6, flexShrink: 0 }} />
                )}
              </div>
            ))}
            {!items.length && (
              <div style={{ textAlign: 'center', padding: 48, color: 'var(--muted)' }}>
                <Bell size={32} style={{ marginBottom: 12, opacity: 0.3 }} />
                <div style={{ fontSize: 14 }}>{unreadOnly ? 'No unread notifications' : 'No notifications yet'}</div>
              </div>
            )}
          </>
        )}
      </div>

      {Math.ceil(total / 20) > 1 && (
        <div style={{ display: 'flex', justifyContent: 'center', gap: 6, marginTop: 16 }}>
          {Array.from({ length: Math.min(Math.ceil(total / 20), 7) }, (_, i) => i + 1).map(p => (
            <button key={p} className={`page-btn ${p === page ? 'active' : ''}`} onClick={() => setPage(p)}>{p}</button>
          ))}
        </div>
      )}
    </>
  )
}
