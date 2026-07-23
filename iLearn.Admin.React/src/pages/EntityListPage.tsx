import { useMemo, useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { Layers, Plus } from 'lucide-react'
import { AppButton } from '../components/ui/AppButton'
import { Badge } from '../components/ui/Badge'
import { StatusBadge } from '../components/ui/StatusBadge'
import { ADMIN_LABELS, learnerStatusLabel, t, tf } from '../lib/labels'
import { DataGridSurface } from '../components/ui/DataGridSurface'
import { AppTable } from '../components/ui/AppTable'
import { createAdminDataSource } from '../lib/createDataSource'
import { createRestDataSource } from '../lib/createRestDataSource'
import { fetchWithAccessControl } from '../lib/apiClient'
import { toast } from '../lib/toast'
import type { AdminListConfig } from './moduleConfigs'
import { useSession } from '../lib/sessionContext'

type EntityListPageProps = {
  config: AdminListConfig
}

export function EntityListPage({ config }: EntityListPageProps) {
  const navigate = useNavigate()
  const { isSuperAdmin } = useSession()
  const title = t(config.title)
  
  const [divisions, setDivisions] = useState<any[]>([])

  useEffect(() => {
    if (config.controller === 'AssignmentsCRUD') {
      fetchWithAccessControl<any>('Divisions/lookup')
        .then(res => {
          if (res && Array.isArray(res.data)) {
            setDivisions(res.data)
          } else if (Array.isArray(res)) {
            setDivisions(res)
          }
        })
        .catch(err => {
          console.error('Failed to load divisions for lookup', err)
          toast.error(t(ADMIN_LABELS.failedToLoadDivisionLookup))
        })
    }
  }, [config.controller])

  const mappedColumns = useMemo(() => {
    return config.columns.map(col => {
      if (col.dataField === 'divisionId') {
        return {
          ...col,
          cellRender: ({ value }: any) => {
            if (value === null || value === undefined) return '—'
            const div = divisions.find(d => d.id === Number(value))
            return div ? div.name : tf(ADMIN_LABELS.divisionWithId, value)
          }
        }
      }
      if (col.dataField === 'status' && config.controller === 'AssignmentsCRUD') {
        return {
          ...col,
          cellRender: ({ value }: any) => {
            if (!value) return '—'
            return <StatusBadge size="xxs">{learnerStatusLabel(String(value))}</StatusBadge>
          }
        }
      }
      if (col.dataField === 'isCompleted' && config.controller === 'EnrollmentsCRUD') {
        return {
          ...col,
          cellRender: ({ data }: any) => {
            const status = data?.isCompleted
              ? 'Completed'
              : Number(data?.progress || 0) > 0 ? 'InProgress' : 'NotStarted'
            return <StatusBadge size="xxs">{learnerStatusLabel(status)}</StatusBadge>
          }
        }
      }
      if (col.dataField === 'courseNames') {
        return {
          ...col,
          cellRender: ({ value }: any) => {
            if (!value) return '—'
            const list = String(value).split(',').map(c => c.trim()).filter(Boolean)
            if (list.length === 0) return '—'
            if (list.length === 1) return <span title={value}>{list[0]}</span>

            const tooltip = list.join('\n')
            return (
              <div className="flex items-center gap-1.5 max-w-[280px]" title={tooltip}>
                <span className="truncate">{list[0]}</span>
                <Badge variant="outline" tone="info" size="xxs" className="min-h-0 px-1.5">
                  +{list.length - 1}
                </Badge>
              </div>
            )
          }
        }
      }
      return col
    })
  }, [config.columns, config.controller, divisions])

  const isCrudEnabled = false
  const isReadOnly =
    config.controller === 'LearningLogsCRUD' || config.controller === 'EnrollmentsCRUD'

  const store = useMemo(() => {
    if (config.controller === 'ContentItemsCRUD') {
      return createRestDataSource<any>({
        controller: 'ContentItems',
        key: config.key,
        enableCrud: isCrudEnabled,
      })
    }
    return createAdminDataSource<any>({
      controller: config.controller,
      key: config.key,
      enableCrud: isCrudEnabled,
      ...(config.basePath ? { basePath: config.basePath } : {}),
    })
  }, [config.controller, config.key, isCrudEnabled, config.basePath])

  const getRoutePrefix = (controller: string) => {
    if (controller === 'CoursesCRUD') return '/courses'
    if (controller === 'ContentItemsCRUD') return '/content-library'
    if (controller === 'AssignmentsCRUD') return '/assignments'
    if (controller === 'UsersCRUD') return '/users'
    if (controller === 'Learners') return '/learners'
    if (controller === 'LearningLogsCRUD') return '/learning-logs'
    if (controller === 'EnrollmentsCRUD') return '/enrollments'
    if (controller === 'DivisionsCRUD') return '/master-data/divisions'
    if (controller === 'CategoriesCRUD') return '/master-data/categories'
    if (controller === 'CourseTypesCRUD') return '/master-data/course-types'
    if (controller === 'RolesCRUD') return '/master-data/roles'
    return '/master-data'
  }

  const handleRowDoubleClick = (e: { data: any }) => {
    if (!e.data) return
    const prefix = getRoutePrefix(config.controller)
    
    if (config.controller === 'Learners') {
      // Learners rows are camelCase since LearnersController deserializes to a typed DTO
      const code = e.data.nid || e.data.eId
      if (code) navigate(`${prefix}/${code}/profile`)
    } else if (config.controller === 'UsersCRUD') {
      if (e.data.nid) navigate(`${prefix}/${e.data.nid}`)
    } else if (e.data.id) {
      navigate(`${prefix}/${e.data.id}`)
    }
  }

  // Action buttons matching the old grid buttons layout
  const actionButtons = useMemo(() => {
    if (
      isCrudEnabled ||
      isReadOnly
    ) {
      return undefined
    }

    return [{
      hint: t(ADMIN_LABELS.openDetails),
      icon: 'info',
      onClick: (e: { row: { data: any } }) => {
        if (!e.row?.data) return
        const prefix = getRoutePrefix(config.controller)
        if (config.controller === 'Learners') {
          const code = e.row.data.nid || e.row.data.eId
          if (code) navigate(`${prefix}/${code}/profile`)
        } else if (config.controller === 'UsersCRUD') {
          if (e.row.data.nid) navigate(`${prefix}/${e.row.data.nid}`)
        } else if (e.row.data.id) {
          navigate(`${prefix}/${e.row.data.id}`)
        }
      }
    }]
  }, [config.controller, navigate, isCrudEnabled, isReadOnly])

  const isMasterData =
    config.controller === 'DivisionsCRUD' ||
    config.controller === 'CategoriesCRUD' ||
    config.controller === 'CourseTypesCRUD' ||
    config.controller === 'RolesCRUD'

  const hasGridActions =
    config.controller === 'AssignmentsCRUD' ||
    config.controller === 'ContentItemsCRUD' ||
    isMasterData

  const gridActions = hasGridActions ? (
    <div className="flex items-center gap-2">
      {isMasterData && (
        <Link to={`${getRoutePrefix(config.controller)}/new`}>
          <AppButton variant="primary" icon={Plus}>
            {tf(ADMIN_LABELS.create, title.replace(/s$/, ''))}
          </AppButton>
        </Link>
      )}

      {config.controller === 'AssignmentsCRUD' && (
        <>
          <Link to="/assignments/gantt">
            <AppButton variant="secondary" icon={Layers}>
              {t(ADMIN_LABELS.schedule)}
            </AppButton>
          </Link>
          <Link to="/assignments/bulk">
            <AppButton variant="primary" icon={Plus}>
              {t(ADMIN_LABELS.assignCourses)}
            </AppButton>
          </Link>
        </>
      )}

      {config.controller === 'ContentItemsCRUD' && isSuperAdmin && (
        <Link to="/content-library/new">
          <AppButton variant="primary" icon={Plus}>
            {t(ADMIN_LABELS.uploadScorm)}
          </AppButton>
        </Link>
      )}
    </div>
  ) : undefined

  return (
    <>
      <DataGridSurface title={t(config.gridTitle)} note={t(config.gridNote)} actions={gridActions}>
        {/* key forces a full AppTable remount when switching entity routes —
            React reuses this same EntityListPage instance across routes, so
            without it the previous entity's rows/page state leak into the new list */}
        <AppTable
          key={config.controller}
          store={store}
          columns={mappedColumns}
          noDataText={tf(ADMIN_LABELS.noDataFor, title.toLowerCase())}
          onRowDblClick={isReadOnly ? undefined : handleRowDoubleClick}
          searchPlaceholder={t(ADMIN_LABELS.searchRecords)}
          searchExpr={config.searchExpr}
          actionButtons={actionButtons}
        />
      </DataGridSurface>
    </>
  )
}