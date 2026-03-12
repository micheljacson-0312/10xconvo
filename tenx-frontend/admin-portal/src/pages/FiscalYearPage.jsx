import React, { useState, useEffect } from 'react'
import { toast } from 'react-hot-toast'
import { Plus, Pencil, Trash2, X, CalendarDays, CheckCircle, Circle, ToggleLeft, ToggleRight } from 'lucide-react'
import { fiscalYearApi } from '../api'

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

const empty = { name: '', startDate: '', endDate: '', isActive: true }

export default function FiscalYearPage() {
  const [items, setItems]       = useState([])
  const [loading, setLoading]   = useState(true)
  const [modal, setModal]       = useState(false)
  const [editing, setEditing]   = useState(null)
  const [form, setForm]         = useState(empty)
  const [saving, setSaving]     = useState(false)
  const [delId, setDelId]       = useState(null)

  const load = async () => {
    try {
      setLoading(true)
      const { data } = await fiscalYearApi.getAll()
      setItems(data.data || [])
    } catch { toast.error('Failed to load fiscal years') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [])

  const openCreate = () => { setEditing(null); setForm(empty); setModal(true) }
  const openEdit   = item => {
    setEditing(item)
    setForm({
      name:      item.name,
      startDate: item.startDate?.slice(0, 10),
      endDate:   item.endDate?.slice(0, 10),
      isActive:  item.isActive,
    })
    setModal(true)
  }

  const save = async () => {
    if (!form.name.trim()) return toast.error('Name required')
    if (!form.startDate || !form.endDate) return toast.error('Both dates required')
    setSaving(true)
    try {
      if (editing) await fiscalYearApi.update(editing.id, form)
      else         await fiscalYearApi.create(form)
      toast.success(editing ? 'Updated!' : 'Created!')
      setModal(false); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Error') }
    finally { setSaving(false) }
  }

  const setCurrent = async id => {
    try { await fiscalYearApi.setCurrent(id); toast.success('Set as current FY'); load() }
    catch (e) { toast.error(e.response?.data?.message || 'Error') }
  }

  const toggle = async id => {
    try { await fiscalYearApi.toggle(id); load() }
    catch (e) { toast.error(e.response?.data?.message || 'Error') }
  }

  const del = async () => {
    try { await fiscalYearApi.delete(delId); toast.success('Deleted'); setDelId(null); load() }
    catch (e) { toast.error(e.response?.data?.message || 'Cannot delete') }
  }

  const fmtDate = d => d ? new Date(d).toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' }) : '—'

  return (
    <>
      <div className="page-header">
        <h2>Fiscal Years</h2>
        <button className="btn btn-primary" onClick={openCreate}><Plus size={14} /> New FY</button>
      </div>

      {loading ? (
        <div style={{ padding: 40, textAlign: 'center', color: 'var(--muted)' }}>Loading…</div>
      ) : (
        <div className="card">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Start Date</th>
                <th>End Date</th>
                <th>Status</th>
                <th>Current</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr><td colSpan={6} style={{ textAlign:'center', color:'var(--muted)', padding:32 }}>
                  No fiscal years yet. Create one to get started.
                </td></tr>
              )}
              {items.map(fy => (
                <tr key={fy.id}>
                  <td>
                    <div style={{ display:'flex', alignItems:'center', gap:8 }}>
                      <CalendarDays size={14} color="var(--accent)" />
                      <strong>{fy.name}</strong>
                    </div>
                  </td>
                  <td>{fmtDate(fy.startDate)}</td>
                  <td>{fmtDate(fy.endDate)}</td>
                  <td>
                    <span className={`badge ${fy.isActive ? 'badge-green' : 'badge-gray'}`}>
                      {fy.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    {fy.isCurrent ? (
                      <span style={{ display:'flex', alignItems:'center', gap:4, color:'var(--green)', fontSize:12, fontWeight:600 }}>
                        <CheckCircle size={14} /> Current
                      </span>
                    ) : (
                      <button className="btn btn-ghost" style={{ fontSize:11, padding:'3px 8px' }} onClick={() => setCurrent(fy.id)}>
                        Set Current
                      </button>
                    )}
                  </td>
                  <td>
                    <div style={{ display:'flex', gap:6, justifyContent:'flex-end' }}>
                      <button className="btn-icon" title={fy.isActive ? 'Deactivate' : 'Activate'} onClick={() => toggle(fy.id)}>
                        {fy.isActive ? <ToggleRight size={14} color="var(--green)" /> : <ToggleLeft size={14} />}
                      </button>
                      <button className="btn-icon" onClick={() => openEdit(fy)}><Pencil size={13} /></button>
                      {!fy.isCurrent && (
                        <button className="btn-icon danger" onClick={() => setDelId(fy.id)}><Trash2 size={13} /></button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create / Edit Modal */}
      <Modal open={modal} title={editing ? 'Edit Fiscal Year' : 'New Fiscal Year'} onClose={() => setModal(false)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setModal(false)}>Cancel</button>
          <button className="btn btn-primary" onClick={save} disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
        </>}>
        <div className="form-group">
          <label>Name <span style={{color:'var(--accent2)'}}>*</span></label>
          <input className="input" placeholder="e.g. FY 2026–2027" value={form.name}
            onChange={e => setForm(f => ({...f, name: e.target.value}))} />
        </div>
        <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:12 }}>
          <div className="form-group">
            <label>Start Date <span style={{color:'var(--accent2)'}}>*</span></label>
            <input className="input" type="date" value={form.startDate}
              onChange={e => setForm(f => ({...f, startDate: e.target.value}))} />
          </div>
          <div className="form-group">
            <label>End Date <span style={{color:'var(--accent2)'}}>*</span></label>
            <input className="input" type="date" value={form.endDate}
              onChange={e => setForm(f => ({...f, endDate: e.target.value}))} />
          </div>
        </div>
        <div className="form-group">
          <label style={{ display:'flex', alignItems:'center', gap:8, cursor:'pointer' }}>
            <input type="checkbox" checked={form.isActive}
              onChange={e => setForm(f => ({...f, isActive: e.target.checked}))} />
            Active
          </label>
        </div>
      </Modal>

      {/* Delete Confirm */}
      <Modal open={!!delId} title="Confirm Delete" onClose={() => setDelId(null)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setDelId(null)}>Cancel</button>
          <button className="btn btn-danger" onClick={del}>Delete</button>
        </>}>
        <p style={{ color:'var(--muted)' }}>Delete this fiscal year? This cannot be undone.</p>
      </Modal>
    </>
  )
}
