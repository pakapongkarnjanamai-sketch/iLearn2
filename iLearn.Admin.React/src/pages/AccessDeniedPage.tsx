import { ShieldAlert } from 'lucide-react'
import { ADMIN_LABELS, t } from '../lib/labels'

export function AccessDeniedPage() {
  return (
    <>
      <section className="grid min-h-[360px] place-items-center rounded-lg border border-slate-200 bg-white p-6 text-center shadow-xs [&_svg]:h-8 [&_svg]:w-8 [&_svg]:text-indigo-600 [&_h2]:mt-3 [&_h2]:mb-1.5 [&_h2]:text-xl [&_p]:max-w-[520px] [&_p]:text-slate-500" aria-label={t(ADMIN_LABELS.accessControlAria)}>
        <div>
          <ShieldAlert aria-hidden="true" />
          <h2>{t(ADMIN_LABELS.accessPolicyRequired)}</h2><p>{t(ADMIN_LABELS.apiAuthorizationRequired)}</p>
        </div>
      </section>
    </>
  )
}