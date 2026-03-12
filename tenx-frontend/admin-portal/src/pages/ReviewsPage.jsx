import React, { useState, useEffect } from 'react'
import { toast } from 'react-hot-toast'
import { Star, Trash2, X, Filter } from 'lucide-react'
import { reviewAdminApi } from '../api'

function Modal({ open, title, onClose, children, footer }) {
  if (!open) return null
  return (
    <div className="modal-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="modal">
        <div className="modal-header"><h3>{title}</h3><button className="btn-icon" onClick={onClose}><X size={14}/></button></div>
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-footer">{footer}</div>}
      </div>
    </div>
  )
}

function Stars({ rating }) {
  return (
    <div style={{ display:'flex', gap:2 }}>
      {[1,2,3,4,5].map(s => (
        <Star key={s} size={13} fill={s <= rating ? '#f7c948' : 'none'} color={s <= rating ? '#f7c948' : 'var(--border)'} />
      ))}
    </div>
  )
}

export default function ReviewsPage() {
  const [items, setItems]     = useState([])
  const [total, setTotal]     = useState(0)
  const [page, setPage]       = useState(1)
  const [loading, setLoading] = useState(true)
  const [minRating, setMin]   = useState('')
  const [maxRating, setMax]   = useState('')
  const [delId, setDelId]     = useState(null)
  const PS = 20

  const load = async () => {
    setLoading(true)
    try {
      const { data } = await reviewAdminApi.getAll(page, PS, minRating||undefined, maxRating||undefined)
      setItems(data.data?.items || [])
      setTotal(data.data?.totalRecords || 0)
    } catch { toast.error('Failed to load reviews') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [page, minRating, maxRating])

  const del = async () => {
    try { await reviewAdminApi.delete(delId); toast.success('Review removed'); setDelId(null); load() }
    catch { toast.error('Delete failed') }
  }

  const fmtDate = d => d ? new Date(d).toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' }) : '—'

  // Rating distribution from current page
  const dist = [5,4,3,2,1].map(s => ({ stars:s, count: items.filter(i => i.rating===s).length }))

  return (
    <>
      <div className="page-header">
        <h2>Consultant Reviews</h2>
        <span style={{ color:'var(--muted)', fontSize:13 }}>{total} total reviews</span>
      </div>

      {/* Filter bar */}
      <div style={{ display:'flex', alignItems:'center', gap:10, marginBottom:16 }}>
        <Filter size={13} color="var(--muted)" />
        <label style={{ fontSize:12, color:'var(--muted)' }}>Stars:</label>
        <select className="input" style={{ maxWidth:100, fontSize:12 }} value={minRating}
          onChange={e => { setMin(e.target.value); setPage(1) }}>
          <option value="">Min</option>
          {[1,2,3,4,5].map(s => <option key={s} value={s}>{s}★</option>)}
        </select>
        <span style={{ color:'var(--muted)' }}>–</span>
        <select className="input" style={{ maxWidth:100, fontSize:12 }} value={maxRating}
          onChange={e => { setMax(e.target.value); setPage(1) }}>
          <option value="">Max</option>
          {[1,2,3,4,5].map(s => <option key={s} value={s}>{s}★</option>)}
        </select>
        {(minRating||maxRating) && (
          <button className="btn btn-ghost" style={{ fontSize:12 }} onClick={() => { setMin(''); setMax(''); setPage(1) }}>
            Clear
          </button>
        )}
      </div>

      {loading ? (
        <div style={{ padding:40, textAlign:'center', color:'var(--muted)' }}>Loading…</div>
      ) : (
        <div className="card">
          <table className="table">
            <thead>
              <tr>
                <th>Reviewer</th>
                <th>Consultant</th>
                <th>Rating</th>
                <th>Comment</th>
                <th>Date</th>
                <th style={{ textAlign:'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr><td colSpan={6} style={{ textAlign:'center', color:'var(--muted)', padding:32 }}>
                  No reviews found.
                </td></tr>
              )}
              {items.map(r => (
                <tr key={r.id}>
                  <td><strong>{r.reviewerName}</strong></td>
                  <td style={{ color:'var(--muted)' }}>{r.consultantName}</td>
                  <td><Stars rating={r.rating} /></td>
                  <td style={{ maxWidth:260, overflow:'hidden', textOverflow:'ellipsis', whiteSpace:'nowrap', color:'var(--muted)', fontSize:12 }}>
                    {r.comment || <em>No comment</em>}
                  </td>
                  <td style={{ color:'var(--muted)', fontSize:12 }}>{fmtDate(r.createdAt)}</td>
                  <td style={{ textAlign:'right' }}>
                    <button className="btn-icon danger" onClick={() => setDelId(r.id)}><Trash2 size={13}/></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {total > PS && (
            <div style={{ display:'flex', justifyContent:'space-between', alignItems:'center', marginTop:16 }}>
              <span style={{ color:'var(--muted)', fontSize:12 }}>{total} reviews</span>
              <div className="pagination">
                {Array.from({length:Math.min(Math.ceil(total/PS),7)},(_,i)=>i+1).map(p=>(
                  <button key={p} className={`page-btn ${p===page?'active':''}`} onClick={()=>setPage(p)}>{p}</button>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      <Modal open={!!delId} title="Remove Review" onClose={() => setDelId(null)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setDelId(null)}>Cancel</button>
          <button className="btn btn-danger" onClick={del}>Remove</button>
        </>}>
        <p style={{ color:'var(--muted)' }}>Remove this review permanently? This cannot be undone.</p>
      </Modal>
    </>
  )
}
