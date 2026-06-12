import { useEffect, useState, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Edit3,
  Trash2,
  X,
  Save
} from 'lucide-react'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { formatDateTime } from '../../lib/format'
import { adminListConfigs } from '../moduleConfigs'
import { createAdminDataSource } from '../../lib/createDataSource'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'

type MasterDataDetailPageProps = {
  isNew?: boolean
}

export function MasterDataDetailPage({ isNew = false }: MasterDataDetailPageProps) {
  const { type, id } = useParams<{ type: string; id: string }>()
  const navigate = useNavigate()
  const { setLabel } = useBreadcrumbs()
  const { confirm, confirmDialog } = useConfirm()

  const isNewItem = isNew || id === 'new'
  const [isEditing, setIsEditing] = useState(isNewItem)
  const [loading, setLoading] = useState(!isNewItem)
  const [busy, setBusy] = useState(false)
  const [item, setItem] = useState<any>(null)
  const [activeValues, setActiveValues] = useState<any>({ isActive: true })

  // Map the route ':type' parameter to the corresponding configuration
  const config = useMemo(() => {
    if (type === 'divisions') return adminListConfigs.masterDataDivisions
    if (type === 'categories') return adminListConfigs.masterDataCategories
    if (type === 'course-types') return adminListConfigs.masterDataCourseTypes
    if (type === 'roles') return adminListConfigs.masterDataRoles
    return null
  }, [type])

  const store = useMemo(() => {
    if (!config) return null
    return createAdminDataSource<any>({
      controller: config.controller,
      key: config.key,
      enableCrud: true
    })
  }, [config])

  useEffect(() => {
    if (config && isNewItem) {
      setLabel('new', `New ${config.title.replace(/s$/, '')}`)
    }
  }, [config, isNewItem, setLabel])

  const loadItem = async () => {
    if (isNewItem || !store || !id) return
    setLoading(true)
    try {
      const result = await store.load({
        filter: [['id', '=', Number(id)]]
      })
      if (result.data && result.data.length > 0) {
        const record = result.data[0]
        setItem(record)
        setActiveValues({ ...record })
        if (record.name && id) {
          setLabel(id, record.name)
        }
      } else {
        toast.error('Record not found')
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to load item details')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadItem()
  }, [id, store, isNew])

  const handleFieldChange = (field: string, val: any) => {
    setActiveValues((prev: any) => ({ ...prev, [field]: val }))
  }

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!store || !config || busy) return
    if (!activeValues.name?.trim()) {
      toast.error('Name is required')
      return
    }

    setBusy(true)
    try {
      const payload = { 
        ...activeValues, 
        name: activeValues.name.trim(),
        description: activeValues.description?.trim() || null
      }
      if (isNewItem) {
        const newRecord = await store.insert!(payload)
        toast.success(`${config.title.replace(/s$/, '')} created successfully`)
        navigate(`/master-data/${type}/${newRecord.id}`, { replace: true })
      } else {
        await store.update!(Number(id), payload)
        toast.success('Changes saved successfully')
        setIsEditing(false)
        await loadItem()
      }
    } catch (err) {
      console.error(err)
    } finally {
      setBusy(false)
    }
  }

  const handleDelete = async () => {
    if (isNew || !store || !config || busy) return
    const entityName = config.title.replace(/s$/, '').toLowerCase()
    if (!(await confirm({
      title: `Delete ${config.title.replace(/s$/, '')}`,
      message: `Are you absolutely sure you want to delete this ${entityName}? This action cannot be undone.`,
      confirmLabel: 'Delete',
      danger: true,
    }))) return
    
    setBusy(true)
    try {
      await store.remove!(Number(id))
      toast.success(`${config.title.replace(/s$/, '')} deleted successfully`)
      navigate(`/master-data/${type}`)
    } catch (err) {
      console.error(err)
    } finally {
      setBusy(false)
    }
  }

  if (!config) {
    return (
      <NotFoundState
        title="Invalid Master Data Type"
        message="The requested directory section could not be resolved."
        backTo="/master-data"
        backLabel="Back to Master Data"
        tone="danger"
      />
    )
  }

  if (loading) {
    return <LoadingState />
  }

  const entityTitle = config.title.replace(/s$/, '')

  return (
    <>
      <header className="mb-4">
        <div className="text-xxs font-extrabold uppercase text-slate-400">Master Data &rsaquo; {config.title}</div>
        <h1 className="text-2xl font-extrabold text-slate-900 select-none">
          {isNew ? `Create New ${entityTitle}` : item?.name || 'Record Details'}
        </h1>
      </header>

      <form onSubmit={handleSave} className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start">
        {/* Main Details Panel */}
        <section className="bg-white border border-slate-200 rounded-lg p-6 shadow-xs min-w-0">
          <h2 className="text-[13px] font-extrabold uppercase text-indigo-600 border-b border-slate-200 pb-3 mb-5 select-none">
            Properties Details
          </h2>

          <div className="space-y-4 max-w-xl">
            {isEditing ? (
              // Edit Form Fields
              <>
                <div className="space-y-1.5">
                  <label htmlFor="name-field" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">
                    {entityTitle} Name
                  </label>
                  <input
                    id="name-field"
                    type="text"
                    required
                    value={activeValues.name || ''}
                    onChange={(e) => handleFieldChange('name', e.target.value)}
                    placeholder={`Enter ${entityTitle.toLowerCase()} name...`}
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-lg text-sm text-slate-800 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 bg-slate-50/30 transition duration-150"
                  />
                </div>

                {type === 'roles' && (
                  <div className="space-y-1.5">
                    <label htmlFor="desc-field" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">
                      Description
                    </label>
                    <textarea
                      id="desc-field"
                      rows={3}
                      value={activeValues.description || ''}
                      onChange={(e) => handleFieldChange('description', e.target.value)}
                      placeholder="Enter description..."
                      className="w-full px-3.5 py-2 border border-slate-200 rounded-lg text-sm text-slate-800 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 bg-slate-50/30 transition duration-150"
                    />
                  </div>
                )}

                <div className="flex items-center gap-3 py-2">
                  <input
                    type="checkbox"
                    id="active-field"
                    checked={Boolean(activeValues.isActive)}
                    onChange={(e) => handleFieldChange('isActive', e.target.checked)}
                    className="h-4.5 w-4.5 text-indigo-500 rounded border-slate-300 focus:ring-indigo-400 cursor-pointer"
                  />
                  <label 
                    htmlFor="active-field" 
                    className="text-xs sm:text-[13px] font-bold text-slate-700 select-none cursor-pointer"
                  >
                    Active Status
                  </label>
                </div>
              </>
            ) : (
              // Read-only Details View
              <dl className="grid grid-cols-1 gap-y-4 sm:grid-cols-2 gap-x-6 text-sm">
                <div className="sm:col-span-2">
                  <dt className="text-slate-400 font-bold text-xs uppercase tracking-wider">Name</dt>
                  <dd className="text-slate-800 font-bold text-base mt-1 select-all">{item?.name || '—'}</dd>
                </div>

                {type === 'roles' && (
                  <div className="sm:col-span-2">
                    <dt className="text-slate-400 font-bold text-xs uppercase tracking-wider">Description</dt>
                    <dd className="text-slate-600 mt-1 font-semibold leading-relaxed whitespace-pre-wrap">{item?.description || '—'}</dd>
                  </div>
                )}

                <div>
                  <dt className="text-slate-400 font-bold text-xs uppercase tracking-wider">Status</dt>
                  <dd className="mt-1.5">
                    <StatusBadge tone={item?.isActive ? 'success' : 'neutral'} size="xxs">
                      {item?.isActive ? 'Active' : 'Inactive'}
                    </StatusBadge>
                  </dd>
                </div>

                <div>
                  <dt className="text-slate-400 font-bold text-xs uppercase tracking-wider">Last Modified</dt>
                  <dd className="text-slate-500 font-bold mt-1.5 text-xs">
                    {item?.updatedAt ? formatDateTime(item.updatedAt) : '—'}
                  </dd>
                </div>
              </dl>
            )}
          </div>
        </section>

        {/* Dynamic Sidebar Controls */}
        <ControlsSidebar backTo={`/master-data/${type}`} backLabel="Back to Directory">
          {isEditing ? (
            // Edit Mode Actions
            <>
              <ControlAction type="submit" icon={Save} loading={busy} variant="primary">
                {busy ? 'Saving...' : 'Save Changes'}
              </ControlAction>
              <ControlAction
                icon={X}
                disabled={busy}
                onClick={() => {
                  if (isNew) {
                    navigate(`/master-data/${type}`)
                  } else {
                    setIsEditing(false)
                    setActiveValues({ ...item })
                  }
                }}
              >
                Cancel
              </ControlAction>
            </>
          ) : (
            // View Mode Actions
            <>
              <ControlAction icon={Edit3} onClick={() => setIsEditing(true)}>Edit Properties</ControlAction>
              <ControlAction icon={Trash2} onClick={handleDelete} variant="danger">Delete Record</ControlAction>
            </>
          )}
        </ControlsSidebar>
      </form>

      {confirmDialog}
    </>
  )
}
