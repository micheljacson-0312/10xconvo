// ═══════════════════════════════════════════════════════════════════════════
//  ADMIN PORTAL — PART B PAGES
//  FiscalYearPage, RolePermissionsPage, DocMovementsPage,
//  NotificationTemplatesPage, NotificationsPage, ReviewsAdminPage,
//  DataExtPage (Districts/Tehsils/Areas/DocTypes/Criteria/ClientCats)
// ═══════════════════════════════════════════════════════════════════════════
import React, { useState, useEffect, useCallback, useRef } from 'react'
import { toast } from 'react-hot-toast'
import {
  Plus, Pencil, Trash2, X, Search, RefreshCw, Eye, Send, Star,
  ChevronDown, ChevronUp, CheckCircle, XCircle, CalendarDays,
  Copy, FileDigit, Shield, Bell, MessageSquare, Mail, Smartphone,
  Globe, Radio, Zap, Filter, ToggleLeft, ToggleRight, Play
} from 'lucide-react'
import {
  fiscalYearApi, roleModuleApi, docMovApi, templateApi, notifyApi,
  reviewAdminApi, dataExtApi, roleApi, userApi
} from '../api'

// ── SHARED COMPONENTS ────────────────────────────────────────────────────────

function Modal({ open, title, onClose, children, footer, wide }) {
  if (!open) return null
  return (
    <div className="modal-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="modal" style={wide ? { maxWidth: 760 } : {}}>
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

function ConfirmDelete({ open, name, onConfirm, onClose }) {
  return (
    <Modal open={open} title="Confirm Delete" onClose={onClose}
      footer={<><button className="btn btn-ghost" onClick={onClose}>Cancel</button><button className="btn btn-danger" onClick={onConfirm}>Delete</button></>}>
      <p style={{ color: 'var(--muted)' }}>Delete <strong style={{ color: 'var(--text)' }}>{name}</strong>? This cannot be undone.</p>
    </Modal>
  )
}

function Pagination({ page, total, pageSize, onChange }) {
  const pages = Math.ceil(total / pageSize)
  if (pages <= 1) return null
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 16 }}>
      <span style={{ color: 'var(--muted)', fontSize: 12 }}>{total} records</span>
      <div className="pagination">
        {Array.from({ length: Math.min(pages, 7) }, (_, i) => i + 1).map(p => (
          <button key={p} className={`page-btn ${p === page ? 'active' : ''}`} onClick={() => onChange(p)}>{p}</button>
        ))}
      </div>
    </div>
  )
}

function StatusBadge({ status }) {
  const color = status === 'active' ? 'var(--green)' : 'var(--muted)'
  return <span style={{ fontSize: 11, color, background: color + '22', padding: '2px 8px', borderRadius: 20, fontWeight: 600 }}>{status}</span>
}

function Field({ label, children }) {
  return (
    <div className="field">
      <label className="label">{label}</label>
      {children}
    </div>
  )
}

// ── 1. FISCAL YEARS ───────────────────────────────────────────────────────────

export function FiscalYearPage() {
  const [items, setItems]     = useState([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal]     = useState(null) // null | 'create' | 'edit'
  const [delItem, setDelItem] = useState(null)
  const [editing, setEditing] = useState(null)
  const [form, setForm]       = useState({ name: '', startDate: '', endDate: '', isActive: true })

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const r = await fiscalYearApi.getAll()
      setItems(r.data.data)
    } catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [])

  useEffect(() => { load() }, [load])

  const openCreate = () => { setForm({ name: '', startDate: '', endDate: '', isActive: true }); setEditing(null); setModal('form') }
  const openEdit   = item => { setForm({ name: item.name, startDate: item.startDate?.slice(0,10), endDate: item.endDate?.slice(0,10), isActive: item.isActive }); setEditing(item); setModal('form') }

  const save = async () => {
    try {
      if (editing) await fiscalYearApi.update(editing.id, form)
      else         await fiscalYearApi.create(form)
      toast.success(editing ? 'Updated' : 'Created')
      setModal(null); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
  }

  const setCurrent = async id => {
    try { await fiscalYearApi.setCurrent(id); toast.success('Set as current FY'); load() }
    catch (e) { toast.error(e.response?.data?.message || 'Failed') }
  }

  const toggleActive = async id => {
    try { await fiscalYearApi.toggle(id); load() }
    catch (e) { toast.error(e.response?.data?.message || 'Cannot deactivate current FY') }
  }

  const del = async () => {
    try { await fiscalYearApi.delete(delItem.id); toast.success('Deleted'); setDelItem(null); load() }
    catch (e) { toast.error(e.response?.data?.message || 'Failed') }
  }

  return (
    <>
      <div className="page-header">
        <h2>Fiscal Years</h2>
        <button className="btn btn-primary" onClick={openCreate}><Plus size={14} /> Add Fiscal Year</button>
      </div>
      <div className="card">
        {loading ? <div className="spinner" /> : (
          <table className="table">
            <thead><tr><th>Name</th><th>Start Date</th><th>End Date</th><th>Status</th><th>Current</th><th>Actions</th></tr></thead>
            <tbody>
              {items.map(fy => (
                <tr key={fy.id}>
                  <td><strong>{fy.name}</strong></td>
                  <td>{fy.startDate?.slice(0,10)}</td>
                  <td>{fy.endDate?.slice(0,10)}</td>
                  <td><StatusBadge status={fy.isActive ? 'active' : 'inactive'} /></td>
                  <td>
                    {fy.isCurrent
                      ? <span style={{ color: 'var(--green)', fontWeight: 700, fontSize: 12 }}>✓ CURRENT</span>
                      : <button className="btn btn-ghost" style={{ fontSize: 11, padding: '2px 8px' }} onClick={() => setCurrent(fy.id)}>Set Current</button>
                    }
                  </td>
                  <td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button className="btn-icon" onClick={() => openEdit(fy)}><Pencil size={13} /></button>
                      <button className="btn-icon" onClick={() => toggleActive(fy.id)}>
                        {fy.isActive ? <ToggleRight size={13} style={{ color: 'var(--green)' }} /> : <ToggleLeft size={13} />}
                      </button>
                      {!fy.isCurrent && <button className="btn-icon" onClick={() => setDelItem(fy)}><Trash2 size={13} /></button>}
                    </div>
                  </td>
                </tr>
              ))}
              {!items.length && <tr><td colSpan={6} style={{ textAlign: 'center', color: 'var(--muted)', padding: 32 }}>No fiscal years yet</td></tr>}
            </tbody>
          </table>
        )}
      </div>

      <Modal open={modal === 'form'} title={editing ? 'Edit Fiscal Year' : 'Add Fiscal Year'} onClose={() => setModal(null)}
        footer={<><button className="btn btn-ghost" onClick={() => setModal(null)}>Cancel</button><button className="btn btn-primary" onClick={save}>Save</button></>}>
        <Field label="Name (e.g. FY 2025-2026)">
          <input className="input" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} placeholder="FY 2025-2026" />
        </Field>
        <Field label="Start Date">
          <input className="input" type="date" value={form.startDate} onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))} />
        </Field>
        <Field label="End Date">
          <input className="input" type="date" value={form.endDate} onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))} />
        </Field>
        <label style={{ display: 'flex', gap: 8, alignItems: 'center', cursor: 'pointer', marginTop: 8 }}>
          <input type="checkbox" checked={form.isActive} onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))} />
          <span style={{ fontSize: 13 }}>Active</span>
        </label>
      </Modal>

      <ConfirmDelete open={!!delItem} name={delItem?.name} onConfirm={del} onClose={() => setDelItem(null)} />
    </>
  )
}

