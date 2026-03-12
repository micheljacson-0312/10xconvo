import React, { useState, useEffect } from 'react'
import { toast } from 'react-hot-toast'
import { Star, Pencil, Trash2, X, MessageSquare } from 'lucide-react'
import { reviewApi } from '../api'

function Modal({ open, title, onClose, children, footer }) {
  if (!open) return null
  return (
    <div className="modal-overlay" onClick={e => e.target===e.currentTarget&&onClose()}>
      <div className="modal">
        <div className="modal-header"><h3>{title}</h3><button className="btn-icon" onClick={onClose}><X size={14}/></button></div>
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-footer">{footer}</div>}
      </div>
    </div>
  )
}

function StarPicker({ value, onChange }) {
  const [hover, setHover] = useState(0)
  return (
    <div style={{ display:'flex', gap:4 }}>
      {[1,2,3,4,5].map(s => (
        <button key={s} onClick={() => onChange(s)}
          onMouseEnter={() => setHover(s)} onMouseLeave={() => setHover(0)}
          style={{ background:'none', border:'none', cursor:'pointer', padding:2 }}>
          <Star size={28} fill={(hover||value) >= s ? '#f7c948' : 'none'}
            color={(hover||value) >= s ? '#f7c948' : 'var(--border)'} />
        </button>
      ))}
    </div>
  )
}

function StarDisplay({ rating, size=14 }) {
  return (
    <div style={{ display:'flex', gap:2 }}>
      {[1,2,3,4,5].map(s => (
        <Star key={s} size={size} fill={s<=rating?'#f7c948':'none'} color={s<=rating?'#f7c948':'var(--border)'} />
      ))}
    </div>
  )
}

