import { useState, useEffect, useMemo, useCallback } from 'react'
import { Shield, UserPlus, X, Check, Search } from 'lucide-react'
import { AppTable, type AdminGridColumn } from '../../components/ui/AppTable'
import { DataGridSurface } from '../../components/ui/DataGridSurface'
import { AppButton } from '../../components/ui/AppButton'
import { createAdminDataSource } from '../../lib/createDataSource'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'

type RoleInfo = {
  Id: number
  Name: string
  RoleType: string | null
  DivisionId: number | null
}

type UserRoleInfo = {
  UserId: number
  RoleId: number
  Role: RoleInfo | null
}

type AdminUser = {
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
  const [refreshKey, setRefreshKey] = useState(0)
  const [selectedUser, setSelectedUser] = useState<AdminUser | null>(null)
  const [allRoles, setAllRoles] = useState<RoleInfo[]>([])
  const [pendingRoleIds, setPendingRoleIds] = useState<number[]>([])
  const [saving, setSaving] = useState(false)
  const [showAddPanel, setShowAddPanel] = useState(false)
  const [addNid, setAddNid] = useState('')
  const [adding, setAdding] = useState(false)

  // Load available roles on mount
  useEffect(() => {
    fetchWithAccessControl<{ data: RoleInfo[] }>('admin/RolesCRUD/Get?requireTotalCount=false')
      .then((res) => {
        const roles = Array.isArray(res) ? res : (res as any)?.data ?? []
        setAllRoles(roles)
      })
      .catch(() => toast.error('Failed to load roles'))
  }, [])

  const store = useMemo(
    () => createAdminDataSource<AdminUser>({ controller: 'UsersCRUD', key: 'id' }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [refreshKey],
  )

  const handleSelectUser = useCallback((user: AdminUser) => {
    setSelectedUser(user)
    const roleIds = (user.userRoles ?? [])
      .map((ur) => ur.RoleId ?? ur.Role?.Id)
      .filter((id): id is number => id != null)
    setPendingRoleIds(roleIds)
  }, [])

  const toggleRole = (roleId: number) => {
    setPendingRoleIds((prev) =>
      prev.includes(roleId) ? prev.filter((id) => id !== roleId) : [...prev, roleId],
    )
  }

  const handleSaveRoles = async () => {
    if (!selectedUser) return
    setSaving(true)
    try {
      const formData = new FormData()
      formData.append('key', String(selectedUser.id))
      formData.append('values', JSON.stringify({ roleIds: pendingRoleIds }))
      await fetchWithAccessControl('admin/UsersCRUD/Put', {
        method: 'PUT',
        body: formData,
      })
      toast.success(`Roles updated for ${selectedUser.fullName || selectedUser.nid}`)
      setSelectedUser(null)
      setRefreshKey((k) => k + 1)
    } catch {
      toast.error('Failed to update roles')
    } finally {
      setSaving(false)
    }
  }

  const handleAddUser = async () => {
    const nid = addNid.trim()
    if (!nid) return
    setAdding(true)
    try {
      const formData = new FormData()
      formData.append('values', JSON.stringify({ nid }))
      await fetchWithAccessControl('admin/UsersCRUD/Post', {
        method: 'POST',
        body: formData,
      })
      toast.success(`User ${nid} added`)
      setAddNid('')
      setShowAddPanel(false)
      setRefreshKey((k) => k + 1)
    } catch {
      toast.error('Failed to add user')
    } finally {
      setAdding(false)
    }
  }

  const hasRoleChanges = useMemo(() => {
    if (!selectedUser) return false
    const currentIds = (selectedUser.userRoles ?? [])
      .map((ur) => ur.RoleId ?? ur.Role?.Id)
      .filter((id): id is number => id != null)
      .sort()
    const pendingSorted = [...pendingRoleIds].sort()
    return JSON.stringify(currentIds) !== JSON.stringify(pendingSorted)
  }, [selectedUser, pendingRoleIds])

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
            .map((ur) => ur.Role?.Name ?? ur.Role?.RoleType)
            .filter(Boolean)
          if (roles.length === 0) return <span className="text-slate-400">No roles</span>
          return (
            <div className="flex flex-wrap gap-1">
              {roles.map((r, i) => (
                <span
                  key={i}
                  className={`inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-bold ${
                    r === 'SuperAdmin'
                      ? 'bg-purple-100 text-purple-700'
                      : 'bg-indigo-100 text-indigo-700'
                  }`}
                >
                  {r}
                </span>
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
    <AppButton variant="primary" icon={UserPlus} onClick={() => setShowAddPanel(true)}>
      Add Admin User
    </AppButton>
  )

  return (
    <>
      <DataGridSurface title="Admin User Directory" note="Manage admin roles and access control. Click a row to edit roles." actions={gridActions}>
        <AppTable
          store={store}
          columns={columns}
          noDataText="No admin users found"
          searchPlaceholder="Search by NID, name, or division..."
          searchExpr={['nid', 'fullName', 'division']}
          onRowDblClick={(e) => handleSelectUser(e.data)}
          actionButtons={[
            {
              hint: 'Edit Roles',
              icon: <Shield className="h-3.5 w-3.5" />,
              onClick: (e) => handleSelectUser(e.row.data),
            },
          ]}
        />
      </DataGridSurface>

      {/* Role editor panel */}
      {selectedUser && (
        <div className="fixed inset-0 z-50 flex items-start justify-end">
          <div className="absolute inset-0 bg-black/20" onClick={() => setSelectedUser(null)} />
          <aside className="relative z-10 mt-12 mr-6 w-96 max-h-[calc(100vh-6rem)] overflow-y-auto rounded-lg border border-slate-200 bg-white shadow-lg">
            <div className="flex items-center justify-between gap-3 border-b border-slate-200 p-3.5">
              <h2 className="text-sm font-bold">Roles — {selectedUser.fullName || selectedUser.nid}</h2>
              <button onClick={() => setSelectedUser(null)} className="text-slate-400 hover:text-slate-600"><X className="h-4 w-4" /></button>
            </div>
          <div className="flex flex-col gap-4 p-4">
            <div className="space-y-1">
              <div className="text-[10px] font-extrabold uppercase text-slate-400">NID</div>
              <div className="text-xs text-slate-700">{selectedUser.nid}</div>
            </div>
            {selectedUser.fullName && (
              <div className="space-y-1">
                <div className="text-[10px] font-extrabold uppercase text-slate-400">Name</div>
                <div className="text-xs text-slate-700">{selectedUser.fullName}</div>
              </div>
            )}
            {selectedUser.division && (
              <div className="space-y-1">
                <div className="text-[10px] font-extrabold uppercase text-slate-400">Division</div>
                <div className="text-xs text-slate-700">{selectedUser.division}</div>
              </div>
            )}

            <div className="border-t border-slate-200 pt-3">
              <div className="text-[10px] font-extrabold uppercase text-slate-400 mb-2">Assigned Roles</div>
              <div className="flex flex-col gap-2">
                {allRoles.map((role) => {
                  const isChecked = pendingRoleIds.includes(role.Id)
                  return (
                    <label
                      key={role.Id}
                      className={`flex items-center gap-2.5 rounded-md border px-3 py-2 cursor-pointer transition-colors ${
                        isChecked
                          ? 'border-indigo-300 bg-indigo-50'
                          : 'border-slate-200 bg-white hover:border-slate-300'
                      }`}
                    >
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={() => toggleRole(role.Id)}
                        className="accent-indigo-600"
                      />
                      <div>
                        <div className="text-xs font-semibold text-slate-800">{role.Name}</div>
                        {role.RoleType && (
                          <div className="text-[10px] text-slate-500">{role.RoleType}</div>
                        )}
                      </div>
                    </label>
                  )
                })}
              </div>
            </div>

            <div className="flex items-center gap-2 pt-2">
              <AppButton
                variant="primary"
                icon={Check}
                onClick={handleSaveRoles}
                disabled={!hasRoleChanges || saving}
              >
                {saving ? 'Saving…' : 'Save Roles'}
              </AppButton>
              <AppButton variant="secondary" icon={X} onClick={() => setSelectedUser(null)}>
                Cancel
              </AppButton>
            </div>
            </div>
          </aside>
        </div>
      )}

      {/* Add user panel */}
      {showAddPanel && (
        <div className="fixed inset-0 z-50 flex items-start justify-end">
          <div className="absolute inset-0 bg-black/20" onClick={() => setShowAddPanel(false)} />
          <aside className="relative z-10 mt-12 mr-6 w-96 rounded-lg border border-slate-200 bg-white shadow-lg">
            <div className="flex items-center justify-between gap-3 border-b border-slate-200 p-3.5">
              <h2 className="text-sm font-bold">Add Admin User</h2>
              <button onClick={() => setShowAddPanel(false)} className="text-slate-400 hover:text-slate-600"><X className="h-4 w-4" /></button>
            </div>
          <div className="flex flex-col gap-4 p-4">
            <div className="space-y-1">
              <div className="text-[10px] font-extrabold uppercase text-slate-400">Employee NID</div>
              <div className="flex items-center gap-2">
                <div className="relative flex-1">
                  <Search className="absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-400" />
                  <input
                    type="text"
                    value={addNid}
                    onChange={(e) => setAddNid(e.target.value.toUpperCase())}
                    placeholder="e.g. N4734"
                    className="w-full rounded-md border border-slate-200 py-1.5 pl-8 pr-3 text-xs text-slate-700 focus:border-indigo-400 focus:ring-1 focus:ring-indigo-400 focus:outline-none"
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') handleAddUser()
                    }}
                  />
                </div>
              </div>
              <p className="text-[10px] text-slate-400">Enter the Windows NID of the employee to add as an admin user.</p>
            </div>

            <div className="flex items-center gap-2 pt-2">
              <AppButton
                variant="primary"
                icon={UserPlus}
                onClick={handleAddUser}
                disabled={!addNid.trim() || adding}
              >
                {adding ? 'Adding…' : 'Add User'}
              </AppButton>
              <AppButton variant="secondary" icon={X} onClick={() => setShowAddPanel(false)}>
                Cancel
              </AppButton>
            </div>
          </div>
          </aside>
        </div>
      )}
    </>
  )
}
