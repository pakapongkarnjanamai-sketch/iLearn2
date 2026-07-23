import { useEffect, useState, useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Search, UserPlus, Check } from 'lucide-react'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { AppWizard, type WizardStep } from '../../components/ui/AppWizard'
import { Badge } from '../../components/ui/Badge'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import type { RoleInfo, UserRoleInfo, AdminUser } from './AdminUsersPage'
import { ADMIN_LABELS, t, tf } from '../../lib/labels'

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
      .catch(() => toast.error(t(ADMIN_LABELS.failedToLoadRoles)))
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
          toast.error(t(ADMIN_LABELS.failedToLoadUserInfo))
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
      toast.error(t(ADMIN_LABELS.employeeNidRequired))
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
            toast.success(tf(ADMIN_LABELS.userCreatedWithRoles, nid.trim()))
          } catch (err) {
            console.error(err)
            toast.error(t(ADMIN_LABELS.roleAssignmentFailedAfterCreate))
          }
        } else {
          toast.success(tf(ADMIN_LABELS.userCreated, nid.trim()))
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
        toast.success(tf(ADMIN_LABELS.rolesUpdatedFor, user?.fullName || nid))
      }
      navigate('/users')
    } catch (err) {
      console.error(err)
      toast.error(t(isCreate ? ADMIN_LABELS.failedToCreateUser : ADMIN_LABELS.failedToSaveRoles))
    } finally {
      setSubmitting(false)
    }
  }

  const renderUserStep = () => (
    <div className="space-y-4">
      <div className="space-y-1.5">
        <label className="wiz-label">
          {t(ADMIN_LABELS.employeeNid)} <span className="text-red-500">*</span>
        </label>
        <div className="relative">
          <Search className="absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            value={nid}
            onChange={(e) => setNid(e.target.value.toUpperCase())}
            placeholder={t(ADMIN_LABELS.enterNid)}
            className="w-full rounded-md border border-slate-200 py-1.5 pl-8 pr-3 text-xs text-slate-700 focus:border-indigo-400 focus:ring-1 focus:ring-indigo-400 focus:outline-none"
            autoFocus
          />
        </div>
        <p className="text-[10px] text-slate-400">{t(ADMIN_LABELS.adminNidHelp)}</p>
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
              <div className="text-[10px] font-extrabold uppercase text-slate-400">{t(ADMIN_LABELS.name)}</div>
              <div className="text-xs font-bold text-slate-700">{user.fullName}</div>
            </div>
          )}
          {user.division && (
            <div className="space-y-0.5">
              <div className="text-[10px] font-extrabold uppercase text-slate-400">{t(ADMIN_LABELS.division)}</div>
              <div className="text-xs font-bold text-slate-700">{user.division}</div>
            </div>
          )}
        </div>
      )}

      <div className="space-y-2 select-none">
        <label className="wiz-label">{t(ADMIN_LABELS.assignedRoles)}</label>
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
          <dt className="wiz-label">{t(ADMIN_LABELS.employeeNid)}</dt>
          <dd className="col-span-2 text-slate-700 font-bold">{nid.trim() || '—'}</dd>
        </div>
        {!isCreate && user && (
          <>
            {user.fullName && (
              <div className="grid grid-cols-3 py-2.5 font-semibold">
                <dt className="wiz-label">{t(ADMIN_LABELS.name)}</dt>
                <dd className="col-span-2 text-slate-700">{user.fullName}</dd>
              </div>
            )}
            {user.division && (
              <div className="grid grid-cols-3 py-2.5 font-semibold">
                <dt className="wiz-label">{t(ADMIN_LABELS.division)}</dt>
                <dd className="col-span-2 text-slate-700">{user.division}</dd>
              </div>
            )}
          </>
        )}
      </dl>

      {isCreate ? (
        <div className="space-y-1.5 select-none">
          <div className="text-[10px] font-extrabold uppercase text-slate-400">{tf(ADMIN_LABELS.selectedRoles, pendingRoleIds.length)}</div>
          {pendingRoleIds.length === 0 ? (
            <p className="text-xs text-slate-400 font-semibold italic">{t(ADMIN_LABELS.noRolesSelected)}</p>
          ) : (
            <div className="flex flex-wrap gap-1.5">
              {pendingRoleIds.map((roleId) => (
                <Badge key={roleId} variant="outline" tone="info">
                  {rolesMap.get(roleId) || `Role ${roleId}`}
                </Badge>
              ))}
            </div>
          )}
        </div>
      ) : (
        <div className="space-y-3 select-none">
          <div className="text-[10px] font-extrabold uppercase text-slate-400">{t(ADMIN_LABELS.roleChanges)}</div>
          {addedRoles.length === 0 && removedRoles.length === 0 ? (
            <p className="text-xs text-slate-400 font-semibold italic">{t(ADMIN_LABELS.noRoleChanges)}</p>
          ) : (
            <div className="space-y-2 text-xs">
              {addedRoles.length > 0 && (
                <div>
                  <div className="font-bold text-emerald-600 mb-1">{t(ADMIN_LABELS.toBeAdded)}</div>
                  <div className="flex flex-wrap gap-1.5">
                    {addedRoles.map((name) => (
                      <Badge key={name} variant="outline" tone="success">
                        + {name}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
              {removedRoles.length > 0 && (
                <div>
                  <div className="font-bold text-rose-600 mb-1">{t(ADMIN_LABELS.toBeRemoved)}</div>
                  <div className="flex flex-wrap gap-1.5">
                    {removedRoles.map((name) => (
                      <Badge key={name} variant="outline" tone="danger" size="xxs">
                        - {name}
                      </Badge>
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

  const steps: WizardStep[] = isCreate
    ? [
        { label: t(ADMIN_LABELS.userStep), validate: validateUser, render: renderUserStep }, { label: t(ADMIN_LABELS.roles), validate: validateRoles, render: renderRolesStep }, { label: t(ADMIN_LABELS.review), render: renderReviewStep },
      ]
    : [
        { label: t(ADMIN_LABELS.roles), validate: validateRoles, render: renderRolesStep }, { label: t(ADMIN_LABELS.review), render: renderReviewStep },
      ]

  if (loadingUser) {
    return <LoadingState label={t(ADMIN_LABELS.loadingUserData)} />
  }

  if (notFound) {
    return (
      <NotFoundState
        title={t(ADMIN_LABELS.adminUserNotFound)} message={t(ADMIN_LABELS.adminUserNotFoundMessage)}
        backTo="/users"
        backLabel={t(ADMIN_LABELS.backToDirectory)}
      />
    )
  }

  return (
    <AppWizard
      title={t(isCreate ? ADMIN_LABELS.newAdminUser : ADMIN_LABELS.editAdminRoles)} description={t(isCreate ? ADMIN_LABELS.registerAdminUser : ADMIN_LABELS.manageAdminRoles)} eyebrow={t(ADMIN_LABELS.adminUsersTitle)}
      steps={steps}
      currentStep={currentStep}
      onStepChange={setCurrentStep}
      onCancel={() => navigate('/users')}
      onSubmit={handleSubmit}
      submitLabel={t(isCreate ? ADMIN_LABELS.createUser : ADMIN_LABELS.saveChanges)}
      isSubmitting={submitting}
      submitIcon={isCreate ? <UserPlus className="h-3.5 w-3.5" /> : <Check className="h-3.5 w-3.5" />}
    />
  )
}
