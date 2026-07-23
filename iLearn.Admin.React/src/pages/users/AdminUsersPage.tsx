import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { UserPlus, Edit3 } from 'lucide-react'
import { AppTable, type AdminGridColumn } from '../../components/ui/AppTable'
import { Badge } from '../../components/ui/Badge'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { AppButton } from '../../components/ui/AppButton'
import { createAdminDataSource } from '../../lib/createDataSource'
import { ADMIN_LABELS, t } from '../../lib/labels'

// Mirrors UsersCRUDController.Get (iLearn.API/Controllers/Base/UsersCRUDController.cs)
export type RoleInfo = {
  id: number
  name: string
  roleType: number | string | null
  divisionId: number | null
}

// Mirrors UsersCRUDController.Get (iLearn.API/Controllers/Base/UsersCRUDController.cs)
export type UserRoleInfo = {
  userId: number
  roleId: number
  role: RoleInfo | null
}

// Mirrors UsersCRUDController.Get (iLearn.API/Controllers/Base/UsersCRUDController.cs)
export type AdminUser = {
  id: number
  nid: string
  lastLogin: string
  isActive: boolean
  fullName: string
  email: string
  division: string
  department: string
  position: string
  userRoles: UserRoleInfo[]
}

export function AdminUsersPage() {
  const navigate = useNavigate()

  const store = useMemo(
    () => createAdminDataSource<AdminUser>({ controller: 'UsersCRUD', key: 'id' }),
    [],
  )

  const columns: AdminGridColumn<AdminUser>[] = useMemo(
    () => [
      { dataField: 'nid', caption: { th: 'NID', en: 'NID' }, width: 120 }, { dataField: 'fullName', caption: ADMIN_LABELS.fullName, minWidth: 200 }, { dataField: 'division', caption: ADMIN_LABELS.division, width: 150 }, { dataField: 'department', caption: ADMIN_LABELS.department, width: 150 }, { dataField: 'position', caption: ADMIN_LABELS.position, width: 160 },
      {
        dataField: 'userRoles',
        caption: ADMIN_LABELS.roles,
        minWidth: 200,
        cellRender: ({ data }) => {
          const roles = (data.userRoles ?? [])
            .map((ur) => ur.role?.name ?? (ur.role?.roleType != null ? String(ur.role.roleType) : ''))
            .filter(Boolean)
          if (roles.length === 0) return <span className="text-slate-400">{t(ADMIN_LABELS.noRoles)}</span>
          return (
            <div className="flex flex-wrap gap-1">
              {roles.map((r, i) => (
                <Badge
                  key={i}
                  variant="soft"
                  size="xxs"
                  tone={r === 'SuperAdmin' ? 'warning' : 'info'}
                  className="rounded-full"
                >
                  {r}
                </Badge>
              ))}
            </div>
          )
        },
      },
      { dataField: 'lastLogin', caption: ADMIN_LABELS.lastLogin, dataType: 'datetime', width: 170 },
    ],
    [],
  )

  const gridActions = (
    <Link to="/users/new">
      <AppButton variant="primary" icon={UserPlus}>
        {t(ADMIN_LABELS.addAdminUser)}
      </AppButton>
    </Link>
  )

  return (
    <DataGridSurface title={t(ADMIN_LABELS.adminUsersTitle)} note={t(ADMIN_LABELS.adminUsersDescription)} actions={gridActions}>
      <AppTable
        store={store}
        columns={columns}
        noDataText={t(ADMIN_LABELS.noAdminUsers)}
        searchPlaceholder={t(ADMIN_LABELS.searchAdmins)}
        searchExpr={['nid', 'fullName', 'division']}
        onRowDblClick={(e) => navigate(`/users/${e.data.id}`)}
        actionButtons={[
          {
            hint: t(ADMIN_LABELS.openDetails),
            icon: 'info',
            onClick: (e) => navigate(`/users/${e.row.data.id}`),
          },
          {
            hint: t(ADMIN_LABELS.editRoles),
            icon: <Edit3 className="h-3.5 w-3.5" />,
            onClick: (e) => navigate(`/users/${e.row.data.id}/edit`),
          },
        ]}
      />
    </DataGridSurface>
  )
}
