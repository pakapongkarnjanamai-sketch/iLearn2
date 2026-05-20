import { SearchX } from 'lucide-react'
import { PageHeader } from '../components/ui/PageHeader'

export function NotFoundPage() {
  return (
    <>
      <PageHeader title="Page Not Found" eyebrow="Navigation" description="The requested Admin route is not registered in the React shell." />
      <section className="admin-empty-state" aria-label="Route not found">
        <div>
          <SearchX aria-hidden="true" />
          <h2>Route unavailable</h2>
          <p>Select an Admin module from the navigation menu.</p>
        </div>
      </section>
    </>
  )
}