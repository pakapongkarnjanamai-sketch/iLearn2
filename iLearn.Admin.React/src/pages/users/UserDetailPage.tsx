import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Edit3, Trash2, User } from 'lucide-react'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import {
  DetailLayout,
  DetailSubSection,
  Fact,
  FactGrid,
} from '../../components/ui/detail'
import { Card } from '../../components/ui/Card'
import { StatusText } from '../../components/ui/StatusText'
import { Badge } from '../../components/ui/Badge'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { formatDateTime } from '../../lib/format'
import type { AdminUser } from './AdminUsersPage'
import { ADMIN_LABELS, t, tf } from '../../lib/labels'

export function UserDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { confirm } = useConfirm()
  const { setLabel } = useBreadcrumbs()
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [user, setUser] = useState<AdminUser | null>(null)

  useEffect(() => {
    if (user && id) {
      setLabel(String(id), user.fullName || user.nid)
    }
  }, [user, id, setLabel])

  const load = async () => {
    setLoading(true)
    try {
      const filter = JSON.stringify([['id', '=', Number(id)]])
      const res = await fetchWithAccessControl<{ data: AdminUser[] }>(
        `admin/UsersCRUD/Get?filter=${encodeURIComponent(filter)}`,
      )
      const list = Array.isArray(res) ? res : res?.data ?? []
      if (list.length > 0) {
        setUser(list[0])
      } else {
        setUser(null)
      }
    } catch {
      toast.error(t(ADMIN_LABELS.failedToLoadUserDetails))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  const handleDelete = async () => {
    if (!user) return
    const ok = await confirm({
      title: t(ADMIN_LABELS.deleteAdminUser), message: tf(ADMIN_LABELS.deleteAdminUserConfirm, user.fullName || user.nid), confirmLabel: t(ADMIN_LABELS.deleteUser),
      danger: true,
    })
    if (!ok) return

    setBusy(true)
    try {
      const fd = new FormData()
      fd.append('key', String(user.id))
      await fetchWithAccessControl('admin/UsersCRUD/Delete', { method: 'DELETE', body: fd })
      toast.success(t(ADMIN_LABELS.adminUserDeleted))
      navigate('/users')
    } catch {
      toast.error(t(ADMIN_LABELS.failedToDeleteAdminUser))
    } finally {
      setBusy(false)
    }
  }

  if (loading) {
    return <LoadingState label={t(ADMIN_LABELS.loadingUserDetails)} />
  }

  if (!user) {
    return (
      <NotFoundState
        title={t(ADMIN_LABELS.adminUserNotFound)} message={t(ADMIN_LABELS.adminUserNotFoundDetail)}
        backTo="/users"
        backLabel={t(ADMIN_LABELS.backToDirectory)}
      />
    )
  }

  const roles = (user.userRoles ?? [])
    .map((ur) => ur.role?.name ?? (ur.role?.roleType != null ? String(ur.role.roleType) : ''))
    .filter(Boolean)

  return (
    <>
      <DetailLayout
        sidebar={
          <ControlsSidebar>
            <ControlAction
              onClick={() => navigate(`/users/${id}/edit`)}
              icon={Edit3}
              disabled={busy}
            >
              {t(ADMIN_LABELS.editRoles)}
            </ControlAction>
            <ControlAction
              onClick={handleDelete}
              icon={Trash2}
              variant="danger"
              disabled={busy}
            >
              {t(ADMIN_LABELS.deleteUser)}
            </ControlAction>
          </ControlsSidebar>
        }
      >
        <Card icon={User} title={t(ADMIN_LABELS.overview)} bodyClassName="p-5 space-y-6">
          <FactGrid>
            <Fact label={t(ADMIN_LABELS.status)}>
              <StatusText active={user.isActive} />
            </Fact>

            <Fact label={t(ADMIN_LABELS.employeeNid)} mono valueClassName="font-bold">{user.nid}</Fact>

            <Fact label={t(ADMIN_LABELS.lastLogin)}>
              {user.lastLogin ? formatDateTime(new Date(user.lastLogin)) : '—'}
            </Fact>

            {user.email && (
              <Fact label={t(ADMIN_LABELS.emailAddress)} colSpan={2} valueClassName="break-all">
                {user.email}
              </Fact>
            )}
          </FactGrid>

          <DetailSubSection title={t(ADMIN_LABELS.organizationInfo)}>
            <FactGrid cols={2}>
              <Fact label={t(ADMIN_LABELS.division)} valueClassName="font-semibold">
                {user.division || '—'}
              </Fact>
              <Fact label={t(ADMIN_LABELS.department)} valueClassName="font-semibold">
                {user.department || '—'}
              </Fact>
              <Fact label={t(ADMIN_LABELS.position)} valueClassName="font-semibold">
                {user.position || '—'}
              </Fact>
            </FactGrid>
          </DetailSubSection>

          <DetailSubSection title={t(ADMIN_LABELS.administrativeRoles)}>
            <div className="flex flex-wrap gap-1.5 select-none pt-1">
              {roles.length === 0 ? (
                <span className="text-xs text-slate-400 font-semibold italic">{t(ADMIN_LABELS.noRolesAssigned)}</span>
              ) : (
                roles.map((r, i) => (
                  <Badge
                    key={i}
                    variant="outline"
                    tone={r === 'SuperAdmin' ? 'success' : 'info'}
                  >
                    {r}
                  </Badge>
                ))
              )}
            </div>
          </DetailSubSection>
        </Card>
      </DetailLayout>
    </>
  )
}
