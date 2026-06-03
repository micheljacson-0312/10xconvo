import React from 'react';
import { toast } from 'react-hot-toast';
import { ChevronRight, Loader } from 'lucide-react';
import { creditsApi } from '../../api';

export default function PurchaseModal({
  showPurchase,
  setShowPurchase,
  cart,
  setCart,
  purchasing,
  setPurchasing,
  gateway,
  setGateway
}) {
  if (!showPurchase) return null;

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

  return (
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
  )
}
