import { ShieldAlert } from 'lucide-react'

export function AccessDeniedPage() {
  return (
    <>
      <section className="admin-card admin-empty-state" aria-label="Access control status">
        <div>
          <ShieldAlert aria-hidden="true" />
          <h2>Server-side policy required</h2>
          <p>API authorization is required.</p>
        </div>
      </section>
    </>
  )
}