// ═══════════════════════════════════════════════════════════════════════════
//  ALL ADMIN PAGES — DashboardPage, UsersPage, RolesPage, LocationsPage,
//                    SettingsPage, ErrorLogsPage, DataConstantsPage
// ═══════════════════════════════════════════════════════════════════════════
import React, { useState, useEffect, useCallback } from 'react'
import { toast } from 'react-hot-toast'
import { Search, Plus, Pencil, Trash2, X, RefreshCw, Eye, Users, Shield, MapPin, AlertTriangle, Settings, Gift } from 'lucide-react'
import { userApi, roleApi, locationApi, settingsApi, errorLogApi, dataApi, consultantConfigApi, creditsApi } from '../api'

// ── REUSABLE COMPONENTS ───────────────────────────────────────────────────────

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

function Pagination({ page, total, pageSize, onChange }) {
  const pages = Math.ceil(total / pageSize)
  if (pages <= 1) return null
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 16 }}>
      <span style={{ color: 'var(--muted)', fontSize: 12 }}>{total} total records</span>
      <div className="pagination">
        {Array.from({ length: Math.min(pages, 7) }, (_, i) => i + 1).map(p => (
          <button key={p} className={`page-btn ${p === page ? 'active' : ''}`} onClick={() => onChange(p)}>{p}</button>
        ))}
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

