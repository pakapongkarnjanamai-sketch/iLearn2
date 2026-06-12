import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Edit3, Trash2, User } from 'lucide-react'
import { LoadingState } from '../../components/ui/LoadingState'
import { NotFoundState } from '../../components/ui/NotFoundState'
import { ControlsSidebar, ControlAction } from '../../components/ui/ControlsSidebar'
import { SectionHeader } from '../../components/ui/SectionHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
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
    .map((ur) => ur.Role?.Name ?? ur.Role?.RoleType)
    .filter(Boolean)

  return (
    <>
      <header className="mb-4">
        <div className="text-xxs font-extrabold uppercase text-slate-400">Admin Users</div>
        <h1 className="text-2xl font-extrabold text-slate-900 select-none">
          {user.fullName || user.nid}
        </h1>
      </header>

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start">
        <div className="min-w-0">
          <section className="rounded-lg border border-slate-200 bg-white p-5 space-y-5">
            <SectionHeader icon={User}>User Overview</SectionHeader>

            <dl className="grid grid-cols-2 sm:grid-cols-3 gap-x-6 gap-y-5 text-xs">
              <div>
                <dt className="text-slate-400 font-bold uppercase tracking-wider">Status</dt>
                <dd className="mt-1">
                  <StatusBadge tone={user.isActive ? 'success' : 'neutral'}>
                    {user.isActive ? 'Active' : 'Inactive'}
                  </StatusBadge>
                </dd>
              </div>

              <div>
                <dt className="text-slate-400 font-bold uppercase tracking-wider">Employee NID</dt>
                <dd className="mt-1 font-bold text-slate-700">{user.nid}</dd>
              </div>

              <div>
                <dt className="text-slate-400 font-bold uppercase tracking-wider">Last Login</dt>
                <dd className="mt-1 text-slate-700">
                  {user.lastLogin ? formatDateTime(new Date(user.lastLogin)) : '—'}
                </dd>
              </div>

              {user.email && (
                <div className="col-span-2">
                  <dt className="text-slate-400 font-bold uppercase tracking-wider">Email Address</dt>
                  <dd className="mt-1 text-slate-700 break-all">{user.email}</dd>
                </div>
              )}
            </dl>

            <hr className="border-slate-100" />

            <div className="space-y-2">
              <div className="text-xxs font-extrabold uppercase text-slate-400">Organization Info</div>
              <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs select-none">
                <div>
                  <dt className="text-slate-400 font-semibold">Division</dt>
                  <dd className="mt-0.5 text-slate-700 font-bold">{user.division || '—'}</dd>
                </div>
                <div>
                  <dt className="text-slate-400 font-semibold">Department</dt>
                  <dd className="mt-0.5 text-slate-700 font-bold">{user.department || '—'}</dd>
                </div>
                <div>
                  <dt className="text-slate-400 font-semibold">Position</dt>
                  <dd className="mt-0.5 text-slate-700 font-bold">{user.position || '—'}</dd>
                </div>
              </dl>
            </div>

            <hr className="border-slate-100" />

            <div className="space-y-2">
              <div className="text-xxs font-extrabold uppercase text-slate-400">Administrative Roles</div>
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
            </div>
          </section>
        </div>

        <ControlsSidebar backTo="/users">
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
      </div>
    </>
  )
}
