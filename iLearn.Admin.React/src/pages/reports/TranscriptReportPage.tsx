import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  GraduationCap,
  Printer,
  Search,
  BookOpen,
  X,
  UserCheck,
} from 'lucide-react'
import { Card } from '../../components/ui/Card'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { ProgressBar } from '../../components/ui/ProgressBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { AppButton } from '../../components/ui/AppButton'
import { Badge } from '../../components/ui/Badge'
import { ListToolbar } from '../../components/ui/ListToolbar'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { formatDate, formatPercent, formatDuration, formatNumber } from '../../lib/format'
import { DASHBOARD_LABELS, NAV_LABELS, REPORT_LABELS, learnerStatusLabel, t, tf } from '../../lib/labels'
import { DETAIL_TABLE_CHUNK_SIZE } from '../../lib/tableStandards'
import { toast } from '../../lib/toast'
import type { TranscriptReportDto } from './reportTypes'

export function TranscriptReportPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const codeParam = searchParams.get('code') || ''

  const [inputCode, setInputCode] = useState(codeParam)
  const [loading, setLoading] = useState(false)
  const [data, setData] = useState<TranscriptReportDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [hasSearched, setHasSearched] = useState(false)
  const [recordSearch, setRecordSearch] = useState('')
  const [visibleRows, setVisibleRows] = useState(DETAIL_TABLE_CHUNK_SIZE)

  useEffect(() => {
    setInputCode(codeParam)
    if (!codeParam) {
      setData(null)
      setError(null)
      setHasSearched(false)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)
    setHasSearched(true)

    fetchWithAccessControl<{ success: boolean; data: TranscriptReportDto }>(`Reports/transcript/${codeParam}`)
      .then((resp) => {
        if (cancelled) return
        if (resp.success && resp.data) {
          setData(resp.data)
        } else {
          setData(null)
          setError(t(REPORT_LABELS.trNotFoundTitle))
        }
      })
      .catch((err) => {
        if (cancelled) return
        setData(null)
        setError(err.message || t(REPORT_LABELS.trNotFoundTitle))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [codeParam])

  useEffect(() => {
    setVisibleRows(DETAIL_TABLE_CHUNK_SIZE)
  }, [recordSearch])

  const filteredTranscriptRows = useMemo(() => {
    if (!data) return []
    const q = recordSearch.trim().toLowerCase()
    if (!q) return data.rows
    return data.rows.filter((r) =>
      [r.courseCode, r.courseTitle, r.status, r.assignmentNo]
        .filter(Boolean)
        .some((val) => val!.toLowerCase().includes(q))
    )
  }, [data, recordSearch])

  const visibleTranscriptRows = useMemo(() => {
    return filteredTranscriptRows.slice(0, visibleRows)
  }, [filteredTranscriptRows, visibleRows])

  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const target = e.currentTarget
    const threshold = target.scrollHeight - target.scrollTop - target.clientHeight
    if (threshold <= 60 && visibleRows < filteredTranscriptRows.length) {
      setVisibleRows((prev) => prev + DETAIL_TABLE_CHUNK_SIZE)
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const trimmed = inputCode.trim()
    if (!trimmed) {
      toast.info(t(REPORT_LABELS.trEnterCode))
      return
    }
    setSearchParams({ code: trimmed })
    setVisibleRows(DETAIL_TABLE_CHUNK_SIZE)
  }

  const handleClearSearch = () => {
    setInputCode('')
    setSearchParams({})
    setData(null)
    setError(null)
    setHasSearched(false)
  }

  const handlePrint = () => {
    if (!data) return
    setVisibleRows(data.rows.length)
    setTimeout(() => window.print(), 150)
  }

  return (
    <div className="h-full flex flex-col min-h-0 gap-5 overflow-auto custom-scrollbar">
      {/* Search Header Card */}
      <Card
        title={t(REPORT_LABELS.trSearchTitle)}
        icon={GraduationCap}
        className="shrink-0 print:hidden"
        actions={
          <form onSubmit={handleSubmit} className="flex items-center gap-2">
            <div className="relative">
              <input
                type="text"
                placeholder={t(REPORT_LABELS.trSearchPlaceholder)}
                value={inputCode}
                onChange={(e) => setInputCode(e.target.value)}
                className="appearance-none rounded-lg border border-slate-200 bg-white pl-3 pr-8 py-1.5 text-xs font-semibold text-slate-700 hover:border-slate-300 focus:outline-none focus:border-indigo-500 w-56 shadow-xs"
              />
              {inputCode && (
                <button
                  type="button"
                  onClick={handleClearSearch}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 p-0.5"
                  title={t(REPORT_LABELS.trClear)}
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
            <AppButton type="submit" size="sm" icon={Search}>
              {t(REPORT_LABELS.trSearch)}
            </AppButton>
          </form>
        }
      >
        {!hasSearched && (
          <div className="p-8 text-center text-slate-400 font-medium flex flex-col items-center justify-center gap-2.5">
            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-indigo-50 text-indigo-600">
              <UserCheck className="h-6 w-6" />
            </div>
            <div className="text-sm font-bold text-slate-700">{t(REPORT_LABELS.trIntroTitle)}</div>
            <div className="text-xs text-slate-500 max-w-md leading-relaxed">
              {t(REPORT_LABELS.trIntroBody)}
            </div>
          </div>
        )}
      </Card>

      {loading && <LoadingState label={t(REPORT_LABELS.trSearching)} />}

      {!loading && error && (
        <NotFoundState
          title={t(REPORT_LABELS.trNotFoundTitle)}
          message={tf(REPORT_LABELS.trNotFoundBody, `"${codeParam}"`)}
          backTo="/reports"
          backLabel={t(REPORT_LABELS.trBackToHub)}
          tone="danger"
        />
      )}

      {!loading && data && !error && (
        <div className="flex-1 flex flex-col min-h-0 gap-5">
          {/* Printable Transcript Header (only visible when printing) */}
          <div className="hidden print:block mb-4 pb-4 border-b border-slate-200">
            <h1 className="text-xl font-bold text-slate-900">Official Learner Training Transcript</h1>
            <p className="text-xs text-slate-500 mt-1">
              Generated at: {formatDate(data.generatedAt)}
            </p>
          </div>

          {/* Learner Information Card */}
          <Card title={t(REPORT_LABELS.trLearnerInfo)} icon={GraduationCap} className="relative shrink-0">
            <div className="absolute top-3.5 right-4 print:hidden">
              <AppButton onClick={handlePrint} icon={Printer} variant="secondary" size="sm">
                {t(REPORT_LABELS.trPrint)}
              </AppButton>
            </div>
            <div className="p-5 grid grid-cols-1 md:grid-cols-2 gap-4 text-xs text-slate-700">
              <div className="flex flex-col gap-3">
                <div>
                  <span className="font-bold text-slate-400 uppercase tracking-wider block text-xxs mb-0.5">{t(REPORT_LABELS.trName)}</span>
                  <span className="text-sm font-extrabold text-slate-800">{data.learnerName || '—'}</span>
                </div>
                <div>
                  <span className="font-bold text-slate-400 uppercase tracking-wider block text-xxs mb-0.5">{t(REPORT_LABELS.trCode)}</span>
                  <span className="font-mono font-bold text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded text-xs inline-block">
                    {data.learnerCode}
                  </span>
                </div>
                {data.learnerGroups && data.learnerGroups.length > 0 && (
                  <div>
                    <span className="font-bold text-slate-400 uppercase tracking-wider block text-xxs mb-0.5">{t(NAV_LABELS.learnerGroups)}</span>
                    <div className="flex flex-wrap gap-1 mt-1">
                      {data.learnerGroups.map((g) => (
                        <Badge key={g} tone="neutral" variant="soft">
                          {g}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              <div className="flex flex-col gap-3">
                <div>
                  <span className="font-bold text-slate-400 uppercase tracking-wider block text-xxs mb-0.5">{t(REPORT_LABELS.colDivision)}</span>
                  <span className="text-slate-800 font-semibold">{data.division || '—'}</span>
                </div>
                <div>
                  <span className="font-bold text-slate-400 uppercase tracking-wider block text-xxs mb-0.5">{t(REPORT_LABELS.colDepartment)}</span>
                  <span className="text-slate-800 font-semibold">{data.department || '—'}</span>
                </div>
                <div>
                  <span className="font-bold text-slate-400 uppercase tracking-wider block text-xxs mb-0.5">{t(DASHBOARD_LABELS.completionRate)}</span>
                  <div className="flex items-center gap-2 mt-1">
                    <ProgressBar
                      value={data.totalCourses > 0 ? (data.completedCourses / data.totalCourses) * 100 : 0}
                      completed={data.completedCourses === data.totalCourses && data.totalCourses > 0}
                    />
                    <span className="text-xxs font-bold text-slate-600 tabular-nums">
                      {tf(REPORT_LABELS.trCompletedOf, data.completedCourses, data.totalCourses)} ({data.totalCourses > 0 ? formatPercent((data.completedCourses / data.totalCourses) * 100) : '—'})
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </Card>

          {/* Training Records Card */}
          <Card
            title={t(REPORT_LABELS.trHistoryTitle)}
            icon={BookOpen}
            className="flex-1 flex flex-col min-h-64"
            bodyClassName="flex-1 flex flex-col min-h-0"
          >
            <div className="border-b border-slate-100 bg-slate-50/20 px-5 shrink-0 print:hidden">
              <ListToolbar
                searchValue={recordSearch}
                onSearchChange={setRecordSearch}
                searchPlaceholder={t(REPORT_LABELS.trFilterPlaceholder)}
              />
            </div>

            <div
              onScroll={handleScroll}
              className="flex-1 overflow-x-auto overflow-y-auto min-h-0 custom-scrollbar"
            >
              <table className="w-full text-left text-sm border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold uppercase text-xxs sticky top-0 z-10 shadow-xs">
                    <th className="p-3 pl-5">{t(DASHBOARD_LABELS.colCourse)}</th>
                    <th className="p-3">{t(DASHBOARD_LABELS.colStatus)}</th>
                    <th className="p-3">{t(REPORT_LABELS.colProgress)}</th>
                    <th className="p-3 text-center">{t(REPORT_LABELS.trColScore)}</th>
                    <th className="p-3 text-center">{t(REPORT_LABELS.trColTimeSpent)}</th>
                    <th className="p-3 pr-5">{t(REPORT_LABELS.trColTimeline)}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {visibleTranscriptRows.map((row, idx) => (
                    <tr key={`${row.enrollmentId}-${idx}`} className="hover:bg-slate-50/50 transition duration-100">
                      <td className="p-3 pl-5 select-all">
                        <div className="font-bold text-slate-800 text-xs sm:text-[13px]">{row.courseTitle || '—'}</div>
                        <div className="text-xxs font-mono text-slate-400 mt-0.5">
                          {[row.courseCode, row.assignmentNo ? `Assign: ${row.assignmentNo}` : null]
                            .filter(Boolean)
                            .join(' · ')}
                        </div>
                      </td>
                      <td className="p-3">
                        <StatusBadge size="xxs">{learnerStatusLabel(row.status)}</StatusBadge>
                      </td>
                      <td className="p-3">
                        <ProgressBar value={row.progress} completed={row.status === 'Completed'} />
                      </td>
                      <td className="p-3 text-center text-xs font-semibold tabular-nums">
                        {row.status === 'Completed' ? formatNumber(row.totalScore) : '—'}
                      </td>
                      <td className="p-3 text-center text-xs font-semibold tabular-nums">
                        {formatDuration(row.totalTimeSpentSeconds)}
                      </td>
                      <td className="p-3 pr-5 text-xxs text-slate-500 leading-relaxed font-semibold">
                        {row.startDate && <div>{tf(REPORT_LABELS.trStarted, formatDate(row.startDate))}</div>}
                        {row.dueDate && <div className="mt-0.5">{tf(REPORT_LABELS.duePrefix, formatDate(row.dueDate))}</div>}
                        {row.completedDate && <div className="mt-0.5 text-emerald-600">{tf(REPORT_LABELS.trCompletedOn, formatDate(row.completedDate))}</div>}
                      </td>
                    </tr>
                  ))}
                  {filteredTranscriptRows.length === 0 && (
                    <tr>
                      <td colSpan={6} className="p-6 text-center text-slate-400 text-xs font-medium">
                        {t(REPORT_LABELS.trNoHistory)}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </Card>
        </div>
      )}
    </div>
  )
}

