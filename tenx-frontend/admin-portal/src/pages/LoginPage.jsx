import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'react-hot-toast'
import { useAuthStore } from '../store/authStore'
import { ChevronRight, Loader } from 'lucide-react'

export default function LoginPage() {
  const navigate = useNavigate()
  const { step1, step2, loading } = useAuthStore()

  const [phase,   setPhase]   = useState(1)  // 1 = email, 2 = password+dropdowns
  const [step1Data, setStep1Data] = useState(null)

  const [email,    setEmail]    = useState('')
  const [password, setPassword] = useState('')
  const [locationId,   setLocationId]   = useState('')
  const [fiscalYearId, setFiscalYearId] = useState('')
  const [connection,   setConnection]   = useState('Production')
  const [rememberMe,   setRememberMe]   = useState(false)

  // ── STEP 1 ────────────────────────────────────────────────────────────────
  const handleStep1 = async e => {
    e.preventDefault()
    try {
      const data = await step1(email)
      setStep1Data(data)
      // Pre-select current fiscal year
      const cur = data.fiscalYears?.find(f => f.isCurrent)
      if (cur) setFiscalYearId(cur.id)
      if (data.locations?.length === 1) setLocationId(data.locations[0].id)
      setPhase(2)
    } catch (err) {
      toast.error(err.message)
    }
  }

  // ── STEP 2 ────────────────────────────────────────────────────────────────
  const handleStep2 = async e => {
    e.preventDefault()
    if (!locationId)   { toast.error('Please select a location');    return }
    if (!fiscalYearId) { toast.error('Please select a fiscal year'); return }
    try {
      await step2({ email, password, locationId, fiscalYearId, connection, rememberMe })
      toast.success('Welcome back!')
      navigate('/')
    } catch (err) {
      toast.error(err.message)
    }
  }

  return (
    <div className="login-page">
      <div className="login-box">
        <div className="login-logo">10X <span style={{ color: 'var(--accent)' }}>Admin</span></div>
        <p className="login-sub">
          {phase === 1 ? 'Enter your email to continue' : `Welcome, ${step1Data?.userName}`}
        </p>

        {/* ── PHASE 1: EMAIL ── */}
        {phase === 1 && (
          <form onSubmit={handleStep1}>
            <div className="form-group">
              <label>Email Address</label>
              <input
                type="email" value={email} onChange={e => setEmail(e.target.value)}
                placeholder="you@company.com" required autoFocus
              />
            </div>
            <button className="btn btn-primary" type="submit" disabled={loading}
              style={{ width: '100%', justifyContent: 'center', padding: '11px' }}>
              {loading ? <Loader size={14} className="spin" /> : <>Continue <ChevronRight size={14} /></>}
            </button>
          </form>
        )}

        {/* ── PHASE 2: PASSWORD + DROPDOWNS ── */}
        {phase === 2 && (
          <form onSubmit={handleStep2}>
            <div className="form-group">
              <label>Password</label>
              <input
                type="password" value={password} onChange={e => setPassword(e.target.value)}
                placeholder="••••••••" required autoFocus
              />
            </div>

            <div className="form-group">
              <label>Location</label>
              <select value={locationId} onChange={e => setLocationId(e.target.value)} required>
                <option value="">Select location…</option>
                {step1Data?.locations?.map(l => (
                  <option key={l.id} value={l.id}>{l.locationName} ({l.locationTypeName})</option>
                ))}
              </select>
            </div>

            <div className="grid-2">
              <div className="form-group">
                <label>Fiscal Year</label>
                <select value={fiscalYearId} onChange={e => setFiscalYearId(e.target.value)} required>
                  <option value="">Select year…</option>
                  {step1Data?.fiscalYears?.map(f => (
                    <option key={f.id} value={f.id}>{f.name}{f.isCurrent ? ' ★' : ''}</option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <label>Connection</label>
                <select value={connection} onChange={e => setConnection(e.target.value)}>
                  {step1Data?.connections?.map(c => <option key={c} value={c}>{c}</option>)}
                </select>
              </div>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <input type="checkbox" id="rem" checked={rememberMe} onChange={e => setRememberMe(e.target.checked)} style={{ width: 'auto' }} />
              <label htmlFor="rem" style={{ margin: 0, textTransform: 'none', fontSize: 13 }}>Remember me</label>
            </div>

            <div style={{ display: 'flex', gap: 8 }}>
              <button type="button" className="btn btn-ghost" onClick={() => setPhase(1)} style={{ flex: 1, justifyContent: 'center' }}>Back</button>
              <button type="submit"  className="btn btn-primary" disabled={loading} style={{ flex: 2, justifyContent: 'center', padding: '11px' }}>
                {loading ? <Loader size={14} className="spin" /> : 'Sign In'}
              </button>
            </div>
          </form>
        )}
      </div>

      <style>{`.spin { animation: spin 0.8s linear infinite; } @keyframes spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  )
}
