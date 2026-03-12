import React, { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { toast } from 'react-hot-toast'
import { Mail, KeyRound, Lock, ArrowLeft, CheckCircle } from 'lucide-react'
import { forgotApi } from '../api'

export default function ForgotPasswordPage() {
  const navigate = useNavigate()
  const [step, setStep]             = useState(1)  // 1=email, 2=otp, 3=new password
  const [email, setEmail]           = useState('')
  const [otp, setOtp]               = useState('')
  const [sessionToken, setSession]  = useState('')
  const [pwd, setPwd]               = useState('')
  const [confirmPwd, setConfirm]    = useState('')
  const [loading, setLoading]       = useState(false)
  const [done, setDone]             = useState(false)

  // Step 1 — Request OTP
  const requestOtp = async () => {
    if (!email) return toast.error('Email required')
    setLoading(true)
    try {
      await forgotApi.request(email)
      toast.success('If your email is registered, a code has been sent')
      setStep(2)
    } catch { toast.error('Request failed. Try again.') }
    finally { setLoading(false) }
  }

  // Step 2 — Verify OTP
  const verifyOtp = async () => {
    if (!otp || otp.length < 6) return toast.error('Enter 6-digit code')
    setLoading(true)
    try {
      const { data } = await forgotApi.verify(email, otp)
      setSession(data.data.resetSessionToken)
      toast.success('Code verified!')
      setStep(3)
    } catch (e) { toast.error(e.response?.data?.message || 'Invalid or expired code') }
    finally { setLoading(false) }
  }

  // Step 3 — Set new password
  const resetPwd = async () => {
    if (!pwd || pwd.length < 8)         return toast.error('Password must be at least 8 characters')
    if (!/[A-Z]/.test(pwd))             return toast.error('Must contain uppercase letter')
    if (!/[0-9]/.test(pwd))             return toast.error('Must contain a number')
    if (pwd !== confirmPwd)             return toast.error('Passwords do not match')
    setLoading(true)
    try {
      await forgotApi.reset(email, sessionToken, pwd)
      setDone(true)
    } catch (e) { toast.error(e.response?.data?.message || 'Reset failed') }
    finally { setLoading(false) }
  }

  // Success state
  if (done) return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'var(--bg)', padding: 20,
    }}>
      <div style={{ textAlign: 'center', maxWidth: 380 }}>
        <CheckCircle size={56} color="var(--green)" style={{ marginBottom: 20 }} />
        <h2 style={{ fontFamily: 'var(--font-head)', marginBottom: 10 }}>Password Reset!</h2>
        <p style={{ color: 'var(--muted)', marginBottom: 24 }}>
          Your password has been successfully updated. You can now log in with your new password.
        </p>
        <button className="btn btn-primary" style={{ width: '100%' }} onClick={() => navigate('/login')}>
          Go to Login
        </button>
      </div>
    </div>
  )

  const steps = [
    { n: 1, label: 'Email' },
    { n: 2, label: 'Verify Code' },
    { n: 3, label: 'New Password' },
  ]

  return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'var(--bg)', padding: 20,
    }}>
      <div style={{ width: '100%', maxWidth: 400 }}>
        {/* Back */}
        <Link to="/login" style={{
          display: 'inline-flex', alignItems: 'center', gap: 6,
          color: 'var(--muted)', fontSize: 13, textDecoration: 'none',
          marginBottom: 28,
        }}>
          <ArrowLeft size={14} /> Back to Login
        </Link>

        {/* Header */}
        <div style={{ marginBottom: 28 }}>
          <div style={{ fontFamily: 'var(--font-head)', fontSize: 24, fontWeight: 800, marginBottom: 6 }}>
            Reset Password
          </div>
          <p style={{ color: 'var(--muted)', fontSize: 14 }}>
            {step === 1 && "Enter your email to receive a reset code"}
            {step === 2 && `Enter the 6-digit code sent to ${email}`}
            {step === 3 && "Create a strong new password"}
          </p>
        </div>

        {/* Step indicator */}
        <div style={{ display: 'flex', gap: 4, marginBottom: 28 }}>
          {steps.map((s, i) => (
            <React.Fragment key={s.n}>
              <div style={{
                display: 'flex', alignItems: 'center', gap: 6,
                opacity: step === s.n ? 1 : step > s.n ? 0.7 : 0.35,
              }}>
                <div style={{
                  width: 24, height: 24, borderRadius: '50%', fontSize: 11, fontWeight: 700,
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  background: step >= s.n ? 'var(--accent)' : 'var(--bg3)',
                  color: step >= s.n ? '#fff' : 'var(--muted)',
                }}>{step > s.n ? '✓' : s.n}</div>
                <span style={{ fontSize: 12, color: step === s.n ? 'var(--text)' : 'var(--muted)' }}>
                  {s.label}
                </span>
              </div>
              {i < steps.length - 1 && (
                <div style={{ flex: 1, height: 1, background: step > s.n ? 'var(--accent)' : 'var(--border)', alignSelf: 'center' }} />
              )}
            </React.Fragment>
          ))}
        </div>

        {/* Card */}
        <div style={{
          background: 'var(--bg2)', border: '1px solid var(--border)',
          borderRadius: 12, padding: 28,
        }}>
          {/* STEP 1 */}
          {step === 1 && (
            <>
              <div className="form-group">
                <label>Email Address</label>
                <div style={{ position: 'relative' }}>
                  <Mail size={14} style={{ position:'absolute', left:12, top:'50%', transform:'translateY(-50%)', color:'var(--muted)' }} />
                  <input className="input" type="email" placeholder="your@email.com"
                    value={email} onChange={e => setEmail(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && requestOtp()}
                    style={{ paddingLeft: 36 }} />
                </div>
              </div>
              <button className="btn btn-primary" onClick={requestOtp} disabled={loading}
                style={{ width: '100%' }}>
                {loading ? 'Sending…' : 'Send Reset Code'}
              </button>
            </>
          )}

          {/* STEP 2 */}
          {step === 2 && (
            <>
              <div className="form-group">
                <label>6-Digit Code</label>
                <input className="input" placeholder="000000" maxLength={6}
                  value={otp} onChange={e => setOtp(e.target.value.replace(/\D/g,''))}
                  onKeyDown={e => e.key === 'Enter' && verifyOtp()}
                  style={{
                    fontSize: 28, fontWeight: 800, letterSpacing: 12,
                    textAlign: 'center', fontFamily: 'monospace',
                  }} />
                <small style={{ color: 'var(--muted)' }}>Code expires in 15 minutes</small>
              </div>
              <button className="btn btn-primary" onClick={verifyOtp} disabled={loading}
                style={{ width: '100%' }}>
                {loading ? 'Verifying…' : 'Verify Code'}
              </button>
              <button className="btn btn-ghost" onClick={() => { setStep(1); setOtp('') }}
                style={{ width: '100%', marginTop: 8 }}>
                Wrong email? Go back
              </button>
            </>
          )}

          {/* STEP 3 */}
          {step === 3 && (
            <>
              <div className="form-group">
                <label>New Password</label>
                <div style={{ position: 'relative' }}>
                  <Lock size={14} style={{ position:'absolute', left:12, top:'50%', transform:'translateY(-50%)', color:'var(--muted)' }} />
                  <input className="input" type="password" placeholder="Min 8 chars, uppercase + number"
                    value={pwd} onChange={e => setPwd(e.target.value)}
                    style={{ paddingLeft: 36 }} />
                </div>
                {/* Strength hints */}
                <div style={{ display: 'flex', gap: 8, marginTop: 6, flexWrap: 'wrap' }}>
                  {[
                    { ok: pwd.length >= 8,    label: '8+ chars' },
                    { ok: /[A-Z]/.test(pwd),  label: 'Uppercase' },
                    { ok: /[0-9]/.test(pwd),  label: 'Number' },
                  ].map(h => (
                    <span key={h.label} style={{
                      fontSize: 11, padding: '2px 8px', borderRadius: 10,
                      background: h.ok ? 'rgba(0,212,170,0.15)' : 'var(--bg3)',
                      color: h.ok ? 'var(--green)' : 'var(--muted)',
                    }}>{h.ok ? '✓' : '○'} {h.label}</span>
                  ))}
                </div>
              </div>
              <div className="form-group">
                <label>Confirm Password</label>
                <div style={{ position: 'relative' }}>
                  <Lock size={14} style={{ position:'absolute', left:12, top:'50%', transform:'translateY(-50%)', color:'var(--muted)' }} />
                  <input className="input" type="password" placeholder="Repeat password"
                    value={confirmPwd} onChange={e => setConfirm(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && resetPwd()}
                    style={{ paddingLeft: 36, borderColor: confirmPwd && pwd !== confirmPwd ? 'var(--accent2)' : '' }} />
                </div>
              </div>
              <button className="btn btn-primary" onClick={resetPwd} disabled={loading}
                style={{ width: '100%' }}>
                {loading ? 'Resetting…' : 'Set New Password'}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
