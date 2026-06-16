import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { UserPlus, Edit3 } from 'lucide-react'
import { AppTable, type AdminGridColumn } from '../../components/ui/AppTable'
import { Badge } from '../../components/ui/Badge'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { AppButton } from '../../components/ui/AppButton'
import { createAdminDataSource } from '../../lib/createDataSource'

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
      { dataField: 'nid', caption: 'NID', width: 120 },
      { dataField: 'fullName', caption: 'Full Name', minWidth: 200 },
      { dataField: 'division', caption: 'Division', width: 150 },
      { dataField: 'department', caption: 'Department', width: 150 },
      { dataField: 'position', caption: 'Position', width: 160 },
      {
        dataField: 'userRoles',
        caption: 'Roles',
        minWidth: 200,
        cellRender: ({ data }) => {
          const roles = (data.userRoles ?? [])
            .map((ur) => ur.role?.name ?? (ur.role?.roleType != null ? String(ur.role.roleType) : ''))
            .filter(Boolean)
          if (roles.length === 0) return <span className="text-slate-400">No roles</span>
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
      { dataField: 'lastLogin', caption: 'Last Login', dataType: 'datetime', width: 170 },
    ],
    [],
  )

  const gridActions = (
    <Link to="/users/new">
      <AppButton variant="primary" icon={UserPlus}>
        Add Admin User
      </AppButton>
    </Link>
  )

  return (
    <DataGridSurface title="Admin Users" note="Manage administrative roles and access control." actions={gridActions}>
      <AppTable
        store={store}
        columns={columns}
        noDataText="No admin users found"
        searchPlaceholder="Search by NID, name, or division..."
        searchExpr={['nid', 'fullName', 'division']}
        onRowDblClick={(e) => navigate(`/users/${e.data.id}`)}
        actionButtons={[
          {
            hint: 'Open Details',
            icon: 'info',
            onClick: (e) => navigate(`/users/${e.row.data.id}`),
          },
          {
            hint: 'Edit Roles',
            icon: <Edit3 className="h-3.5 w-3.5" />,
            onClick: (e) => navigate(`/users/${e.row.data.id}/edit`),
          },
        ]}
      />
    </DataGridSurface>
  )
}
