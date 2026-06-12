import { useEffect, useState, useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Search, UserPlus, Check } from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import type { RoleInfo, UserRoleInfo, AdminUser } from './AdminUsersPage'

export function UserEditorPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isCreate = !id

  const [loadingUser, setLoadingUser] = useState(!isCreate)
  const [notFound, setNotFound] = useState(false)
  const [user, setUser] = useState<AdminUser | null>(null)
  const [allRoles, setAllRoles] = useState<RoleInfo[]>([])
  const [pendingRoleIds, setPendingRoleIds] = useState<number[]>([])
  const [nid, setNid] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [currentStep, setCurrentStep] = useState(1)

  useEffect(() => {
    // Load all roles for lookup
    fetchWithAccessControl<{ data: RoleInfo[] }>('admin/RolesCRUD/Get?requireTotalCount=false')
      .then((res) => {
        const roles = Array.isArray(res) ? res : (res as any)?.data ?? []
        setAllRoles(roles)
      })
      .catch(() => toast.error('Failed to load roles'))
  }, [])

  useEffect(() => {
    if (isCreate) return
    let cancelled = false
    setLoadingUser(true)

    const loadUser = async () => {
      try {
        const filter = JSON.stringify([['id', '=', Number(id)]])
        const res = await fetchWithAccessControl<{ data: AdminUser[] }>(
          `admin/UsersCRUD/Get?filter=${encodeURIComponent(filter)}`,
        )
        if (cancelled) return

        const list = Array.isArray(res) ? res : res?.data ?? []
        if (list.length > 0) {
          const u = list[0]
          setUser(u)
          setNid(u.nid)
          const roleIds = (u.userRoles ?? [])
            .map((ur: UserRoleInfo) => ur.roleId ?? ur.role?.id)
            .filter((roleId: number | null | undefined): roleId is number => roleId != null)
          setPendingRoleIds(roleIds)
        } else {
          setNotFound(true)
        }
      } catch (err) {
        console.error(err)
        if (!cancelled) {
          toast.error('Failed to load user info')
          setNotFound(true)
        }
      } finally {
        if (!cancelled) setLoadingUser(false)
      }
    }

    void loadUser()

    return () => {
      cancelled = true
    }
  }, [id, isCreate])

  const toggleRole = (roleId: number) => {
    setPendingRoleIds((prev) =>
      prev.includes(roleId) ? prev.filter((roleIdValue) => roleIdValue !== roleId) : [...prev, roleId],
    )
  }

  const validateUser = () => {
    if (!nid.trim()) {
      toast.error('Employee NID is required')
      return false
    }
    return true
  }

  const validateRoles = () => true

  const handleSubmit = async () => {
    setSubmitting(true)
    try {
      if (isCreate) {
        // 1. Create the user
        const formData = new FormData()
        formData.append('values', JSON.stringify({ nid: nid.trim() }))
        
        const newUser = await fetchWithAccessControl<AdminUser>('admin/UsersCRUD/Post', {
          method: 'POST',
          body: formData,
        })

        // 2. Assign selected roles (if any)
        if (pendingRoleIds.length > 0) {
          try {
            const roleData = new FormData()
            roleData.append('key', String(newUser.id))
            roleData.append('values', JSON.stringify({ roleIds: pendingRoleIds }))
            await fetchWithAccessControl('admin/UsersCRUD/Put', {
              method: 'PUT',
              body: roleData,
            })
            toast.success(`User ${nid.trim()} created with assigned roles`)
          } catch (err) {
            console.error(err)
            toast.error('User was created, but role assignment failed. Please update roles on the edit page.')
          }
        } else {
          toast.success(`User ${nid.trim()} created successfully`)
        }
      } else {
        // Edit mode
        const formData = new FormData()
        formData.append('key', String(id))
        formData.append('values', JSON.stringify({ roleIds: pendingRoleIds }))
        await fetchWithAccessControl('admin/UsersCRUD/Put', {
          method: 'PUT',
          body: formData,
        })
        toast.success(`Roles updated for ${user?.fullName || nid}`)
      }
      navigate('/users')
    } catch (err) {
      console.error(err)
      toast.error(isCreate ? 'Failed to create user' : 'Failed to save role changes')
    } finally {
      setSubmitting(false)
    }
  }

  const renderUserStep = () => (
    <div className="space-y-4">
      <div className="space-y-1.5">
        <label className="wiz-label">
          Employee NID <span className="text-red-500">*</span>
        </label>
        <div className="relative">
          <Search className="absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            value={nid}
            onChange={(e) => setNid(e.target.value.toUpperCase())}
            placeholder="e.g. N4734"
            className="w-full rounded-md border border-slate-200 py-1.5 pl-8 pr-3 text-xs text-slate-700 focus:border-indigo-400 focus:ring-1 focus:ring-indigo-400 focus:outline-none"
            autoFocus
          />
        </div>
        <p className="text-[10px] text-slate-400">Enter the Windows NID of the employee to add as an admin user.</p>
      </div>
    </div>
  )

  const renderRolesStep = () => (
    <div className="space-y-4">
      {!isCreate && user && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3 bg-slate-50/50 p-4 rounded-lg border border-slate-100 select-none mb-4">
          <div className="space-y-0.5">
            <div className="text-[10px] font-extrabold uppercase text-slate-400">NID</div>
            <div className="text-xs font-bold text-slate-700">{user.nid}</div>
          </div>
          {user.fullName && (
            <div className="space-y-0.5">
              <div className="text-[10px] font-extrabold uppercase text-slate-400">Name</div>
              <div className="text-xs font-bold text-slate-700">{user.fullName}</div>
            </div>
          )}
          {user.division && (
            <div className="space-y-0.5">
              <div className="text-[10px] font-extrabold uppercase text-slate-400">Division</div>
              <div className="text-xs font-bold text-slate-700">{user.division}</div>
            </div>
          )}
        </div>
      )}

      <div className="space-y-2 select-none">
        <label className="wiz-label">Assigned Roles</label>
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          {allRoles.map((role) => {
            const isChecked = pendingRoleIds.includes(role.id)
            return (
              <label
                key={role.id}
                className={`flex items-center gap-2.5 rounded-md border px-3 py-2 cursor-pointer transition-colors ${
                  isChecked
                    ? 'border-indigo-300 bg-indigo-50'
                    : 'border-slate-200 bg-white hover:border-slate-300'
                }`}
              >
                <input
                  type="checkbox"
                  checked={isChecked}
                  onChange={() => toggleRole(role.id)}
                  className="accent-indigo-600"
                />
                <div>
                  <div className="text-xs font-semibold text-slate-800">{role.name}</div>
                  {role.roleType != null && (
                    <div className="text-[10px] text-slate-500">{String(role.roleType)}</div>
                  )}
                </div>
              </label>
            )
          })}
        </div>
      </div>
    </div>
  )

  const initialRoleIds = useMemo(() => {
    if (isCreate || !user) return []
    return (user.userRoles ?? [])
      .map((ur) => ur.roleId ?? ur.role?.id)
      .filter((roleId): roleId is number => roleId != null)
  }, [user, isCreate])

  const rolesMap = useMemo(() => {
    const map = new Map<number, string>()
    for (const r of allRoles) {
      map.set(r.id, r.name)
    }
    return map
  }, [allRoles])

  const addedRoles = useMemo(() => {
    return pendingRoleIds.filter((roleId) => !initialRoleIds.includes(roleId)).map((roleId) => rolesMap.get(roleId) || `Role ${roleId}`)
  }, [pendingRoleIds, initialRoleIds, rolesMap])

  const removedRoles = useMemo(() => {
    return initialRoleIds.filter((roleId) => !pendingRoleIds.includes(roleId)).map((roleId) => rolesMap.get(roleId) || `Role ${roleId}`)
  }, [pendingRoleIds, initialRoleIds, rolesMap])

  const renderReviewStep = () => (
    <div className="space-y-4">
      <dl className="divide-y divide-slate-100 text-sm select-none">
        <div className="grid grid-cols-3 py-2.5 font-semibold">
          <dt className="wiz-label">Employee NID</dt>
          <dd className="col-span-2 text-slate-700 font-bold">{nid.trim() || '—'}</dd>
        </div>
        {!isCreate && user && (
          <>
            {user.fullName && (
              <div className="grid grid-cols-3 py-2.5 font-semibold">
                <dt className="wiz-label">Name</dt>
                <dd className="col-span-2 text-slate-700">{user.fullName}</dd>
              </div>
            )}
            {user.division && (
              <div className="grid grid-cols-3 py-2.5 font-semibold">
                <dt className="wiz-label">Division</dt>
                <dd className="col-span-2 text-slate-700">{user.division}</dd>
              </div>
            )}
          </>
        )}
      </dl>

      {isCreate ? (
        <div className="space-y-1.5 select-none">
          <div className="text-[10px] font-extrabold uppercase text-slate-400">Selected Roles ({pendingRoleIds.length})</div>
          {pendingRoleIds.length === 0 ? (
            <p className="text-xs text-slate-400 font-semibold italic">No roles selected. User will have no administrative permissions.</p>
          ) : (
            <div className="flex flex-wrap gap-1.5">
              {pendingRoleIds.map((roleId) => (
                <span key={roleId} className="inline-flex items-center rounded-full bg-indigo-50 border border-indigo-100 px-2.5 py-0.5 text-xs font-bold text-indigo-700">
                  {rolesMap.get(roleId) || `Role ${roleId}`}
                </span>
              ))}
            </div>
          )}
        </div>
      ) : (
        <div className="space-y-3 select-none">
          <div className="text-[10px] font-extrabold uppercase text-slate-400">Role Changes Summary</div>
          {addedRoles.length === 0 && removedRoles.length === 0 ? (
            <p className="text-xs text-slate-400 font-semibold italic">No role changes detected.</p>
          ) : (
            <div className="space-y-2 text-xs">
              {addedRoles.length > 0 && (
                <div>
                  <div className="font-bold text-emerald-600 mb-1">To Be Added:</div>
                  <div className="flex flex-wrap gap-1.5">
                    {addedRoles.map((name) => (
                      <span key={name} className="inline-flex items-center rounded-full bg-emerald-50 border border-emerald-100 px-2.5 py-0.5 text-xs font-bold text-emerald-700">
                        + {name}
                      </span>
                    ))}
                  </div>
                </div>
              )}
              {removedRoles.length > 0 && (
                <div>
                  <div className="font-bold text-rose-600 mb-1">To Be Removed:</div>
                  <div className="flex flex-wrap gap-1.5">
                    {removedRoles.map((name) => (
                      <span key={name} className="inline-flex items-center rounded-full bg-rose-50 border border-rose-100 px-2.5 py-0.5 text-xs font-bold text-rose-700">
                        - {name}
                      </span>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  )

  const steps: WizardStep[] = useMemo(() => {
    if (isCreate) {
      return [
        { label: 'User', validate: validateUser, render: renderUserStep },
        { label: 'Roles', validate: validateRoles, render: renderRolesStep },
        { label: 'Review', render: renderReviewStep },
      ]
    }
    return [
      { label: 'Roles', validate: validateRoles, render: renderRolesStep },
      { label: 'Review', render: renderReviewStep },
    ]
  }, [isCreate, nid, pendingRoleIds, allRoles, user])

  if (loadingUser) {
    return <LoadingState label="Loading user data..." />
  }

  if (notFound) {
    return (
      <NotFoundState
        title="Admin User Not Found"
        message="The admin user you are trying to edit does not exist or has been deleted."
        backTo="/users"
        backLabel="Back to Directory"
      />
    )
  }

  return (
    <AppWizard
      title={isCreate ? 'Add Admin User' : 'Edit Admin Roles'}
      description={isCreate ? 'Add a new administrative user by NID and assign access permissions.' : 'Adjust division roles and privilege levels for this administrator.'}
      eyebrow="Admin Users"
      steps={steps}
      currentStep={currentStep}
      onStepChange={setCurrentStep}
      onCancel={() => navigate('/users')}
      onSubmit={handleSubmit}
      submitLabel={isCreate ? 'Create User' : 'Save Changes'}
      isSubmitting={submitting}
      submitIcon={isCreate ? <UserPlus className="h-3.5 w-3.5" /> : <Check className="h-3.5 w-3.5" />}
    />
  )
}
