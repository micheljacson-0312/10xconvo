// ═══════════════════════════════════════════════════════════════════════════
//  USER PORTAL — PART B PAGES
//  ReviewsPage, ConsultantAvailabilityPage, NotificationsPage, ForgotPasswordPage
// ═══════════════════════════════════════════════════════════════════════════
import React, { useState, useEffect, useCallback } from 'react'
import { toast } from 'react-hot-toast'
import { useNavigate, useParams } from 'react-router-dom'
import { Star, Bell, CheckCheck, Clock, Mail, KeyRound, Eye, EyeOff, ArrowLeft } from 'lucide-react'
import axios from 'axios'

const BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000'
const api  = axios.create({ baseURL: BASE })
api.interceptors.request.use(cfg => {
  const t = localStorage.getItem('accessToken')
  if (t) cfg.headers.Authorization = `Bearer ${t}`
  return cfg
})

// ── API ───────────────────────────────────────────────────────────────────────
const reviewApi = {
  getAll:  (consultantUserId, page) => api.get(`/api/user/consultants/${consultantUserId}/reviews`, { params: { page, pageSize: 10 } }),
  create:  (consultantUserId, body) => api.post(`/api/user/consultants/${consultantUserId}/reviews`, body),
  update:  (consultantUserId, reviewId, body) => api.put(`/api/user/consultants/${consultantUserId}/reviews/${reviewId}`, body),
  delete:  (consultantUserId, reviewId) => api.delete(`/api/user/consultants/${consultantUserId}/reviews/${reviewId}`),
}
const availPublicApi = {
  get: consultantUserId => api.get(`/api/user/consultants/${consultantUserId}/availability`),
}
const userNotifApi = {
  get:         (unreadOnly, page) => api.get('/api/user/notifications', { params: { unreadOnly, page, pageSize: 20 } }),
  markRead:    id                 => api.put(`/api/user/notifications/${id}/read`),
  markAllRead: ()                 => api.put('/api/user/notifications/read-all'),
}
const forgotApi = {
  request: email              => axios.post(`${BASE}/api/auth/forgot-password`, { email }),
  verify:  (email, token)     => axios.post(`${BASE}/api/auth/verify-reset-token`, { email, token }),
  reset:   (email, resetSessionToken, newPassword) =>
    axios.post(`${BASE}/api/auth/reset-password`, { email, resetSessionToken, newPassword }),
}

// ── Star Rating Input ─────────────────────────────────────────────────────────
function StarInput({ value, onChange }) {
  const [hover, setHover] = useState(0)
  return (
    <div style={{ display: 'flex', gap: 4 }}>
      {[1,2,3,4,5].map(s => (
        <button key={s} type="button"
          onMouseEnter={() => setHover(s)} onMouseLeave={() => setHover(0)}
          onClick={() => onChange(s)}
          style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 2 }}>
          <Star size={24} fill={(hover || value) >= s ? '#f7c948' : 'transparent'}
            style={{ color: (hover || value) >= s ? '#f7c948' : 'var(--border)', transition: 'color .1s' }} />
        </button>
      ))}
    </div>
  )
}

function StarDisplay({ rating }) {
  return (
    <div style={{ display: 'flex', gap: 2 }}>
      {[1,2,3,4,5].map(s => (
        <Star key={s} size={14} fill={s <= rating ? '#f7c948' : 'transparent'}
          style={{ color: s <= rating ? '#f7c948' : 'var(--border)' }} />
      ))}
    </div>
  )
}