// ── 2. ROLE PERMISSIONS (modules + menus) ────────────────────────────────────

const ALL_MODULES = [
  { key: 'DASHBOARD',     name: 'Dashboard' },
  { key: 'USERS',         name: 'User Registrations' },
  { key: 'ROLES',         name: 'Roles' },
  { key: 'LOCATIONS',     name: 'Locations' },
  { key: 'SETTINGS',      name: 'Settings' },
  { key: 'DATA',          name: 'Data Constants' },
  { key: 'NOTIFICATIONS', name: 'Notifications' },
  { key: 'REPORTS',       name: 'Reports' },
  { key: 'MESSAGES',      name: 'Messages' },
  { key: 'CLIENTS',       name: 'Clients' },
]

export function RolePermissionsPage() {
  const [roles, setRoles]         = useState([])
  const [selectedRole, setSelected] = useState(null)
  const [modules, setModules]     = useState([])
  const [saving, setSaving]       = useState(false)
  const [tab, setTab]             = useState('modules') // 'modules' | 'menus'

  useEffect(() => {
    roleApi.getAll(1, 100, '').then(r => setRoles(r.data.data.items)).catch(() => {})
  }, [])

  const loadModules = async (roleId) => {
    try {
      const r = await roleModuleApi.getModules(roleId)
      const existing = r.data.data || []
      const merged = ALL_MODULES.map(m => {
        const found = existing.find(e => e.moduleKey === m.key)
        return { moduleKey: m.key, moduleName: m.name, canView: found?.canView ?? true, canCreate: found?.canCreate ?? false, canEdit: found?.canEdit ?? false, canDelete: found?.canDelete ?? false, canExport: found?.canExport ?? false }
      })
      setModules(merged)
    } catch { toast.error('Load failed') }
  }

  const selectRole = r => { setSelected(r); loadModules(r.id) }

  const toggle = (key, perm) => {
    setModules(ms => ms.map(m => m.moduleKey === key ? { ...m, [perm]: !m[perm] } : m))
  }

  const saveModules = async () => {
    if (!selectedRole) return
    setSaving(true)
    try {
      await roleModuleApi.bulkModules(selectedRole.id, modules)
      toast.success('Permissions saved')
    } catch { toast.error('Save failed') }
    finally { setSaving(false) }
  }

  const permCols = ['canView', 'canCreate', 'canEdit', 'canDelete', 'canExport']

  return (
    <>
      <div className="page-header">
        <h2>Role Permissions</h2>
        {selectedRole && <button className="btn btn-primary" onClick={saveModules} disabled={saving}>{saving ? 'Saving...' : 'Save Permissions'}</button>}
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: '220px 1fr', gap: 16 }}>
        {/* Role list */}
        <div className="card" style={{ padding: 0 }}>
          <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border)', fontSize: 12, fontWeight: 700, color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: 1 }}>Select Role</div>
          {roles.map(r => (
            <button key={r.id} onClick={() => selectRole(r)}
              style={{ width: '100%', textAlign: 'left', padding: '10px 16px', background: selectedRole?.id === r.id ? 'var(--accent)22' : 'transparent', color: selectedRole?.id === r.id ? 'var(--accent)' : 'var(--text)', border: 'none', cursor: 'pointer', fontSize: 13, display: 'flex', alignItems: 'center', gap: 8, borderLeft: selectedRole?.id === r.id ? '3px solid var(--accent)' : '3px solid transparent' }}>
              <Shield size={13} /> {r.roleName}
            </button>
          ))}
        </div>

        {/* Permission matrix */}
        <div className="card">
          {!selectedRole ? (
            <div style={{ textAlign: 'center', color: 'var(--muted)', padding: 48, fontSize: 14 }}>← Select a role to configure permissions</div>
          ) : (
            <>
              <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
                {['modules', 'menus'].map(t => (
                  <button key={t} className={`btn ${tab === t ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setTab(t)} style={{ fontSize: 12, textTransform: 'capitalize' }}>{t}</button>
                ))}
              </div>
              {tab === 'modules' && (
                <table className="table">
                  <thead>
                    <tr>
                      <th>Module</th>
                      {permCols.map(p => <th key={p} style={{ textAlign: 'center', textTransform: 'capitalize', fontSize: 11 }}>{p.replace('can','')}</th>)}
                    </tr>
                  </thead>
                  <tbody>
                    {modules.map(m => (
                      <tr key={m.moduleKey}>
                        <td><strong style={{ fontSize: 13 }}>{m.moduleName}</strong></td>
                        {permCols.map(p => (
                          <td key={p} style={{ textAlign: 'center' }}>
                            <input type="checkbox" checked={!!m[p]} onChange={() => toggle(m.moduleKey, p)}
                              style={{ width: 15, height: 15, cursor: 'pointer', accentColor: 'var(--accent)' }} />
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
              {tab === 'menus' && (
                <div style={{ color: 'var(--muted)', fontSize: 13, padding: 24, textAlign: 'center' }}>
                  Menu configuration — add menu keys for sidebar visibility control per role.
                  <br /><br />
                  <span style={{ fontSize: 11, background: 'var(--surface)', padding: '4px 10px', borderRadius: 6 }}>
                    Use the API directly: PUT /api/admin/users/roles/{'{roleId}'}/menus/bulk
                  </span>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </>
  )
}

// ── 3. DOCUMENT MOVEMENTS ────────────────────────────────────────────────────

export function DocMovementsPage() {
  const [items, setItems]     = useState([])
  const [search, setSearch]   = useState('')
  const [loading, setLoading] = useState(true)
  const [modal, setModal]     = useState(false)
  const [delItem, setDelItem] = useState(null)
  const [editing, setEditing] = useState(null)
  const [form, setForm]       = useState({ name: '', prefix: '', startFrom: 1 })
  const [previewNum, setPreviewNum] = useState(null)
  const [generating, setGenerating] = useState(null)

  const load = useCallback(async () => {
    setLoading(true)
    try { const r = await docMovApi.getAll(search); setItems(r.data.data) }
    catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [search])

  useEffect(() => { load() }, [load])

  const openCreate = () => { setForm({ name: '', prefix: '', startFrom: 1 }); setEditing(null); setModal(true) }
  const openEdit   = item => { setForm({ name: item.documentMovementName, prefix: item.prefix, startFrom: item.prefixNo }); setEditing(item); setModal(true) }

  const save = async () => {
    try {
      if (editing) await docMovApi.update(editing.id, { name: form.name, resetTo: parseInt(form.startFrom) })
      else         await docMovApi.create({ name: form.name, prefix: form.prefix.toUpperCase(), startFrom: parseInt(form.startFrom) })
      toast.success(editing ? 'Updated' : 'Created')
      setModal(false); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
  }

  const generate = async (item) => {
    setGenerating(item.id)
    try {
      const r = await docMovApi.getNext(item.id)
      setPreviewNum({ number: r.data.data.documentNumber, item })
      load()
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
    finally { setGenerating(null) }
  }

  const del = async () => {
    try { await docMovApi.delete(delItem.id); toast.success('Deleted'); setDelItem(null); load() }
    catch { toast.error('Failed') }
  }

  return (
    <>
      <div className="page-header">
        <h2>Document Movements</h2>
        <button className="btn btn-primary" onClick={openCreate}><Plus size={14} /> Add</button>
      </div>

      <div className="card">
        <div className="toolbar">
          <div className="search-box"><Search size={14} /><input placeholder="Search..." value={search} onChange={e => setSearch(e.target.value)} /></div>
          <button className="btn-icon" onClick={load}><RefreshCw size={14} /></button>
        </div>

        {loading ? <div className="spinner" /> : (
          <table className="table">
            <thead><tr><th>Name</th><th>Prefix</th><th>Current Counter</th><th>Next Number</th><th>Actions</th></tr></thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td><strong>{item.documentMovementName}</strong></td>
                  <td><span style={{ fontFamily: 'monospace', background: 'var(--surface)', padding: '2px 8px', borderRadius: 6, fontSize: 12 }}>{item.prefix}</span></td>
                  <td style={{ color: 'var(--muted)', fontSize: 13 }}>{item.prefixNo - 1} generated</td>
                  <td>
                    <span style={{ fontFamily: 'monospace', color: 'var(--accent)', fontWeight: 700, fontSize: 13 }}>{item.nextNumber}</span>
                  </td>
                  <td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button className="btn btn-ghost" style={{ fontSize: 11, padding: '3px 10px', display: 'flex', alignItems: 'center', gap: 4 }}
                        onClick={() => generate(item)} disabled={generating === item.id}>
                        <Play size={11} /> {generating === item.id ? '...' : 'Generate'}
                      </button>
                      <button className="btn-icon" onClick={() => openEdit(item)}><Pencil size={13} /></button>
                      <button className="btn-icon" onClick={() => setDelItem(item)}><Trash2 size={13} /></button>
                    </div>
                  </td>
                </tr>
              ))}
              {!items.length && <tr><td colSpan={5} style={{ textAlign: 'center', color: 'var(--muted)', padding: 32 }}>No document movements</td></tr>}
            </tbody>
          </table>
        )}
      </div>

      {previewNum && (
        <Modal open title="Number Generated" onClose={() => setPreviewNum(null)}
          footer={<><button className="btn btn-ghost" onClick={() => { navigator.clipboard.writeText(previewNum.number); toast.success('Copied!') }}><Copy size={12} /> Copy</button><button className="btn btn-primary" onClick={() => setPreviewNum(null)}>Done</button></>}>
          <div style={{ textAlign: 'center', padding: '24px 0' }}>
            <div style={{ fontSize: 36, fontWeight: 900, fontFamily: 'monospace', color: 'var(--accent)', letterSpacing: 4 }}>{previewNum.number}</div>
            <div style={{ color: 'var(--muted)', fontSize: 13, marginTop: 8 }}>Generated from <strong>{previewNum.item.documentMovementName}</strong></div>
          </div>
        </Modal>
      )}

      <Modal open={modal} title={editing ? 'Edit' : 'Add Document Movement'} onClose={() => setModal(false)}
        footer={<><button className="btn btn-ghost" onClick={() => setModal(false)}>Cancel</button><button className="btn btn-primary" onClick={save}>Save</button></>}>
        <Field label="Name">
          <input className="input" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} placeholder="e.g. Client Party" />
        </Field>
        {!editing && <Field label="Prefix Code (UPPERCASE)">
          <input className="input" value={form.prefix} onChange={e => setForm(f => ({ ...f, prefix: e.target.value.toUpperCase() }))} placeholder="e.g. CLTPTY" maxLength={10} style={{ fontFamily: 'monospace' }} />
        </Field>}
        <Field label={editing ? 'Reset Counter To (forward only)' : 'Start From'}>
          <input className="input" type="number" min={1} value={form.startFrom} onChange={e => setForm(f => ({ ...f, startFrom: e.target.value }))} />
        </Field>
        {!editing && form.prefix && (
          <div style={{ background: 'var(--surface)', padding: '8px 12px', borderRadius: 8, fontSize: 13, marginTop: 4 }}>
            First number will be: <strong style={{ fontFamily: 'monospace', color: 'var(--accent)' }}>{form.prefix.toUpperCase()}-{String(form.startFrom||1).padStart(4,'0')}</strong>
          </div>
        )}
      </Modal>

      <ConfirmDelete open={!!delItem} name={delItem?.documentMovementName} onConfirm={del} onClose={() => setDelItem(null)} />
    </>
  )
}

// ── 4. NOTIFICATION TEMPLATES ────────────────────────────────────────────────

const TEMPLATE_TABS = [
  { key: 'wa',    label: 'WhatsApp', icon: Smartphone, color: '#25d366' },
  { key: 'sms',   label: 'SMS',      icon: MessageSquare, color: '#0ea5e9' },
  { key: 'email', label: 'Email',    icon: Mail,       color: '#f7c948' },
  { key: 'web',   label: 'Web Push', icon: Globe,      color: '#a78bfa' },
]

export function NotificationTemplatesPage() {
  const [tab, setTab]         = useState('wa')
  const [items, setItems]     = useState([])
  const [search, setSearch]   = useState('')
  const [page, setPage]       = useState(1)
  const [total, setTotal]     = useState(0)
  const [loading, setLoading] = useState(false)
  const [modal, setModal]     = useState(false)
  const [editing, setEditing] = useState(null)
  const [delItem, setDelItem] = useState(null)
  const [previewModal, setPreviewModal] = useState(null)
  const [form, setForm]       = useState({ templateName: '', templateTitle: '', activity: '', subject: '', body: '', variables: '' })

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const r = await templateApi[tab].getAll(page, 20, search)
      setItems(r.data.data.items)
      setTotal(r.data.data.totalRecords)
    } catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [tab, page, search])

  useEffect(() => { setPage(1); setItems([]) }, [tab])
  useEffect(() => { load() }, [load])

  const openCreate = () => { setForm({ templateName: '', templateTitle: '', activity: '', subject: '', body: '', variables: '' }); setEditing(null); setModal(true) }
  const openEdit = item => { setForm({ templateName: item.templateName, templateTitle: item.templateTitle, activity: item.activity||'', subject: item.subject||'', body: item.body, variables: item.variables||'' }); setEditing(item); setModal(true) }

  const save = async () => {
    try {
      if (editing) await templateApi[tab].update(editing.id, form)
      else         await templateApi[tab].create(form)
      toast.success(editing ? 'Updated' : 'Created')
      setModal(false); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
  }

  const toggleStatus = async item => {
    try {
      await templateApi[tab].setStatus(item.id, item.status === 'active' ? 'inactive' : 'active')
      load()
    } catch { toast.error('Failed') }
  }

  const del = async () => {
    try { await templateApi[tab].delete(delItem.id); toast.success('Deleted'); setDelItem(null); load() }
    catch { toast.error('Failed') }
  }

  const openPreview = async item => {
    try {
      const r = await templateApi[tab].getById(item.id)
      setPreviewModal(r.data.data)
    } catch { toast.error('Load failed') }
  }

  const currentTab = TEMPLATE_TABS.find(t => t.key === tab)

  return (
    <>
      <div className="page-header">
        <h2>Notification Templates</h2>
        <button className="btn btn-primary" onClick={openCreate}><Plus size={14} /> New Template</button>
      </div>

      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        {TEMPLATE_TABS.map(t => (
          <button key={t.key} onClick={() => setTab(t.key)}
            className={`btn ${tab === t.key ? 'btn-primary' : 'btn-ghost'}`}
            style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13 }}>
            <t.icon size={13} style={{ color: tab === t.key ? 'inherit' : t.color }} />
            {t.label}
          </button>
        ))}
      </div>

      <div className="card">
        <div className="toolbar">
          <div className="search-box"><Search size={14} /><input placeholder="Search templates..." value={search} onChange={e => setSearch(e.target.value)} /></div>
          <button className="btn-icon" onClick={load}><RefreshCw size={14} /></button>
        </div>

        {loading ? <div className="spinner" /> : (
          <table className="table">
            <thead><tr><th>Name</th><th>Title</th><th>Activity</th><th>Status</th><th>Actions</th></tr></thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td><strong>{item.templateName}</strong></td>
                  <td style={{ color: 'var(--muted)', fontSize: 13 }}>{item.templateTitle}</td>
                  <td style={{ fontSize: 12 }}>{item.activity || '—'}</td>
                  <td><StatusBadge status={item.status} /></td>
                  <td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button className="btn-icon" title="Preview" onClick={() => openPreview(item)}><Eye size={13} /></button>
                      <button className="btn-icon" onClick={() => openEdit(item)}><Pencil size={13} /></button>
                      <button className="btn-icon" onClick={() => toggleStatus(item)}>
                        {item.status === 'active' ? <ToggleRight size={13} style={{ color: 'var(--green)' }} /> : <ToggleLeft size={13} />}
                      </button>
                      <button className="btn-icon" onClick={() => setDelItem(item)}><Trash2 size={13} /></button>
                    </div>
                  </td>
                </tr>
              ))}
              {!items.length && <tr><td colSpan={5} style={{ textAlign: 'center', color: 'var(--muted)', padding: 32 }}>No templates</td></tr>}
            </tbody>
          </table>
        )}
        <Pagination page={page} total={total} pageSize={20} onChange={setPage} />
      </div>

      {/* Create/Edit Modal */}
      <Modal open={modal} title={editing ? `Edit ${currentTab?.label} Template` : `New ${currentTab?.label} Template`} onClose={() => setModal(false)} wide
        footer={<><button className="btn btn-ghost" onClick={() => setModal(false)}>Cancel</button><button className="btn btn-primary" onClick={save}>Save</button></>}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="Template Name">
            <input className="input" value={form.templateName} onChange={e => setForm(f => ({ ...f, templateName: e.target.value }))} placeholder="welcome-msg" />
          </Field>
          <Field label="Title / Label">
            <input className="input" value={form.templateTitle} onChange={e => setForm(f => ({ ...f, templateTitle: e.target.value }))} placeholder="Welcome Message" />
          </Field>
        </div>
        <Field label="Activity (optional)">
          <input className="input" value={form.activity} onChange={e => setForm(f => ({ ...f, activity: e.target.value }))} placeholder="e.g. user-registration, password-reset" />
        </Field>
        {tab === 'email' && <Field label="Subject">
          <input className="input" value={form.subject} onChange={e => setForm(f => ({ ...f, subject: e.target.value }))} placeholder="Welcome to 10X Convo, {{name}}!" />
        </Field>}
        <Field label="Body">
          <textarea className="input" rows={6} value={form.body} onChange={e => setForm(f => ({ ...f, body: e.target.value }))}
            placeholder="Hi {{name}}, welcome to 10X Convo! Your account is ready." style={{ resize: 'vertical', fontFamily: 'monospace', fontSize: 13 }} />
        </Field>
        {(tab === 'wa') && <Field label="Variables (comma separated)">
          <input className="input" value={form.variables} onChange={e => setForm(f => ({ ...f, variables: e.target.value }))} placeholder="name, code, date" />
        </Field>}
        <div style={{ background: 'var(--surface)', padding: '8px 12px', borderRadius: 8, fontSize: 12, color: 'var(--muted)' }}>
          💡 Use <code style={{ background: 'var(--border)', padding: '1px 4px', borderRadius: 4 }}>{'{{variable}}'}</code> for dynamic values e.g. <code>{'{{name}}'}</code>, <code>{'{{code}}'}</code>
        </div>
      </Modal>

      {/* Preview Modal */}
      <Modal open={!!previewModal} title="Template Preview" onClose={() => setPreviewModal(null)}>
        {previewModal && (
          <div>
            <div style={{ marginBottom: 12 }}>
              <span style={{ fontSize: 12, color: 'var(--muted)' }}>Template Name: </span>
              <strong>{previewModal.templateName}</strong>
            </div>
            {previewModal.subject && <div style={{ marginBottom: 12 }}>
              <span style={{ fontSize: 12, color: 'var(--muted)' }}>Subject: </span>
              <strong>{previewModal.subject}</strong>
            </div>}
            <div style={{ background: 'var(--surface)', padding: 16, borderRadius: 8, fontFamily: 'monospace', fontSize: 13, whiteSpace: 'pre-wrap', lineHeight: 1.6, maxHeight: 300, overflowY: 'auto' }}>
              {previewModal.body}
            </div>
            {previewModal.variables && <div style={{ marginTop: 12, fontSize: 12, color: 'var(--muted)' }}>Variables: <strong>{previewModal.variables}</strong></div>}
          </div>
        )}
      </Modal>

      <ConfirmDelete open={!!delItem} name={delItem?.templateName} onConfirm={del} onClose={() => setDelItem(null)} />
    </>
  )
}

// ── 5. SEND / HISTORY (WA, SMS, Email, WebPush, App) ─────────────────────────

const NOTIF_TABS = [
  { key: 'wa',     label: 'WhatsApp',   icon: Smartphone,    color: '#25d366' },
  { key: 'sms',    label: 'SMS',        icon: MessageSquare, color: '#0ea5e9' },
  { key: 'email',  label: 'Email',      icon: Mail,          color: '#f7c948' },
  { key: 'webpush',label: 'Web Push',   icon: Bell,          color: '#a78bfa' },
  { key: 'app',    label: 'App Notif',  icon: Radio,         color: '#fb923c' },
]

export function NotificationsPage() {
  const [tab, setTab]       = useState('wa')
  const [items, setItems]   = useState([])
  const [page, setPage]     = useState(1)
  const [total, setTotal]   = useState(0)
  const [loading, setLoading] = useState(false)
  const [sendModal, setSendModal] = useState(false)
  const [sending, setSending]   = useState(false)
  const [form, setForm]     = useState({ to: '', subject: '', body: '', title: '', sendToAll: true, targetRoleName: '' })

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const r = await notifyApi[tab].getAll(page, 20, '')
      setItems(r.data.data.items)
      setTotal(r.data.data.totalRecords)
    } catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [tab, page])

  useEffect(() => { setPage(1); setItems([]) }, [tab])
  useEffect(() => { load() }, [load])

  const send = async () => {
    setSending(true)
    try {
      switch (tab) {
        case 'wa':      await notifyApi.wa.send({ phoneNumber: form.to, message: form.body }); break
        case 'sms':     await notifyApi.sms.send({ phoneNumber: form.to, message: form.body }); break
        case 'email':   await notifyApi.email.send({ toEmail: form.to, subject: form.subject, htmlBody: form.body }); break
        case 'webpush': await notifyApi.webpush.broadcast({ title: form.title, body: form.body, url: '/' }); break
        case 'app':     await notifyApi.app.create({ title: form.title, message: form.body, type: 'General', sendToAll: form.sendToAll, targetRoleName: form.targetRoleName || undefined }); break
      }
      toast.success('Sent!')
      setSendModal(false); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Send failed') }
    finally { setSending(false) }
  }

  const currentTab = NOTIF_TABS.find(t => t.key === tab)

  return (
    <>
      <div className="page-header">
        <h2>Send &amp; History</h2>
        <button className="btn btn-primary" onClick={() => { setForm({ to: '', subject: '', body: '', title: '', sendToAll: true, targetRoleName: '' }); setSendModal(true) }}>
          <Send size={14} /> Send New
        </button>
      </div>

      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        {NOTIF_TABS.map(t => (
          <button key={t.key} onClick={() => setTab(t.key)} className={`btn ${tab === t.key ? 'btn-primary' : 'btn-ghost'}`}
            style={{ fontSize: 12, display: 'flex', alignItems: 'center', gap: 5 }}>
            <t.icon size={12} style={{ color: tab === t.key ? 'inherit' : t.color }} /> {t.label}
          </button>
        ))}
      </div>

      <div className="card">
        {loading ? <div className="spinner" /> : (
          <table className="table">
            <thead>
              <tr>
                <th>To / Target</th>
                <th>{tab === 'app' ? 'Title' : tab === 'email' ? 'Subject' : 'Message'}</th>
                <th>Status</th>
                <th>Date</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td style={{ fontSize: 13 }}>{item.toNumber || item.toEmail || item.sender || item.loginId || '—'}</td>
                  <td style={{ maxWidth: 280 }}>
                    <span style={{ fontSize: 13, display: 'block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {item.subject || item.message || item.body || item.title || '—'}
                    </span>
                  </td>
                  <td><StatusBadge status={item.status || (item.isActive !== undefined ? (item.isActive ? 'active' : 'inactive') : 'sent')} /></td>
                  <td style={{ fontSize: 12, color: 'var(--muted)' }}>{new Date(item.createdOn || item.sentAt).toLocaleString()}</td>
                </tr>
              ))}
              {!items.length && <tr><td colSpan={4} style={{ textAlign: 'center', color: 'var(--muted)', padding: 32 }}>No history</td></tr>}
            </tbody>
          </table>
        )}
        <Pagination page={page} total={total} pageSize={20} onChange={setPage} />
      </div>

      {/* Send Modal */}
      <Modal open={sendModal} title={`Send ${currentTab?.label}`} onClose={() => setSendModal(false)}
        footer={<><button className="btn btn-ghost" onClick={() => setSendModal(false)}>Cancel</button><button className="btn btn-primary" onClick={send} disabled={sending}>{sending ? 'Sending...' : 'Send'}</button></>}>

        {(tab === 'wa' || tab === 'sms') && (
          <Field label="Phone Number (+923001234567)">
            <input className="input" value={form.to} onChange={e => setForm(f => ({ ...f, to: e.target.value }))} placeholder="+923001234567" />
          </Field>
        )}
        {tab === 'email' && <>
          <Field label="To Email">
            <input className="input" value={form.to} onChange={e => setForm(f => ({ ...f, to: e.target.value }))} placeholder="user@email.com" />
          </Field>
          <Field label="Subject">
            <input className="input" value={form.subject} onChange={e => setForm(f => ({ ...f, subject: e.target.value }))} placeholder="Email subject..." />
          </Field>
        </>}
        {(tab === 'webpush' || tab === 'app') && (
          <Field label="Title">
            <input className="input" value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} placeholder="Notification title" />
          </Field>
        )}
        {tab === 'app' && <>
          <label style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 12, cursor: 'pointer' }}>
            <input type="checkbox" checked={form.sendToAll} onChange={e => setForm(f => ({ ...f, sendToAll: e.target.checked }))} />
            <span style={{ fontSize: 13 }}>Send to all users</span>
          </label>
          {!form.sendToAll && <Field label="Target Role Name">
            <input className="input" value={form.targetRoleName} onChange={e => setForm(f => ({ ...f, targetRoleName: e.target.value }))} placeholder="e.g. Consultant Role" />
          </Field>}
        </>}
        <Field label={tab === 'email' ? 'HTML Body' : 'Message'}>
          <textarea className="input" rows={5} value={form.body} onChange={e => setForm(f => ({ ...f, body: e.target.value }))}
            placeholder="Message content..." style={{ resize: 'vertical' }} />
        </Field>
      </Modal>
    </>
  )
}

// ── 6. REVIEWS ADMIN ─────────────────────────────────────────────────────────

function StarRating({ rating }) {
  return (
    <div style={{ display: 'flex', gap: 2 }}>
      {[1,2,3,4,5].map(s => (
        <Star key={s} size={13} fill={s <= rating ? '#f7c948' : 'transparent'} style={{ color: s <= rating ? '#f7c948' : 'var(--border)' }} />
      ))}
    </div>
  )
}

export function ReviewsAdminPage() {
  const [items, setItems]     = useState([])
  const [page, setPage]       = useState(1)
  const [total, setTotal]     = useState(0)
  const [loading, setLoading] = useState(true)
  const [delItem, setDelItem] = useState(null)
  const [minRating, setMinRating] = useState('')
  const [maxRating, setMaxRating] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const r = await reviewAdminApi.getAll(page, 20, minRating || undefined, maxRating || undefined)
      setItems(r.data.data.items)
      setTotal(r.data.data.totalRecords)
    } catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [page, minRating, maxRating])

  useEffect(() => { load() }, [load])

  const del = async () => {
    try { await reviewAdminApi.delete(delItem.id); toast.success('Removed'); setDelItem(null); load() }
    catch { toast.error('Failed') }
  }

  return (
    <>
      <div className="page-header">
        <h2>Reviews</h2>
        <span style={{ color: 'var(--muted)', fontSize: 13 }}>{total} total reviews</span>
      </div>

      <div className="card">
        <div className="toolbar" style={{ flexWrap: 'wrap', gap: 8 }}>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <span style={{ fontSize: 12, color: 'var(--muted)' }}>Min ⭐</span>
            <select className="input" style={{ width: 80 }} value={minRating} onChange={e => setMinRating(e.target.value)}>
              <option value="">Any</option>
              {[1,2,3,4,5].map(n => <option key={n} value={n}>{n}</option>)}
            </select>
            <span style={{ fontSize: 12, color: 'var(--muted)' }}>Max ⭐</span>
            <select className="input" style={{ width: 80 }} value={maxRating} onChange={e => setMaxRating(e.target.value)}>
              <option value="">Any</option>
              {[1,2,3,4,5].map(n => <option key={n} value={n}>{n}</option>)}
            </select>
          </div>
          <button className="btn-icon" onClick={load}><RefreshCw size={14} /></button>
        </div>

        {loading ? <div className="spinner" /> : (
          <table className="table">
            <thead><tr><th>Consultant</th><th>Reviewer</th><th>Rating</th><th>Comment</th><th>Date</th><th>Actions</th></tr></thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td><strong style={{ fontSize: 13 }}>{item.consultantName}</strong></td>
                  <td style={{ fontSize: 13, color: 'var(--muted)' }}>{item.reviewerName}</td>
                  <td><StarRating rating={item.rating} /></td>
                  <td style={{ maxWidth: 240 }}>
                    <span style={{ fontSize: 12, color: 'var(--muted)', display: 'block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {item.comment || '—'}
                    </span>
                  </td>
                  <td style={{ fontSize: 12, color: 'var(--muted)' }}>{new Date(item.createdAt).toLocaleDateString()}</td>
                  <td><button className="btn-icon" onClick={() => setDelItem(item)}><Trash2 size={13} /></button></td>
                </tr>
              ))}
              {!items.length && <tr><td colSpan={6} style={{ textAlign: 'center', color: 'var(--muted)', padding: 32 }}>No reviews</td></tr>}
            </tbody>
          </table>
        )}
        <Pagination page={page} total={total} pageSize={20} onChange={setPage} />
      </div>

      <ConfirmDelete open={!!delItem} name={`review by ${delItem?.reviewerName}`} onConfirm={del} onClose={() => setDelItem(null)} />
    </>
  )
}

// ── 7. EXTENDED DATA CONSTANTS (Districts, Tehsils, Areas, DocTypes etc.) ────

const DATA_TABS = [
  { key: 'documentTypes',    label: 'Doc Types',       fields: [{ key: 'documentTypeName', label: 'Name' }, { key: 'documentTypePrefix', label: 'Prefix' }] },
  { key: 'criteriaTypes',    label: 'Criteria Types',  fields: [{ key: 'criteriaTypeName', label: 'Name' }, { key: 'criteriaTypePrefix', label: 'Prefix' }] },
  { key: 'clientCategories', label: 'Client Categories', fields: [{ key: 'clientCategoryName', label: 'Name' }] },
]

export function DataExtPage() {
  const [tab, setTab]         = useState('documentTypes')
  const [items, setItems]     = useState([])
  const [search, setSearch]   = useState('')
  const [loading, setLoading] = useState(false)
  const [modal, setModal]     = useState(false)
  const [editing, setEditing] = useState(null)
  const [form, setForm]       = useState({})
  const [delItem, setDelItem] = useState(null)

  const tabCfg = DATA_TABS.find(t => t.key === tab)

  const load = useCallback(async () => {
    setLoading(true)
    try { const r = await dataExtApi[tab].getAll(search); setItems(r.data.data) }
    catch { toast.error('Load failed') }
    finally { setLoading(false) }
  }, [tab, search])

  useEffect(() => { setItems([]); setSearch('') }, [tab])
  useEffect(() => { load() }, [load])

  const openCreate = () => { const f = {}; tabCfg.fields.forEach(fi => f[fi.key] = ''); setForm(f); setEditing(null); setModal(true) }
  const openEdit   = item => { const f = {}; tabCfg.fields.forEach(fi => f[fi.key] = item[fi.key] || ''); setForm(f); setEditing(item); setModal(true) }

  const save = async () => {
    try {
      if (editing) await dataExtApi[tab].update(editing.id, form)
      else         await dataExtApi[tab].create(form)
      toast.success(editing ? 'Updated' : 'Created')
      setModal(false); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
  }

  const del = async () => {
    try { await dataExtApi[tab].delete(delItem.id); toast.success('Deleted'); setDelItem(null); load() }
    catch { toast.error('Failed') }
  }

  const nameKey = tabCfg?.fields[0]?.key

  return (
    <>
      <div className="page-header">
        <h2>Data Constants (Extended)</h2>
        <button className="btn btn-primary" onClick={openCreate}><Plus size={14} /> Add</button>
      </div>

      <div style={{ display: 'flex', gap: 8, marginBottom: 16, flexWrap: 'wrap' }}>
        {DATA_TABS.map(t => (
          <button key={t.key} onClick={() => setTab(t.key)}
            className={`btn ${tab === t.key ? 'btn-primary' : 'btn-ghost'}`} style={{ fontSize: 12 }}>
            {t.label}
          </button>
        ))}
      </div>

      <div className="card">
        <div className="toolbar">
          <div className="search-box"><Search size={14} /><input placeholder={`Search ${tabCfg?.label}...`} value={search} onChange={e => setSearch(e.target.value)} /></div>
          <button className="btn-icon" onClick={load}><RefreshCw size={14} /></button>
        </div>

        {loading ? <div className="spinner" /> : (
          <table className="table">
            <thead><tr>{tabCfg?.fields.map(f => <th key={f.key}>{f.label}</th>)}<th>Actions</th></tr></thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  {tabCfg?.fields.map(f => <td key={f.key} style={{ fontSize: 13 }}>{item[f.key] || '—'}</td>)}
                  <td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button className="btn-icon" onClick={() => openEdit(item)}><Pencil size={13} /></button>
                      <button className="btn-icon" onClick={() => setDelItem(item)}><Trash2 size={13} /></button>
                    </div>
                  </td>
                </tr>
              ))}
              {!items.length && <tr><td colSpan={(tabCfg?.fields.length||1)+1} style={{ textAlign: 'center', color: 'var(--muted)', padding: 32 }}>No data</td></tr>}
            </tbody>
          </table>
        )}
      </div>

      <Modal open={modal} title={editing ? 'Edit' : 'Add'} onClose={() => setModal(false)}
        footer={<><button className="btn btn-ghost" onClick={() => setModal(false)}>Cancel</button><button className="btn btn-primary" onClick={save}>Save</button></>}>
        {tabCfg?.fields.map(fi => (
          <Field key={fi.key} label={fi.label}>
            <input className="input" value={form[fi.key] || ''} onChange={e => setForm(f => ({ ...f, [fi.key]: e.target.value }))} />
          </Field>
        ))}
      </Modal>

      <ConfirmDelete open={!!delItem} name={delItem?.[nameKey] || 'item'} onConfirm={del} onClose={() => setDelItem(null)} />
    </>
  )
}
