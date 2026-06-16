import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

type SectionHeaderProps = {
  icon?: LucideIcon | undefined
  children: ReactNode
  actions?: ReactNode | undefined
  /** 'plain' for headers above open content, 'card' for headers inside bordered panels. */
  variant?: 'plain' | 'card' | undefined
}

/** Standard section heading with icon, used above tables and detail panels. */
export function SectionHeader({ icon: Icon, children, actions, variant = 'plain' }: SectionHeaderProps) {
  if (variant === 'card') {
    return (
      <div className="flex items-center justify-between gap-3 border-b border-slate-200 p-4">
        <h2 className="flex items-center gap-2 text-sm font-bold text-slate-800">
          {Icon && <Icon className="h-4 w-4 text-indigo-600" aria-hidden="true" />}
          {children}
        </h2>
        {actions}
      </div>
    )
  }

  return (
    <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-2.5 mb-3">
      <h2 className="flex items-center gap-2 text-[13px] font-extrabold uppercase [&_svg]:h-4 [&_svg]:w-4 [&_svg]:text-indigo-600">
        {Icon && <Icon aria-hidden="true" />}
        {children}
      </h2>
      {actions}
    </div>
  )
}
