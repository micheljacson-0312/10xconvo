import React, { useState, useEffect, useRef } from 'react'
import { toast } from 'react-hot-toast'
import { Plus, Pencil, Trash2, X, Eye, Send, MessageSquare, Mail, Bell, Smartphone } from 'lucide-react'
import { templateApi } from '../api'

function Modal({ open, title, onClose, children, footer, wide }) {
  if (!open) return null
  return (
    <div className="modal-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="modal" style={wide ? { maxWidth: 720, width: '95vw' } : {}}>
        <div className="modal-header">
          <h3>{title}</h3>
          <button className="btn-icon" onClick={onClose}><X size={14} /></button>
        </div>
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-footer">{footer}</div>}
      </div>
    </div>
  )
}

function StatusBadge({ status }) {
  return (
    <span className={`badge ${status === 'active' ? 'badge-green' : 'badge-gray'}`}>
      {status}
    </span>
  )
}

// ── Generic template list with CRUD ──────────────────────────────────────────
function TemplateList({ api: tApi, channel, fields, extraActions }) {
  const [items, setItems]     = useState([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch]   = useState('')
  const [modal, setModal]     = useState(false)
  const [editing, setEditing] = useState(null)
  const [form, setForm]       = useState({})
  const [saving, setSaving]   = useState(false)
  const [delId, setDelId]     = useState(null)
  const [page, setPage]       = useState(1)
  const [total, setTotal]     = useState(0)
  const PS = 15

  const load = async () => {
    setLoading(true)
    try {
      const { data } = await tApi.getAll(page, PS, search)
      setItems(data.data?.items || [])
      setTotal(data.data?.totalRecords || 0)
    } catch { toast.error('Failed to load') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [page, search])

  const emptyForm = () => fields.reduce((a, f) => ({ ...a, [f.key]: f.default ?? '' }), {})

  const openCreate = () => { setEditing(null); setForm(emptyForm()); setModal(true) }
  const openEdit   = item => { setEditing(item); setForm(fields.reduce((a,f) => ({...a,[f.key]:item[f.key]??''}),{})); setModal(true) }

  const save = async () => {
    for (const f of fields) {
      if (f.required && !form[f.key]?.toString().trim())
        return toast.error(`${f.label} is required`)
    }
    setSaving(true)
    try {
      if (editing) await tApi.update(editing.id, form)
      else         await tApi.create(form)
      toast.success(editing ? 'Updated!' : 'Created!')
      setModal(false); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Error') }
    finally { setSaving(false) }
  }

  const del = async () => {
    try { await tApi.delete(delId); toast.success('Deleted'); setDelId(null); load() }
    catch { toast.error('Delete failed') }
  }

  const toggleStatus = async item => {
    const newStatus = item.status === 'active' ? 'inactive' : 'active'
    try { await tApi.setStatus(item.id, newStatus); load() }
    catch { toast.error('Failed') }
  }

  return (
    <>
      <div style={{ display:'flex', gap:10, marginBottom:16 }}>
        <input className="input" placeholder={`Search ${channel} templates…`} value={search}
          onChange={e => { setSearch(e.target.value); setPage(1) }}
          style={{ maxWidth: 280 }} />
        <button className="btn btn-primary" onClick={openCreate}><Plus size={14} /> New Template</button>
      </div>

      {loading ? (
        <div style={{ padding:40, textAlign:'center', color:'var(--muted)' }}>Loading…</div>
      ) : (
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Title</th>
              <th>Activity</th>
              <th>Status</th>
              <th style={{textAlign:'right'}}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && (
              <tr><td colSpan={5} style={{textAlign:'center',color:'var(--muted)',padding:32}}>
                No templates yet.
              </td></tr>
            )}
            {items.map(item => (
              <tr key={item.id}>
                <td><strong>{item.templateName}</strong></td>
                <td style={{color:'var(--muted)'}}>{item.templateTitle}</td>
                <td>{item.activity || '—'}</td>
                <td>
                  <button style={{background:'none',border:'none',cursor:'pointer',padding:0}}
                    onClick={() => toggleStatus(item)} title="Toggle status">
                    <StatusBadge status={item.status} />
                  </button>
                </td>
                <td>
                  <div style={{display:'flex',gap:6,justifyContent:'flex-end'}}>
                    {extraActions && extraActions(item)}
                    <button className="btn-icon" onClick={() => openEdit(item)}><Pencil size={13}/></button>
                    <button className="btn-icon danger" onClick={() => setDelId(item.id)}><Trash2 size={13}/></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {/* Pagination */}
      {total > PS && (
        <div style={{display:'flex',justifyContent:'space-between',alignItems:'center',marginTop:12}}>
          <span style={{color:'var(--muted)',fontSize:12}}>{total} templates</span>
          <div className="pagination">
            {Array.from({length:Math.ceil(total/PS)},(_,i)=>i+1).slice(0,7).map(p=>(
              <button key={p} className={`page-btn ${p===page?'active':''}`} onClick={()=>setPage(p)}>{p}</button>
            ))}
          </div>
        </div>
      )}

      {/* Create/Edit Modal */}
      <Modal open={modal} title={editing ? `Edit ${channel} Template` : `New ${channel} Template`}
        onClose={() => setModal(false)} wide={channel === 'Email'}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setModal(false)}>Cancel</button>
          <button className="btn btn-primary" onClick={save} disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
        </>}>
        {fields.map(f => (
          <div className="form-group" key={f.key}>
            <label>{f.label} {f.required && <span style={{color:'var(--accent2)'}}>*</span>}</label>
            {f.type === 'textarea' ? (
              <textarea className="input" rows={f.rows || 4} placeholder={f.placeholder}
                value={form[f.key] || ''} onChange={e => setForm(x => ({...x,[f.key]:e.target.value}))} />
            ) : (
              <input className="input" type={f.inputType || 'text'} placeholder={f.placeholder}
                value={form[f.key] || ''} onChange={e => setForm(x => ({...x,[f.key]:e.target.value}))} />
            )}
            {f.hint && <small style={{color:'var(--muted)'}}>{f.hint}</small>}
          </div>
        ))}
      </Modal>

      {/* Delete Confirm */}
      <Modal open={!!delId} title="Confirm Delete" onClose={() => setDelId(null)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setDelId(null)}>Cancel</button>
          <button className="btn btn-danger" onClick={del}>Delete</button>
        </>}>
        <p style={{color:'var(--muted)'}}>Delete this template? This cannot be undone.</p>
      </Modal>
    </>
  )
}

// ── PREVIEW MODAL ──────────────────────────────────────────────────────────────
function PreviewModal({ open, item, channel, onClose }) {
  const [vars, setVars]       = useState({})
  const [preview, setPreview] = useState('')
  const [loading, setLoading] = useState(false)

  const doPreview = async () => {
    setLoading(true)
    try {
      const api = channel === 'WA' ? templateApi.wa : channel === 'SMS' ? templateApi.sms : templateApi.email
      const { data } = await api.preview(item.id, vars)
      setPreview(data.data?.preview || data.data?.body || '')
    } catch { toast.error('Preview failed') }
    finally { setLoading(false) }
  }

  useEffect(() => { if (open && item) { setVars({}); setPreview(item.body || '') } }, [open, item])

  if (!open || !item) return null
  return (
    <Modal open={open} title={`Preview — ${item.templateName}`} onClose={onClose} wide>
      <div style={{marginBottom:12}}>
        <label style={{color:'var(--muted)',fontSize:12}}>Template Body</label>
        <div style={{background:'var(--bg)',border:'1px solid var(--border)',borderRadius:8,padding:12,
          fontFamily:'monospace',fontSize:13,color:'var(--text)',whiteSpace:'pre-wrap',marginTop:4}}>
          {item.body}
        </div>
      </div>
      {item.variables && (
        <>
          <p style={{color:'var(--muted)',fontSize:12,marginBottom:8}}>
            Variables: <code style={{color:'var(--accent)'}}>{item.variables}</code>
          </p>
          <div style={{display:'grid',gridTemplateColumns:'repeat(2,1fr)',gap:8,marginBottom:12}}>
            {item.variables.split(',').map(v => v.trim()).filter(Boolean).map(v => (
              <div key={v}>
                <label style={{color:'var(--muted)',fontSize:11}}>{`{{${v}}}`}</label>
                <input className="input" placeholder={`Value for ${v}`} value={vars[v]||''}
                  onChange={e => setVars(x => ({...x,[v]:e.target.value}))} />
              </div>
            ))}
          </div>
          <button className="btn btn-primary" onClick={doPreview} disabled={loading} style={{marginBottom:12}}>
            {loading ? 'Loading…' : 'Generate Preview'}
          </button>
        </>
      )}
      {preview && (
        <div>
          <label style={{color:'var(--muted)',fontSize:12}}>Preview Output</label>
          <div style={{background:'var(--bg3)',border:'1px solid var(--border)',borderRadius:8,padding:12,
            fontSize:13,color:'var(--text)',whiteSpace:'pre-wrap',marginTop:4,minHeight:60}}>
            {preview}
          </div>
        </div>
      )}
    </Modal>
  )
}

// ── TEST EMAIL MODAL ──────────────────────────────────────────────────────────
function TestEmailModal({ open, item, onClose }) {
  const [email, setEmail]   = useState('')
  const [vars, setVars]     = useState({})
  const [sending, setSending] = useState(false)

  const send = async () => {
    if (!email) return toast.error('Email required')
    setSending(true)
    try {
      await templateApi.email.sendTest(item.id, { testEmail: email, variables: vars })
      toast.success('Test email sent!')
      onClose()
    } catch { toast.error('Send failed') }
    finally { setSending(false) }
  }

  if (!open || !item) return null
  return (
    <Modal open={open} title={`Send Test — ${item.templateName}`} onClose={onClose}
      footer={<>
        <button className="btn btn-ghost" onClick={onClose}>Cancel</button>
        <button className="btn btn-primary" onClick={send} disabled={sending}>{sending ? 'Sending…' : 'Send Test'}</button>
      </>}>
      <div className="form-group">
        <label>Send to Email</label>
        <input className="input" type="email" placeholder="test@example.com" value={email}
          onChange={e => setEmail(e.target.value)} />
      </div>
      {item.variables && (
        <div style={{display:'grid',gridTemplateColumns:'repeat(2,1fr)',gap:8}}>
          {item.variables.split(',').map(v => v.trim()).filter(Boolean).map(v => (
            <div key={v} className="form-group">
              <label style={{fontSize:11}}>{`{{${v}}}`}</label>
              <input className="input" placeholder={v} value={vars[v]||''}
                onChange={e => setVars(x => ({...x,[v]:e.target.value}))} />
            </div>
          ))}
        </div>
      )}
    </Modal>
  )
}

// ── MAIN PAGE ─────────────────────────────────────────────────────────────────
const TABS = [
  { key: 'wa',    label: 'WhatsApp', icon: MessageSquare },
  { key: 'sms',   label: 'SMS',      icon: Smartphone },
  { key: 'email', label: 'Email',    icon: Mail },
  { key: 'web',   label: 'Web Push', icon: Bell },
]

const WA_FIELDS = [
  { key:'templateName',  label:'Name',      required:true,  placeholder:'e.g. welcome_user' },
  { key:'templateTitle', label:'Title',     required:true,  placeholder:'Display title' },
  { key:'activity',      label:'Activity',  required:false, placeholder:'e.g. user_registration' },
  { key:'body',          label:'Body',      required:true,  type:'textarea', rows:5, placeholder:'Hello {{name}}, welcome!', hint:'Use {{variable}} for dynamic content' },
  { key:'variables',     label:'Variables', required:false, placeholder:'name, phone, date', hint:'Comma-separated list of variable names' },
]
const SMS_FIELDS = [
  { key:'templateName',  label:'Name',    required:true, placeholder:'e.g. otp_sms' },
  { key:'templateTitle', label:'Title',   required:true, placeholder:'Display title' },
  { key:'activity',      label:'Activity',required:false,placeholder:'e.g. otp_verify' },
  { key:'body',          label:'Body',    required:true, type:'textarea', rows:4, placeholder:'Your OTP is {{otp}}. Valid for 10 mins.' },
]
const EMAIL_FIELDS = [
  { key:'templateName',  label:'Name',    required:true, placeholder:'e.g. welcome_email' },
  { key:'templateTitle', label:'Title',   required:true, placeholder:'Display title' },
  { key:'activity',      label:'Activity',required:false,placeholder:'e.g. user_signup' },
  { key:'subject',       label:'Subject', required:true, placeholder:'Welcome to 10X Convo, {{name}}!' },
  { key:'body',          label:'HTML Body',required:true,type:'textarea',rows:8, placeholder:'<p>Hello {{name}}</p>...', hint:'HTML supported. Use {{variable}} for dynamic content' },
]
const WEB_FIELDS = [
  { key:'templateName',  label:'Name',    required:true, placeholder:'e.g. new_message_push' },
  { key:'templateTitle', label:'Title',   required:true, placeholder:'Push title' },
  { key:'activity',      label:'Activity',required:false,placeholder:'e.g. new_message' },
  { key:'body',          label:'Body',    required:true, type:'textarea', rows:3, placeholder:'You have a new message from {{sender}}' },
]

export default function NotificationTemplatesPage() {
  const [tab, setTab]           = useState('wa')
  const [previewItem, setPreview] = useState(null)
  const [testItem, setTest]     = useState(null)

  const tabConfig = {
    wa:    { api: templateApi.wa,    fields: WA_FIELDS,    hasPreview: true,  hasTest: false },
    sms:   { api: templateApi.sms,   fields: SMS_FIELDS,   hasPreview: false, hasTest: false },
    email: { api: templateApi.email, fields: EMAIL_FIELDS, hasPreview: true,  hasTest: true  },
    web:   { api: templateApi.web,   fields: WEB_FIELDS,   hasPreview: false, hasTest: false },
  }

  const cfg = tabConfig[tab]

  const extraActions = item => (
    <>
      {cfg.hasPreview && (
        <button className="btn-icon" title="Preview" onClick={() => setPreview(item)}>
          <Eye size={13} />
        </button>
      )}
      {cfg.hasTest && (
        <button className="btn-icon" title="Send Test" onClick={() => setTest(item)}>
          <Send size={13} color="var(--green)" />
        </button>
      )}
    </>
  )

  return (
    <>
      <div className="page-header">
        <h2>Notification Templates</h2>
      </div>

      {/* Tabs */}
      <div style={{ display:'flex', gap:4, marginBottom:20, borderBottom:'1px solid var(--border)', paddingBottom:0 }}>
        {TABS.map(t => {
          const Icon = t.icon
          return (
            <button key={t.key}
              onClick={() => setTab(t.key)}
              style={{
                display:'flex', alignItems:'center', gap:6,
                padding:'10px 18px', border:'none', background:'none', cursor:'pointer',
                color: tab === t.key ? 'var(--accent)' : 'var(--muted)',
                borderBottom: tab === t.key ? '2px solid var(--accent)' : '2px solid transparent',
                fontSize:13, fontWeight: tab===t.key ? 600 : 400,
                transition:'all 0.15s', marginBottom:-1,
              }}>
              <Icon size={14} /> {t.label}
            </button>
          )
        })}
      </div>

      <div className="card">
        <TemplateList
          key={tab}
          api={cfg.api}
          channel={tab.toUpperCase()}
          fields={cfg.fields}
          extraActions={extraActions}
        />
      </div>

      <PreviewModal open={!!previewItem} item={previewItem}
        channel={tab.toUpperCase()} onClose={() => setPreview(null)} />
      <TestEmailModal open={!!testItem} item={testItem} onClose={() => setTest(null)} />
    </>
  )
}
