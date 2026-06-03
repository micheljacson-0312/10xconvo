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

import PurchaseModal from '../components/Billing/PurchaseModal';
import TransactionsTable from '../components/Billing/TransactionsTable';
import InvoicesTable from '../components/Billing/InvoicesTable';
import CreditsTable from '../components/Billing/CreditsTable';

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
          {tab === 'credits' && <CreditsTable pricing={pricing} />}

          {/* ── Tab 2: Invoices ── */}
          {tab === 'invoices' && (
            <InvoicesTable loading={loading} invoices={invoices} openInvoice={openInvoice} />
          )}

          {/* ── Tab 3: Transactions (usage log) ── */}
          {tab === 'transactions' && (
            <TransactionsTable loading={loading} transactions={transactions} />
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
      <PurchaseModal
        showPurchase={showPurchase}
        setShowPurchase={setShowPurchase}
        cart={cart}
        setCart={setCart}
        purchasing={purchasing}
        setPurchasing={setPurchasing}
        gateway={gateway}
        setGateway={setGateway}
      />
    </div>
  )
}
