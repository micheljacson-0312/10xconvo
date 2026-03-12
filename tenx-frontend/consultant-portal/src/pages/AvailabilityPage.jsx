import React, { useState, useEffect } from 'react'
import { toast } from 'react-hot-toast'
import { Save, Plus, Trash2, Clock, Calendar } from 'lucide-react'
import { availApi } from '../api'

const DAYS = [
  { value: 0, label: 'Sunday',    short: 'Sun' },
  { value: 1, label: 'Monday',    short: 'Mon' },
  { value: 2, label: 'Tuesday',   short: 'Tue' },
  { value: 3, label: 'Wednesday', short: 'Wed' },
  { value: 4, label: 'Thursday',  short: 'Thu' },
  { value: 5, label: 'Friday',    short: 'Fri' },
  { value: 6, label: 'Saturday',  short: 'Sat' },
]

const TIME_SLOTS = Array.from({ length: 48 }, (_, i) => {
  const h = Math.floor(i / 2).toString().padStart(2, '0')
  const m = i % 2 === 0 ? '00' : '30'
  return `${h}:${m}`
})

function newSlot() {
  return { id: Math.random(), startTime: '09:00', endTime: '17:00', isAvailable: true }
}

export default function AvailabilityPage() {
  const [schedule, setSchedule] = useState(
    DAYS.map(d => ({ ...d, isWorkDay: false, slots: [] }))
  )
  const [loading, setLoading] = useState(true)
  const [saving, setSaving]   = useState(false)

  useEffect(() => {
    const load = async () => {
      try {
        const { data } = await availApi.get()
        const serverDays = data.data || []
        setSchedule(DAYS.map(d => {
          const match = serverDays.find(s => s.dayNumber === d.value)
          return {
            ...d,
            isWorkDay: match?.isWorkDay || false,
            slots: (match?.slots || []).map(s => ({
              id:          s.id || Math.random(),
              startTime:   s.startTime,
              endTime:     s.endTime,
              isAvailable: s.isAvailable,
            }))
          }
        }))
      } catch { toast.error('Failed to load schedule') }
      finally { setLoading(false) }
    }
    load()
  }, [])

  const toggleDay = (dayIdx) => {
    setSchedule(sch => sch.map((d, i) => {
      if (i !== dayIdx) return d
      const isWorkDay = !d.isWorkDay
      return {
        ...d,
        isWorkDay,
        slots: isWorkDay && d.slots.length === 0 ? [newSlot()] : d.slots,
      }
    }))
  }

  const addSlot = (dayIdx) => {
    setSchedule(sch => sch.map((d, i) =>
      i !== dayIdx ? d : { ...d, slots: [...d.slots, newSlot()] }
    ))
  }

  const removeSlot = (dayIdx, slotId) => {
    setSchedule(sch => sch.map((d, i) => {
      if (i !== dayIdx) return d
      const slots = d.slots.filter(s => s.id !== slotId)
      return { ...d, slots, isWorkDay: slots.length > 0 }
    }))
  }

  const updateSlot = (dayIdx, slotId, field, val) => {
    setSchedule(sch => sch.map((d, i) =>
      i !== dayIdx ? d : {
        ...d,
        slots: d.slots.map(s => s.id === slotId ? { ...s, [field]: val } : s)
      }
    ))
  }

  const save = async () => {
    // Build flat list of slots
    const allSlots = []
    for (const day of schedule) {
      if (!day.isWorkDay || day.slots.length === 0) continue
      for (const slot of day.slots) {
        if (slot.startTime >= slot.endTime) {
          toast.error(`${day.label}: Start time must be before end time`)
          return
        }
        allSlots.push({ dayOfWeek: day.value, startTime: slot.startTime, endTime: slot.endTime, isAvailable: true })
      }
    }

    setSaving(true)
    try {
      await availApi.save(allSlots)
      toast.success('Schedule saved!')
    } catch (e) { toast.error(e.response?.data?.message || 'Save failed') }
    finally { setSaving(false) }
  }

  if (loading) return <div style={{ padding: 40, textAlign: 'center', color: 'var(--muted)' }}>Loading schedule…</div>

  const workDays = schedule.filter(d => d.isWorkDay).length

  return (
    <>
      <div className="page-header">
        <div>
          <h2>My Availability</h2>
          <p style={{ color: 'var(--muted)', fontSize: 13, marginTop: 2 }}>
            Set your weekly schedule — clients will see when you're available
          </p>
        </div>
        <button className="btn btn-primary" onClick={save} disabled={saving}>
          <Save size={13} /> {saving ? 'Saving…' : 'Save Schedule'}
        </button>
      </div>

      {/* Summary bar */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
        {DAYS.map((d, i) => (
          <button key={d.value} onClick={() => toggleDay(i)}
            style={{
              padding: '6px 12px', borderRadius: 20, fontSize: 12, fontWeight: 600,
              cursor: 'pointer', border: 'none', transition: 'all 0.15s',
              background: schedule[i].isWorkDay ? 'var(--accent)' : 'var(--bg3)',
              color:      schedule[i].isWorkDay ? '#fff' : 'var(--muted)',
            }}>
            {d.short}
          </button>
        ))}
        <span style={{ marginLeft: 'auto', color: 'var(--muted)', fontSize: 12, alignSelf: 'center' }}>
          {workDays} work day{workDays !== 1 ? 's' : ''} configured
        </span>
      </div>

      {/* Day cards */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {schedule.map((day, dayIdx) => (
          <div key={day.value}
            style={{
              borderRadius: 10, border: '1px solid var(--border)',
              background: day.isWorkDay ? 'var(--bg2)' : 'var(--bg)',
              overflow: 'hidden', transition: 'all 0.15s',
              opacity: day.isWorkDay ? 1 : 0.5,
            }}>
            {/* Day header */}
            <div style={{
              display: 'flex', alignItems: 'center', justifyContent: 'space-between',
              padding: '12px 16px',
              borderBottom: day.isWorkDay ? '1px solid var(--border)' : 'none',
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                {/* Toggle */}
                <div onClick={() => toggleDay(dayIdx)}
                  style={{
                    width: 36, height: 20, borderRadius: 10, cursor: 'pointer', transition: 'all 0.2s',
                    background: day.isWorkDay ? 'var(--accent)' : 'var(--border)', position: 'relative',
                  }}>
                  <div style={{
                    position: 'absolute', top: 2, left: day.isWorkDay ? 18 : 2,
                    width: 16, height: 16, borderRadius: '50%', background: '#fff', transition: 'all 0.2s',
                  }} />
                </div>
                <span style={{ fontWeight: 600, fontSize: 14 }}>{day.label}</span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                {day.isWorkDay && (
                  <span style={{ fontSize: 12, color: 'var(--muted)' }}>
                    <Clock size={11} /> {day.slots.length} slot{day.slots.length !== 1 ? 's' : ''}
                  </span>
                )}
                {day.isWorkDay && (
                  <button className="btn btn-ghost" style={{ fontSize: 11, padding: '3px 8px' }}
                    onClick={() => addSlot(dayIdx)}>
                    <Plus size={11} /> Add Slot
                  </button>
                )}
              </div>
            </div>

            {/* Slots */}
            {day.isWorkDay && (
              <div style={{ padding: '10px 16px', display: 'flex', flexDirection: 'column', gap: 8 }}>
                {day.slots.map(slot => (
                  <div key={slot.id} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <Clock size={13} color="var(--muted)" />
                    <select className="input" style={{ maxWidth: 110, fontSize: 13, padding: '5px 8px' }}
                      value={slot.startTime} onChange={e => updateSlot(dayIdx, slot.id, 'startTime', e.target.value)}>
                      {TIME_SLOTS.map(t => <option key={t} value={t}>{t}</option>)}
                    </select>
                    <span style={{ color: 'var(--muted)', fontSize: 13 }}>to</span>
                    <select className="input" style={{ maxWidth: 110, fontSize: 13, padding: '5px 8px' }}
                      value={slot.endTime} onChange={e => updateSlot(dayIdx, slot.id, 'endTime', e.target.value)}>
                      {TIME_SLOTS.map(t => <option key={t} value={t}>{t}</option>)}
                    </select>
                    <span style={{ fontSize: 12, color: 'var(--muted)' }}>
                      {(() => {
                        const [sh, sm] = slot.startTime.split(':').map(Number)
                        const [eh, em] = slot.endTime.split(':').map(Number)
                        const mins = (eh * 60 + em) - (sh * 60 + sm)
                        if (mins <= 0) return '⚠️ invalid'
                        const h = Math.floor(mins / 60), m = mins % 60
                        return `${h ? h + 'h ' : ''}${m ? m + 'm' : ''}`
                      })()}
                    </span>
                    {day.slots.length > 1 && (
                      <button className="btn-icon danger" onClick={() => removeSlot(dayIdx, slot.id)}>
                        <Trash2 size={12} />
                      </button>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        ))}
      </div>
    </>
  )
}
