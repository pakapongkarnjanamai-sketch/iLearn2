import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  BookOpen,
  Download,
  ArrowUpDown,
  ArrowLeft,
  Users,
  CheckCircle2,
  AlertTriangle,
  Percent,
  Layers,
  HelpCircle,
  Info,
} from 'lucide-react'
import { Card } from '../../components/ui/Card'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { LoadingState } from '../../components/ui/LoadingState'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { AppButton } from '../../components/ui/AppButton'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { Modal } from '../../components/ui/Modal'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatPercent, formatNumber } from '../../lib/format'
import { exportRowsAsCsv } from '../../lib/csvExport'
import { DETAIL_TABLE_CHUNK_SIZE } from '../../lib/tableStandards'
import { toast } from '../../lib/toast'
import type { CourseSummaryReportDto } from './reportTypes'

type SortKey =
  | 'code'
  | 'title'
  | 'categoryName'
  | 'assignmentCount'
  | 'enrolledLearners'
  | 'completedCount'
  | 'overdueCount'
  | 'avgProgress'
  | 'completionRate'
  | 'avgScore'

export function CourseSummaryReportPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<CourseSummaryReportDto | null>(null)
  const [search, setSearch] = useState('')
  const [sortKey, setSortKey] = useState<SortKey>('completionRate')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')
  const [visibleRows, setVisibleRows] = useState(DETAIL_TABLE_CHUNK_SIZE)
  const [isHelpModalOpen, setIsHelpModalOpen] = useState(false)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    fetchWithAccessControl<{ success: boolean; data: CourseSummaryReportDto }>('Reports/course-summary')
      .then((resp) => {
        if (cancelled) return
        if (resp.success) {
          setData(resp.data)
        }
      })
      .catch(() => toast.error('Failed to load course summary report'))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    setVisibleRows(DETAIL_TABLE_CHUNK_SIZE)
  }, [search])

  const handleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortKey(key)
      setSortOrder('asc')
    }
  }

  const renderSortIndicator = (key: SortKey) => {
    if (sortKey !== key) {
      return <ArrowUpDown className="inline-block ml-1 h-3 w-3 text-slate-300" />
    }
    return sortOrder === 'asc' ? ' ▲' : ' ▼'
  }

  const summaryStats = useMemo(() => {
    if (!data || data.rows.length === 0) {
      return {
        totalCourses: 0,
        totalEnrolled: 0,
        totalCompleted: 0,
        totalOverdue: 0,
        avgCompletionRate: 0,
      }
    }
    const totalCourses = data.rows.length
    const totalEnrolled = data.rows.reduce((acc, r) => acc + r.enrolledLearners, 0)
    const totalCompleted = data.rows.reduce((acc, r) => acc + r.completedCount, 0)
    const totalOverdue = data.rows.reduce((acc, r) => acc + r.overdueCount, 0)
    const avgCompletionRate = totalEnrolled > 0 ? (totalCompleted / totalEnrolled) * 100 : 0

    return {
      totalCourses,
      totalEnrolled,
      totalCompleted,
      totalOverdue,
      avgCompletionRate,
    }
  }, [data])

  const sortedRows = useMemo(() => {
    if (!data) return []
    return [...data.rows].sort((a, b) => {
      const aVal = a[sortKey]
      const bVal = b[sortKey]

      if (aVal === null || aVal === undefined) return sortOrder === 'asc' ? 1 : -1
      if (bVal === null || bVal === undefined) return sortOrder === 'asc' ? -1 : 1

      if (typeof aVal === 'string' && typeof bVal === 'string') {
        return sortOrder === 'asc' ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal)
      }

      // numeric sorting
      return sortOrder === 'asc' ? (aVal as number) - (bVal as number) : (bVal as number) - (aVal as number)
    })
  }, [data, sortKey, sortOrder])

  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return sortedRows
    return sortedRows.filter((r) =>
      [r.code, r.title, r.categoryName]
        .filter(Boolean)
        .some((val) => val!.toLowerCase().includes(q))
    )
  }, [sortedRows, search])

  const visibleCourseRows = useMemo(
    () => filteredRows.slice(0, visibleRows),
    [filteredRows, visibleRows]
  )

  const handleExportCsv = () => {
    if (!data || data.rows.length === 0) {
      toast.info('No course records to export')
      return
    }
    const header = [
      'Course Code',
      'Course Title',
      'Category',
      'Assignments',
      'Enrolled Learners',
      'Completed Count',
      'Overdue Count',
      'Avg Progress %',
      'Completion Rate %',
      'Avg Score',
    ]
    const body = data.rows.map((r) => [
      r.code ?? '',
      r.title ?? '',
      r.categoryName ?? '',
      r.assignmentCount,
      r.enrolledLearners,
      r.completedCount,
      r.overdueCount,
      formatPercent(r.avgProgress).replace('%', ''),
      formatPercent(r.completionRate).replace('%', ''),
      r.avgScore !== null && r.avgScore !== undefined ? formatNumber(r.avgScore) : '',
    ])
    const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    exportRowsAsCsv(`course-summary-report-${stamp}.csv`, header, body)
  }

  if (loading) {
    return <LoadingState label="Loading course summary report..." />
  }

  if (!data) {
    return (
      <div className="py-12 text-center text-slate-500 font-semibold">
        No report data available.
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      {/* Header with Navigation & Help Guide Action */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-col gap-2">
          <Link
            to="/reports"
            className="inline-flex items-center gap-1.5 text-xs font-bold text-slate-500 hover:text-indigo-600 w-fit transition-colors"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            <span>Back to Report Hub</span>
          </Link>
          <SectionHeader icon={BookOpen}>Course Completion Summary</SectionHeader>
        </div>

        <AppButton
          onClick={() => setIsHelpModalOpen(true)}
          icon={HelpCircle}
          variant="secondary"
          size="sm"
        >
          Metrics Guide
        </AppButton>
      </div>

      {/* KPI Summary Grid */}
      <section className="grid grid-cols-2 lg:grid-cols-5 gap-4">
        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              Total Courses
            </span>
            <Layers className="h-4 w-4 text-slate-400" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight mt-1">
            {formatNumber(summaryStats.totalCourses)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">Catalog courses</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              Enrolled Learners
            </span>
            <Users className="h-4 w-4 text-blue-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-slate-800 tabular-nums leading-tight mt-1">
            {formatNumber(summaryStats.totalEnrolled)}
          </div>
          <span className="text-xxs text-slate-400 font-medium">Cumulative enrollments</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              Completed
            </span>
            <CheckCircle2 className="h-4 w-4 text-emerald-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-emerald-600 tabular-nums leading-tight mt-1">
            {formatNumber(summaryStats.totalCompleted)}
          </div>
          <span className="text-xxs text-emerald-600 font-medium">Finished completions</span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <span className="text-xxs font-extrabold text-slate-400 uppercase tracking-wider">
              Overdue Count
            </span>
            <AlertTriangle
              className={`h-4 w-4 ${summaryStats.totalOverdue > 0 ? 'text-rose-500' : 'text-slate-400'}`}
              aria-hidden="true"
            />
          </div>
          <div
            className={`text-2xl font-extrabold tabular-nums leading-tight mt-1 ${
              summaryStats.totalOverdue > 0 ? 'text-rose-600' : 'text-slate-800'
            }`}
          >
            {formatNumber(summaryStats.totalOverdue)}
          </div>
          <span className="text-xxs text-rose-600 font-semibold">
            {summaryStats.totalOverdue > 0 ? 'Action required' : 'Zero overdue'}
          </span>
        </Card>

        <Card bodyClassName="p-4 flex flex-col gap-1">
          <div className="flex items-center justify-between">
            <button
              type="button"
              onClick={() => setIsHelpModalOpen(true)}
              className="flex items-center gap-1 text-xxs font-extrabold text-slate-400 hover:text-indigo-600 uppercase tracking-wider text-left transition-colors"
            >
              <span>Avg Completion Rate</span>
              <Info className="h-3 w-3 text-indigo-400" />
            </button>
            <Percent className="h-4 w-4 text-indigo-500" aria-hidden="true" />
          </div>
          <div className="text-2xl font-extrabold text-indigo-600 tabular-nums leading-tight mt-1">
            {formatPercent(summaryStats.avgCompletionRate)}
          </div>
          <span className="text-xxs text-indigo-600 font-medium">Weighted completion</span>
        </Card>
      </section>

      {/* Courses Performance List */}
      <Card
        title="Courses Performance List"
        actions={
          data.rows.length > 0 && (
            <AppButton
              onClick={handleExportCsv}
              icon={Download}
              variant="secondary"
              size="sm"
            >
              Export CSV
            </AppButton>
          )
        }
      >
        <div className="border-b border-slate-100 bg-slate-50/20 px-5">
          <ListToolbar
            searchValue={search}
            onSearchChange={setSearch}
            searchPlaceholder="Search course code, title or category..."
          />
        </div>

        <div className="overflow-x-auto custom-scrollbar">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs select-none">
                <th
                  className="p-3 pl-5 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('code')}
                >
                  Code{renderSortIndicator('code')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('title')}
                >
                  Title{renderSortIndicator('title')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('categoryName')}
                >
                  Category{renderSortIndicator('categoryName')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('assignmentCount')}
                >
                  Assignments{renderSortIndicator('assignmentCount')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('enrolledLearners')}
                >
                  Learners{renderSortIndicator('enrolledLearners')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('completedCount')}
                >
                  Completed{renderSortIndicator('completedCount')}
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('overdueCount')}
                >
                  Overdue{renderSortIndicator('overdueCount')}
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('avgProgress')}
                >
                  <div className="flex items-center gap-1" onClick={(e) => { e.stopPropagation(); setIsHelpModalOpen(true); }}>
                    <span>Avg Progress</span>
                    <Info className="h-3 w-3 text-slate-400 hover:text-indigo-600" />
                    {renderSortIndicator('avgProgress')}
                  </div>
                </th>
                <th
                  className="p-3 cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('completionRate')}
                >
                  <div className="flex items-center gap-1" onClick={(e) => { e.stopPropagation(); setIsHelpModalOpen(true); }}>
                    <span>Completion Rate</span>
                    <Info className="h-3 w-3 text-slate-400 hover:text-indigo-600" />
                    {renderSortIndicator('completionRate')}
                  </div>
                </th>
                <th
                  className="p-3 text-center cursor-pointer hover:bg-slate-100/70 transition duration-100"
                  onClick={() => handleSort('avgScore')}
                >
                  Avg Score{renderSortIndicator('avgScore')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {visibleCourseRows.map((row) => (
                <tr key={row.courseId} className="hover:bg-slate-50/50 transition duration-100">
                  <td className="p-3 pl-5 text-xs font-mono font-bold text-slate-700 select-all">
                    {row.code || '—'}
                  </td>
                  <td className="p-3 text-xs font-bold text-slate-800">
                    {row.title || '—'}
                  </td>
                  <td className="p-3 text-xs font-semibold text-slate-500">
                    {row.categoryName || '—'}
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {formatNumber(row.assignmentCount)}
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {formatNumber(row.enrolledLearners)}
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {formatNumber(row.completedCount)}
                  </td>
                  <td className="p-3 text-center text-xs font-bold tabular-nums">
                    <span className={row.overdueCount > 0 ? 'text-rose-600' : 'text-slate-400'}>
                      {row.overdueCount}
                    </span>
                  </td>
                  <td className="p-3">
                    <div className="flex items-center gap-2">
                      <ProgressBar value={row.avgProgress} completed={row.completionRate >= 100} />
                    </div>
                  </td>
                  <td className="p-3">
                    <div className="flex items-center gap-3">
                      <ProgressBar value={row.completionRate} completed={row.completionRate >= 100} maxWidthClass="max-w-20" />
                      <span className="text-xxs font-bold text-slate-500 tabular-nums">
                        {formatPercent(row.completionRate)}
                      </span>
                    </div>
                  </td>
                  <td className="p-3 text-center text-xs font-semibold tabular-nums">
                    {row.avgScore !== null && row.avgScore !== undefined ? formatNumber(row.avgScore) : '—'}
                  </td>
                </tr>
              ))}
              {filteredRows.length === 0 && (
                <tr>
                  <td colSpan={10} className="p-6 text-center text-slate-400 text-xs font-medium">
                    No course records found
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {filteredRows.length > visibleCourseRows.length && (
          <div className="border-t border-slate-100 p-3 text-center">
            <AppButton
              variant="secondary"
              size="sm"
              onClick={() => setVisibleRows((v) => v + DETAIL_TABLE_CHUNK_SIZE)}
            >
              Load more
            </AppButton>
          </div>
        )}
      </Card>

      {/* Metrics Guide Help Popup Modal */}
      <Modal
        open={isHelpModalOpen}
        onClose={() => setIsHelpModalOpen(false)}
        title="Course Metrics Guide / คำอธิบายตัววัดสถิติ"
        size="lg"
      >
        <div className="p-6 flex flex-col gap-6 text-xs text-slate-700 leading-relaxed max-h-[75vh] overflow-y-auto custom-scrollbar">
          {/* Section 1: Completion Rate */}
          <div className="p-4 rounded-xl bg-indigo-50/50 border border-indigo-100 flex flex-col gap-2">
            <div className="flex items-center gap-2 text-indigo-700 font-extrabold text-sm">
              <Percent className="h-4 w-4" />
              <span>Completion Rate (%) — อัตราผู้เรียนสำเร็จ</span>
            </div>
            <p className="text-slate-600">
              วัดจากสัดส่วนเปอร์เซ็นต์ของผู้เรียนที่ <strong>เรียนจบตามเกณฑ์ 100% (Completed)</strong> แล้ว
              เปรียบเทียบกับจำนวนผู้เรียนทั้งหมดที่ถูกมอบหมายในคอร์สนั้น
            </p>
            <div className="bg-white p-2.5 rounded-lg border border-indigo-100 font-mono text-xxs font-bold text-indigo-800">
              สูตร: (จำนวนผู้เรียนที่สำเร็จ ÷ จำนวนผู้เรียนทั้งหมดที่ถูกมอบหมาย) × 100
            </div>
          </div>

          {/* Section 2: Avg Progress */}
          <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 flex flex-col gap-2">
            <div className="flex items-center gap-2 text-slate-800 font-extrabold text-sm">
              <BookOpen className="h-4 w-4 text-indigo-600" />
              <span>Avg Progress (%) — ความคืบหน้าบทเรียนเฉลี่ย</span>
            </div>
            <p className="text-slate-600">
              วัดจากค่าเฉลี่ยของ <strong>เปอร์เซ็นต์เนื้อหาบทเรียนที่ผู้เรียนทุกคนสะสมมาได้</strong> แม้ผู้เรียนจะยังเรียนไม่เสร็จ 100%
              ก็นำ % ความคืบหน้าปัจจุบันมาร่วมคิดค่าเฉลี่ยด้วย
            </p>
            <div className="bg-white p-2.5 rounded-lg border border-slate-200 font-mono text-xxs font-bold text-slate-700">
              สูตร: ผลรวม % ความคืบหน้าของผู้เรียนทุกคน ÷ จำนวนผู้เรียนทั้งหมด
            </div>
          </div>

          {/* Section 3: Comparison Example */}
          <div className="border border-amber-200 bg-amber-50/40 rounded-xl p-4 flex flex-col gap-3">
            <h4 className="font-bold text-amber-900 text-xs uppercase tracking-wider">
              💡 ตัวอย่างเปรียบเทียบความแตกต่าง (Comparison Example)
            </h4>
            <p className="text-amber-800">
              สมมติคอร์สมีผู้เรียน 2 คน:
              <br />
              • <strong>นาย A:</strong> เรียนเนื้อหาไปได้ 90% (ยังไม่เสร็จสิ้น)
              <br />
              • <strong>นาย B:</strong> เรียนเนื้อหาครบ 100% (เรียนจบแล้ว - Completed)
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-1">
              <div className="bg-white p-3 rounded-lg border border-amber-200 flex flex-col gap-1">
                <span className="font-bold text-slate-500 text-xxs uppercase">Avg Progress</span>
                <span className="text-base font-extrabold text-slate-800 tabular-nums">95.0%</span>
                <span className="text-xxs text-slate-500">(90% + 100%) ÷ 2 = ความคืบหน้าสะสมสูงมาก</span>
              </div>
              <div className="bg-white p-3 rounded-lg border border-amber-200 flex flex-col gap-1">
                <span className="font-bold text-slate-500 text-xxs uppercase">Completion Rate</span>
                <span className="text-base font-extrabold text-indigo-600 tabular-nums">50.0%</span>
                <span className="text-xxs text-slate-500">นับเฉพาะ นาย B คนเดียวที่สำเร็จ 100%</span>
              </div>
            </div>
          </div>

          {/* Section 4: Other Metrics */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="p-3.5 rounded-lg border border-slate-200 bg-white">
              <span className="font-bold text-slate-800 block mb-1">Overdue Count</span>
              <p className="text-slate-500 text-xxs leading-relaxed">
                จำนวนผู้เรียนที่เกินกำหนดวันส่งงาน/วันสิ้นสุดการเรียน (Due Date) แต่ยังเรียนไม่สำเร็จ
              </p>
            </div>
            <div className="p-3.5 rounded-lg border border-slate-200 bg-white">
              <span className="font-bold text-slate-800 block mb-1">Avg Score</span>
              <p className="text-slate-500 text-xxs leading-relaxed">
                คะแนนสอบ/แบบทดสอบเฉลี่ยของผู้เรียนทุกคนที่มีผลคะแนนในคอร์สนั้น
              </p>
            </div>
          </div>

          <div className="flex justify-end pt-2">
            <AppButton onClick={() => setIsHelpModalOpen(false)} variant="primary" size="sm">
              Got It / เข้าใจแล้ว
            </AppButton>
          </div>
        </div>
      </Modal>
    </div>
  )
}
