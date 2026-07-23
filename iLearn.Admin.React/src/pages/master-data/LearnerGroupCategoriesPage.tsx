import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Edit3, Trash2, FolderTree } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { IconButton } from '../../components/ui/IconButton'
import { LoadingState } from '../../components/ui/LoadingState'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { ADMIN_LABELS, t, tf } from '../../lib/labels'

// Mirrors LearnerGroupCategoryDto (iLearn.Application/DTOs/LearnerGroupCategoryDto.cs)
export type LearnerGroupCategory = {
  id: number
  name: string
  description?: string | null
  divisionId?: number | null
  parentId?: number | null
  parentName?: string | null
  depth: number
  hasChildren: boolean
  childCount: number
  learnerGroupCount: number
}


export type ApiListResponse<T> = {
  success: boolean
  data: T
  totalCount?: number
}

export function LearnerGroupCategoriesPage() {
  const navigate = useNavigate()
  const { confirm, confirmDialog } = useConfirm()
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [items, setItems] = useState<LearnerGroupCategory[]>([])

  const load = async () => {
    setLoading(true)
    try {
      const result = await fetchWithAccessControl<
        LearnerGroupCategory[] | ApiListResponse<LearnerGroupCategory[]>
      >('LearnerGroupCategories')
      setItems(Array.isArray(result) ? result : result.data)
    } catch {
      toast.error(t(ADMIN_LABELS.failedToLoadGroupCategories))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  const handleDelete = async (c: LearnerGroupCategory) => {
    if (c.hasChildren) {
      toast.error(t(ADMIN_LABELS.categoryHasChildren))
      return
    }
    if (c.learnerGroupCount > 0) {
      toast.error(tf(ADMIN_LABELS.categoryReferenced, c.learnerGroupCount))
      return
    }
    if (!(await confirm({
      title: t(ADMIN_LABELS.deleteCategory), message: tf(ADMIN_LABELS.deleteCategoryConfirm, c.name), confirmLabel: t(ADMIN_LABELS.delete),
      danger: true,
    }))) return
    setBusy(true)
    try {
      await fetchWithAccessControl(`learnerGroupCategories/${c.id}`, { method: 'DELETE' })
      toast.success(t(ADMIN_LABELS.categoryDeleted))
      await load()
    } catch {
      toast.error(t(ADMIN_LABELS.deleteFailed))
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <DataGridSurface
        title={t(ADMIN_LABELS.learnerGroupCategories)} note={t(ADMIN_LABELS.learnerGroupCategoriesNote)}
        actions={
          <AppButton
            variant="primary"
            icon={Plus}
            onClick={() => navigate('/master-data/learner-group-categories/new')}
          >
            {t(ADMIN_LABELS.newCategory)}
          </AppButton>
        }
      >
        <div className="flex min-h-0 flex-1 flex-col pt-3">
          {loading ? (
            <LoadingState size="section" />
          ) : items.length === 0 ? (
            <div className="flex flex-1 flex-col items-center justify-center gap-2 rounded-lg border border-slate-200 bg-slate-50/40 p-8 text-sm text-slate-400">
              <FolderTree className="h-8 w-8" />
              <p>{t(ADMIN_LABELS.noCategories)}</p>
            </div>
          ) : (
            <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg border border-slate-200/80 bg-white">
              <div className="flex items-center justify-between border-b border-slate-200 px-3 py-2.5">
                <span className="text-xs font-semibold text-slate-500">
                  {tf(ADMIN_LABELS.showingCategories, items.length)}
                </span>
              </div>

              <div className="min-h-0 flex-1 overflow-auto custom-scrollbar">
                <table className="min-w-full border-collapse text-left text-xs">
                  <thead className="sticky top-0 z-10 border-b border-slate-200 bg-slate-50/90 text-xxs font-extrabold uppercase text-slate-500">
                    <tr>
                      <th className="px-3 py-2.5">{t(ADMIN_LABELS.name)}</th><th className="px-3 py-2.5">{t(ADMIN_LABELS.description)}</th><th className="px-3 py-2.5">{t(ADMIN_LABELS.parent)}</th><th className="px-3 py-2.5 text-center">{t(ADMIN_LABELS.depth)}</th><th className="px-3 py-2.5 text-center">{t(ADMIN_LABELS.children)}</th><th className="px-3 py-2.5 text-center">{t(ADMIN_LABELS.learnerGroupCategories)}</th><th className="px-3 py-2.5 text-right">{t(ADMIN_LABELS.actions)}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-slate-700">
                    {items.map((c) => (
                      <tr key={c.id} className="hover:bg-slate-50/70">
                        <td className="px-3 py-2.5">
                          <div className="flex items-center gap-1.5" style={{ paddingLeft: c.depth * 16 }}>
                            <FolderTree className="h-3.5 w-3.5 text-slate-400" />
                            <span className="font-bold text-slate-800">{c.name}</span>
                          </div>
                        </td>
                        <td className="px-3 py-2.5 text-slate-500">{c.description || '—'}</td>
                        <td className="px-3 py-2.5 text-slate-500">{c.parentName || '—'}</td>
                        <td className="px-3 py-2.5 text-center font-mono">{c.depth}</td>
                        <td className="px-3 py-2.5 text-center font-mono">{c.childCount}</td>
                        <td className="px-3 py-2.5 text-center font-mono">{c.learnerGroupCount}</td>
                        <td className="px-3 py-2.5">
                          <div className="flex items-center justify-end gap-1">
                            <IconButton
                              type="button"
                              title={t(ADMIN_LABELS.edit)}
                              onClick={() => navigate(`/master-data/learner-group-categories/${c.id}/edit`)}
                              icon={Edit3}
                              tone="primary"
                              size="sm"
                            />
                            <IconButton
                              type="button"
                              title={t(ADMIN_LABELS.delete)}
                              onClick={() => handleDelete(c)}
                              disabled={busy || c.hasChildren || c.learnerGroupCount > 0}
                              icon={Trash2}
                              tone="danger"
                              size="sm"
                            />
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      </DataGridSurface>

      {confirmDialog}
    </>
  )
}
