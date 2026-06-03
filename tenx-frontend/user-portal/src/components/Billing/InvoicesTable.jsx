import React from 'react';
import { Download, Loader } from 'lucide-react';

export default function InvoicesTable({ loading, invoices, openInvoice }) {
  if (loading) {
    return <div style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}><Loader size={20} className="spin" /></div>;
  }

  if (invoices.length === 0) {
    return <div style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}>No invoices yet. Purchase credits to see invoices here.</div>;
  }

  return (
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
  );
}
