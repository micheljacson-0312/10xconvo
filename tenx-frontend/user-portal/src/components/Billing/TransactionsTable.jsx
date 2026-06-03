import React from 'react';
import { Loader } from 'lucide-react';

export default function TransactionsTable({ loading, transactions }) {
  if (loading) {
    return <div style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}><Loader size={20} className="spin" /></div>;
  }

  if (transactions.length === 0) {
    return <div style={{ textAlign: 'center', padding: 40, color: 'var(--muted)' }}>No transactions yet.</div>;
  }

  return (
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
  );
}