// ── DASHBOARD ─────────────────────────────────────────────────────────────────
export function DashboardPage() {
  const [stats, setStats] = useState({ users: 0, roles: 0, locations: 0, errors: 0 })

  useEffect(() => {
    Promise.all([
      userApi.getAll(1, 1, ''),
      roleApi.getAll(1, 1, ''),
      locationApi.getAll(1, 1, ''),
      errorLogApi.getAll(1, 1, ''),
    ]).then(([u, r, l, e]) => setStats({
      users:     u.data.data.totalRecords,
      roles:     r.data.data.totalRecords,
      locations: l.data.data.totalRecords,
      errors:    e.data.data.totalRecords,
    })).catch(() => {})
  }, [])

  const cards = [
    { label: 'Total Users',     val: stats.users,     icon: Users,        color: 'var(--accent)' },
    { label: 'Roles',           val: stats.roles,     icon: Shield,       color: 'var(--green)' },
    { label: 'Locations',       val: stats.locations, icon: MapPin,       color: '#f7c948' },
    { label: 'Error Logs',      val: stats.errors,    icon: AlertTriangle,color: 'var(--accent2)' },
  ]

  return (
    <>
      <div className="page-header">
        <h2>Dashboard</h2>
        <span style={{ color: 'var(--muted)', fontSize: 13 }}>10X Convo Admin Portal</span>
      </div>

      <div className="stats-grid">
        {cards.map(c => (
          <div key={c.label} className="stat-card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 }}>
              <span className="stat-label">{c.label}</span>
              <div style={{ width: 32, height: 32, borderRadius: 8, background: c.color + '22', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <c.icon size={15} style={{ color: c.color }} />
              </div>
            </div>
            <div className="stat-val" style={{ color: c.color }}>{c.val}</div>
          </div>
        ))}
      </div>

      <div className="card">
        <h3 style={{ fontFamily: 'var(--font-head)', marginBottom: 8 }}>Quick Actions</h3>
        <p style={{ color: 'var(--muted)', fontSize: 13 }}>Navigate using the sidebar to manage users, roles, settings, and data constants.</p>
      </div>
    </>
  )
}

// ── USERS ─────────────────────────────────────────────────────────────────────
export function UsersPage() {
  const [users, setUsers]       = useState([])
  const [total, setTotal]       = useState(0)
  const [page, setPage]         = useState(1)
  const [search, setSearch]     = useState('')
  const [loading, setLoading]   = useState(false)
  const [modal, setModal]       = useState(null) // null | 'create' | 'edit' | 'delete' | 'config'
  const [selected, setSelected] = useState(null)
  const [form, setForm]         = useState({ userName: '', loginId: '', email: '', password: '', cellNo: '', isActive: true })
  const [roles, setRoles]       = useState([])

  // ── Consultant Config State ──
  const [configTab, setConfigTab]   = useState('services')
  const [configData, setConfigData] = useState(null)
  const [configSaving, setConfigSaving] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await userApi.getAll(page, 10, search)
      setUsers(data.data.items); setTotal(data.data.totalRecords)
    } catch { toast.error('Failed to load users') }
    finally { setLoading(false) }
  }, [page, search])

  useEffect(() => { load() }, [load])
  useEffect(() => { roleApi.getAll(1, 100, '').then(r => setRoles(r.data.data.items || [])).catch(() => {}) }, [])

  const openCreate = () => { setForm({ userName: '', loginId: '', email: '', password: '', cellNo: '', roleId: '', isActive: true }); setModal('create') }
  const openEdit   = u => { setSelected(u); setForm({ userName: u.userName, email: u.email, cellNo: u.cellNo || '', roleId: u.roleId || '', isActive: u.isActive }); setModal('edit') }
  const openDelete = u => { setSelected(u); setModal('delete') }

  // ── Open Consultant Config ⚙️ ──
  const openConfig = async (u) => {
    setSelected(u)
    setConfigTab('services')
    setConfigData(null)
    setModal('config')
    try {
      const { data } = await consultantConfigApi.get(u.id)
      setConfigData(data.data)
    } catch { toast.error('Failed to load config') }
  }

  const saveConfig = async () => {
    if (!configData) return
    setConfigSaving(true)
    try {
      await consultantConfigApi.save(selected.id, configData)
      toast.success('Consultant config saved!')
    } catch (e) { toast.error(e.response?.data?.message || 'Save failed') }
    finally { setConfigSaving(false) }
  }

  const isConsultantRole = (roleName) => {
    const r = (roleName || '').toLowerCase()
    return r.includes('consultant')
  }

  const isClientRole = (roleName) => {
    const r = (roleName || '').toLowerCase()
    return r.includes('client') || r.includes('web')
  }

  // ── Grant Credits State ──
  const [grantData, setGrantData] = useState({ textChars: 0, audioMins: 0, videoMins: 0, imageCredits: 0, fileCredits: 0 })
  const [grantSaving, setGrantSaving] = useState(false)

  const openGrant = (u) => {
    setSelected(u)
    setGrantData({ textChars: 0, audioMins: 0, videoMins: 0, imageCredits: 0, fileCredits: 0 })
    setModal('grant')
  }

  const saveGrant = async () => {
    if (grantData.textChars <= 0 && grantData.audioMins <= 0 && grantData.videoMins <= 0 && grantData.imageCredits <= 0 && grantData.fileCredits <= 0) {
      toast.error('Enter at least one credit amount'); return
    }
    setGrantSaving(true)
    try {
      await creditsApi.grantCredits({ userId: selected.id, ...grantData })
      toast.success(`Credits granted to ${selected.userName}!`)
      setModal(null)
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
    finally { setGrantSaving(false) }
  }

  const handleCreate = async () => {
    try { await userApi.create(form); toast.success('User created'); setModal(null); load() }
    catch (e) { toast.error(e.response?.data?.message || 'Error') }
  }
  const handleUpdate = async () => {
    try { await userApi.update(selected.id, form); toast.success('Updated'); setModal(null); load() }
    catch (e) { toast.error(e.response?.data?.message || 'Error') }
  }
  const handleDelete = async () => {
    try { await userApi.delete(selected.id); toast.success('Deleted'); setModal(null); load() }
    catch { toast.error('Delete failed') }
  }

  const F = ({ name, label, type = 'text', ...p }) => (
    <div className="form-group">
      <label>{label}</label>
      <input type={type} value={form[name] || ''} onChange={e => setForm(f => ({ ...f, [name]: e.target.value }))} {...p} />
    </div>
  )

  // ── Config field helper ──
  const Cf = (field, label, type = 'checkbox') => {
    if (type === 'checkbox') return (
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 0' }}>
        <label className="toggle" style={{ flexShrink: 0 }}>
          <input type="checkbox" checked={configData?.[field] ?? false} onChange={e => setConfigData(d => ({ ...d, [field]: e.target.checked }))} />
          <span className="toggle-slider" />
        </label>
        <span style={{ fontSize: 13 }}>{label}</span>
      </div>
    )
    if (type === 'number') return (
      <div className="form-group" style={{ marginBottom: 10 }}>
        <label style={{ fontSize: 12 }}>{label}</label>
        <input type="number" step="0.01" value={configData?.[field] ?? ''} onChange={e => setConfigData(d => ({ ...d, [field]: e.target.value ? Number(e.target.value) : null }))} placeholder="Use global default" />
      </div>
    )
    return (
      <div className="form-group" style={{ marginBottom: 10 }}>
        <label style={{ fontSize: 12 }}>{label}</label>
        <input value={configData?.[field] ?? ''} onChange={e => setConfigData(d => ({ ...d, [field]: e.target.value || null }))} placeholder={label} />
      </div>
    )
  }

  return (
    <>
      <div className="page-header">
        <h2>Users</h2>
        <button className="btn btn-primary" onClick={openCreate}><Plus size={14} /> Add User</button>
      </div>

      <div className="card">
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          <div className="search-bar" style={{ flex: 1 }}>
            <Search /><input placeholder="Search by name, email, login ID…" value={search}
              onChange={e => { setSearch(e.target.value); setPage(1) }} />
          </div>
          <button className="btn-icon" onClick={load}><RefreshCw size={14} /></button>
        </div>

        <div className="table-wrap">
          <table>
            <thead><tr><th>Name</th><th>Email</th><th>Login ID</th><th>Role</th><th>Status</th><th>Actions</th></tr></thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={6} style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}>Loading…</td></tr>
              ) : users.length === 0 ? (
                <tr><td colSpan={6}><div className="empty"><Users /><p>No users found</p></div></td></tr>
              ) : users.map(u => (
                <tr key={u.id}>
                  <td><div style={{ fontWeight: 500 }}>{u.userName}</div></td>
                  <td className="td-muted">{u.email}</td>
                  <td className="td-muted">{u.loginId}</td>
                  <td><span className="badge badge-purple">{u.roleName || '—'}</span></td>
                  <td><span className={`dot ${u.isActive ? 'dot-green' : 'dot-red'}`} /></td>
                  <td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      {isConsultantRole(u.roleName) && (
                        <button className="btn-icon" title="Consultant Settings" onClick={() => openConfig(u)} style={{ color: 'var(--accent)' }}><Settings size={14} /></button>
                      )}
                      {isClientRole(u.roleName) && (
                        <button className="btn-icon" title="Grant Free Credits" onClick={() => openGrant(u)} style={{ color: 'var(--green, #22c55e)' }}><Gift size={14} /></button>
                      )}
                      <button className="btn-icon" onClick={() => openEdit(u)}><Pencil /></button>
                      <button className="btn-icon" style={{ color: 'var(--accent2)' }} onClick={() => openDelete(u)}><Trash2 /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination page={page} total={total} pageSize={10} onChange={setPage} />
      </div>

      {/* Create/Edit Modal */}
      <Modal open={modal === 'create' || modal === 'edit'} title={modal === 'create' ? 'Add User' : 'Edit User'}
        onClose={() => setModal(null)}
        footer={<><button className="btn btn-ghost" onClick={() => setModal(null)}>Cancel</button>
          <button className="btn btn-primary" onClick={modal === 'create' ? handleCreate : handleUpdate}>Save</button></>}>
        <div className="grid-2">
          <F name="userName" label="Full Name" placeholder="John Doe" />
          {modal === 'create' && <F name="loginId" label="Login ID" placeholder="john@company.com" />}
        </div>
        <F name="email" label="Email" type="email" placeholder="john@company.com" />
        {modal === 'create' && <F name="password" label="Password" type="password" />}
        <div className="grid-2">
          <F name="cellNo" label="Cell No" placeholder="+92300…" />
          <div className="form-group">
            <label>Role</label>
            <select value={form.roleId || ''} onChange={e => setForm(f => ({ ...f, roleId: e.target.value }))}>
              <option value="">No role</option>
              {roles.map(r => <option key={r.id} value={r.id}>{r.roleName}</option>)}
            </select>
          </div>
        </div>
        {modal === 'edit' && (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <label className="toggle">
              <input type="checkbox" checked={form.isActive} onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))} />
              <span className="toggle-slider" />
            </label>
            <span style={{ fontSize: 13 }}>{form.isActive ? 'Active' : 'Inactive'}</span>
          </div>
        )}
      </Modal>

      {/* ── CONSULTANT CONFIG MODAL ⚙️ ── */}
      <Modal open={modal === 'config'} title={`⚙️ ${selected?.userName || ''} — Consultant Settings`}
        onClose={() => setModal(null)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setModal(null)}>Cancel</button>
          <button className="btn btn-primary" onClick={saveConfig} disabled={configSaving}>
            {configSaving ? 'Saving…' : 'Save Settings'}
          </button>
        </>}>

        {!configData ? (
          <div style={{ textAlign: 'center', padding: 30, color: 'var(--muted)' }}>Loading config…</div>
        ) : (
          <>
            {/* Tab Bar */}
            <div className="tab-bar" style={{ marginBottom: 16 }}>
              {[['services', '📱 Services'], ['pricing', '💰 Pricing'], ['gateways', '🏦 Payment Gateways']].map(([key, label]) => (
                <button key={key} className={`tab ${configTab === key ? 'active' : ''}`} onClick={() => setConfigTab(key)}>
                  {label}
                </button>
              ))}
            </div>

            {/* ── Tab 1: Services ── */}
            {configTab === 'services' && (
              <div>
                <p style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 12 }}>Enable or disable message types this consultant can receive from clients. No limits — client can send as much as their wallet balance allows.</p>
                {Cf('textEnabled', 'Text Messages')}
                {Cf('audioEnabled', 'Audio Messages')}
                {Cf('videoEnabled', 'Video Messages')}
                {Cf('imageEnabled', 'Image / Photo')}
                {Cf('fileEnabled', 'File Attachments')}
              </div>
            )}

            {/* ── Tab 2: Pricing ── */}
            {configTab === 'pricing' && (
              <div>
                <p style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 12 }}>Override global rates for this consultant. Leave empty to use the default global rate.</p>

                <div className="form-group" style={{ marginBottom: 14, maxWidth: 180 }}>
                  <label style={{ fontSize: 12 }}>Currency</label>
                  <select value={configData.currency || 'USD'} onChange={e => setConfigData(d => ({ ...d, currency: e.target.value }))}>
                    {['USD', 'PKR', 'EUR', 'GBP', 'AED', 'SAR', 'INR'].map(c => <option key={c}>{c}</option>)}
                  </select>
                </div>

                <div className="grid-2" style={{ gap: 10 }}>
                  {Cf('textRate', 'Text Rate (per 250 chars)', 'number')}
                  {Cf('audioRate', 'Audio Rate (per minute)', 'number')}
                  {Cf('videoRate', 'Video Rate (per minute)', 'number')}
                  {Cf('imageRate', 'Image Rate (flat)', 'number')}
                </div>
                {Cf('fileRate', 'File Rate (flat)', 'number')}
              </div>
            )}

            {/* ── Tab 3: Payment Gateways ── */}
            {configTab === 'gateways' && (
              <div>
                <p style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 12 }}>Enable payment methods clients can use when paying this consultant.</p>

                {Cf('stripeEnabled', '💳 Stripe (Card — Visa/MasterCard)')}
                {Cf('jazzCashEnabled', '📱 JazzCash (Mobile Wallet / OTC)')}
                {Cf('easyPaisaEnabled', '📱 EasyPaisa (Mobile Account / OTC)')}

                <div style={{ marginTop: 16, padding: '12px 0', borderTop: '1px solid var(--border)' }}>
                  <p style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 10 }}>Payout Accounts (for consultant earnings)</p>
                  <div className="grid-2" style={{ gap: 10 }}>
                    {Cf('stripeAccountId', 'Stripe Connect ID', 'text')}
                    {Cf('jazzCashAccount', 'JazzCash Number', 'text')}
                    {Cf('easyPaisaAccount', 'EasyPaisa Number', 'text')}
                    {Cf('bankName', 'Bank Name', 'text')}
                  </div>
                  {Cf('bankAccountNo', 'Bank Account Number', 'text')}
                </div>
              </div>
            )}
          </>
        )}
      </Modal>

      {/* ── GRANT FREE CREDITS MODAL 🎁 ── */}
      <Modal open={modal === 'grant'} title={`🎁 Grant Credits — ${selected?.userName || ''}`}
        onClose={() => setModal(null)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setModal(null)}>Cancel</button>
          <button className="btn btn-primary" onClick={saveGrant} disabled={grantSaving}>
            {grantSaving ? 'Granting…' : 'Grant Credits'}
          </button>
        </>}>
        <p style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 16 }}>
          Grant free credits to <strong style={{ color: 'var(--text)' }}>{selected?.userName}</strong>. These are added to their existing balance.
        </p>
        <div className="grid-2" style={{ gap: 10 }}>
          <div className="form-group" style={{ marginBottom: 8 }}>
            <label style={{ fontSize: 12 }}>📝 Text (characters)</label>
            <input type="number" min="0" value={grantData.textChars || ''} onChange={e => setGrantData(d => ({ ...d, textChars: Number(e.target.value) || 0 }))} placeholder="e.g. 5000" />
          </div>
          <div className="form-group" style={{ marginBottom: 8 }}>
            <label style={{ fontSize: 12 }}>🎙️ Audio (minutes)</label>
            <input type="number" min="0" step="0.5" value={grantData.audioMins || ''} onChange={e => setGrantData(d => ({ ...d, audioMins: Number(e.target.value) || 0 }))} placeholder="e.g. 10" />
          </div>
          <div className="form-group" style={{ marginBottom: 8 }}>
            <label style={{ fontSize: 12 }}>🎬 Video (minutes)</label>
            <input type="number" min="0" step="0.5" value={grantData.videoMins || ''} onChange={e => setGrantData(d => ({ ...d, videoMins: Number(e.target.value) || 0 }))} placeholder="e.g. 5" />
          </div>
          <div className="form-group" style={{ marginBottom: 8 }}>
            <label style={{ fontSize: 12 }}>🖼️ Images (count)</label>
            <input type="number" min="0" value={grantData.imageCredits || ''} onChange={e => setGrantData(d => ({ ...d, imageCredits: Number(e.target.value) || 0 }))} placeholder="e.g. 20" />
          </div>
        </div>
        <div className="form-group" style={{ marginBottom: 0, maxWidth: '48%' }}>
          <label style={{ fontSize: 12 }}>📎 Files (count)</label>
          <input type="number" min="0" value={grantData.fileCredits || ''} onChange={e => setGrantData(d => ({ ...d, fileCredits: Number(e.target.value) || 0 }))} placeholder="e.g. 10" />
        </div>
      </Modal>

      <ConfirmDelete open={modal === 'delete'} name={selected?.userName} onConfirm={handleDelete} onClose={() => setModal(null)} />
    </>
  )
}

