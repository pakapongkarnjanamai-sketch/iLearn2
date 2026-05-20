import { ShieldAlert } from 'lucide-react'
import { PageHeader } from '../components/ui/PageHeader'

export function AccessDeniedPage() {
  return (
    <>
      <PageHeader title="Access Control" eyebrow="Authorization" description="Admin access is resolved by the existing API policies and Windows-auth claims." />
      <section className="admin-empty-state" aria-label="Access control status">
        <div>
          <ShieldAlert aria-hidden="true" />
          <h2>Server-side policy required</h2>
          <p>React routes do not replace API authorization, role checks, or division-scoped data filtering.</p>
        </div>
      </section>
    </>
  )
}