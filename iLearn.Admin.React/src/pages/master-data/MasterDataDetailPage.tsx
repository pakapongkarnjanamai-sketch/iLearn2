import { useEffect, useState, useMemo } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { 
  ArrowLeft, 
  Settings, 
  Edit3, 
  Trash2, 
  Loader2, 
  AlertTriangle,
  X,
  Save
} from 'lucide-react'
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

  const [isEditing, setIsEditing] = useState(isNew)
  const [loading, setLoading] = useState(!isNew)
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
    if (config && isNew) {
      setLabel('new', `New ${config.title.replace(/s$/, '')}`)
    }
  }, [config, isNew, setLabel])

  const loadItem = async () => {
    if (isNew || !store || !id) return
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
      if (isNew) {
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
    if (!window.confirm(`Are you absolutely sure you want to delete this ${entityName}? This action cannot be undone.`)) return
    
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
      <div className="text-center py-12">
        <AlertTriangle className="h-12 w-12 text-red-500 mx-auto" />
        <h2 className="text-lg font-bold text-slate-700 mt-4">Invalid Master Data Type</h2>
        <p className="text-slate-400 mt-2">The requested directory section could not be resolved.</p>
        <Link to="/master-data" className="mt-6 inline-flex items-center text-indigo-500 font-semibold hover:underline">
          <ArrowLeft className="h-4 w-4 mr-1" /> Back to Master Data
        </Link>
      </div>
    )
  }

  if (loading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-indigo-500" />
      </div>
    )
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
                    <span className={`inline-flex px-2.5 py-0.5 rounded-full text-xxs font-extrabold uppercase tracking-wide ${
                      item?.isActive ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'
                    }`}>
                      {item?.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </dd>
                </div>

                <div>
                  <dt className="text-slate-400 font-bold text-xs uppercase tracking-wider">Last Modified</dt>
                  <dd className="text-slate-500 font-bold mt-1.5 text-xs">
                    {item?.updatedAt ? new Date(item.updatedAt).toLocaleString() : '—'}
                  </dd>
                </div>
              </dl>
            )}
          </div>
        </section>

        {/* Dynamic Sidebar Controls */}
        <aside className="lg:sticky lg:top-5 rounded-lg border border-slate-200 bg-white p-4 space-y-2 select-none shadow-xs">
          <div className="flex items-center gap-2 pb-2 mb-1 border-b border-slate-200">
            <Settings className="h-4 w-4 text-indigo-650" aria-hidden="true" />
            <h2 className="text-sm font-bold text-slate-800">Controls</h2>
          </div>

          {isEditing ? (
            // Edit Mode Actions
            <>
              <button
                type="submit"
                disabled={busy}
                className="w-full flex items-center gap-2.5 rounded-md bg-indigo-600 hover:bg-indigo-750 text-white p-2 transition cursor-pointer text-left focus:outline-none"
              >
                <div className="h-7 w-7 rounded bg-indigo-500 flex items-center justify-center shrink-0 text-white">
                  {busy ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Save className="h-3.5 w-3.5 shrink-0" />}
                </div>
                <span className="text-[13px] font-bold">{busy ? 'Saving...' : 'Save Changes'}</span>
              </button>
              
              <button
                type="button"
                onClick={() => {
                  if (isNew) {
                    navigate(`/master-data/${type}`)
                  } else {
                    setIsEditing(false)
                    setActiveValues({ ...item })
                  }
                }}
                disabled={busy}
                className="group w-full flex items-center gap-2.5 rounded-md border border-slate-200 bg-white p-2 text-slate-700 hover:border-slate-300 hover:bg-slate-50 transition cursor-pointer text-left"
              >
                <div className="h-7 w-7 rounded bg-slate-100 flex items-center justify-center shrink-0 text-slate-500 transition-colors">
                  <X className="h-3.5 w-3.5 shrink-0" />
                </div>
                <span className="text-[13px] font-bold text-slate-850 transition-colors">Cancel</span>
              </button>
            </>
          ) : (
            // View Mode Actions
            <>
              <button
                type="button"
                onClick={() => setIsEditing(true)}
                className="group w-full flex items-center gap-2.5 rounded-md border border-slate-200 bg-white p-2 text-slate-700 hover:border-indigo-300 hover:bg-indigo-50/40 transition cursor-pointer text-left"
              >
                <div className="h-7 w-7 rounded bg-slate-100 group-hover:bg-indigo-100 flex items-center justify-center shrink-0 text-slate-500 group-hover:text-indigo-600 transition-colors">
                  <Edit3 className="h-3.5 w-3.5 shrink-0" />
                </div>
                <span className="text-[13px] font-bold text-slate-800 group-hover:text-indigo-800 transition-colors">Edit Properties</span>
              </button>

              <button
                type="button"
                onClick={handleDelete}
                className="group w-full flex items-center gap-2.5 rounded-md border border-red-200 bg-white p-2 text-red-650 hover:border-red-300 hover:bg-red-50/50 transition cursor-pointer text-left animate-pulse-slow"
              >
                <div className="h-7 w-7 rounded bg-red-50 group-hover:bg-red-100 flex items-center justify-center shrink-0 text-red-500 group-hover:text-red-600 transition-colors">
                  <Trash2 className="h-3.5 w-3.5 shrink-0" />
                </div>
                <span className="text-[13px] font-bold text-red-700 group-hover:text-red-800 transition-colors">Delete Record</span>
              </button>
            </>
          )}

          <div className="pt-2 border-t border-slate-100">
            <Link 
              to={`/master-data/${type}`}
              className="w-full flex items-center justify-center gap-1.5 text-slate-400 hover:text-slate-750 transition font-semibold text-xs py-1.5"
            >
              <ArrowLeft className="h-3.5 w-3.5" />
              <span>Back to Directory</span>
            </Link>
          </div>
        </aside>
      </form>
    </>
  )
}
