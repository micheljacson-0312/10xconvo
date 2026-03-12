import React, { useState, useEffect } from 'react'
import { toast } from 'react-hot-toast'
import { Send, MessageSquare, Mail, Bell, Smartphone, Radio, Clock, CheckCircle, XCircle, RefreshCw } from 'lucide-react'
import { notifyApi, userApi } from '../api'

const TABS = [
  { key:'wa',      label:'WhatsApp', icon: MessageSquare, color:'#25d366' },
  { key:'sms',     label:'SMS',      icon: Smartphone,    color:'#f7c948' },
  { key:'email',   label:'Email',    icon: Mail,          color:'var(--accent)' },
  { key:'webpush', label:'WebPush',  icon: Bell,          color:'var(--green)' },
  { key:'app',     label:'Broadcast',icon: Radio,         color:'var(--accent2)' },
]

function StatusPill({ status }) {
  const map = { sent:'badge-green', 'mock-sent':'badge-gray', failed:'badge-danger', pending:'badge-gray', queued:'badge-gray' }
  return <span className={`badge ${map[status] || 'badge-gray'}`}>{status}</span>
}

function fmtDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleString('en-GB', { day:'2-digit', month:'short', year:'numeric', hour:'2-digit', minute:'2-digit' })
}

// ── WA TAB ───────────────────────────────────────────────────────────────────
function WaTab() {
  const [history, setHistory] = useState([])
  const [total, setTotal]     = useState(0)
  const [page, setPage]       = useState(1)
  const [search, setSearch]   = useState('')
  const [loading, setLoading] = useState(false)
  const [phone, setPhone]     = useState('')
  const [message, setMessage] = useState('')
  const [sending, setSending] = useState(false)

  const load = async () => {
    setLoading(true)
    try { const {data} = await notifyApi.wa.getAll(page, 20, search); setHistory(data.data?.items||[]); setTotal(data.data?.totalRecords||0) }
    catch {} finally { setLoading(false) }
  }
  useEffect(() => { load() }, [page, search])

  const send = async () => {
    if (!phone || !message) return toast.error('Phone and message required')
    setSending(true)
    try {
      const {data} = await notifyApi.wa.send({ phoneNumber: phone, message })
      toast.success(data.message)
      setPhone(''); setMessage(''); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Send failed') }
    finally { setSending(false) }
  }

  return (
    <div style={{display:'grid', gridTemplateColumns:'1fr 1.5fr', gap:20}}>
      {/* Send Form */}
      <div className="card">
        <h4 style={{marginBottom:16, color:'#25d366', display:'flex', alignItems:'center', gap:6}}>
          <MessageSquare size={14}/> Send WhatsApp
        </h4>
        <div className="form-group">
          <label>Phone Number</label>
          <input className="input" placeholder="+923001234567" value={phone}
            onChange={e => setPhone(e.target.value)} />
        </div>
        <div className="form-group">
          <label>Message</label>
          <textarea className="input" rows={5} placeholder="Your message…" value={message}
            onChange={e => setMessage(e.target.value)} />
          <small style={{color:'var(--muted)'}}>{message.length} / 4096 chars</small>
        </div>
        <button className="btn btn-primary" onClick={send} disabled={sending} style={{width:'100%'}}>
          {sending ? 'Sending…' : <><Send size={13}/> Send WhatsApp</>}
        </button>
      </div>

      {/* History */}
      <div className="card">
        <div style={{display:'flex', justifyContent:'space-between', alignItems:'center', marginBottom:12}}>
          <h4>Send History</h4>
          <div style={{display:'flex',gap:8}}>
            <input className="input" placeholder="Search…" value={search}
              onChange={e=>{setSearch(e.target.value);setPage(1)}} style={{maxWidth:160,fontSize:12}} />
            <button className="btn-icon" onClick={load}><RefreshCw size={13}/></button>
          </div>
        </div>
        {loading ? <div style={{textAlign:'center',color:'var(--muted)',padding:24}}>Loading…</div> : (
          <div style={{overflowY:'auto',maxHeight:360}}>
            {history.length === 0 && <p style={{color:'var(--muted)',textAlign:'center',padding:24}}>No messages yet</p>}
            {history.map(h => (
              <div key={h.id} style={{padding:'10px 0',borderBottom:'1px solid var(--border)'}}>
                <div style={{display:'flex',justifyContent:'space-between',alignItems:'center'}}>
                  <span style={{fontWeight:600,fontSize:13}}>{h.sendNo}</span>
                  <StatusPill status={h.status}/>
                </div>
                <div style={{color:'var(--muted)',fontSize:12,marginTop:3}}>{h.message?.slice(0,80)}{h.message?.length>80?'…':''}</div>
                <div style={{color:'var(--muted)',fontSize:11,marginTop:2}}><Clock size={10}/> {fmtDate(h.submitOn)}</div>
              </div>
            ))}
          </div>
        )}
        <div style={{display:'flex',justifyContent:'space-between',alignItems:'center',marginTop:10,fontSize:12,color:'var(--muted)'}}>
          <span>{total} total</span>
          <div style={{display:'flex',gap:4}}>
            {page>1 && <button className="page-btn" onClick={()=>setPage(p=>p-1)}>‹</button>}
            <span className="page-btn active">{page}</span>
            {page*20<total && <button className="page-btn" onClick={()=>setPage(p=>p+1)}>›</button>}
          </div>
        </div>
      </div>
    </div>
  )
}

// ── SMS TAB ──────────────────────────────────────────────────────────────────
function SmsTab() {
  const [history, setHistory] = useState([])
  const [total, setTotal]     = useState(0)
  const [page, setPage]       = useState(1)
  const [phone, setPhone]     = useState('')
  const [message, setMessage] = useState('')
  const [sending, setSending] = useState(false)

  const load = async () => {
    try { const {data} = await notifyApi.sms.getAll(page, 20, ''); setHistory(data.data?.items||[]); setTotal(data.data?.totalRecords||0) } catch {}
  }
  useEffect(() => { load() }, [page])

  const send = async () => {
    if (!phone || !message) return toast.error('Phone and message required')
    if (message.length > 1600) return toast.error('SMS max 1600 chars')
    setSending(true)
    try {
      const {data} = await notifyApi.sms.send({ phoneNumber: phone, message })
      toast.success(data.message); setPhone(''); setMessage(''); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Send failed') }
    finally { setSending(false) }
  }

  const smsCount = Math.ceil((message.length || 1) / 160)

  return (
    <div style={{display:'grid', gridTemplateColumns:'1fr 1.5fr', gap:20}}>
      <div className="card">
        <h4 style={{marginBottom:16, color:'#f7c948', display:'flex', alignItems:'center', gap:6}}>
          <Smartphone size={14}/> Send SMS
        </h4>
        <div className="form-group">
          <label>Phone Number</label>
          <input className="input" placeholder="+923001234567" value={phone} onChange={e=>setPhone(e.target.value)} />
        </div>
        <div className="form-group">
          <label>Message</label>
          <textarea className="input" rows={5} placeholder="SMS message…" value={message} onChange={e=>setMessage(e.target.value)} />
          <small style={{color:'var(--muted)'}}>{message.length} chars — {smsCount} SMS{smsCount>1?'es':''} ({160-(message.length%160)} remaining)</small>
        </div>
        <button className="btn btn-primary" onClick={send} disabled={sending} style={{width:'100%'}}>
          {sending ? 'Sending…' : <><Send size={13}/> Send SMS</>}
        </button>
      </div>
      <div className="card">
        <h4 style={{marginBottom:12}}>Send History</h4>
        <div style={{overflowY:'auto',maxHeight:380}}>
          {history.length === 0 && <p style={{color:'var(--muted)',textAlign:'center',padding:24}}>No SMS yet</p>}
          {history.map(h => (
            <div key={h.id} style={{padding:'10px 0',borderBottom:'1px solid var(--border)'}}>
              <div style={{display:'flex',justifyContent:'space-between'}}>
                <span style={{fontWeight:600,fontSize:13}}>{h.phoneNumber}</span>
                <StatusPill status={h.status}/>
              </div>
              <div style={{color:'var(--muted)',fontSize:12,marginTop:2}}>{h.message?.slice(0,80)}</div>
              <div style={{color:'var(--muted)',fontSize:11,marginTop:2}}>{fmtDate(h.submitDate)}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

// ── EMAIL TAB ─────────────────────────────────────────────────────────────────
function EmailTab() {
  const [history, setHistory] = useState([])
  const [total, setTotal]     = useState(0)
  const [page, setPage]       = useState(1)
  const [to, setTo]           = useState('')
  const [subject, setSubject] = useState('')
  const [body, setBody]       = useState('')
  const [sending, setSending] = useState(false)

  const load = async () => {
    try { const {data} = await notifyApi.email.getAll(page, 20, ''); setHistory(data.data?.items||[]); setTotal(data.data?.totalRecords||0) } catch {}
  }
  useEffect(() => { load() }, [page])

  const send = async () => {
    if (!to || !subject || !body) return toast.error('All fields required')
    setSending(true)
    try {
      const {data} = await notifyApi.email.send({ toEmail:to, subject, htmlBody:body })
      toast.success(data.message); setTo(''); setSubject(''); setBody(''); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Send failed') }
    finally { setSending(false) }
  }

  return (
    <div style={{display:'grid', gridTemplateColumns:'1fr 1.2fr', gap:20}}>
      <div className="card">
        <h4 style={{marginBottom:16, color:'var(--accent)', display:'flex', alignItems:'center', gap:6}}>
          <Mail size={14}/> Send Email
        </h4>
        <div className="form-group"><label>To Email</label>
          <input className="input" type="email" placeholder="user@example.com" value={to} onChange={e=>setTo(e.target.value)} />
        </div>
        <div className="form-group"><label>Subject</label>
          <input className="input" placeholder="Email subject" value={subject} onChange={e=>setSubject(e.target.value)} />
        </div>
        <div className="form-group"><label>Body (HTML)</label>
          <textarea className="input" rows={6} placeholder="<p>Hello...</p>" value={body} onChange={e=>setBody(e.target.value)} />
        </div>
        <button className="btn btn-primary" onClick={send} disabled={sending} style={{width:'100%'}}>
          {sending ? 'Sending…' : <><Send size={13}/> Send Email</>}
        </button>
      </div>
      <div className="card">
        <h4 style={{marginBottom:12}}>Send History</h4>
        <div style={{overflowY:'auto',maxHeight:400}}>
          {history.length === 0 && <p style={{color:'var(--muted)',textAlign:'center',padding:24}}>No emails yet</p>}
          {history.map(h => (
            <div key={h.id} style={{padding:'10px 0',borderBottom:'1px solid var(--border)'}}>
              <div style={{display:'flex',justifyContent:'space-between'}}>
                <span style={{fontWeight:600,fontSize:13}}>{h.toEmail}</span>
                <StatusPill status={h.status}/>
              </div>
              <div style={{color:'var(--text)',fontSize:12,marginTop:2}}>{h.subject}</div>
              <div style={{color:'var(--muted)',fontSize:11,marginTop:2}}>{fmtDate(h.submitDate)}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

// ── APP BROADCAST TAB ─────────────────────────────────────────────────────────
function AppBroadcastTab() {
  const [history, setHistory]       = useState([])
  const [title, setTitle]           = useState('')
  const [message, setMessage]       = useState('')
  const [type, setType]             = useState('General')
  const [sendToAll, setSendToAll]   = useState(true)
  const [role, setRole]             = useState('')
  const [sending, setSending]       = useState(false)

  const load = async () => {
    try { const {data} = await notifyApi.app.getAll(1,20,''); setHistory(data.data?.items||[]) } catch {}
  }
  useEffect(() => { load() }, [])

  const send = async () => {
    if (!title || !message) return toast.error('Title and message required')
    setSending(true)
    try {
      const body = { title, message, type, sendToAll, targetRoleName: !sendToAll&&role ? role : undefined }
      const {data} = await notifyApi.app.create(body)
      toast.success(data.message)
      setTitle(''); setMessage(''); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Send failed') }
    finally { setSending(false) }
  }

  return (
    <div style={{display:'grid', gridTemplateColumns:'1fr 1.2fr', gap:20}}>
      <div className="card">
        <h4 style={{marginBottom:16, color:'var(--accent2)', display:'flex', alignItems:'center', gap:6}}>
          <Radio size={14}/> Broadcast Notification
        </h4>
        <div className="form-group"><label>Title</label>
          <input className="input" placeholder="e.g. System Maintenance" value={title} onChange={e=>setTitle(e.target.value)} />
        </div>
        <div className="form-group"><label>Message</label>
          <textarea className="input" rows={4} placeholder="Notification body…" value={message} onChange={e=>setMessage(e.target.value)} />
        </div>
        <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:12}}>
          <div className="form-group"><label>Type</label>
            <select className="input" value={type} onChange={e=>setType(e.target.value)}>
              <option>General</option>
              <option>Holiday</option>
              <option>Urgent</option>
              <option>Maintenance</option>
            </select>
          </div>
          <div className="form-group"><label>Target</label>
            <select className="input" value={sendToAll?'all':'role'} onChange={e=>setSendToAll(e.target.value==='all')}>
              <option value="all">All Users</option>
              <option value="role">By Role</option>
            </select>
          </div>
        </div>
        {!sendToAll && (
          <div className="form-group"><label>Role Name</label>
            <input className="input" placeholder="e.g. Consultant Role" value={role} onChange={e=>setRole(e.target.value)} />
          </div>
        )}
        <button className="btn btn-primary" onClick={send} disabled={sending} style={{width:'100%'}}>
          {sending ? 'Sending…' : <><Radio size={13}/> Broadcast</>}
        </button>
      </div>

      <div className="card">
        <h4 style={{marginBottom:12}}>Broadcast History</h4>
        <div style={{overflowY:'auto',maxHeight:400}}>
          {history.length === 0 && <p style={{color:'var(--muted)',textAlign:'center',padding:24}}>No broadcasts yet</p>}
          {history.map(h => (
            <div key={h.id} style={{padding:'10px 0',borderBottom:'1px solid var(--border)'}}>
              <div style={{display:'flex',justifyContent:'space-between',alignItems:'center'}}>
                <strong style={{fontSize:13}}>{h.title}</strong>
                <span className="badge badge-gray">{h.type}</span>
              </div>
              <div style={{color:'var(--muted)',fontSize:12,marginTop:3}}>{h.message?.slice(0,80)}</div>
              <div style={{display:'flex',justifyContent:'space-between',marginTop:4,fontSize:11,color:'var(--muted)'}}>
                <span>By {h.sender}</span>
                <span>{h.targetCount} recipients · {h.readCount} read</span>
              </div>
              <div style={{color:'var(--muted)',fontSize:11}}>{fmtDate(h.createdOn)}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export default function NotificationsPage() {
  const [tab, setTab] = useState('wa')

  return (
    <>
      <div className="page-header">
        <h2>Send Notifications</h2>
      </div>

      <div style={{display:'flex',gap:4,marginBottom:20,borderBottom:'1px solid var(--border)'}}>
        {TABS.map(t => {
          const Icon = t.icon
          return (
            <button key={t.key} onClick={() => setTab(t.key)}
              style={{
                display:'flex',alignItems:'center',gap:6,
                padding:'10px 16px',border:'none',background:'none',cursor:'pointer',
                color: tab===t.key ? t.color : 'var(--muted)',
                borderBottom: tab===t.key ? `2px solid ${t.color}` : '2px solid transparent',
                fontSize:13, fontWeight: tab===t.key?600:400,
                transition:'all 0.15s', marginBottom:-1,
              }}>
              <Icon size={14}/> {t.label}
            </button>
          )
        })}
      </div>

      {tab === 'wa'      && <WaTab />}
      {tab === 'sms'     && <SmsTab />}
      {tab === 'email'   && <EmailTab />}
      {tab === 'webpush' && <div className="card"><p style={{color:'var(--muted)',padding:24,textAlign:'center'}}>WebPush — use broadcast button from WebPush Tokens page</p></div>}
      {tab === 'app'     && <AppBroadcastTab />}
    </>
  )
}
