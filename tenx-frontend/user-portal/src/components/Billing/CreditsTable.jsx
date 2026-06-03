import React from 'react';

export default function CreditsTable({ pricing }) {
  return (
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
  );
}
