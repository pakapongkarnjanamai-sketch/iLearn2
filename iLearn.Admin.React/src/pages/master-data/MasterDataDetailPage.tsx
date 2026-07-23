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
import { ADMIN_LABELS, t, tf } from '../../lib/labels'

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

  const configTitle = config ? t(config.title) : ''
  const entityTitle = configTitle.replace(/s$/, '')

  useEffect(() => {
    if (config && isNewItem) {
      setLabel('new', `${t(ADMIN_LABELS.create)} ${entityTitle}`)
    }
  }, [config, entityTitle, isNewItem, setLabel])

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
        toast.error(t(ADMIN_LABELS.recordNotFound))
      }
    } catch (err) {
      console.error(err)
      toast.error(t(ADMIN_LABELS.failedToLoadItem))
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
      toast.error(t(ADMIN_LABELS.nameRequired))
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
        toast.success(tf(ADMIN_LABELS.createdSuccessfully, entityTitle))
        navigate(`/master-data/${type}/${newRecord.id}`, { replace: true })
      } else {
        await store.update!(Number(id), payload)
        toast.success(t(ADMIN_LABELS.changesSaved))
        setIsEditing(false)
        await loadItem()
      }
    } catch (err) {
      console.error(err)
      toast.error(t(isNewItem ? ADMIN_LABELS.createFailed : ADMIN_LABELS.saveFailed))
    } finally {
      setBusy(false)
    }
  }

  const handleDelete = async () => {
    if (isNew || !store || !config || busy) return
    const entityName = entityTitle.toLowerCase()
    if (!(await confirm({
      title: `${t(ADMIN_LABELS.delete)} ${entityTitle}`,
      message: tf(ADMIN_LABELS.deleteRecordConfirm, entityName),
      confirmLabel: t(ADMIN_LABELS.delete),
      danger: true,
    }))) return
    
    setBusy(true)
    try {
      await store.remove!(Number(id))
      toast.success(tf(ADMIN_LABELS.deletedSuccessfully, entityTitle))
      navigate(`/master-data/${type}`)
    } catch (err) {
      console.error(err)
      toast.error(tf(ADMIN_LABELS.failedToDelete, entityName))
    } finally {
      setBusy(false)
    }
  }

  if (!config) {
    return (
      <NotFoundState
        title={t(ADMIN_LABELS.invalidMasterDataType)}
        message={t(ADMIN_LABELS.invalidMasterDataMessage)}
        backTo="/master-data"
        backLabel={t(ADMIN_LABELS.backToMasterData)}
        tone="danger"
      />
    )
  }

  if (loading) {
    return <LoadingState />
  }

  return (
    <>
      <form onSubmit={handleSave}>
        <DetailLayout
          sidebar={
            <ControlsSidebar>
              {isEditing ? (
                // Edit Mode Actions
                <>
                  <ControlAction key="save" type="submit" icon={Save} loading={busy} variant="primary">
                    {busy ? t(ADMIN_LABELS.saving) : t(ADMIN_LABELS.saveChanges)}
                  </ControlAction>
                  <ControlAction
                    key="cancel"
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
                    {t(ADMIN_LABELS.cancel)}
                  </ControlAction>
                </>
              ) : (
                // View Mode Actions
                <>
                  <ControlAction key="edit" icon={Edit3} onClick={() => setIsEditing(true)}>{t(ADMIN_LABELS.editProperties)}</ControlAction>
                  <ControlAction key="delete" icon={Trash2} onClick={handleDelete} variant="danger">{t(ADMIN_LABELS.deleteRecord)}</ControlAction>
                </>
              )}
            </ControlsSidebar>
          }
        >
          {/* Main Details Panel */}
          <Card icon={Settings} title={t(ADMIN_LABELS.overview)} bodyClassName="p-5 space-y-5">
            <div className="space-y-4 max-w-xl">
              {isEditing ? (
                // Edit Form Fields
                <>
                  <div className="space-y-1.5">
                    <label htmlFor="name-field" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">
                      {entityTitle} {t(ADMIN_LABELS.name)}
                    </label>
                    <input
                      id="name-field"
                      type="text"
                      required
                      value={activeValues.name || ''}
                      onChange={(e) => handleFieldChange('name', e.target.value)}
                      placeholder={tf(ADMIN_LABELS.enterName, entityTitle.toLowerCase())}
                      className="w-full px-3.5 py-2 border border-slate-200 rounded-lg text-sm text-slate-800 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 bg-slate-50/30 transition duration-150"
                    />
                  </div>

                  {type === 'categories' && (
                    <div className="space-y-1.5">
                      <label htmlFor="sort-order-field" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">
                        {t(ADMIN_LABELS.sortOrder)}
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
                        placeholder={t(ADMIN_LABELS.enterSortOrder)}
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
                      {t(ADMIN_LABELS.activeStatus)}
                    </label>
                  </div>

                  {config.hasDescription && (
                    <div className="space-y-1.5">
                      <label htmlFor="description-field" className="block text-xxs font-extrabold text-slate-500 uppercase tracking-wider select-none">
                        {t(ADMIN_LABELS.description)}
                      </label>
                      <textarea
                        id="description-field"
                        value={activeValues.description || ''}
                        onChange={(e) => handleFieldChange('description', e.target.value)}
                        placeholder={t(ADMIN_LABELS.enterDescription)}
                        maxLength={500}
                        rows={3}
                        className="w-full px-3.5 py-2 border border-slate-200 rounded-lg text-sm text-slate-800 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-400 bg-slate-50/30 transition duration-150 custom-scrollbar resize-y font-semibold"
                      />
                    </div>
                  )}
                </>
              ) : (
                // Read-only Details View
                <FactGrid cols={2}>
                  <Fact label={t(ADMIN_LABELS.status)}>
                    <StatusText active={item?.isActive} />
                  </Fact>

                  <Fact
                    label={t(ADMIN_LABELS.name)}
                    colSpan="full"
                    valueClassName="font-semibold select-all"
                  >
                    {item?.name || '—'}
                  </Fact>

                  {config.hasDescription && (
                    <Fact
                      label={t(ADMIN_LABELS.description)}
                      colSpan="full"
                      valueClassName="font-semibold"
                    >
                      {item?.description || '—'}
                    </Fact>
                  )}

                  {type === 'categories' && (
                    <Fact
                      label={t(ADMIN_LABELS.sortOrder)}
                      valueClassName="font-semibold"
                    >
                      {item?.sortOrder !== undefined && item?.sortOrder !== null ? item.sortOrder : '—'}
                    </Fact>
                  )}

                  <Fact
                    label={t(ADMIN_LABELS.lastModified)}
                    valueClassName="font-semibold"
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
