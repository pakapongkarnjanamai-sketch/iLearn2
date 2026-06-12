import { useEffect, useState, type FormEvent } from 'react'
import { Plus, Edit3, Trash2, X, FolderTree, Save } from 'lucide-react'
import { AppButton } from '../../components/ui/AppButton'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { LoadingState } from '../../components/ui/LoadingState'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

type LearnerGroupCategory = {
  id: number
  name: string
  description?: string | null
  parentId?: number | null
  parentName?: string | null
  depth: number
  hasChildren: boolean
  childCount: number
  learnerGroupCount: number
}

type ApiListResponse<T> = {
  success: boolean
  data: T
  totalCount?: number
}

type FormState = {
  id?: number
  name: string
  description: string
  parentId: number | ''
}

const EMPTY_FORM: FormState = { name: '', description: '', parentId: '' }

export function LearnerGroupCategoriesPage() {
  const { confirm, confirmDialog } = useConfirm()
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [items, setItems] = useState<LearnerGroupCategory[]>([])
  const [form, setForm] = useState<FormState | null>(null)

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

  const openCreate = () => setForm({ ...EMPTY_FORM })
  const openEdit = (c: LearnerGroupCategory) =>
    setForm({
      id: c.id,
      name: c.name,
      description: c.description ?? '',
      parentId: c.parentId ?? '',
    })
  const closeForm = () => setForm(null)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!form) return
    if (!form.name.trim()) {
      toast.error('Name is required')
      return
    }
    const payload = {
      name: form.name.trim(),
      description: form.description.trim() || null,
      parentId: form.parentId === '' ? null : Number(form.parentId),
    }
    setBusy(true)
    try {
      if (form.id) {
        await fetchWithAccessControl(`learnerGroupCategories/${form.id}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
        toast.success('Category updated')
      } else {
        await fetchWithAccessControl('learnerGroupCategories', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
        toast.success('Category created')
      }
      closeForm()
      await load()
    } catch {
      toast.error(form.id ? 'Update failed' : 'Create failed')
    } finally {
      setBusy(false)
    }
  }

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

  const parentOptions = items.filter((c) => !form?.id || c.id !== form.id)

  return (
    <>
      <DataGridSurface
        title="Learner Group Categories"
        note="Manage hierarchy folders used by learner groups."
        actions={
          <AppButton variant="primary" icon={Plus} onClick={openCreate}>
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
                              onClick={() => openEdit(c)}
                              className="rounded-md border border-transparent p-1.5 text-slate-500 transition hover:border-slate-200 hover:bg-slate-100 hover:text-slate-700"
                            >
                              <Edit3 className="h-3.5 w-3.5" />
                            </button>
                            <button
                              type="button"
                              title="Delete"
                              onClick={() => handleDelete(c)}
                              disabled={busy || c.hasChildren || c.learnerGroupCount > 0}
                              className="rounded-md border border-transparent p-1.5 text-slate-500 transition hover:border-rose-200 hover:bg-rose-50 hover:text-rose-600 disabled:cursor-not-allowed disabled:opacity-30"
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

      {form && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/35 p-4 backdrop-blur-[1px]"
          onClick={closeForm}
        >
          <form
            onSubmit={handleSubmit}
            onClick={(e) => e.stopPropagation()}
            className="w-full max-w-lg rounded-lg border border-slate-200 bg-white p-5 shadow-xl"
          >
            <div className="mb-4 flex items-center justify-between border-b border-slate-200 pb-3">
              <h2 className="text-xs font-extrabold uppercase text-slate-700">
                {form.id ? 'Edit Category' : 'New Category'}
              </h2>
              <button
                type="button"
                onClick={closeForm}
                className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="space-y-3">
              <div>
                <label className="text-[10px] font-extrabold uppercase text-slate-500">Name</label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                  className="mt-1 w-full rounded-md border border-slate-200 px-2.5 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none"
                  autoFocus
                  required
                />
              </div>
              <div>
                <label className="text-[10px] font-extrabold uppercase text-slate-500">Description</label>
                <input
                  type="text"
                  value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })}
                  className="mt-1 w-full rounded-md border border-slate-200 px-2.5 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none"
                />
              </div>
              <div>
                <label className="text-[10px] font-extrabold uppercase text-slate-500">Parent Category</label>
                <select
                  value={form.parentId}
                  onChange={(e) =>
                    setForm({ ...form, parentId: e.target.value === '' ? '' : Number(e.target.value) })
                  }
                  className="mt-1 w-full rounded-md border border-slate-200 px-2.5 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none"
                >
                  <option value="">— Root (no parent) —</option>
                  {parentOptions.map((p) => (
                    <option key={p.id} value={p.id}>
                      {'  '.repeat(p.depth)}{p.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="mt-5 flex justify-end gap-2 border-t border-slate-200 pt-3">
              <AppButton variant="ghost" onClick={closeForm} type="button">
                Cancel
              </AppButton>
              <AppButton variant="primary" icon={Save} type="submit" disabled={busy}>
                {form.id ? 'Save Changes' : 'Create Category'}
              </AppButton>
            </div>
          </form>
        </div>
      )}

      {confirmDialog}
    </>
  )
}