// ── 1. REVIEWS PAGE ───────────────────────────────────────────────────────────
export function ReviewsPage() {
  const { consultantId } = useParams()
  const [items, setItems]     = useState([])
  const [page, setPage]       = useState(1)
  const [total, setTotal]     = useState(0)
  const [avg, setAvg]         = useState(0)
  const [breakdown, setBreakdown] = useState([])
  const [loading, setLoading] = useState(true)
  const [myReview, setMyReview]   = useState(null)
  const [form, setForm]       = useState({ rating: 0, comment: '' })
  const [submitting, setSubmitting] = useState(false)
  const [editMode, setEditMode]   = useState(false)

  const load = useCallback(async () => {
    if (!consultantId) return
    setLoading(true)
    try {
      const r = await reviewApi.getAll(consultantId, page)
      const d = r.data.data
      setItems(d.items)
      setTotal(d.totalRecords)
      setAvg(d.averageRating)
      setBreakdown(d.ratingBreakdown || [])
      const mine = d.items.find(i => i.isMyReview)
      if (mine) { setMyReview(mine); setForm({ rating: mine.rating, comment: mine.comment || '' }) }
    } catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [consultantId, page])

  useEffect(() => { load() }, [load])

  const submit = async () => {
    if (form.rating === 0) return toast.error('Please select a rating')
    setSubmitting(true)
    try {
      if (editMode && myReview) await reviewApi.update(consultantId, myReview.id, form)
      else                      await reviewApi.create(consultantId, form)
      toast.success(editMode ? 'Review updated!' : 'Review submitted!')
      setEditMode(false); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
    finally { setSubmitting(false) }
  }

  const deleteReview = async () => {
    if (!confirm('Delete your review?')) return
    try { await reviewApi.delete(consultantId, myReview.id); toast.success('Deleted'); setMyReview(null); setForm({ rating: 0, comment: '' }); load() }
    catch { toast.error('Failed') }
  }

  const totalReviews = breakdown.reduce((s, b) => s + b.count, 0) || 1

  return (
    <div style={{ maxWidth: 700, margin: '0 auto' }}>
      <div className="page-header">
        <h2>Reviews</h2>
      </div>

      {/* Summary */}
      <div className="card" style={{ display: 'flex', gap: 32, alignItems: 'center', marginBottom: 16 }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: 48, fontWeight: 900, color: 'var(--accent)', lineHeight: 1 }}>{avg}</div>
          <StarDisplay rating={Math.round(avg)} />
          <div style={{ fontSize: 12, color: 'var(--muted)', marginTop: 4 }}>{total} reviews</div>
        </div>
        <div style={{ flex: 1 }}>
          {[5,4,3,2,1].map(stars => {
            const count = breakdown.find(b => b.stars === stars)?.count || 0
            const pct = Math.round((count / totalReviews) * 100)
            return (
              <div key={stars} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
                <span style={{ fontSize: 12, width: 8, textAlign: 'right', color: 'var(--muted)' }}>{stars}</span>
                <Star size={11} fill="#f7c948" style={{ color: '#f7c948' }} />
                <div style={{ flex: 1, height: 6, background: 'var(--border)', borderRadius: 3, overflow: 'hidden' }}>
                  <div style={{ width: `${pct}%`, height: '100%', background: '#f7c948', borderRadius: 3, transition: 'width .4s' }} />
                </div>
                <span style={{ fontSize: 11, color: 'var(--muted)', width: 24 }}>{count}</span>
              </div>
            )
          })}
        </div>
      </div>

      {/* Write review */}
      {localStorage.getItem('accessToken') && (
        <div className="card" style={{ marginBottom: 16 }}>
          <h4 style={{ marginBottom: 12 }}>{myReview && !editMode ? 'Your Review' : editMode ? 'Edit Your Review' : 'Write a Review'}</h4>
          {myReview && !editMode ? (
            <div>
              <StarDisplay rating={myReview.rating} />
              <p style={{ fontSize: 13, color: 'var(--muted)', margin: '8px 0' }}>{myReview.comment || 'No comment'}</p>
              <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
                <button className="btn btn-ghost" onClick={() => setEditMode(true)} style={{ fontSize: 12 }}>Edit</button>
                <button className="btn btn-ghost" onClick={deleteReview} style={{ fontSize: 12, color: 'var(--accent2)' }}>Delete</button>
              </div>
            </div>
          ) : (
            <div>
              <div style={{ marginBottom: 12 }}>
                <label style={{ fontSize: 12, color: 'var(--muted)', display: 'block', marginBottom: 6 }}>Rating</label>
                <StarInput value={form.rating} onChange={v => setForm(f => ({ ...f, rating: v }))} />
              </div>
              <div style={{ marginBottom: 12 }}>
                <label style={{ fontSize: 12, color: 'var(--muted)', display: 'block', marginBottom: 6 }}>Comment (optional)</label>
                <textarea className="input" rows={3} value={form.comment} onChange={e => setForm(f => ({ ...f, comment: e.target.value }))}
                  placeholder="Share your experience..." style={{ resize: 'none' }} />
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                <button className="btn btn-primary" onClick={submit} disabled={submitting || form.rating === 0}>
                  {submitting ? 'Submitting...' : editMode ? 'Update' : 'Submit Review'}
                </button>
                {editMode && <button className="btn btn-ghost" onClick={() => setEditMode(false)}>Cancel</button>}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Review list */}
      {loading ? <div className="spinner" /> : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {items.filter(i => !i.isMyReview || !myReview).concat(myReview ? [] : []).map(item => (
            <div key={item.id} className="card" style={{ padding: '14px 18px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 8 }}>
                <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
                  <div style={{ width: 34, height: 34, borderRadius: '50%', background: 'var(--accent)33', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 14, color: 'var(--accent)' }}>
                    {item.reviewerName?.charAt(0)?.toUpperCase()}
                  </div>
                  <div>
                    <strong style={{ fontSize: 14 }}>{item.reviewerName}</strong>
                    <div><StarDisplay rating={item.rating} /></div>
                  </div>
                </div>
                <span style={{ fontSize: 11, color: 'var(--muted)' }}>{new Date(item.createdAt).toLocaleDateString()}</span>
              </div>
              {item.comment && <p style={{ fontSize: 13, color: 'var(--muted)', margin: 0, lineHeight: 1.6 }}>{item.comment}</p>}
            </div>
          ))}
          {!items.length && !loading && <div style={{ textAlign: 'center', color: 'var(--muted)', padding: 32, fontSize: 14 }}>No reviews yet. Be the first!</div>}
        </div>
      )}

      {Math.ceil(total / 10) > 1 && (
        <div style={{ display: 'flex', justifyContent: 'center', gap: 6, marginTop: 16 }}>
          {Array.from({ length: Math.min(Math.ceil(total / 10), 7) }, (_, i) => i + 1).map(p => (
            <button key={p} className={`page-btn ${p === page ? 'active' : ''}`} onClick={() => setPage(p)}>{p}</button>
          ))}
        </div>
      )}
    </div>
  )
}

// ── 2. CONSULTANT AVAILABILITY (public view) ──────────────────────────────────
export function ConsultantAvailabilityPage() {
  const { consultantId } = useParams()
  const [slots, setSlots]     = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!consultantId) return
    availPublicApi.get(consultantId)
      .then(r => setSlots(r.data.data))
      .catch(() => toast.error('Load failed'))
      .finally(() => setLoading(false))
  }, [consultantId])

  const byDay = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'].map((day, i) => ({
    day, slots: slots.filter(s => s.dayNumber === i)
  }))

  return (
    <div style={{ maxWidth: 500, margin: '0 auto' }}>
      <div className="page-header"><h2>Available Times</h2></div>
      {loading ? <div className="spinner" /> : (
        <div className="card" style={{ padding: 0 }}>
          {byDay.map(({ day, slots: ds }) => (
            <div key={day} style={{ padding: '12px 20px', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 16 }}>
              <span style={{ width: 100, fontWeight: ds.length ? 600 : 400, color: ds.length ? 'var(--text)' : 'var(--muted)', fontSize: 14 }}>{day}</span>
              {ds.length === 0 ? (
                <span style={{ fontSize: 13, color: 'var(--muted)' }}>Not available</span>
              ) : (
                <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                  {ds.map((s, i) => (
                    <span key={i} style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 13, background: 'var(--accent)15', color: 'var(--accent)', padding: '3px 10px', borderRadius: 20, fontWeight: 600 }}>
                      <Clock size={11} /> {s.startTime} — {s.endTime}
                    </span>
                  ))}
                </div>
              )}
            </div>
          ))}
          {slots.length === 0 && !loading && (
            <div style={{ textAlign: 'center', padding: 32, color: 'var(--muted)', fontSize: 14 }}>No schedule set yet</div>
          )}
        </div>
      )}
    </div>
  )
}

