import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Edit3, Trash2, FolderTree } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { LoadingState } from '../../components/ui/LoadingState'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

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
      toast.error('Failed to load categories')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  const handleDelete = async (c: LearnerGroupCategory) => {
    if (c.hasChildren) {
      toast.error('Cannot delete: category has child categories')
      return
    }
    if (c.learnerGroupCount > 0) {
      toast.error(`Cannot delete: ${c.learnerGroupCount} learner group(s) reference this category`)
      return
    }
    if (!(await confirm({
      title: 'Delete Category',
      message: `Delete category "${c.name}"?`,
      confirmLabel: 'Delete',
      danger: true,
    }))) return
    setBusy(true)
    try {
      await fetchWithAccessControl(`learnerGroupCategories/${c.id}`, { method: 'DELETE' })
      toast.success('Category deleted')
      await load()
    } catch {
      toast.error('Delete failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <DataGridSurface
        title="Learner Group Categories"
        note="Manage hierarchy folders used by learner groups."
        actions={
          <AppButton
            variant="primary"
            icon={Plus}
            onClick={() => navigate('/master-data/learner-group-categories/new')}
          >
            New Category
          </AppButton>
        }
      >
        <div className="flex min-h-0 flex-1 flex-col pt-3">
          {loading ? (
            <LoadingState size="section" />
          ) : items.length === 0 ? (
            <div className="flex flex-1 flex-col items-center justify-center gap-2 rounded-lg border border-slate-200 bg-slate-50/40 p-8 text-sm text-slate-400">
              <FolderTree className="h-8 w-8" />
              <p>No categories.</p>
            </div>
          ) : (
            <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg border border-slate-200/80 bg-white">
              <div className="flex items-center justify-between border-b border-slate-200 px-3 py-2.5">
                <span className="text-xs font-semibold text-slate-500">
                  Showing <strong className="text-slate-800">{items.length}</strong> categories
                </span>
              </div>

              <div className="min-h-0 flex-1 overflow-auto custom-scrollbar">
                <table className="min-w-full border-collapse text-left text-xs">
                  <thead className="sticky top-0 z-10 border-b border-slate-200 bg-slate-50/90 text-xxs font-extrabold uppercase text-slate-500">
                    <tr>
                      <th className="px-3 py-2.5">Name</th>
                      <th className="px-3 py-2.5">Description</th>
                      <th className="px-3 py-2.5">Parent</th>
                      <th className="px-3 py-2.5 text-center">Depth</th>
                      <th className="px-3 py-2.5 text-center">Children</th>
                      <th className="px-3 py-2.5 text-center">Learner Groups</th>
                      <th className="px-3 py-2.5 text-right">Actions</th>
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
                            <button
                              type="button"
                              title="Edit"
                              onClick={() => navigate(`/master-data/learner-group-categories/${c.id}/edit`)}
                              className="p-1 text-indigo-500 hover:bg-indigo-50 rounded-md transition cursor-pointer"
                            >
                              <Edit3 className="h-3.5 w-3.5" />
                            </button>
                            <button
                              type="button"
                              title="Delete"
                              onClick={() => handleDelete(c)}
                              disabled={busy || c.hasChildren || c.learnerGroupCount > 0}
                              className="p-1 text-red-500 hover:bg-rose-50 rounded-md transition cursor-pointer disabled:cursor-not-allowed disabled:opacity-30"
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </button>
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