// ── ROLES ─────────────────────────────────────────────────────────────────────
export function RolesPage() {
  const [roles, setRoles]       = useState([])
  const [total, setTotal]       = useState(0)
  const [page, setPage]         = useState(1)
  const [search, setSearch]     = useState('')
  const [modal, setModal]       = useState(null)
  const [selected, setSelected] = useState(null)
  const [name, setName]         = useState('')

  const load = useCallback(async () => {
    try { const { data } = await roleApi.getAll(page, 10, search); setRoles(data.data.items); setTotal(data.data.totalRecords) }
    catch { toast.error('Failed to load roles') }
  }, [page, search])

  useEffect(() => { load() }, [load])

  const handleSave = async () => {
    try {
      if (modal === 'create') await roleApi.create(name)
      else await roleApi.update(selected.id, name)
      toast.success('Saved'); setModal(null); load()
    } catch { toast.error('Error') }
  }
  const handleDelete = async () => {
    try { await roleApi.delete(selected.id); toast.success('Deleted'); setModal(null); load() }
    catch { toast.error('Delete failed') }
  }

  return (
    <>
      <div className="page-header">
        <h2>Roles</h2>
        <button className="btn btn-primary" onClick={() => { setName(''); setModal('create') }}><Plus size={14} /> Add Role</button>
      </div>
      <div className="card">
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          <div className="search-bar" style={{ flex: 1 }}><Search />
            <input placeholder="Search roles…" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} />
          </div>
        </div>
        <div className="table-wrap">
          <table>
            <thead><tr><th>Role Name</th><th>Created</th><th>Actions</th></tr></thead>
            <tbody>
              {roles.map(r => (
                <tr key={r.id}>
                  <td><span className="badge badge-purple">{r.roleName}</span></td>
                  <td className="td-muted">{new Date(r.createdOn).toLocaleDateString()}</td>
                  <td><div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn-icon" onClick={() => { setSelected(r); setName(r.roleName); setModal('edit') }}><Pencil /></button>
                    <button className="btn-icon" style={{ color: 'var(--accent2)' }} onClick={() => { setSelected(r); setModal('delete') }}><Trash2 /></button>
                  </div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination page={page} total={total} pageSize={10} onChange={setPage} />
      </div>

      <Modal open={modal === 'create' || modal === 'edit'} title={modal === 'create' ? 'Add Role' : 'Edit Role'}
        onClose={() => setModal(null)}
        footer={<><button className="btn btn-ghost" onClick={() => setModal(null)}>Cancel</button><button className="btn btn-primary" onClick={handleSave}>Save</button></>}>
        <div className="form-group"><label>Role Name</label>
          <input value={name} onChange={e => setName(e.target.value)} placeholder="e.g. Admin Role" autoFocus />
        </div>
      </Modal>
      <ConfirmDelete open={modal === 'delete'} name={selected?.roleName} onConfirm={handleDelete} onClose={() => setModal(null)} />
    </>
  )
}

// ── LOCATIONS ─────────────────────────────────────────────────────────────────
export function LocationsPage() {
  const [locs, setLocs]         = useState([])
  const [total, setTotal]       = useState(0)
  const [page, setPage]         = useState(1)
  const [search, setSearch]     = useState('')
  const [modal, setModal]       = useState(null)
  const [selected, setSelected] = useState(null)
  const [locTypes, setLocTypes] = useState([])
  const [form, setForm]         = useState({ locationName: '', locationTypeId: '', locationAddress: '', isActive: true })

  const load = useCallback(async () => {
    try { const { data } = await locationApi.getAll(page, 10, search); setLocs(data.data.items); setTotal(data.data.totalRecords) }
    catch { toast.error('Failed') }
  }, [page, search])

  useEffect(() => { load() }, [load])
  useEffect(() => { dataApi.locationTypes.getAll('').then(r => setLocTypes(r.data.data || [])).catch(() => {}) }, [])

  const handleSave = async () => {
    try {
      if (modal === 'create') await locationApi.create(form)
      else await locationApi.update(selected.id, form)
      toast.success('Saved'); setModal(null); load()
    } catch { toast.error('Error') }
  }
  const handleDelete = async () => {
    try { await locationApi.delete(selected.id); toast.success('Deleted'); setModal(null); load() }
    catch { toast.error('Cannot delete') }
  }

  return (
    <>
      <div className="page-header">
        <h2>Locations</h2>
        <button className="btn btn-primary" onClick={() => { setForm({ locationName: '', locationTypeId: '', locationAddress: '', isActive: true }); setModal('create') }}><Plus size={14} /> Add Location</button>
      </div>
      <div className="card">
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          <div className="search-bar" style={{ flex: 1 }}><Search /><input placeholder="Search…" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /></div>
        </div>
        <div className="table-wrap">
          <table>
            <thead><tr><th>Location</th><th>Type</th><th>Address</th><th>Status</th><th>Actions</th></tr></thead>
            <tbody>
              {locs.map(l => (
                <tr key={l.id}>
                  <td style={{ fontWeight: 500 }}>{l.locationName}</td>
                  <td><span className="badge badge-gray">{l.locationTypeName}</span></td>
                  <td className="td-muted">{l.locationAddress || '—'}</td>
                  <td><span className={`badge ${l.isActive ? 'badge-green' : 'badge-red'}`}>{l.isActive ? 'Active' : 'Inactive'}</span></td>
                  <td><div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn-icon" onClick={() => { setSelected(l); setForm({ locationName: l.locationName, locationTypeId: l.locationTypeId || '', locationAddress: l.locationAddress || '', isActive: l.isActive }); setModal('edit') }}><Pencil /></button>
                    <button className="btn-icon" style={{ color: 'var(--accent2)' }} onClick={() => { setSelected(l); setModal('delete') }}><Trash2 /></button>
                  </div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination page={page} total={total} pageSize={10} onChange={setPage} />
      </div>

      <Modal open={modal === 'create' || modal === 'edit'} title={modal === 'create' ? 'Add Location' : 'Edit Location'}
        onClose={() => setModal(null)}
        footer={<><button className="btn btn-ghost" onClick={() => setModal(null)}>Cancel</button><button className="btn btn-primary" onClick={handleSave}>Save</button></>}>
        <div className="form-group"><label>Location Name</label><input value={form.locationName} onChange={e => setForm(f => ({ ...f, locationName: e.target.value }))} /></div>
        <div className="form-group"><label>Location Type</label>
          <select value={form.locationTypeId} onChange={e => setForm(f => ({ ...f, locationTypeId: e.target.value }))}>
            <option value="">Select type…</option>
            {locTypes.map(t => <option key={t.id} value={t.id}>{t.locationTypeName}</option>)}
          </select>
        </div>
        <div className="form-group"><label>Address</label><input value={form.locationAddress} onChange={e => setForm(f => ({ ...f, locationAddress: e.target.value }))} /></div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <label className="toggle"><input type="checkbox" checked={form.isActive} onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))} /><span className="toggle-slider" /></label>
          <span style={{ fontSize: 13 }}>Active</span>
        </div>
      </Modal>
      <ConfirmDelete open={modal === 'delete'} name={selected?.locationName} onConfirm={handleDelete} onClose={() => setModal(null)} />
    </>
  )
}

// ── SETTINGS ──────────────────────────────────────────────────────────────────
export function SettingsPage() {
  const [tab, setTab]     = useState('website')
  const [data, setData]   = useState(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    settingsApi.get().then(r => setData(r.data.data)).catch(() => toast.error('Failed to load settings'))
  }, [])

  const save = async (payload) => {
    setSaving(true)
    try {
      if (tab === 'website')  await settingsApi.updateWebsite(payload)
      if (tab === 'business') await settingsApi.updateBusiness(payload)
      toast.success('Settings saved')
    } catch { toast.error('Save failed') }
    finally { setSaving(false) }
  }

  if (!data) return <div style={{ color: 'var(--muted)' }}>Loading settings…</div>

  return (
    <>
      <div className="page-header"><h2>Settings</h2></div>
      <div className="tab-bar" style={{ maxWidth: 400 }}>
        {['website', 'business', 'roles'].map(t => (
          <button key={t} className={`tab ${tab === t ? 'active' : ''}`} onClick={() => setTab(t)}>
            {t.charAt(0).toUpperCase() + t.slice(1)}
          </button>
        ))}
      </div>

      {/* Website Tab */}
      {tab === 'website' && (
        <div className="card" style={{ maxWidth: 600 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 20 }}>
            <label className="toggle">
              <input type="checkbox" checked={data.isWebsiteOnline} onChange={e => setData(d => ({ ...d, isWebsiteOnline: e.target.checked }))} />
              <span className="toggle-slider" />
            </label>
            <span style={{ fontWeight: 500 }}>{data.isWebsiteOnline ? '🟢 Website Online' : '🔴 Website Offline'}</span>
          </div>
          <div className="form-group">
            <label>Footer Description</label>
            <textarea rows={4} value={data.footerDescription || ''} onChange={e => setData(d => ({ ...d, footerDescription: e.target.value }))} />
          </div>
          <button className="btn btn-primary" disabled={saving} onClick={() => save({ isWebsiteOnline: data.isWebsiteOnline, footerDescription: data.footerDescription })}>
            {saving ? 'Saving…' : 'Save Changes'}
          </button>
        </div>
      )}

      {/* Business Tab */}
      {tab === 'business' && (
        <div className="card" style={{ maxWidth: 600 }}>
          {[['businessName', 'Business Name'], ['businessNature', 'Business Nature'], ['businessProvince', 'Province'], ['fbrToken', 'FBR Token'], ['validationToken', 'Validation Token']].map(([k, l]) => (
            <div key={k} className="form-group">
              <label>{l}</label>
              <input value={data[k] || ''} onChange={e => setData(d => ({ ...d, [k]: e.target.value }))} />
            </div>
          ))}
          <button className="btn btn-primary" disabled={saving}
            onClick={() => save({ businessName: data.businessName, businessNature: data.businessNature, businessProvince: data.businessProvince, fbrToken: data.fbrToken, validationToken: data.validationToken })}>
            {saving ? 'Saving…' : 'Save Changes'}
          </button>
        </div>
      )}

      {/* Roles Tab */}
      {tab === 'roles' && (
        <div className="card" style={{ maxWidth: 600 }}>
          <div className="form-group"><label>Chat Link URL</label>
            <input value={data.chatLinkUrl || ''} onChange={e => setData(d => ({ ...d, chatLinkUrl: e.target.value }))} placeholder="https://consultant.10xdigitalventures.com/login" />
          </div>
          <p style={{ color: 'var(--muted)', fontSize: 12, marginTop: 8 }}>Consultant default role: <strong style={{ color: 'var(--text)' }}>{data.consultantDefaultRoleName || '—'}</strong></p>
          <p style={{ color: 'var(--muted)', fontSize: 12, marginTop: 4 }}>Client default role: <strong style={{ color: 'var(--text)' }}>{data.clientDefaultRoleName || '—'}</strong></p>
        </div>
      )}
    </>
  )
}

// ── ERROR LOGS ────────────────────────────────────────────────────────────────
export function ErrorLogsPage() {
  const [logs, setLogs]         = useState([])
  const [total, setTotal]       = useState(0)
  const [page, setPage]         = useState(1)
  const [search, setSearch]     = useState('')
  const [selected, setSelected] = useState(null)

  const load = useCallback(async () => {
    try { const { data } = await errorLogApi.getAll(page, 10, search); setLogs(data.data.items); setTotal(data.data.totalRecords) }
    catch { toast.error('Failed') }
  }, [page, search])

  useEffect(() => { load() }, [load])

  const handleDelete = async id => {
    try { await errorLogApi.delete(id); toast.success('Deleted'); load() }
    catch { toast.error('Failed') }
  }

  return (
    <>
      <div className="page-header"><h2>Error Logs</h2></div>
      <div className="card">
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          <div className="search-bar" style={{ flex: 1 }}><Search /><input placeholder="Search errors…" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /></div>
        </div>
        <div className="table-wrap">
          <table>
            <thead><tr><th>Controller / Action</th><th>Code</th><th>Message</th><th>Date</th><th>Actions</th></tr></thead>
            <tbody>
              {logs.map(l => (
                <tr key={l.id}>
                  <td className="td-muted">{l.controllerName}/{l.actionName}</td>
                  <td><span className={`badge ${l.code >= 500 ? 'badge-red' : 'badge-gray'}`}>{l.code}</span></td>
                  <td style={{ maxWidth: 300, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{l.errorMessage}</td>
                  <td className="td-muted">{new Date(l.createdOn).toLocaleString()}</td>
                  <td><div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn-icon" onClick={() => setSelected(l)}><Eye /></button>
                    <button className="btn-icon" style={{ color: 'var(--accent2)' }} onClick={() => handleDelete(l.id)}><Trash2 /></button>
                  </div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination page={page} total={total} pageSize={10} onChange={setPage} />
      </div>

      <Modal open={!!selected} title="Error Detail" onClose={() => setSelected(null)}>
        {selected && (
          <>
            <p style={{ marginBottom: 12 }}><strong>Path:</strong> <span style={{ color: 'var(--muted)' }}>{selected.requestPath}</span></p>
            <p style={{ marginBottom: 12 }}><strong>Message:</strong> {selected.errorMessage}</p>
            {selected.stackTrace && (
              <pre style={{ background: 'var(--bg3)', padding: 12, borderRadius: 8, fontSize: 11, overflow: 'auto', maxHeight: 300, color: 'var(--muted)' }}>
                {selected.stackTrace}
              </pre>
            )}
          </>
        )}
      </Modal>
    </>
  )
}

// ── DATA CONSTANTS ────────────────────────────────────────────────────────────
export function DataConstantsPage() {
  const [tab, setTab] = useState('controlTypes')
  const [items, setItems] = useState([])
  const [search, setSearch] = useState('')
  const [modal, setModal] = useState(null)
  const [selected, setSelected] = useState(null)
  const [form, setForm] = useState({})

  const configs = {
    controlTypes:  { api: dataApi.controlTypes,  label: 'Control Types',  fields: [['ControlTypeName', 'Name'], ['ControlTypePrefix', 'Prefix']] },
    clientAreas:   { api: dataApi.clientAreas,   label: 'Client Areas',   fields: [['ControlAreaName', 'Name'], ['ControlAreaPrefix', 'Prefix']] },
    currencies:    { api: dataApi.currencies,    label: 'Currencies',     fields: [['CountryName', 'Country'], ['CurrencyName', 'Currency'], ['Symbol', 'Symbol']] },
    countries:     { api: dataApi.countries,     label: 'Countries',      fields: [['Name', 'Country Name'], ['Code', 'Code'], ['Prefix', 'Prefix']] },
    locationTypes: { api: dataApi.locationTypes, label: 'Location Types', fields: [['Name', 'Name'], ['ShortName', 'Short Name']] },
  }

  const cfg = configs[tab]

  const load = useCallback(async () => {
    try { const { data } = await cfg.api.getAll(search); setItems(data.data || []) }
    catch { toast.error('Failed to load') }
  }, [tab, search])

  useEffect(() => { load(); setModal(null) }, [load])

  const handleSave = async () => {
    try {
      if (modal === 'create') await cfg.api.create(form)
      else await cfg.api.update(selected.id, form)
      toast.success('Saved'); setModal(null); load()
    } catch { toast.error('Error') }
  }
  const handleDelete = async () => {
    try { await cfg.api.delete(selected.id); toast.success('Deleted'); setModal(null); load() }
    catch { toast.error('Cannot delete') }
  }

  return (
    <>
      <div className="page-header"><h2>Data Constants</h2></div>
      <div className="tab-bar">
        {Object.entries(configs).map(([k, v]) => (
          <button key={k} className={`tab ${tab === k ? 'active' : ''}`} onClick={() => { setTab(k); setSearch('') }}>{v.label}</button>
        ))}
      </div>
      <div className="card">
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          <div className="search-bar" style={{ flex: 1 }}><Search /><input placeholder={`Search ${cfg.label.toLowerCase()}…`} value={search} onChange={e => setSearch(e.target.value)} /></div>
          <button className="btn btn-primary btn-sm" onClick={() => { setForm({}); setModal('create') }}><Plus size={13} /> Add</button>
        </div>
        <div className="table-wrap">
          <table>
            <thead><tr>{cfg.fields.map(([, l]) => <th key={l}>{l}</th>)}<th>Actions</th></tr></thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  {cfg.fields.map(([k]) => <td key={k} style={{ fontSize: 13 }}>{item[k.charAt(0).toLowerCase() + k.slice(1)] || item[k] || '—'}</td>)}
                  <td><div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn-icon" onClick={() => {
                      setSelected(item)
                      const f = {}
                      cfg.fields.forEach(([k]) => { const lk = k.charAt(0).toLowerCase() + k.slice(1); f[k] = item[lk] || item[k] || '' })
                      setForm(f); setModal('edit')
                    }}><Pencil /></button>
                    <button className="btn-icon" style={{ color: 'var(--accent2)' }} onClick={() => { setSelected(item); setModal('delete') }}><Trash2 /></button>
                  </div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <Modal open={modal === 'create' || modal === 'edit'} title={modal === 'create' ? `Add ${cfg.label.slice(0, -1)}` : 'Edit'}
        onClose={() => setModal(null)}
        footer={<><button className="btn btn-ghost" onClick={() => setModal(null)}>Cancel</button><button className="btn btn-primary" onClick={handleSave}>Save</button></>}>
        {cfg.fields.map(([k, l]) => (
          <div key={k} className="form-group"><label>{l}</label>
            <input value={form[k] || ''} onChange={e => setForm(f => ({ ...f, [k]: e.target.value }))} />
          </div>
        ))}
      </Modal>
      <ConfirmDelete open={modal === 'delete'} name={selected?.controlTypeName || selected?.controlAreaName || selected?.currencyName || selected?.countryName || selected?.locationTypeName || 'item'} onConfirm={handleDelete} onClose={() => setModal(null)} />
    </>
  )
}