// ── 3. USER NOTIFICATIONS ─────────────────────────────────────────────────────
export function UserNotificationsPage() {
  const [items, setItems]     = useState([])
  const [page, setPage]       = useState(1)
  const [total, setTotal]     = useState(0)
  const [unread, setUnread]   = useState(0)
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const r = await userNotifApi.get(unreadOnly, page)
      setItems(r.data.data.items)
      setTotal(r.data.data.totalRecords)
      setUnread(r.data.data.unreadCount)
    } catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [unreadOnly, page])

  useEffect(() => { load() }, [load])

  const markRead = async id => {
    try { await userNotifApi.markRead(id); load() }
    catch { toast.error('Failed') }
  }

  const markAll = async () => {
    try { await userNotifApi.markAllRead(); toast.success('All marked as read'); load() }
    catch { toast.error('Failed') }
  }

  return (
    <>
      <div className="page-header">
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <h2>Notifications</h2>
          {unread > 0 && <span style={{ background: 'var(--accent)', color: 'white', fontSize: 11, fontWeight: 700, padding: '2px 8px', borderRadius: 20 }}>{unread}</span>}
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <label style={{ display: 'flex', gap: 6, alignItems: 'center', fontSize: 13, cursor: 'pointer', color: 'var(--muted)' }}>
            <input type="checkbox" checked={unreadOnly} onChange={e => { setUnreadOnly(e.target.checked); setPage(1) }} />
            Unread only
          </label>
          {unread > 0 && <button className="btn btn-ghost" onClick={markAll} style={{ fontSize: 12 }}><CheckCheck size={13} /> Mark all read</button>}
        </div>
      </div>

      <div className="card" style={{ padding: 0 }}>
        {loading ? <div className="spinner" style={{ margin: 40 }} /> : (
          <>
            {items.map(n => (
              <div key={n.id}
                style={{ padding: '14px 20px', borderBottom: '1px solid var(--border)', display: 'flex', gap: 14, alignItems: 'flex-start', background: n.isRead ? 'transparent' : 'var(--accent)08', cursor: n.isRead ? 'default' : 'pointer' }}
                onClick={() => !n.isRead && markRead(n.id)}>
                <div style={{ width: 36, height: 36, borderRadius: 10, background: 'var(--accent)22', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                  <Bell size={16} style={{ color: 'var(--accent)' }} />
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                    <strong style={{ fontSize: 14 }}>{n.title}</strong>
                    <span style={{ fontSize: 11, color: 'var(--muted)', whiteSpace: 'nowrap', marginLeft: 8 }}>{new Date(n.createdOn).toLocaleDateString()}</span>
                  </div>
                  <p style={{ fontSize: 13, color: 'var(--muted)', margin: '4px 0 0', lineHeight: 1.5 }}>{n.body}</p>
                  {n.url && <a href={n.url} style={{ fontSize: 12, color: 'var(--accent)', marginTop: 4, display: 'inline-block' }}>View →</a>}
                </div>
                {!n.isRead && <div style={{ width: 8, height: 8, borderRadius: '50%', background: 'var(--accent)', marginTop: 6, flexShrink: 0 }} />}
              </div>
            ))}
            {!items.length && (
              <div style={{ textAlign: 'center', padding: 48, color: 'var(--muted)' }}>
                <Bell size={32} style={{ marginBottom: 12, opacity: 0.3 }} />
                <div style={{ fontSize: 14 }}>No notifications</div>
              </div>
            )}
          </>
        )}
      </div>
    </>
  )
}

// ── 4. FORGOT PASSWORD (3-step flow) ─────────────────────────────────────────
export function ForgotPasswordPage() {
  const navigate = useNavigate()
  const [step, setStep]     = useState(1) // 1=email, 2=OTP, 3=new password
  const [email, setEmail]   = useState('')
  const [otp, setOtp]       = useState('')
  const [sessionToken, setSessionToken] = useState('')
  const [password, setPassword]         = useState('')
  const [confirmPass, setConfirmPass]   = useState('')
  const [showPass, setShowPass]         = useState(false)
  const [loading, setLoading]           = useState(false)

  const step1 = async () => {
    if (!email) return toast.error('Enter your email')
    setLoading(true)
    try {
      await forgotApi.request(email)
      toast.success('Reset code sent! Check your email.')
      setStep(2)
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
    finally { setLoading(false) }
  }

  const step2 = async () => {
    if (otp.length !== 6) return toast.error('Enter the 6-digit code')
    setLoading(true)
    try {
      const r = await forgotApi.verify(email, otp)
      setSessionToken(r.data.data.resetSessionToken)
      toast.success('Code verified!')
      setStep(3)
    } catch (e) { toast.error(e.response?.data?.message || 'Invalid or expired code') }
    finally { setLoading(false) }
  }

  const step3 = async () => {
    if (password.length < 8)         return toast.error('Password must be at least 8 characters')
    if (!/[A-Z]/.test(password))     return toast.error('Password must have an uppercase letter')
    if (!/[0-9]/.test(password))     return toast.error('Password must have a number')
    if (password !== confirmPass)    return toast.error('Passwords do not match')
    setLoading(true)
    try {
      await forgotApi.reset(email, sessionToken, password)
      toast.success('Password reset! Please log in.')
      navigate('/login')
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
    finally { setLoading(false) }
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'var(--bg)', padding: 20 }}>
      <div style={{ width: '100%', maxWidth: 400 }}>
        {/* Back to login */}
        <button onClick={() => navigate('/login')} style={{ display: 'flex', alignItems: 'center', gap: 6, background: 'none', border: 'none', cursor: 'pointer', color: 'var(--muted)', fontSize: 13, marginBottom: 24 }}>
          <ArrowLeft size={14} /> Back to login
        </button>

        <div className="card">
          {/* Progress dots */}
          <div style={{ display: 'flex', gap: 6, justifyContent: 'center', marginBottom: 24 }}>
            {[1,2,3].map(s => (
              <div key={s} style={{ width: s === step ? 24 : 8, height: 8, borderRadius: 4, background: s <= step ? 'var(--accent)' : 'var(--border)', transition: 'all .3s' }} />
            ))}
          </div>

          {step === 1 && (
            <>
              <div style={{ textAlign: 'center', marginBottom: 24 }}>
                <div style={{ width: 52, height: 52, borderRadius: 14, background: 'var(--accent)22', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 12px' }}>
                  <Mail size={22} style={{ color: 'var(--accent)' }} />
                </div>
                <h3 style={{ margin: '0 0 4px' }}>Forgot Password</h3>
                <p style={{ color: 'var(--muted)', fontSize: 13, margin: 0 }}>Enter your email to receive a reset code</p>
              </div>
              <div className="field">
                <label className="label">Email Address</label>
                <input className="input" type="email" value={email} onChange={e => setEmail(e.target.value)}
                  placeholder="you@example.com" onKeyDown={e => e.key === 'Enter' && step1()} autoFocus />
              </div>
              <button className="btn btn-primary" onClick={step1} disabled={loading} style={{ width: '100%', marginTop: 8, justifyContent: 'center' }}>
                {loading ? 'Sending...' : 'Send Reset Code'}
              </button>
            </>
          )}

          {step === 2 && (
            <>
              <div style={{ textAlign: 'center', marginBottom: 24 }}>
                <div style={{ width: 52, height: 52, borderRadius: 14, background: 'var(--accent)22', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 12px' }}>
                  <KeyRound size={22} style={{ color: 'var(--accent)' }} />
                </div>
                <h3 style={{ margin: '0 0 4px' }}>Enter Reset Code</h3>
                <p style={{ color: 'var(--muted)', fontSize: 13, margin: 0 }}>We sent a 6-digit code to <strong>{email}</strong></p>
              </div>
              <div className="field">
                <label className="label">6-Digit Code</label>
                <input className="input" value={otp} onChange={e => setOtp(e.target.value.replace(/\D/g,'').slice(0,6))}
                  placeholder="000000" maxLength={6} autoFocus
                  style={{ fontSize: 28, letterSpacing: 12, textAlign: 'center', fontFamily: 'monospace', fontWeight: 700 }}
                  onKeyDown={e => e.key === 'Enter' && step2()} />
              </div>
              <p style={{ fontSize: 12, color: 'var(--muted)', textAlign: 'center', margin: '8px 0' }}>Code expires in 15 minutes</p>
              <button className="btn btn-primary" onClick={step2} disabled={loading || otp.length !== 6} style={{ width: '100%', justifyContent: 'center' }}>
                {loading ? 'Verifying...' : 'Verify Code'}
              </button>
              <button onClick={() => { setStep(1); setOtp('') }} style={{ display: 'block', width: '100%', textAlign: 'center', marginTop: 10, background: 'none', border: 'none', cursor: 'pointer', fontSize: 12, color: 'var(--muted)' }}>
                Resend code
              </button>
            </>
          )}

          {step === 3 && (
            <>
              <div style={{ textAlign: 'center', marginBottom: 24 }}>
                <h3 style={{ margin: '0 0 4px' }}>Set New Password</h3>
                <p style={{ color: 'var(--muted)', fontSize: 13, margin: 0 }}>Choose a strong password</p>
              </div>
              <div className="field">
                <label className="label">New Password</label>
                <div style={{ position: 'relative' }}>
                  <input className="input" type={showPass ? 'text' : 'password'} value={password}
                    onChange={e => setPassword(e.target.value)} placeholder="Min 8 chars, uppercase, number" autoFocus
                    style={{ paddingRight: 40 }} />
                  <button onClick={() => setShowPass(s => !s)} style={{ position: 'absolute', right: 10, top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', cursor: 'pointer', color: 'var(--muted)' }}>
                    {showPass ? <EyeOff size={15} /> : <Eye size={15} />}
                  </button>
                </div>
              </div>

              {/* Password strength */}
              {password && (
                <div style={{ margin: '-8px 0 12px', display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                  {[
                    { ok: password.length >= 8, label: '8+ chars' },
                    { ok: /[A-Z]/.test(password), label: 'Uppercase' },
                    { ok: /[0-9]/.test(password), label: 'Number' },
                  ].map(r => (
                    <span key={r.label} style={{ fontSize: 11, padding: '2px 7px', borderRadius: 20, background: r.ok ? 'var(--green)22' : 'var(--border)', color: r.ok ? 'var(--green)' : 'var(--muted)', fontWeight: 600 }}>
                      {r.ok ? '✓' : '○'} {r.label}
                    </span>
                  ))}
                </div>
              )}

              <div className="field">
                <label className="label">Confirm Password</label>
                <input className="input" type={showPass ? 'text' : 'password'} value={confirmPass}
                  onChange={e => setConfirmPass(e.target.value)} placeholder="Re-enter password"
                  onKeyDown={e => e.key === 'Enter' && step3()} />
              </div>
              <button className="btn btn-primary" onClick={step3} disabled={loading || !password || !confirmPass} style={{ width: '100%', marginTop: 8, justifyContent: 'center' }}>
                {loading ? 'Resetting...' : 'Reset Password'}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
