import { SearchX } from 'lucide-react'

export function NotFoundPage() {
  return (
    <>
      <section className="grid min-h-[360px] place-items-center rounded-lg border border-slate-200 bg-white p-6 text-center shadow-xs [&_svg]:h-8 [&_svg]:w-8 [&_svg]:text-indigo-600 [&_h2]:mt-3 [&_h2]:mb-1.5 [&_h2]:text-xl [&_p]:max-w-[520px] [&_p]:text-slate-500" aria-label="Route not found">
        <div>
          <SearchX aria-hidden="true" />
          <h2>Route unavailable</h2>
          <p>Select an Admin module from the navigation menu.</p>
        </div>
      </section>
    </>
  )
}