// TEMPORARY review harness — delete after verifying the month-zoom rework.
import { createRoot } from 'react-dom/client'
import { MemoryRouter } from 'react-router-dom'
import './index.css'
import { AssignmentGanttPage } from './pages/assignments/AssignmentGanttPage'

const iso = (offsetDays: number) => {
  const d = new Date()
  d.setHours(0, 0, 0, 0)
  d.setDate(d.getDate() + offsetDays)
  return d.toISOString()
}

const task = (id: number, no: string, title: string, from: number, to: number) => ({
  id,
  parentId: 0,
  assignmentNo: no,
  title,
  startDate: iso(from),
  dueDate: iso(to),
  progress: 0,
  color: '#1890ff',
  status: 'InProgress',
})

// Mirrors the QA set: 12 batches, one of them (PLAN-079) spanning far past the viewport.
const TASKS = [
  task(1, 'AS-20260721-002', 'AS-20260721-002', -9, 22),
  task(2, 'AS-20260721-001', 'aaaa', -18, -2),
  task(3, 'AS-20260716-004', 'AS-20260716-004', -17, 8),
  task(4, 'AS-20260716-003', 'AAAAA', -16, 9),
  task(5, 'AS-20260716-002', 'AS-20260716-002', -16, 8),
  task(6, 'AS-20260716-001', 'AS-20260716-001', -16, 8),
  task(7, 'AS-20260713-002', 'PLAN-079 E2E SCORM Conformance Test', -19, 120),
  task(8, 'AS-20260702-006', 'Training_Common PD1_2 Revise(Record OK)', -28, -3),
  task(9, 'AS-20260702-005', 'คู่มือการปฏิบัติงานตามหลัก TWI', -28, -3),
  task(10, 'AS-20260702-003', 'KSN_Raising quality awarens', -28, -3),
  task(11, 'AS-20260702-002', 'Training WI_PD2', -28, -3),
  task(12, 'AS-20260702-001', 'Training_Common PD1_2', -28, -3),
]

window.fetch = (async () =>
  new Response(JSON.stringify(TASKS), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  })) as typeof window.fetch

createRoot(document.getElementById('root')!).render(
  <div className="flex h-[700px] flex-col p-4">
    <MemoryRouter>
      <AssignmentGanttPage />
    </MemoryRouter>
  </div>,
)