// ── Main Component — pass consultantUserId as prop or via URL param
export default function ReviewsPage({ consultantUserId, consultantName }) {
  const [reviews, setReviews]   = useState([])
  const [avg, setAvg]           = useState(0)
  const [total, setTotal]       = useState(0)
  const [breakdown, setBreakdown] = useState([])
  const [page, setPage]         = useState(1)
  const [loading, setLoading]   = useState(true)
  const [modal, setModal]       = useState(false)
  const [editing, setEditing]   = useState(null) // {id, rating, comment}
  const [form, setForm]         = useState({ rating: 0, comment: '' })
  const [saving, setSaving]     = useState(false)
  const [delId, setDelId]       = useState(null)
  const PS = 10

  const load = async () => {
    if (!consultantUserId) return
    setLoading(true)
    try {
      const { data } = await reviewApi.getAll(consultantUserId, page, PS)
      setReviews(data.data?.items || [])
      setAvg(data.data?.averageRating || 0)
      setTotal(data.data?.totalRecords || 0)
      setBreakdown(data.data?.ratingBreakdown || [])
    } catch {} finally { setLoading(false) }
  }
  useEffect(() => { load() }, [consultantUserId, page])

  const openCreate = () => { setEditing(null); setForm({ rating:0, comment:'' }); setModal(true) }
  const openEdit   = r  => { setEditing(r); setForm({ rating:r.rating, comment:r.comment||'' }); setModal(true) }

  const save = async () => {
    if (!form.rating) return toast.error('Please select a rating')
    setSaving(true)
    try {
      if (editing) await reviewApi.update(consultantUserId, editing.id, form)
      else         await reviewApi.create(consultantUserId, form)
      toast.success(editing ? 'Review updated!' : 'Review submitted!')
      setModal(false); load()
    } catch (e) { toast.error(e.response?.data?.message || 'Failed') }
    finally { setSaving(false) }
  }

  const del = async () => {
    try { await reviewApi.delete(consultantUserId, delId); toast.success('Review removed'); setDelId(null); load() }
    catch (e) { toast.error(e.response?.data?.message || 'Failed') }
  }

  const fmtDate = d => d ? new Date(d).toLocaleDateString('en-GB', {day:'2-digit',month:'short',year:'numeric'}) : '—'

  const maxBreakdown = Math.max(...breakdown.map(b => b.count), 1)

  if (!consultantUserId) return (
    <div style={{ padding:40, textAlign:'center', color:'var(--muted)' }}>
      Select a consultant to view reviews.
    </div>
  )

  return (
    <div>
      {/* Summary */}
      <div style={{ display:'grid', gridTemplateColumns:'auto 1fr', gap:20, marginBottom:20 }}>
        <div style={{ textAlign:'center', padding:'20px 28px', background:'var(--bg2)',
          borderRadius:12, border:'1px solid var(--border)' }}>
          <div style={{ fontSize:52, fontWeight:900, lineHeight:1, color:'var(--text)',
            fontFamily:'var(--font-head)' }}>{avg}</div>
          <StarDisplay rating={Math.round(avg)} size={16} />
          <div style={{ color:'var(--muted)', fontSize:12, marginTop:6 }}>{total} review{total!==1?'s':''}</div>
        </div>
        <div style={{ padding:'16px 20px', background:'var(--bg2)', borderRadius:12, border:'1px solid var(--border)' }}>
          {[5,4,3,2,1].map(s => {
            const b = breakdown.find(x => x.stars === s)
            const count = b?.count || 0
            const pct = Math.round((count / Math.max(total,1)) * 100)
            return (
              <div key={s} style={{ display:'flex', alignItems:'center', gap:8, marginBottom:6 }}>
                <span style={{ fontSize:12, color:'var(--muted)', width:20, textAlign:'right' }}>{s}★</span>
                <div style={{ flex:1, height:6, background:'var(--bg3)', borderRadius:3, overflow:'hidden' }}>
                  <div style={{ width:`${pct}%`, height:'100%', background:'#f7c948', borderRadius:3, transition:'width 0.5s' }}/>
                </div>
                <span style={{ fontSize:11, color:'var(--muted)', width:28 }}>{count}</span>
              </div>
            )
          })}
        </div>
      </div>

      {/* Action */}
      <div style={{ display:'flex', justifyContent:'space-between', alignItems:'center', marginBottom:14 }}>
        <h4 style={{ fontSize:14 }}>Reviews {total > 0 && `(${total})`}</h4>
        <button className="btn btn-primary" style={{ fontSize:12 }} onClick={openCreate}>
          <Star size={12}/> Write a Review
        </button>
      </div>

      {/* List */}
      {loading ? <div style={{ padding:24, textAlign:'center', color:'var(--muted)' }}>Loading…</div> : (
        <div style={{ display:'flex', flexDirection:'column', gap:10 }}>
          {reviews.length === 0 && (
            <div style={{ textAlign:'center', padding:32 }}>
              <MessageSquare size={28} color="var(--border)" style={{ marginBottom:10 }} />
              <p style={{ color:'var(--muted)', fontSize:13 }}>No reviews yet. Be the first!</p>
            </div>
          )}
          {reviews.map(r => (
            <div key={r.id} style={{
              padding:'14px 16px', background:'var(--bg2)',
              borderRadius:10, border:'1px solid var(--border)',
            }}>
              <div style={{ display:'flex', justifyContent:'space-between', alignItems:'flex-start' }}>
                <div>
                  <div style={{ display:'flex', alignItems:'center', gap:10, marginBottom:6 }}>
                    <div style={{ width:32, height:32, borderRadius:'50%', background:'var(--accent)',
                      display:'flex', alignItems:'center', justifyContent:'center',
                      color:'#fff', fontWeight:700, fontSize:13 }}>
                      {r.reviewerName?.charAt(0)?.toUpperCase() || '?'}
                    </div>
                    <div>
                      <div style={{ fontWeight:600, fontSize:13 }}>{r.reviewerName}</div>
                      <StarDisplay rating={r.rating} size={12} />
                    </div>
                  </div>
                  {r.comment && <p style={{ color:'var(--muted)', fontSize:13, marginLeft:42 }}>{r.comment}</p>}
                </div>
                <div style={{ display:'flex', flexDirection:'column', alignItems:'flex-end', gap:8 }}>
                  <span style={{ color:'var(--muted)', fontSize:11 }}>{fmtDate(r.createdAt)}</span>
                  {r.isMyReview && (
                    <div style={{ display:'flex', gap:4 }}>
                      <button className="btn-icon" onClick={() => openEdit(r)}><Pencil size={12}/></button>
                      <button className="btn-icon danger" onClick={() => setDelId(r.id)}><Trash2 size={12}/></button>
                    </div>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {total > PS && (
        <div style={{ display:'flex', justifyContent:'center', gap:4, marginTop:12 }}>
          {Array.from({length:Math.ceil(total/PS)},(_,i)=>i+1).slice(0,7).map(p=>(
            <button key={p} className={`page-btn ${p===page?'active':''}`} onClick={()=>setPage(p)}>{p}</button>
          ))}
        </div>
      )}

      {/* Write/Edit Modal */}
      <Modal open={modal} title={editing ? 'Edit Your Review' : 'Write a Review'} onClose={() => setModal(false)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setModal(false)}>Cancel</button>
          <button className="btn btn-primary" onClick={save} disabled={saving || !form.rating}>
            {saving ? 'Saving…' : 'Submit Review'}
          </button>
        </>}>
        <div className="form-group">
          <label>Your Rating</label>
          <StarPicker value={form.rating} onChange={r => setForm(f=>({...f,rating:r}))} />
          {!form.rating && <small style={{ color:'var(--accent2)' }}>Please select a rating</small>}
        </div>
        <div className="form-group">
          <label>Comment <span style={{ color:'var(--muted)' }}>(optional)</span></label>
          <textarea className="input" rows={4} placeholder="Share your experience…"
            value={form.comment} onChange={e => setForm(f=>({...f,comment:e.target.value}))} />
        </div>
      </Modal>

      <Modal open={!!delId} title="Remove Review" onClose={() => setDelId(null)}
        footer={<>
          <button className="btn btn-ghost" onClick={() => setDelId(null)}>Cancel</button>
          <button className="btn btn-danger" onClick={del}>Remove</button>
        </>}>
        <p style={{ color:'var(--muted)' }}>Remove your review? This cannot be undone.</p>
      </Modal>
    </div>
  )
}
