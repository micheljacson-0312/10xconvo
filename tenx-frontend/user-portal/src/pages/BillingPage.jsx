// ═══════════════════════════════════════════════════════════════════════════
//  BILLING & CREDITS — Customer-side billing page
//  Inspired by LMS billing page design
//
//  Top section: Credit Balance card + Payment Method card (side by side)
//  3 Tabs: Credits (remaining) | Invoices (download PDF) | Transactions (usage log)
//  Date range filter + pagination
// ═══════════════════════════════════════════════════════════════════════════

import React, { useState, useEffect, useCallback } from 'react'
import { toast } from 'react-hot-toast'
import { Download, CreditCard, MessageSquare, Mic, Video, Image, File, ChevronRight, Loader, Calendar } from 'lucide-react'
import { creditsApi, invoiceApi } from '../api'
import { useAuthStore } from '../api'

export default function BillingPage() {
  const { user } = useAuthStore()
  const [tab, setTab]         = useState('credits')
  const [credits, setCredits] = useState(null)
  const [invoices, setInvoices] = useState([])
  const [transactions, setTransactions] = useState([])
  const [invTotal, setInvTotal] = useState(0)
  const [txnTotal, setTxnTotal] = useState(0)
  const [page, setPage]       = useState(1)
  const [loading, setLoading] = useState(false)
  const [pricing, setPricing] = useState([])
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo]     = useState('')

  // ── Purchase Modal State ──
  const [showPurchase, setShowPurchase] = useState(false)
  const [cart, setCart] = useState({ textChars: 0, audioMins: 0, videoMins: 0, imageCredits: 0, fileCredits: 0 })
  const [purchasing, setPurchasing] = useState(false)
  const [gateway, setGateway] = useState('payfast') // 'stripe' | 'payfast' | 'easypaisa'

  // Check URL for payment return
  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    if (params.get('payment') === 'success') {
      toast.success('Payment successful! Credits added to your account.')
      creditsApi.getBalance().then(r => setCredits(r.data.data)).catch(() => {})
      window.history.replaceState({}, '', '/billing')
    }
    if (params.get('payment') === 'cancelled' || params.get('payment') === 'failed') {
      const error = params.get('error')
      toast.error(error ? `Payment failed: ${error}` : 'Payment cancelled.')
      window.history.replaceState({}, '', '/billing')
    }
  }, [])

  const handlePurchase = async () => {
    if (cart.textChars <= 0 && cart.audioMins <= 0 && cart.videoMins <= 0 && cart.imageCredits <= 0 && cart.fileCredits <= 0) {
      toast.error('Select at least one credit type'); return
    }
    setPurchasing(true)
    try {
      if (gateway === 'stripe') {
        const { data } = await creditsApi.purchase(cart)
        if (data.data?.checkoutUrl) window.location.href = data.data.checkoutUrl
        else toast.error('No checkout URL received')
      } else if (gateway === 'easypaisa') {
        // EasyPaisa — returns HTML form that auto-redirects to EasyPaisa page
        const { data } = await creditsApi.purchaseEasyPaisa(cart)
        if (data.data?.paymentFormHtml) {
          const w = window.open('', '_blank', 'width=900,height=700')
          if (w) { w.document.write(data.data.paymentFormHtml); w.document.close() }
          else { toast.error('Popup blocked — please allow popups') }
          setShowPurchase(false)
          toast('Redirecting to EasyPaisa... Complete payment in the new window.', { icon: '💚', duration: 5000 })
        } else toast.error(data.data?.message || 'Failed to connect to EasyPaisa')
      } else {
        // PayFast — returns HTML form that auto-redirects
        const { data } = await creditsApi.purchasePayFast(cart)
        if (data.data?.paymentFormHtml) {
          // Open PayFast form in new window
          const w = window.open('', '_blank', 'width=900,height=700')
          if (w) { w.document.write(data.data.paymentFormHtml); w.document.close() }
          else { toast.error('Popup blocked — please allow popups') }
          setShowPurchase(false)
          toast('Redirecting to PayFast... Complete payment in the new window.', { icon: '🏦', duration: 5000 })
        } else toast.error(data.data?.message || 'Failed to connect to PayFast')
      }
    } catch (e) {
      toast.error(e.response?.data?.message || 'Purchase failed')
    } finally { setPurchasing(false) }
  }

  // Load credits balance
  useEffect(() => {
    creditsApi.getBalance().then(r => setCredits(r.data.data)).catch(() => {})
    creditsApi.getPricing().then(r => setPricing(r.data.data || [])).catch(() => {})
  }, [])

  // Load tab data
  const loadInvoices = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await invoiceApi.list(page, 10)
      setInvoices(data.data.items || []); setInvTotal(data.data.totalRecords || 0)
    } catch {} finally { setLoading(false) }
  }, [page])

  const loadTransactions = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await creditsApi.getHistory(page, 10)
      setTransactions(data.data || []); setTxnTotal(data.data?.length >= 10 ? 999 : data.data?.length || 0)
    } catch {} finally { setLoading(false) }
  }, [page])

  useEffect(() => { setPage(1) }, [tab])
  useEffect(() => {
    if (tab === 'invoices') loadInvoices()
    if (tab === 'transactions') loadTransactions()
  }, [tab, loadInvoices, loadTransactions])

  const openInvoice = (id) => {
    const token = localStorage.getItem('accessToken')
    window.open(`${invoiceApi.download(id)}?access_token=${token}`, '_blank')
  }

  return (
    <div style={{ maxWidth: 1000, margin: '0 auto', padding: '32px 20px' }}>

      {/* ── Header ── */}
      <div style={{ marginBottom: 28 }}>
        <h1 style={{ fontFamily: 'var(--font-head)', fontSize: 24, fontWeight: 800, marginBottom: 4 }}>
          Billing & Credits
        </h1>
        <p style={{ color: 'var(--muted)', fontSize: 14 }}>Manage your credits, invoices and transaction history</p>
      </div>

      {/* ── Top Cards (Balance + Payment Method) ── */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 340px', gap: 16, marginBottom: 28 }}>

        {/* Credit Balance Card */}
        <div style={{ background: 'var(--bg2)', borderRadius: 14, border: '1px solid var(--border)', padding: 24 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 20 }}>
            <div>
              <h2 style={{ fontFamily: 'var(--font-head)', fontSize: 18, fontWeight: 700, marginBottom: 4 }}>Your Credit Balance</h2>
              <p style={{ fontSize: 12, color: 'var(--muted)' }}>{user?.userName} • {user?.email}</p>
            </div>
            <button className="btn btn-primary" style={{ fontSize: 13, padding: '8px 16px' }}
              onClick={() => setShowPurchase(true)}>
              Buy Credits <ChevronRight size={14} />
            </button>
          </div>

          {credits ? (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 12 }}>
              {[
                { icon: MessageSquare, label: 'Text', value: `${(credits.textCharsRemaining || 0).toLocaleString()}`, unit: 'chars', color: '#0ea5e9' },
                { icon: Mic,           label: 'Audio', value: `${(credits.audioMinsRemaining || 0).toFixed(1)}`, unit: 'min', color: '#22c55e' },
                { icon: Video,         label: 'Video', value: `${(credits.videoMinsRemaining || 0).toFixed(1)}`, unit: 'min', color: '#a855f7' },
                { icon: Image,         label: 'Images', value: `${credits.imageCreditsRemaining || 0}`, unit: 'left', color: '#f59e0b' },
                { icon: File,          label: 'Files', value: `${credits.fileCreditsRemaining || 0}`, unit: 'left', color: '#ec4899' },
              ].map((c, i) => (
                <div key={i} style={{ textAlign: 'center', padding: '16px 8px', borderRadius: 10, background: `${c.color}08`, border: `1px solid ${c.color}20` }}>
                  <c.icon size={20} style={{ color: c.color, marginBottom: 6 }} />
                  <div style={{ fontFamily: 'var(--font-head)', fontSize: 22, fontWeight: 800, color: c.color }}>{c.value}</div>
                  <div style={{ fontSize: 11, color: 'var(--muted)' }}>{c.unit}</div>
                  <div style={{ fontSize: 10, color: 'var(--muted)', marginTop: 2 }}>{c.label}</div>
                </div>
              ))}
            </div>
          ) : (
            <div style={{ textAlign: 'center', padding: 30, color: 'var(--muted)' }}><Loader size={20} className="spin" /></div>
          )}
        </div>

        {/* Payment Method Card */}
        <div style={{ background: 'var(--bg2)', borderRadius: 14, border: '1px solid var(--border)', padding: 24, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', textAlign: 'center' }}>
          <h3 style={{ fontFamily: 'var(--font-head)', fontSize: 16, fontWeight: 700, marginBottom: 16 }}>Payment Method</h3>
          <div style={{ width: 56, height: 56, borderRadius: '50%', background: 'var(--bg3)', display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: 12 }}>
            <CreditCard size={24} style={{ color: 'var(--muted)' }} />
          </div>
          <p style={{ fontSize: 13, color: 'var(--muted)', marginBottom: 12 }}>PayFast • EasyPaisa • Stripe</p>
          <p style={{ fontSize: 12, color: 'var(--muted)' }}>PayFast / EasyPaisa (Pakistan) or Stripe (International)</p>
        </div>
      </div>

      {/* ── 3 Tabs ── */}
      <div style={{ background: 'var(--bg2)', borderRadius: 14, border: '1px solid var(--border)', overflow: 'hidden' }}>

        {/* Tab Bar */}
        <div style={{ display: 'flex', borderBottom: '1px solid var(--border)' }}>
          {[
            ['credits', 'Credits'],
            ['invoices', 'Invoices'],
            ['transactions', 'Transactions'],
          ].map(([key, label]) => (
            <button key={key} onClick={() => setTab(key)}
              style={{
                flex: 1, padding: '14px 0', fontSize: 14, fontWeight: 600, cursor: 'pointer', border: 'none', background: 'none',
                color: tab === key ? 'var(--accent)' : 'var(--muted)',
                borderBottom: tab === key ? '2px solid var(--accent)' : '2px solid transparent',
                transition: 'all 0.15s',
              }}>
              {label}
            </button>
          ))}
        </div>

        <div style={{ padding: 20 }}>

          {/* ── Date Filter Bar (for invoices + transactions) ── */}
          {(tab === 'invoices' || tab === 'transactions') && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 16 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '8px 14px', borderRadius: 8, border: '1px solid var(--border)', background: 'var(--bg3)', fontSize: 13 }}>
                <Calendar size={14} style={{ color: 'var(--muted)' }} />
                <input type="date" value={dateFrom} onChange={e => setDateFrom(e.target.value)}
                  style={{ border: 'none', background: 'none', color: 'var(--text)', fontSize: 13, outline: 'none' }} />
                <span style={{ color: 'var(--muted)' }}>→</span>
                <input type="date" value={dateTo} onChange={e => setDateTo(e.target.value)}
                  style={{ border: 'none', background: 'none', color: 'var(--text)', fontSize: 13, outline: 'none' }} />
              </div>
              <div style={{ flex: 1 }} />
              <span style={{ fontSize: 12, color: 'var(--muted)' }}>10 / page</span>
            </div>
          )}

          {/* ── Tab 1: Credits (pricing table) ── */}
          {tab === 'credits' && (
            <div>
              <p style={{ fontSize: 13, color: 'var(--muted)', marginBottom: 16 }}>Current pricing rates for messaging. Credits are deducted when you send a message.</p>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ borderBottom: '2px solid var(--border)' }}>
                    <th style={{ textAlign: 'left', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)', letterSpacing: 0.5 }}>Message Type</th>
                    <th style={{ textAlign: 'left', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Unit</th>
                    <th style={{ textAlign: 'right', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Price</th>
                    <th style={{ textAlign: 'center', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {pricing.map((p, i) => (
                    <tr key={i} style={{ borderBottom: '1px solid var(--border)' }}>
                      <td style={{ padding: '12px 14px', fontWeight: 600, textTransform: 'capitalize' }}>{p.messageType}</td>
                      <td style={{ padding: '12px 14px', color: 'var(--muted)', fontSize: 13 }}>
                        {p.unitType === 'characters' ? `Per ${p.unitSize} characters` : p.unitType === 'minutes' ? 'Per minute' : 'Per item'}
                      </td>
                      <td style={{ padding: '12px 14px', textAlign: 'right', fontFamily: 'var(--font-head)', fontWeight: 700, color: 'var(--accent)' }}>
                        {p.currency} {Number(p.pricePerUnit).toFixed(2)}
                      </td>
                      <td style={{ padding: '12px 14px', textAlign: 'center' }}>
                        <span style={{ display: 'inline-block', padding: '3px 10px', borderRadius: 20, fontSize: 11, fontWeight: 700, background: p.isActive ? '#dcfce7' : '#fee2e2', color: p.isActive ? '#16a34a' : '#dc2626' }}>
                          {p.isActive ? 'Active' : 'Disabled'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* ── Tab 2: Invoices ── */}
          {tab === 'invoices' && (
            <div>
              {loading ? (
                <div style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}><Loader size={20} className="spin" /></div>
              ) : invoices.length === 0 ? (
                <div style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}>No invoices yet. Purchase credits to see invoices here.</div>
              ) : (
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ borderBottom: '2px solid var(--border)' }}>
                      <th style={{ textAlign: 'left', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Invoice #</th>
                      <th style={{ textAlign: 'left', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Credits</th>
                      <th style={{ textAlign: 'left', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Date</th>
                      <th style={{ textAlign: 'right', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Amount</th>
                      <th style={{ textAlign: 'center', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Status</th>
                      <th style={{ textAlign: 'center', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {invoices.map(inv => (
                      <tr key={inv.id} style={{ borderBottom: '1px solid var(--border)' }}>
                        <td style={{ padding: '12px 14px', fontWeight: 600, fontFamily: 'var(--font-head)', color: 'var(--accent)' }}>{inv.invoiceNumber}</td>
                        <td style={{ padding: '12px 14px', fontSize: 12 }}>
                          {[
                            inv.textCharsBought > 0 && `${inv.textCharsBought.toLocaleString()} chars`,
                            inv.audioMinsBought > 0 && `${inv.audioMinsBought} min audio`,
                            inv.videoMinsBought > 0 && `${inv.videoMinsBought} min video`,
                            inv.imageCreditsBought > 0 && `${inv.imageCreditsBought} images`,
                            inv.fileCreditsBought > 0 && `${inv.fileCreditsBought} files`,
                          ].filter(Boolean).join(' + ')}
                        </td>
                        <td style={{ padding: '12px 14px', color: 'var(--muted)', fontSize: 13 }}>
                          {new Date(inv.issuedAt).toLocaleDateString('en-US', { month: 'short', day: '2-digit', year: 'numeric' })}
                        </td>
                        <td style={{ padding: '12px 14px', textAlign: 'right', fontWeight: 700 }}>{inv.currency} {Number(inv.total).toFixed(2)}</td>
                        <td style={{ padding: '12px 14px', textAlign: 'center' }}>
                          <span style={{ display: 'inline-block', padding: '3px 10px', borderRadius: 20, fontSize: 11, fontWeight: 700, background: inv.status === 'paid' ? '#dcfce7' : '#fef3c7', color: inv.status === 'paid' ? '#16a34a' : '#d97706' }}>
                            {inv.status === 'paid' ? 'Paid' : inv.status}
                          </span>
                        </td>
                        <td style={{ padding: '12px 14px', textAlign: 'center' }}>
                          <button onClick={() => openInvoice(inv.id)}
                            style={{ background: 'none', border: '1px solid var(--border)', borderRadius: 6, padding: '4px 10px', cursor: 'pointer', color: 'var(--accent)', fontSize: 12, fontWeight: 600, display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                            <Download size={12} /> PDF
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          )}

          {/* ── Tab 3: Transactions (usage log) ── */}
          {tab === 'transactions' && (
            <div>
              {loading ? (
                <div style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}><Loader size={20} className="spin" /></div>
              ) : transactions.length === 0 ? (
                <div style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}>No transactions yet.</div>
              ) : (
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ borderBottom: '2px solid var(--border)' }}>
                      <th style={{ textAlign: 'left', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Description</th>
                      <th style={{ textAlign: 'left', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Type</th>
                      <th style={{ textAlign: 'left', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Date</th>
                      <th style={{ textAlign: 'right', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Units</th>
                      <th style={{ textAlign: 'right', padding: '10px 14px', fontSize: 12, textTransform: 'uppercase', color: 'var(--muted)' }}>Balance After</th>
                    </tr>
                  </thead>
                  <tbody>
                    {transactions.map(t => (
                      <tr key={t.id} style={{ borderBottom: '1px solid var(--border)' }}>
                        <td style={{ padding: '12px 14px', fontSize: 13 }}>{t.description || '—'}</td>
                        <td style={{ padding: '12px 14px' }}>
                          <span style={{
                            display: 'inline-block', padding: '3px 10px', borderRadius: 20, fontSize: 11, fontWeight: 700,
                            background: t.type === 'purchase' ? '#dcfce7' : t.type === 'admin_grant' ? '#dbeafe' : '#fef3c7',
                            color: t.type === 'purchase' ? '#16a34a' : t.type === 'admin_grant' ? '#2563eb' : '#d97706'
                          }}>
                            {t.type === 'purchase' ? 'Purchase' : t.type === 'message_charge' ? 'Used' : t.type === 'admin_grant' ? 'Granted' : t.type}
                          </span>
                        </td>
                        <td style={{ padding: '12px 14px', color: 'var(--muted)', fontSize: 13 }}>
                          {new Date(t.createdAt).toLocaleString('en-US', { month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit' })}
                        </td>
                        <td style={{ padding: '12px 14px', textAlign: 'right', fontWeight: 700, fontFamily: 'var(--font-head)', color: t.units > 0 ? '#22c55e' : '#ef4444' }}>
                          {t.units > 0 ? '+' : ''}{t.creditType === 'text' ? `${Math.abs(t.units).toLocaleString()} chars` : t.creditType === 'audio' || t.creditType === 'video' ? `${Math.abs(t.units).toFixed(1)} min` : Math.abs(t.units)}
                        </td>
                        <td style={{ padding: '12px 14px', textAlign: 'right', color: 'var(--muted)', fontSize: 13 }}>
                          {t.creditType === 'text' ? `${t.balanceAfter.toLocaleString()} chars` : t.creditType === 'audio' || t.creditType === 'video' ? `${t.balanceAfter.toFixed(1)} min` : t.balanceAfter}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          )}

          {/* Pagination */}
          {(tab === 'invoices' && invTotal > 10) || (tab === 'transactions' && transactions.length >= 10) ? (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 8, marginTop: 16, paddingTop: 16, borderTop: '1px solid var(--border)' }}>
              <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
                style={{ padding: '6px 14px', borderRadius: 6, border: '1px solid var(--border)', background: 'var(--bg3)', color: 'var(--text)', cursor: 'pointer', fontSize: 13 }}>
                Previous
              </button>
              <span style={{ width: 32, height: 32, borderRadius: 6, background: 'var(--accent)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 13 }}>
                {page}
              </span>
              <button onClick={() => setPage(p => p + 1)}
                style={{ padding: '6px 14px', borderRadius: 6, border: '1px solid var(--border)', background: 'var(--bg3)', color: 'var(--text)', cursor: 'pointer', fontSize: 13 }}>
                Next
              </button>
            </div>
          ) : null}
        </div>
      </div>

      {/* ── PURCHASE MODAL ── */}
      {showPurchase && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', zIndex: 100, display: 'flex', alignItems: 'center', justifyContent: 'center' }}
          onClick={e => e.target === e.currentTarget && setShowPurchase(false)}>
          <div style={{ background: 'var(--bg2)', borderRadius: 16, border: '1px solid var(--border)', width: '100%', maxWidth: 480, maxHeight: '90vh', overflow: 'auto' }}>
            <div style={{ padding: '20px 24px', borderBottom: '1px solid var(--border)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h3 style={{ fontFamily: 'var(--font-head)', fontSize: 18, fontWeight: 700 }}>Buy Credits</h3>
              <button onClick={() => setShowPurchase(false)} style={{ background: 'var(--bg3)', border: '1px solid var(--border)', borderRadius: 8, padding: 6, cursor: 'pointer', color: 'var(--text)' }}>✕</button>
            </div>
            <div style={{ padding: 24 }}>
              <p style={{ fontSize: 13, color: 'var(--muted)', marginBottom: 20 }}>Enter the amount of credits you want to purchase. You'll be redirected to Stripe to complete payment.</p>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                {[
                  { key: 'textChars',    icon: '📝', label: 'Text Characters', placeholder: 'e.g. 5000' },
                  { key: 'audioMins',    icon: '🎙️', label: 'Audio Minutes',   placeholder: 'e.g. 10' },
                  { key: 'videoMins',    icon: '🎬', label: 'Video Minutes',   placeholder: 'e.g. 5' },
                  { key: 'imageCredits', icon: '🖼️', label: 'Images',          placeholder: 'e.g. 20' },
                ].map(f => (
                  <div key={f.key}>
                    <label style={{ fontSize: 12, color: 'var(--muted)', display: 'block', marginBottom: 4 }}>{f.icon} {f.label}</label>
                    <input type="number" min="0" step={f.key.includes('Mins') ? '0.5' : '1'}
                      value={cart[f.key] || ''}
                      onChange={e => setCart(c => ({ ...c, [f.key]: Number(e.target.value) || 0 }))}
                      placeholder={f.placeholder}
                      style={{ width: '100%', padding: '10px 12px', borderRadius: 8, border: '1px solid var(--border)', background: 'var(--bg3)', color: 'var(--text)', fontSize: 14 }} />
                  </div>
                ))}
              </div>
              <div style={{ marginTop: 12 }}>
                <label style={{ fontSize: 12, color: 'var(--muted)', display: 'block', marginBottom: 4 }}>📎 Files</label>
                <input type="number" min="0" value={cart.fileCredits || ''}
                  onChange={e => setCart(c => ({ ...c, fileCredits: Number(e.target.value) || 0 }))}
                  placeholder="e.g. 10"
                  style={{ width: '100%', maxWidth: 200, padding: '10px 12px', borderRadius: 8, border: '1px solid var(--border)', background: 'var(--bg3)', color: 'var(--text)', fontSize: 14 }} />
              </div>

              {/* Gateway Selection */}
              <div style={{ marginTop: 20 }}>
                <label style={{ fontSize: 12, color: 'var(--muted)', display: 'block', marginBottom: 8 }}>Payment Method</label>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  <div style={{ display: 'flex', gap: 10 }}>
                    <button onClick={() => setGateway('payfast')}
                      style={{ flex: 1, padding: '14px 16px', background: gateway === 'payfast' ? 'var(--bg3)' : 'transparent', borderRadius: 10, display: 'flex', alignItems: 'center', gap: 12, border: gateway === 'payfast' ? '2px solid var(--accent)' : '1px solid var(--border)', cursor: 'pointer', textAlign: 'left' }}>
                      <div style={{ width: 40, height: 26, background: '#00a651', borderRadius: 4, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <span style={{ color: '#fff', fontWeight: 800, fontSize: 9 }}>PayFast</span>
                      </div>
                      <div>
                        <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text)' }}>PayFast (Pakistan)</div>
                        <div style={{ fontSize: 11, color: 'var(--muted)' }}>Cards, Bank, Wallet, RAAST</div>
                      </div>
                    </button>
                    <button onClick={() => setGateway('stripe')}
                      style={{ flex: 1, padding: '14px 16px', background: gateway === 'stripe' ? 'var(--bg3)' : 'transparent', borderRadius: 10, display: 'flex', alignItems: 'center', gap: 12, border: gateway === 'stripe' ? '2px solid var(--accent)' : '1px solid var(--border)', cursor: 'pointer', textAlign: 'left' }}>
                      <div style={{ width: 40, height: 26, background: '#635bff', borderRadius: 4, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <span style={{ color: '#fff', fontWeight: 800, fontSize: 9 }}>Stripe</span>
                      </div>
                      <div>
                        <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text)' }}>Stripe (International)</div>
                        <div style={{ fontSize: 11, color: 'var(--muted)' }}>Visa, MasterCard, Amex</div>
                      </div>
                    </button>
                  </div>
                  {/* EasyPaisa — full width row */}
                  <button onClick={() => setGateway('easypaisa')}
                    style={{ width: '100%', padding: '14px 16px', background: gateway === 'easypaisa' ? 'var(--bg3)' : 'transparent', borderRadius: 10, display: 'flex', alignItems: 'center', gap: 12, border: gateway === 'easypaisa' ? '2px solid #00a651' : '1px solid var(--border)', cursor: 'pointer', textAlign: 'left' }}>
                    <div style={{ width: 40, height: 26, background: '#00a651', borderRadius: 4, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                      <span style={{ color: '#fff', fontWeight: 800, fontSize: 8, textAlign: 'center', lineHeight: 1.1 }}>Easy{'\n'}Paisa</span>
                    </div>
                    <div>
                      <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text)' }}>EasyPaisa (Pakistan)</div>
                      <div style={{ fontSize: 11, color: 'var(--muted)' }}>Mobile Account, OTC at any EasyPaisa shop</div>
                    </div>
                    {gateway === 'easypaisa' && (
                      <span style={{ marginLeft: 'auto', fontSize: 11, color: '#00a651', fontWeight: 700, background: '#dcfce7', padding: '2px 8px', borderRadius: 20 }}>Selected</span>
                    )}
                  </button>
                </div>
              </div>
            </div>
            <div style={{ padding: '16px 24px', borderTop: '1px solid var(--border)', display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
              <button className="btn btn-ghost" onClick={() => setShowPurchase(false)}>Cancel</button>
              <button className="btn btn-primary" onClick={handlePurchase} disabled={purchasing} style={{ minWidth: 140, justifyContent: 'center' }}>
                {purchasing ? <Loader size={14} className="spin" /> : <>Pay with {gateway === 'payfast' ? 'PayFast' : gateway === 'easypaisa' ? 'EasyPaisa' : 'Stripe'} <ChevronRight size={14} /></>}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
