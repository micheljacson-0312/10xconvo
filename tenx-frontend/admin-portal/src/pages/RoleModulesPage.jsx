import React, { useState, useEffect } from 'react'
import { toast } from 'react-hot-toast'
import { Shield, Menu, Save, X, ChevronDown, ChevronRight, Check, Minus } from 'lucide-react'
import { roleApi, roleModuleApi } from '../api'

const PERM_COLS = ['canView','canCreate','canEdit','canDelete','canExport']
const COL_LABELS = { canView:'View', canCreate:'Create', canEdit:'Edit', canDelete:'Delete', canExport:'Export' }

// Checkbox with 3 states: true / false / null(mixed)
function PermCheck({ value, onChange }) {
  return (
    <button onClick={() => onChange(!value)}
      style={{
        width:22, height:22, borderRadius:4, border:'1px solid var(--border)',
        background: value ? 'var(--accent)' : 'var(--bg)',
        display:'flex', alignItems:'center', justifyContent:'center',
        cursor:'pointer', transition:'all 0.15s',
      }}>
      {value && <Check size={12} color="#fff" strokeWidth={3} />}
    </button>
  )
}

// ── MODULES TAB ───────────────────────────────────────────────────────────────
function ModulesTab({ roleId }) {
  const [modules, setModules] = useState([])
  const [loading, setLoading] = useState(false)
  const [saving, setSaving]   = useState(false)
  const [newRow, setNewRow]   = useState(null)

  const load = async () => {
    if (!roleId) return
    setLoading(true)
    try {
      const { data } = await roleModuleApi.getModules(roleId)
      setModules(data.data || [])
    } catch { toast.error('Failed to load modules') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [roleId])

  const toggle = (idx, field) => {
    setModules(ms => ms.map((m, i) => i === idx ? { ...m, [field]: !m[field] } : m))
  }

  const toggleAll = (field, val) => {
    setModules(ms => ms.map(m => ({ ...m, [field]: val })))
  }

  const saveAll = async () => {
    setSaving(true)
    try {
      await roleModuleApi.bulkModules(roleId, modules.map(m => ({
        moduleKey:  m.moduleKey,
        moduleName: m.moduleName,
        canView:    m.canView,
        canCreate:  m.canCreate,
        canEdit:    m.canEdit,
        canDelete:  m.canDelete,
        canExport:  m.canExport,
      })))
      toast.success('Permissions saved!')
      load()
    } catch { toast.error('Save failed') }
    finally { setSaving(false) }
  }

  const addRow = () => setNewRow({ moduleKey:'', moduleName:'', canView:true, canCreate:false, canEdit:false, canDelete:false, canExport:false })

  const confirmAdd = async () => {
    if (!newRow.moduleKey || !newRow.moduleName) return toast.error('Key and name required')
    setSaving(true)
    try {
      await roleModuleApi.bulkModules(roleId, [newRow])
      toast.success('Module added!'); setNewRow(null); load()
    } catch { toast.error('Failed') }
    finally { setSaving(false) }
  }

  const remove = async (id) => {
    try { await roleModuleApi.deleteModule(roleId, id); load() }
    catch { toast.error('Delete failed') }
  }

  return (
    <div>
      <div style={{ display:'flex', justifyContent:'space-between', alignItems:'center', marginBottom:16 }}>
        <span style={{ color:'var(--muted)', fontSize:13 }}>{modules.length} modules configured</span>
        <div style={{ display:'flex', gap:8 }}>
          <button className="btn btn-ghost" onClick={addRow}>+ Add Module</button>
          <button className="btn btn-primary" onClick={saveAll} disabled={saving || !modules.length}>
            <Save size={13} /> {saving ? 'Saving…' : 'Save All'}
          </button>
        </div>
      </div>

      {loading ? <div style={{ padding:40, textAlign:'center', color:'var(--muted)' }}>Loading…</div> : (
        <table className="table">
          <thead>
            <tr>
              <th>Module Key</th>
              <th>Module Name</th>
              {PERM_COLS.map(c => (
                <th key={c} style={{ textAlign:'center', width:72 }}>
                  <div style={{ display:'flex', flexDirection:'column', alignItems:'center', gap:4 }}>
                    <span style={{ fontSize:11 }}>{COL_LABELS[c]}</span>
                    <div style={{ display:'flex', gap:3 }}>
                      <button title="All On"  onClick={() => toggleAll(c, true)}  style={{ border:'none', background:'none', cursor:'pointer', color:'var(--green)',  fontSize:10, padding:'1px 3px' }}>✓</button>
                      <button title="All Off" onClick={() => toggleAll(c, false)} style={{ border:'none', background:'none', cursor:'pointer', color:'var(--accent2)', fontSize:10, padding:'1px 3px' }}>✗</button>
                    </div>
                  </div>
                </th>
              ))}
              <th style={{ width:40 }}></th>
            </tr>
          </thead>
          <tbody>
            {modules.length === 0 && !newRow && (
              <tr><td colSpan={8} style={{ textAlign:'center', color:'var(--muted)', padding:32 }}>
                No modules yet. Click "+ Add Module" to begin.
              </td></tr>
            )}
            {modules.map((m, i) => (
              <tr key={m.id || i}>
                <td><code style={{ fontSize:12, color:'var(--accent)' }}>{m.moduleKey}</code></td>
                <td style={{ fontWeight:500 }}>{m.moduleName}</td>
                {PERM_COLS.map(c => (
                  <td key={c} style={{ textAlign:'center' }}>
                    <PermCheck value={m[c]} onChange={() => toggle(i, c)} />
                  </td>
                ))}
                <td>
                  <button className="btn-icon danger" onClick={() => remove(m.id)}><X size={12}/></button>
                </td>
              </tr>
            ))}
            {/* Add row */}
            {newRow && (
              <tr style={{ background:'var(--bg3)' }}>
                <td><input className="input" placeholder="module_key" value={newRow.moduleKey}
                  onChange={e => setNewRow(r => ({...r, moduleKey: e.target.value}))}
                  style={{ fontSize:12, padding:'4px 8px' }} /></td>
                <td><input className="input" placeholder="Module Name" value={newRow.moduleName}
                  onChange={e => setNewRow(r => ({...r, moduleName: e.target.value}))}
                  style={{ fontSize:12, padding:'4px 8px' }} /></td>
                {PERM_COLS.map(c => (
                  <td key={c} style={{ textAlign:'center' }}>
                    <PermCheck value={newRow[c]} onChange={() => setNewRow(r => ({...r,[c]:!r[c]}))} />
                  </td>
                ))}
                <td style={{ display:'flex', gap:4 }}>
                  <button className="btn-icon" onClick={confirmAdd} title="Add"><Check size={12} color="var(--green)"/></button>
                  <button className="btn-icon danger" onClick={() => setNewRow(null)}><X size={12}/></button>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}
    </div>
  )
}

// ── MENUS TAB ─────────────────────────────────────────────────────────────────
const DEFAULT_MENUS = [
  { menuKey:'dashboard',      menuLabel:'Dashboard',      menuPath:'/',              menuOrder:1,  menuIcon:'LayoutDashboard', isVisible:true },
  { menuKey:'users',          menuLabel:'Registrations',  menuPath:'/users',         menuOrder:2,  menuIcon:'Users',           isVisible:true },
  { menuKey:'roles',          menuLabel:'Roles',          menuPath:'/roles',         menuOrder:3,  menuIcon:'Shield',          isVisible:true },
  { menuKey:'locations',      menuLabel:'Locations',      menuPath:'/locations',     menuOrder:4,  menuIcon:'MapPin',          isVisible:true },
  { menuKey:'fiscal-years',   menuLabel:'Fiscal Years',   menuPath:'/fiscal-years',  menuOrder:5,  menuIcon:'CalendarDays',    isVisible:true },
  { menuKey:'settings',       menuLabel:'Settings',       menuPath:'/settings',      menuOrder:6,  menuIcon:'Settings',        isVisible:true },
  { menuKey:'data',           menuLabel:'Data Constants', menuPath:'/data',          menuOrder:7,  menuIcon:'Database',        isVisible:true },
  { menuKey:'doc-movements',  menuLabel:'Doc Movements',  menuPath:'/doc-movements', menuOrder:8,  menuIcon:'FileDigit',       isVisible:true },
  { menuKey:'templates',      menuLabel:'Templates',      menuPath:'/templates',     menuOrder:9,  menuIcon:'FileText',        isVisible:true },
  { menuKey:'notifications',  menuLabel:'Notifications',  menuPath:'/notifications', menuOrder:10, menuIcon:'Bell',            isVisible:true },
  { menuKey:'errors',         menuLabel:'Error Logs',     menuPath:'/errors',        menuOrder:11, menuIcon:'AlertTriangle',   isVisible:true },
  { menuKey:'reviews',        menuLabel:'Reviews',        menuPath:'/reviews',       menuOrder:12, menuIcon:'Star',            isVisible:true },
]

function MenusTab({ roleId }) {
  const [menus, setMenus]     = useState([])
  const [loading, setLoading] = useState(false)
  const [saving, setSaving]   = useState(false)

  const load = async () => {
    if (!roleId) return
    setLoading(true)
    try {
      const { data } = await roleModuleApi.getMenus(roleId)
      const existing = data.data || []
      // Merge with defaults — show all items, mark visibility from server
      const merged = DEFAULT_MENUS.map(def => {
        const srv = existing.find(e => e.menuKey === def.menuKey)
        return srv ? { ...def, ...srv } : { ...def, id: null }
      })
      setMenus(merged)
    } catch { toast.error('Failed to load menus') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [roleId])

  const toggleVisible = idx => {
    setMenus(ms => ms.map((m, i) => i === idx ? { ...m, isVisible: !m.isVisible } : m))
  }

  const saveAll = async () => {
    setSaving(true)
    try {
      const payload = menus.map((m, i) => ({
        menuKey:   m.menuKey,
        menuLabel: m.menuLabel,
        menuPath:  m.menuPath,
        menuOrder: i + 1,
        menuIcon:  m.menuIcon,
        isVisible: m.isVisible,
      }))
      await roleModuleApi.bulkMenus(roleId, payload)
      toast.success('Menu visibility saved!')
      load()
    } catch { toast.error('Save failed') }
    finally { setSaving(false) }
  }

  return (
    <div>
      <div style={{ display:'flex', justifyContent:'space-between', alignItems:'center', marginBottom:16 }}>
        <span style={{ color:'var(--muted)', fontSize:13 }}>
          {menus.filter(m => m.isVisible).length} of {menus.length} menu items visible
        </span>
        <button className="btn btn-primary" onClick={saveAll} disabled={saving}>
          <Save size={13} /> {saving ? 'Saving…' : 'Save Menu'}
        </button>
      </div>

      {loading ? <div style={{ padding:40, textAlign:'center', color:'var(--muted)' }}>Loading…</div> : (
        <div style={{ display:'flex', flexDirection:'column', gap:6 }}>
          {menus.map((m, i) => (
            <div key={m.menuKey}
              style={{
                display:'flex', alignItems:'center', justifyContent:'space-between',
                padding:'10px 14px', borderRadius:8,
                background: m.isVisible ? 'var(--bg3)' : 'var(--bg)',
                border: `1px solid ${m.isVisible ? 'var(--border)' : 'transparent'}`,
                opacity: m.isVisible ? 1 : 0.45,
                transition:'all 0.15s',
              }}>
              <div style={{ display:'flex', alignItems:'center', gap:12 }}>
                <span style={{ color:'var(--muted)', fontSize:11, width:24, textAlign:'center' }}>{i+1}</span>
                <div>
                  <div style={{ fontWeight:500, fontSize:13 }}>{m.menuLabel}</div>
                  <div style={{ color:'var(--muted)', fontSize:11 }}>{m.menuPath}</div>
                </div>
              </div>
              <label style={{ display:'flex', alignItems:'center', gap:8, cursor:'pointer' }}>
                <span style={{ fontSize:12, color:'var(--muted)' }}>{m.isVisible ? 'Visible' : 'Hidden'}</span>
                <div onClick={() => toggleVisible(i)}
                  style={{
                    width:36, height:20, borderRadius:10, cursor:'pointer', transition:'all 0.2s',
                    background: m.isVisible ? 'var(--accent)' : 'var(--border)',
                    position:'relative',
                  }}>
                  <div style={{
                    position:'absolute', top:2, left: m.isVisible ? 18 : 2,
                    width:16, height:16, borderRadius:'50%',
                    background:'#fff', transition:'all 0.2s',
                  }}/>
                </div>
              </label>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ── MAIN PAGE ─────────────────────────────────────────────────────────────────
export default function RoleModulesPage() {
  const [roles, setRoles]   = useState([])
  const [roleId, setRoleId] = useState('')
  const [tab, setTab]       = useState('modules')

  useEffect(() => {
    roleApi.getAll(1, 100, '').then(({ data }) => {
      const list = data.data?.items || []
      setRoles(list)
      if (list.length) setRoleId(list[0].id)
    }).catch(() => {})
  }, [])

  return (
    <>
      <div className="page-header">
        <h2>Role Permissions</h2>
      </div>

      {/* Role selector */}
      <div style={{ display:'flex', alignItems:'center', gap:12, marginBottom:20 }}>
        <Shield size={16} color="var(--accent)" />
        <label style={{ fontSize:13, color:'var(--muted)' }}>Select Role:</label>
        <select className="input" style={{ maxWidth:240 }} value={roleId} onChange={e => setRoleId(e.target.value)}>
          {roles.map(r => <option key={r.id} value={r.id}>{r.roleName}</option>)}
        </select>
      </div>

      {/* Sub-tabs */}
      <div style={{ display:'flex', gap:4, marginBottom:16, borderBottom:'1px solid var(--border)' }}>
        {[{k:'modules',label:'Module Permissions',icon:Shield},{k:'menus',label:'Menu Visibility',icon:Menu}].map(t => {
          const Icon = t.icon
          return (
            <button key={t.k} onClick={() => setTab(t.k)}
              style={{
                display:'flex', alignItems:'center', gap:6,
                padding:'9px 16px', border:'none', background:'none', cursor:'pointer',
                color: tab===t.k ? 'var(--accent)' : 'var(--muted)',
                borderBottom: tab===t.k ? '2px solid var(--accent)' : '2px solid transparent',
                fontSize:13, fontWeight: tab===t.k ? 600 : 400,
                marginBottom:-1, transition:'all 0.15s',
              }}>
              <Icon size={13}/> {t.label}
            </button>
          )
        })}
      </div>

      {roleId ? (
        <div className="card">
          {tab === 'modules' && <ModulesTab key={`m-${roleId}`} roleId={roleId} />}
          {tab === 'menus'   && <MenusTab   key={`n-${roleId}`} roleId={roleId} />}
        </div>
      ) : (
        <div className="card" style={{ padding:40, textAlign:'center', color:'var(--muted)' }}>
          No roles found. Create a role first.
        </div>
      )}
    </>
  )
}
