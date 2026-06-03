import React, { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { toast } from 'react-hot-toast'
import { Clock } from 'lucide-react'
import { availApi } from '../api'

export default function ConsultantAvailabilityPage() {
  const { consultantId } = useParams()
  const [slots, setSlots]     = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!consultantId) return
    availApi.get(consultantId)
      .then(r => setSlots(r.data.data))
      .catch(() => toast.error('Load failed'))
      .finally(() => setLoading(false))
  }, [consultantId])

  const byDay = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'].map((day, i) => ({
    day, slots: slots.filter(s => s.dayNumber === i)
  }))

  return (
    <div style={{ maxWidth: 500, margin: '0 auto' }}>
      <div className="page-header"><h2>Available Times</h2></div>
      {loading ? <div className="spinner" /> : (
        <div className="card" style={{ padding: 0 }}>
          {byDay.map(({ day, slots: ds }) => (
            <div key={day} style={{ padding: '12px 20px', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 16 }}>
              <span style={{ width: 100, fontWeight: ds.length ? 600 : 400, color: ds.length ? 'var(--text)' : 'var(--muted)', fontSize: 14 }}>{day}</span>
              {ds.length === 0 ? (
                <span style={{ fontSize: 13, color: 'var(--muted)' }}>Not available</span>
              ) : (
                <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                  {ds.map((s, i) => (
                    <span key={i} style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 13, background: 'var(--accent)15', color: 'var(--accent)', padding: '3px 10px', borderRadius: 20, fontWeight: 600 }}>
                      <Clock size={11} /> {s.startTime} — {s.endTime}
                    </span>
                  ))}
                </div>
              )}
            </div>
          ))}
          {slots.length === 0 && !loading && (
            <div style={{ textAlign: 'center', padding: 32, color: 'var(--muted)', fontSize: 14 }}>No schedule set yet</div>
          )}
        </div>
      )}
    </div>
  )
}
