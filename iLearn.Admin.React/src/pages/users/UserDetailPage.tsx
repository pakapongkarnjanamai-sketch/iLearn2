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
import { SectionHeader } from '../../components/ui/SectionHeader'
import { StatusText } from '../../components/ui/StatusText'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { fetchWithAccessControl } from '../../lib/apiClient'
import { toast } from '../../lib/toast'
import { useBreadcrumbs } from '../../lib/breadcrumbContext'
import { formatDateTime } from '../../lib/format'
import type { AdminUser } from './AdminUsersPage'

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
      toast.error('Failed to load user details')
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
      title: 'Delete Admin User',
      message: `Delete administrative user "${user.fullName || user.nid}"? This action cannot be undone.`,
      confirmLabel: 'Delete User',
      danger: true,
    })
    if (!ok) return

    setBusy(true)
    try {
      const fd = new FormData()
      fd.append('key', String(user.id))
      await fetchWithAccessControl('admin/UsersCRUD/Delete', { method: 'DELETE', body: fd })
      toast.success('Admin user deleted successfully')
      navigate('/users')
    } catch {
      toast.error('Failed to delete admin user')
    } finally {
      setBusy(false)
    }
  }

  if (loading) {
    return <LoadingState label="Loading user details..." />
  }

  if (!user) {
    return (
      <NotFoundState
        title="Admin User Not Found"
        message="The requested administrative user does not exist or has been deleted."
        backTo="/users"
        backLabel="Back to Directory"
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
              Edit Roles
            </ControlAction>
            <ControlAction
              onClick={handleDelete}
              icon={Trash2}
              variant="danger"
              disabled={busy}
            >
              Delete User
            </ControlAction>
          </ControlsSidebar>
        }
      >
        <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
          <SectionHeader icon={User} variant="card">User Overview</SectionHeader>

          <div className="p-5 space-y-6">
            <FactGrid>
              <Fact label="Status">
                <StatusText tone={user.isActive ? 'success' : 'neutral'}>
                  {user.isActive ? 'Active' : 'Inactive'}
                </StatusText>
              </Fact>

              <Fact label="Employee NID" mono valueClassName="font-bold">{user.nid}</Fact>

              <Fact label="Last Login">
                {user.lastLogin ? formatDateTime(new Date(user.lastLogin)) : '—'}
              </Fact>

              {user.email && (
                <Fact label="Email Address" colSpan={2} valueClassName="break-all">
                  {user.email}
                </Fact>
              )}
            </FactGrid>

            <DetailSubSection title="Organization Info">
              <FactGrid cols={2} className="gap-4 select-none">
                <Fact
                  label="Division"
                  labelClassName="text-slate-400 font-semibold"
                  valueClassName="mt-0.5 font-bold"
                >
                  {user.division || '—'}
                </Fact>
                <Fact
                  label="Department"
                  labelClassName="text-slate-400 font-semibold"
                  valueClassName="mt-0.5 font-bold"
                >
                  {user.department || '—'}
                </Fact>
                <Fact
                  label="Position"
                  labelClassName="text-slate-400 font-semibold"
                  valueClassName="mt-0.5 font-bold"
                >
                  {user.position || '—'}
                </Fact>
              </FactGrid>
            </DetailSubSection>

            <DetailSubSection title="Administrative Roles">
              <div className="flex flex-wrap gap-1.5 select-none pt-1">
                {roles.length === 0 ? (
                  <span className="text-xs text-slate-400 font-semibold italic">No roles assigned</span>
                ) : (
                  roles.map((r, i) => (
                    <span
                      key={i}
                      className={`inline-flex items-center rounded-full border px-3 py-1 text-xs font-bold ${
                        r === 'SuperAdmin'
                          ? 'bg-purple-100 text-purple-700 border-purple-200'
                          : 'bg-indigo-100 text-indigo-700 border-indigo-200'
                      }`}
                    >
                      {r}
                    </span>
                  ))
                )}
              </div>
            </DetailSubSection>
          </div>
        </section>
      </DetailLayout>
    </>
  )
}
