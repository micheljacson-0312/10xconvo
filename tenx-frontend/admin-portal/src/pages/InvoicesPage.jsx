import React, { useState, useEffect, useCallback } from 'react'
import { toast } from 'react-hot-toast'
import { Search, RefreshCw, Download, Receipt, CreditCard, TrendingUp, Users } from 'lucide-react'
import { adminInvoiceApi } from '../api'

// ═══════════════════════════════════════════════════════════════════════════
//  ADMIN — INVOICES & PURCHASE HISTORY
//  Two tabs: Invoices (generated receipts) + Purchases (payment transactions)
// ═══════════════════════════════════════════════════════════════════════════

export default function InvoicesPage() {
  const [tab, setTab]           = useState('invoices')
  const [items, setItems]       = useState([])
  const [total, setTotal]       = useState(0)
  const [page, setPage]         = useState(1)
  const [search, setSearch]     = useState('')
  const [loading, setLoading]   = useState(false)
  const [stats, setStats]       = useState(null)
  const [statusFilter, setStatusFilter] = useState('')

  const loadInvoices = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await adminInvoiceApi.list(page, 15, search)
      setItems(data.data.items); setTotal(data.data.totalRecords)
    } catch { toast.error('Failed to load invoices') }
    finally { setLoading(false) }
  }, [page, search])

  const loadPurchases = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await adminInvoiceApi.purchases(page, 15, statusFilter || undefined, search)
      setItems(data.data.items); setTotal(data.data.totalRecords)
    } catch { toast.error('Failed to load purchases') }
    finally { setLoading(false) }
  }, [page, search, statusFilter])

  const loadStats = async () => {
    try { const { data } = await adminInvoiceApi.stats(); setStats(data.data) } catch {}
  }

  useEffect(() => { loadStats() }, [])
  useEffect(() => { setPage(1) }, [tab, search, statusFilter])
  useEffect(() => { tab === 'invoices' ? loadInvoices() : loadPurchases() }, [tab, loadInvoices, loadPurchases])

  const openInvoice = (id) => {
    window.open(adminInvoiceApi.download(id), '_blank')
  }

  return (
    <>
      <div className="page-header">
        <h2>Invoices & Purchases</h2>
      </div>

      {/* ── Stats Cards ── */}
      {stats && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 14, marginBottom: 20 }}>
          {[
            { label: 'Total Revenue',   val: `$${Number(stats.totalRevenue).toLocaleString(undefined, { minimumFractionDigits: 2 })}`, icon: TrendingUp, color: '#22c55e' },
            { label: 'This Month',      val: `$${Number(stats.thisMonth).toLocaleString(undefined, { minimumFractionDigits: 2 })}`,    icon: CreditCard, color: '#0ea5e9' },
            { label: 'Total Invoices',  val: stats.totalInvoices,  icon: Receipt,     color: '#a855f7' },
            { label: 'Unique Buyers',   val: stats.uniqueBuyers,   icon: Users,       color: '#f59e0b' },
          ].map((s, i) => (
            <div key={i} className="card" style={{ padding: 16, display: 'flex', alignItems: 'center', gap: 14 }}>
              <div style={{ width: 40, height: 40, borderRadius: 10, background: `${s.color}18`, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <s.icon size={18} style={{ color: s.color }} />
              </div>
              <div>
                <div style={{ fontSize: 20, fontWeight: 800, fontFamily: 'var(--font-head)' }}>{s.val}</div>
                <div style={{ fontSize: 11, color: 'var(--muted)' }}>{s.label}</div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* ── Tabs ── */}
      <div className="tab-bar" style={{ maxWidth: 350, marginBottom: 16 }}>
        <button className={`tab ${tab === 'invoices' ? 'active' : ''}`} onClick={() => setTab('invoices')}>🧾 Invoices</button>
        <button className={`tab ${tab === 'purchases' ? 'active' : ''}`} onClick={() => setTab('purchases')}>💳 Purchase History</button>
      </div>

      <div className="card">
        {/* Search + Filter Bar */}
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          <div className="search-bar" style={{ flex: 1 }}>
            <Search />
            <input placeholder="Search by invoice #, user name, email…" value={search}
              onChange={e => { setSearch(e.target.value); setPage(1) }} />
          </div>
          {tab === 'purchases' && (
            <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
              style={{ padding: '0 12px', borderRadius: 8, border: '1px solid var(--border)', background: 'var(--bg2)', color: 'var(--text)', fontSize: 13 }}>
              <option value="">All Status</option>
              <option value="pending">Pending</option>
              <option value="completed">Completed</option>
              <option value="failed">Failed</option>
              <option value="refunded">Refunded</option>
            </select>
          )}
          <button className="btn-icon" onClick={() => { tab === 'invoices' ? loadInvoices() : loadPurchases(); loadStats() }}><RefreshCw size={14} /></button>
        </div>

        {/* ── Invoices Table ── */}
        {tab === 'invoices' && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Invoice #</th>
                  <th>Customer</th>
                  <th>Credits Purchased</th>
                  <th>Total</th>
                  <th>Gateway</th>
                  <th>Date</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}>Loading…</td></tr>
                ) : items.length === 0 ? (
                  <tr><td colSpan={7}><div className="empty"><Receipt size={24} /><p>No invoices yet</p></div></td></tr>
                ) : items.map(inv => (
                  <tr key={inv.id}>
                    <td><span style={{ fontWeight: 600, fontFamily: 'var(--font-head)', color: 'var(--accent)' }}>{inv.invoiceNumber}</span></td>
                    <td>
                      <div style={{ fontWeight: 500 }}>{inv.userName}</div>
                      <div style={{ fontSize: 11, color: 'var(--muted)' }}>{inv.userEmail}</div>
                    </td>
                    <td>
                      <div style={{ fontSize: 12, color: 'var(--muted)', display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                        {inv.textCharsBought > 0 && <span className="badge badge-blue">{inv.textCharsBought.toLocaleString()} chars</span>}
                        {inv.audioMinsBought > 0 && <span className="badge badge-green">{inv.audioMinsBought} min audio</span>}
                        {inv.videoMinsBought > 0 && <span className="badge badge-purple">{inv.videoMinsBought} min video</span>}
                        {inv.imageCreditsBought > 0 && <span className="badge badge-blue">{inv.imageCreditsBought} images</span>}
                        {inv.fileCreditsBought > 0 && <span className="badge badge-blue">{inv.fileCreditsBought} files</span>}
                      </div>
                    </td>
                    <td><strong>{inv.currency} {Number(inv.total).toFixed(2)}</strong></td>
                    <td><span className="badge badge-purple">{inv.gateway}</span></td>
                    <td className="td-muted">{new Date(inv.issuedAt).toLocaleDateString()}</td>
                    <td>
                      <button className="btn-icon" title="Download Invoice" onClick={() => openInvoice(inv.id)} style={{ color: 'var(--accent)' }}>
                        <Download size={14} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* ── Purchases Table ── */}
        {tab === 'purchases' && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Txn Ref</th>
                  <th>Customer</th>
                  <th>Credits</th>
                  <th>Amount</th>
                  <th>Gateway</th>
                  <th>Status</th>
                  <th>Date</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}>Loading…</td></tr>
                ) : items.length === 0 ? (
                  <tr><td colSpan={7}><div className="empty"><CreditCard size={24} /><p>No purchases yet</p></div></td></tr>
                ) : items.map(p => (
                  <tr key={p.id}>
                    <td><span style={{ fontFamily: 'monospace', fontSize: 12 }}>{p.transactionRef}</span></td>
                    <td>
                      <div style={{ fontWeight: 500 }}>{p.userName}</div>
                      <div style={{ fontSize: 11, color: 'var(--muted)' }}>{p.userEmail}</div>
                    </td>
                    <td>
                      <div style={{ fontSize: 12, display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                        {p.textCharsBought > 0 && <span>{p.textCharsBought.toLocaleString()} chars</span>}
                        {p.audioMinsBought > 0 && <span>{p.audioMinsBought} min audio</span>}
                        {p.videoMinsBought > 0 && <span>{p.videoMinsBought} min video</span>}
                        {p.imageCreditsBought > 0 && <span>{p.imageCreditsBought} img</span>}
                        {p.fileCreditsBought > 0 && <span>{p.fileCreditsBought} files</span>}
                      </div>
                    </td>
                    <td><strong>{p.currency} {Number(p.amount).toFixed(2)}</strong></td>
                    <td><span className="badge badge-purple">{p.gateway || '—'}</span></td>
                    <td>
                      <span className={`badge ${p.status === 'completed' ? 'badge-green' : p.status === 'pending' ? 'badge-blue' : 'badge-purple'}`}>
                        {p.status}
                      </span>
                    </td>
                    <td className="td-muted">{new Date(p.createdAt).toLocaleDateString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {total > 15 && (
          <div style={{ display: 'flex', justifyContent: 'center', gap: 6, marginTop: 16 }}>
            {Array.from({ length: Math.ceil(total / 15) }, (_, i) => i + 1).slice(0, 7).map(p => (
              <button key={p} onClick={() => setPage(p)}
                style={{ width: 32, height: 32, borderRadius: 6, border: '1px solid var(--border)', background: p === page ? 'var(--accent)' : 'var(--bg2)', color: p === page ? '#fff' : 'var(--text)', cursor: 'pointer', fontSize: 12, fontWeight: 600 }}>
                {p}
              </button>
            ))}
          </div>
        )}
      </div>
    </>
  )
}
