import React, { useState, useEffect } from 'react'
import { toast } from 'react-hot-toast'
import { Plus, Pencil, Trash2, X, Copy, RefreshCw, FileDigit } from 'lucide-react'
import { docMovApi } from '../api'

function Modal({ open, title, onClose, children, footer }) {
  if (!open) return null
  return (
    <div className="modal-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="modal">
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

export default function DocumentMovementsPage() {
  const [items, setItems]       = useState([])
  const [search, setSearch]     = useState('')
  const [loading, setLoading]   = useState(true)
  const [modal, setModal]       = useState(false)
  const [editing, setEditing]   = useState(null)
  const [form, setForm]         = useState({ name:'', prefix:'', startFrom:1 })
  const [saving, setSaving]     = useState(false)
  const [delId, setDelId]       = useState(null)
  const [generated, setGenerated] = useState(null)
  const [genLoading, setGenLoading] = useState(null) // id being generated

  const load = async () => {
    setLoading(true)
    try { const { data } = await docMovApi.getAll(search); setItems(data.data || []) }
    catch { toast.error('Failed to load') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [search])

  const openCreate = () => { setEditing(null); setForm({ name:'', prefix:'', startFrom:1 }); setModal(true) }
  const openEdit   = item => { setEditing(item); setForm({ name: item.documentMovementName, prefix: item.prefix, startFrom: item.prefixNo }); setModal(true) }

  const save = async () => {
    if (!form.name || !form.prefix) return toast.error('Name and prefix required')
    if (!/^[A-Z0-9]+$/.test(form.prefix.toUpperCase())) return toast.error('Prefix: uppercase letters/digits only')
    setSaving(true)
    try {
      if (editing) await docMovApi.update(editing.id, { name: form.name, resetTo: form.startFrom })
      else         await docMovApi.create({ name: form.name, prefix: form.prefix, startFrom: form.startFrom })
      toast.success(editing ? 'Updated!' : 'Created!')
      setModal(false); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Error') }
    finally { setSaving(false) }
  }

  const del = async () => {
    try { await docMovApi.delete(delId); toast.success('Deleted'); setDelId(null); load() }
    catch { toast.error('Delete failed') }
  }

  const generate = async (item) => {
    setGenLoading(item.id)
    try {
      const { data } = await docMovApi.getNext(item.id)
      const num = data.data?.documentNumber
      setGenerated(num)
      navigator.clipboard?.writeText(num).then(() => toast.success(`Generated: ${num} (copied!)`)).catch(() => toast.success(`Generated: ${num}`))
      load()
    } catch (e) { toast.error(e.response?.data?.message || 'Generate failed') }
    finally { setGenLoading(null) }
  }

  return (
    <>
      <div className="page-header">
        <h2>Document Movements</h2>
        <button className="btn btn-primary" onClick={openCreate}><Plus size={14}/> New</button>
      </div>

      {generated && (
        <div style={{
          background:'var(--bg3)', border:'1px solid var(--green)', borderRadius:10,
          padding:'12px 20px', marginBottom:16, display:'flex', alignItems:'center', gap:12,
        }}>
          <FileDigit size={16} color="var(--green)" />
          <span style={{ fontSize:14, color:'var(--muted)' }}>Last generated:</span>
          <code style={{ fontSize:18, fontWeight:800, color:'var(--green)', letterSpacing:2 }}>{generated}</code>
          <button className="btn-icon" onClick={() => navigator.clipboard?.writeText(generated)} title="Copy">
            <Copy size={13}/>
          </button>
          <button className="btn-icon" onClick={() => setGenerated(null)} style={{ marginLeft:'auto' }}><X size={13}/></button>
        </div>
      )}

      <div style={{ marginBottom:16 }}>
        <input className="input" placeholder="Search by name or prefix…" value={search}
          onChange={e => setSearch(e.target.value)} style={{ maxWidth:280 }} />
      </div>

      {loading ? (
        <div style={{ padding:40, textAlign:'center', color:'var(--muted)' }}>Loading…</div>
      ) : (
        <div className="card">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Prefix</th>
                <th>Next Number</th>
                <th>Total Generated</th>
                <th style={{ textAlign:'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr><td colSpan={5} style={{ textAlign:'center', color:'var(--muted)', padding:32 }}>
                  No document movements yet.
                </td></tr>
              )}
              {items.map(item => (
                <tr key={item.id}>
                  <td><strong>{item.documentMovementName}</strong></td>
                  <td><code style={{ color:'var(--accent)', fontSize:13 }}>{item.prefix}</code></td>
                  <td>
                    <code style={{
                      fontSize:14, fontWeight:700, color:'var(--green)',
                      background:'var(--bg3)', padding:'3px 8px', borderRadius:4,
                    }}>
                      {item.nextNumber}
                    </code>
                  </td>
                  <td style={{ color:'var(--muted)' }}>{item.totalGenerated} used</td>
                  <td>
                    <div style={{ display:'flex', gap:6, justifyContent:'flex-end' }}>
                      <button className="btn btn-ghost" style={{ fontSize:11, padding:'4px 10px', color:'var(--green)' }}
                        onClick={() => generate(item)} disabled={genLoading === item.id}>
                        {genLoading === item.id ? <RefreshCw size={11} className="spin"/> : <><FileDigit size={11}/> Generate</>}
                      </button>
                      <button className="btn-icon" onClick={() => openEdit(item)}><Pencil size={13}/></button>
                      <button className="btn-icon danger" onClick={() => setDelId(item.id)}><Trash2 size={13}/></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Modal open={modal} title={editing ? 'Edit Document Movement' : 'New Document Movement'} onClose={() => setModal(false)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setModal(false)}>Cancel</button>
          <button className="btn btn-primary" onClick={save} disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
        </>}>
        <div className="form-group">
          <label>Name <span style={{ color:'var(--accent2)' }}>*</span></label>
          <input className="input" placeholder="e.g. Client Party" value={form.name}
            onChange={e => setForm(f => ({...f, name: e.target.value}))} />
        </div>
        {!editing && (
          <div className="form-group">
            <label>Prefix <span style={{ color:'var(--accent2)' }}>*</span></label>
            <input className="input" placeholder="e.g. CLTPTY" maxLength={10}
              value={form.prefix} onChange={e => setForm(f => ({...f, prefix: e.target.value.toUpperCase()}))} />
            <small style={{ color:'var(--muted)' }}>Uppercase letters/digits only. Will generate: {form.prefix || 'PREFIX'}-0001</small>
          </div>
        )}
        <div className="form-group">
          <label>{editing ? 'Reset Counter To (only forward)' : 'Start From'}</label>
          <input className="input" type="number" min={1} value={form.startFrom}
            onChange={e => setForm(f => ({...f, startFrom: parseInt(e.target.value)||1}))} />
          {editing && <small style={{ color:'var(--muted)' }}>Counter can only move forward — cannot go below current value.</small>}
        </div>
        <div style={{ background:'var(--bg3)', borderRadius:8, padding:'10px 14px', fontSize:13 }}>
          Preview: <code style={{ color:'var(--green)', fontWeight:700, fontSize:15 }}>
            {form.prefix || 'PREFIX'}-{String(form.startFrom).padStart(4,'0')}
          </code>
        </div>
      </Modal>

      <Modal open={!!delId} title="Confirm Delete" onClose={() => setDelId(null)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setDelId(null)}>Cancel</button>
          <button className="btn btn-danger" onClick={del}>Delete</button>
        </>}>
        <p style={{ color:'var(--muted)' }}>Delete this document movement? Historical documents will lose their reference.</p>
      </Modal>
    </>
  )
}
