import { useEffect, useState, useMemo, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Edit3,
  Trash2,
  X,
  Save,
  Settings
} from 'lucide-react'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { StatusText } from '../../components/ui/StatusText'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { DetailLayout, Fact, FactGrid } from '../../components/ui/detail'
import { Card } from '../../components/ui/Card'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { formatDateTime } from '../../lib/format'
import { adminListConfigs, type AdminListConfig } from '../moduleConfigs'
import { createAdminDataSource } from '../../lib/createDataSource'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'

// Mirrors CategoryDto (iLearn.Application/DTOs/DivisionDto.cs)
export interface CategoryDto {
  id: number
  name: string
  description?: string | null
  divisionId?: number | null
  divisionName?: string | null
  sortOrder: number
  isActive: boolean
  createdAt: string
}

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
  const config = useMemo<AdminListConfig | null>(() => {
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

  const loadItem = useCallback(async () => {
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
  }, [id, isNewItem, setLabel, store])

  useEffect(() => {
    void loadItem()
  }, [loadItem])

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
      toast.error(isNewItem ? 'Failed to create record' : 'Failed to save changes')
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
      toast.error(`Failed to delete ${entityName}`)
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
      <form onSubmit={handleSave}>
        <DetailLayout
          sidebar={
            <ControlsSidebar>
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
          }
        >
          {/* Main Details Panel */}
          <Card icon={Settings} title={`${entityTitle} Details`} bodyClassName="p-5 space-y-5">
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

                  {type === 'categories' && (
                    <div className="space-y-1.5">
                      <label htmlFor="sort-order-field" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">
                        Sort Order (ลำดับ)
                      </label>
                      <input
                        id="sort-order-field"
                        type="number"
                        min={1}
                        required
                        value={activeValues.sortOrder !== undefined && activeValues.sortOrder !== null ? activeValues.sortOrder : ''}
                        onChange={(e) => {
                          const val = e.target.value === '' ? '' : Number(e.target.value)
                          handleFieldChange('sortOrder', val)
                        }}
                        placeholder="Enter sort order (e.g. 1, 2, 3...)"
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

                  {config.hasDescription && (
                    <div className="space-y-1.5">
                      <label htmlFor="description-field" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">
                        Description
                      </label>
                      <textarea
                        id="description-field"
                        value={activeValues.description || ''}
                        onChange={(e) => handleFieldChange('description', e.target.value)}
                        placeholder={`Enter description...`}
                        maxLength={500}
                        rows={3}
                        className="w-full px-3.5 py-2 border border-slate-200 rounded-lg text-sm text-slate-800 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 bg-slate-50/30 transition duration-150 custom-scrollbar resize-y font-semibold"
                      />
                    </div>
                  )}
                </>
              ) : (
                // Read-only Details View
                <FactGrid cols={2} className="text-sm gap-y-4">
                  <Fact
                    label="Name"
                    colSpan="full"
                    valueClassName="text-slate-800 font-bold text-base select-all"
                  >
                    {item?.name || '—'}
                  </Fact>

                  {config.hasDescription && (
                    <Fact
                      label="Description"
                      colSpan="full"
                    >
                      {item?.description || '—'}
                    </Fact>
                  )}

                  {type === 'categories' && (
                    <Fact
                      label="Sort Order"
                    >
                      {item?.sortOrder !== undefined && item?.sortOrder !== null ? item.sortOrder : '—'}
                    </Fact>
                  )}

                  <Fact
                    label="Status"
                  >
                    <StatusText tone={item?.isActive ? 'success' : 'neutral'}>
                      {item?.isActive ? 'Active' : 'Inactive'}
                    </StatusText>
                  </Fact>

                  <Fact
                    label="Last Modified"
                    valueClassName="text-slate-500 font-bold mt-1 text-xs"
                  >
                    {item?.updatedAt ? formatDateTime(item.updatedAt) : '—'}
                  </Fact>
                </FactGrid>
              )}
            </div>
          </Card>
        </DetailLayout>
      </form>

      {confirmDialog}
    </>
  )
}
