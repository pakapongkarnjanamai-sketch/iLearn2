import { ShieldAlert } from 'lucide-react'

export function AccessDeniedPage() {
  return (
    <>
      <section className="grid min-h-[360px] place-items-center rounded-lg border border-slate-200 bg-white p-6 text-center shadow-xs [&_svg]:h-8 [&_svg]:w-8 [&_svg]:text-indigo-600 [&_h2]:mt-3 [&_h2]:mb-1.5 [&_h2]:text-xl [&_p]:max-w-[520px] [&_p]:text-slate-500" aria-label="Access control status">
        <div>
          <ShieldAlert aria-hidden="true" />
          <h2>Server-side policy required</h2>
          <p>API authorization is required.</p>
        </div>
      </section>
    </>
  )
}