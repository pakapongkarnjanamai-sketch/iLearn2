import { useMemo, useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { AppButton } from '../components/ui/AppButton'
import { DataGridSurface } from '../components/ui/DataGridSurface'
import { AppTable } from '../components/ui/AppTable'
import { createAdminDataSource } from '../lib/createDataSource'
import { createRestDataSource } from '../lib/createRestDataSource'
import { fetchWithAccessControl } from '../lib/apiClient'
import type { AdminListConfig } from './moduleConfigs'

type EntityListPageProps = {
  config: AdminListConfig
}

export function EntityListPage({ config }: EntityListPageProps) {
  const navigate = useNavigate()
  
  const [divisions, setDivisions] = useState<any[]>([])
  const [categories, setCategories] = useState<any[]>([])

  useEffect(() => {
    if (config.controller === 'AssignmentsCRUD' || config.controller === 'LearnerGroupsCRUD') {
      fetchWithAccessControl<any>('admin/DivisionsCRUD/Get')
        .then(res => {
          if (res && Array.isArray(res.data)) {
            setDivisions(res.data)
          } else if (Array.isArray(res)) {
            setDivisions(res)
          }
        })
        .catch(err => console.error('Failed to load divisions for lookup', err))
    }
    if (config.controller === 'LearnerGroupsCRUD') {
      fetchWithAccessControl<any>('admin/CategoriesCRUD/Get')
        .then(res => {
          if (res && Array.isArray(res.data)) {
            setCategories(res.data)
          } else if (Array.isArray(res)) {
            setCategories(res)
          }
        })
        .catch(err => console.error('Failed to load categories for lookup', err))
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
            return div ? div.name : `Division ${value}`
          }
        }
      }
      if (col.dataField === 'categoryId') {
        return {
          ...col,
          cellRender: ({ value }: any) => {
            if (value === null || value === undefined) return '—'
            const cat = categories.find(c => c.id === Number(value))
            return cat ? cat.name : `Category ${value}`
          }
        }
      }
      return col
    })
  }, [config.columns, divisions, categories])

  const crudControllers = new Set([
    'LearnerGroupsCRUD',
  ])
  const isCrudEnabled = crudControllers.has(config.controller)
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
    if (config.controller === 'LearnerGroupsCRUD') {
      return createRestDataSource<any>({
        controller: 'LearnerGroups',
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
    if (controller === 'LearnerGroupsCRUD') return '/learner-groups'
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
      const code = e.data.NID || e.data.EId
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
      hint: 'Open Details',
      icon: 'info',
      onClick: (e: { row: { data: any } }) => {
        if (!e.row?.data) return
        const prefix = getRoutePrefix(config.controller)
        if (config.controller === 'Learners') {
          const code = e.row.data.NID || e.row.data.EId
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
    config.controller === 'LearnerGroupsCRUD' ||
    config.controller === 'AssignmentsCRUD' ||
    config.controller === 'ContentItemsCRUD' ||
    isMasterData

  const gridActions = hasGridActions ? (
    <div className="flex items-center gap-2">
      {isMasterData && (
        <Link to={`${getRoutePrefix(config.controller)}/new`}>
          <AppButton variant="primary" icon={Plus}>
            Create {config.title.replace(/s$/, '')}
          </AppButton>
        </Link>
      )}

      {config.controller === 'LearnerGroupsCRUD' && (
        <Link to="/learner-groups/new">
          <AppButton variant="primary" icon={Plus}>
            Create Group
          </AppButton>
        </Link>
      )}

      {config.controller === 'AssignmentsCRUD' && (
        <Link to="/assignments/bulk">
          <AppButton variant="primary" icon={Plus}>
            Bulk Assignment
          </AppButton>
        </Link>
      )}

      {config.controller === 'ContentItemsCRUD' && (
        <Link to="/content-library/new">
          <AppButton variant="primary" icon={Plus}>
            Upload SCORM
          </AppButton>
        </Link>
      )}
    </div>
  ) : undefined

  return (
    <>
      <DataGridSurface title={config.gridTitle} note={config.gridNote} actions={gridActions}>
        {/* key forces a full AppTable remount when switching entity routes —
            React reuses this same EntityListPage instance across routes, so
            without it the previous entity's rows/page state leak into the new list */}
        <AppTable
          key={config.controller}
          store={store}
          columns={mappedColumns}
          noDataText={`No ${config.title.toLowerCase()} data found`}
          onRowDblClick={handleRowDoubleClick}
          searchPlaceholder="Search records..."
          searchExpr={config.searchExpr}
          actionButtons={actionButtons}
        />
      </DataGridSurface>
    </>
  )
}