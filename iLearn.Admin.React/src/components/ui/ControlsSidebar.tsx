import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { LucideIcon } from 'lucide-react'
import { Loader2 } from 'lucide-react'

/*
 * Standard right-hand controls sidebar used by every detail page.
 * Compose with <ControlAction> rows only.
 */

type ControlsSidebarProps = {
  className?: string
  children: ReactNode
}

export function ControlsSidebar({
  className,
  children,
}: ControlsSidebarProps) {
  const classes = [
    'rounded-lg border border-slate-200 bg-white p-4 space-y-2 select-none',
    'lg:sticky lg:top-0 lg:self-start',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <aside className={classes}>
      {children}
    </aside>
  )
}

type ControlActionVariant = 'default' | 'danger' | 'primary'

type ControlActionProps = {
  icon: LucideIcon
  children: ReactNode
  /** Renders a router <Link> when set; otherwise a <button>. */
  to?: string
  onClick?: () => void
  type?: 'button' | 'submit'
  disabled?: boolean
  loading?: boolean
  title?: string | undefined
  variant?: ControlActionVariant
}

const rowStyles: Record<ControlActionVariant, string> = {
  default:
    'group w-full flex items-center gap-2.5 rounded-md border border-slate-200 bg-white p-2 text-slate-700 hover:border-indigo-300 hover:bg-indigo-50/40 transition cursor-pointer text-left',
  danger:
    'group w-full flex items-center gap-2.5 rounded-md border border-red-200 bg-white p-2 text-red-600 hover:border-red-300 hover:bg-red-50/50 transition cursor-pointer text-left',
  primary:
    'w-full flex items-center gap-2.5 rounded-md bg-indigo-600 hover:bg-indigo-700 text-white p-2 transition cursor-pointer text-left focus:outline-none',
}

const iconStyles: Record<ControlActionVariant, string> = {
  default:
    'h-7 w-7 rounded bg-slate-100 group-hover:bg-indigo-100 flex items-center justify-center shrink-0 text-slate-500 group-hover:text-indigo-600 transition-colors',
  danger:
    'h-7 w-7 rounded bg-red-50 group-hover:bg-red-100 flex items-center justify-center shrink-0 text-red-500 group-hover:text-red-600 transition-colors',
  primary: 'h-7 w-7 rounded bg-indigo-500 flex items-center justify-center shrink-0 text-white',
}

const labelStyles: Record<ControlActionVariant, string> = {
  default: 'text-[13px] font-bold text-slate-800 group-hover:text-indigo-800 transition-colors',
  danger: 'text-[13px] font-bold text-red-700 group-hover:text-red-800 transition-colors',
  primary: 'text-[13px] font-bold',
}

export function ControlAction({
  icon: Icon,
  children,
  to,
  onClick,
  type = 'button',
  disabled = false,
  loading = false,
  title,
  variant = 'default',
}: ControlActionProps) {
  if (disabled || loading) {
    return (
      <button
        type="button"
        disabled
        className="w-full flex items-center gap-2.5 rounded-md border border-slate-100 bg-slate-50 p-2 text-slate-300 cursor-not-allowed text-left focus:outline-none"
        title={title}
      >
        <div className="h-7 w-7 rounded bg-slate-100/50 flex items-center justify-center shrink-0 text-slate-300">
          {loading ? (
            <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden="true" />
          ) : (
            <Icon className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
          )}
        </div>
        <span className="text-[13px] font-bold">{children}</span>
      </button>
    )
  }

  const inner = (
    <>
      <div className={iconStyles[variant]}>
        <Icon className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
      </div>
      <span className={labelStyles[variant]}>{children}</span>
    </>
  )

  if (to) {
    return (
      <Link to={to} className={rowStyles[variant]} title={title}>
        {inner}
      </Link>
    )
  }

  return (
    <button type={type} onClick={onClick} className={rowStyles[variant]} title={title}>
      {inner}
    </button>
  )
}
