import { useMemo } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { AppButton } from '../components/ui/AppButton'
import { DataGridSurface } from '../components/ui/DataGridSurface'
import { AppTable } from '../components/ui/AppTable'
import { createAdminDataSource } from '../lib/createDataSource'
import type { AdminListConfig } from './moduleConfigs'

type EntityListPageProps = {
  config: AdminListConfig
}

export function EntityListPage({ config }: EntityListPageProps) {
  const navigate = useNavigate()
  
  const crudControllers = new Set([
    'LearnerGroupsCRUD',
    'DivisionsCRUD',
    'CategoriesCRUD',
    'CourseTypesCRUD',
    'RolesCRUD',
  ])
  const isCrudEnabled = crudControllers.has(config.controller)
  const isReadOnly =
    config.controller === 'LearningLogsCRUD' || config.controller === 'EnrollmentsCRUD'

  const store = useMemo(
    () => createAdminDataSource<any>({ 
      controller: config.controller, 
      key: config.key,
      enableCrud: isCrudEnabled
    }),
    [config.controller, config.key, isCrudEnabled],
  )

  const getRoutePrefix = (controller: string) => {
    if (controller === 'CoursesCRUD') return '/courses'
    if (controller === 'ContentItemsCRUD') return '/content-library'
    if (controller === 'AssignmentsCRUD') return '/assignments'
    if (controller === 'LearnerGroupsCRUD') return '/learner-groups'
    if (controller === 'UsersCRUD') return '/learners'
    if (controller === 'LearningLogsCRUD') return '/learning-logs'
    if (controller === 'EnrollmentsCRUD') return '/enrollments'
    if (controller === 'CategoriesCRUD') return '/master-data/categories'
    if (controller === 'CourseTypesCRUD') return '/master-data/course-types'
    if (controller === 'RolesCRUD') return '/master-data/roles'
    return '/master-data'
  }

  const handleRowDoubleClick = (e: { data: any }) => {
    if (!e.data) return
    const prefix = getRoutePrefix(config.controller)
    
    if (config.controller === 'UsersCRUD') {
      if (e.data.nid) navigate(`${prefix}/${e.data.nid}/profile`)
    } else if (config.controller !== 'DivisionsCRUD' && e.data.id) {
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
        if (config.controller === 'UsersCRUD') {
          if (e.row.data.nid) navigate(`${prefix}/${e.row.data.nid}/profile`)
        } else if (e.row.data.id) {
          navigate(`${prefix}/${e.row.data.id}`)
        }
      }
    }]
  }, [config.controller, navigate, isCrudEnabled, isReadOnly])

  const hasGridActions =
    config.controller === 'LearnerGroupsCRUD' ||
    config.controller === 'AssignmentsCRUD' ||
    config.controller === 'ContentItemsCRUD'

  const gridActions = hasGridActions ? (
    <div className="flex items-center gap-2">
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
        <AppTable
          store={store}
          columns={config.columns}
          noDataText={`No ${config.title.toLowerCase()} data found`}
          onRowDblClick={handleRowDoubleClick}
          searchPlaceholder="Search records..."
          actionButtons={actionButtons}
        />
      </DataGridSurface>
    </>
  )
}