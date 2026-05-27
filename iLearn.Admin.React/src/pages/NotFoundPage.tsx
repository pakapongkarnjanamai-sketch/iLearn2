import { SearchX } from 'lucide-react'

export function NotFoundPage() {
  return (
    <>
      <section className="admin-card admin-empty-state" aria-label="Route not found">
        <div>
          <SearchX aria-hidden="true" />
          <h2>Route unavailable</h2>
          <p>Select an Admin module from the navigation menu.</p>
        </div>
      </section>
    </>
  )
}