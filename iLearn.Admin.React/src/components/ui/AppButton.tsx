import type { ButtonHTMLAttributes, ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

type AppButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost'

type AppButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children: ReactNode
  icon?: LucideIcon
  variant?: AppButtonVariant
}

const variantStyles: Record<AppButtonVariant, string> = {
  primary: 'bg-indigo-600 text-white hover:bg-indigo-700',
  secondary: 'border-slate-200 bg-white text-slate-900 hover:border-slate-300 hover:bg-slate-50',
  danger: 'bg-red-600 text-white hover:bg-red-700',
  ghost: 'border-transparent bg-transparent text-slate-500 hover:border-slate-300 hover:bg-slate-50',
}

export function AppButton({ children, icon, variant = 'secondary', className = '', type = 'button', ...props }: AppButtonProps) {
  const Icon = icon

  return (
    <button
      type={type}
      className={`inline-flex min-h-[34px] items-center justify-center gap-[7px] rounded-md border border-transparent px-3 text-xs sm:text-[13px] font-semibold cursor-pointer disabled:cursor-not-allowed disabled:opacity-55 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 [&_svg]:h-4 [&_svg]:w-4 ${variantStyles[variant]} ${className}`.trim()}
      {...props}
    >
      {Icon ? <Icon aria-hidden="true" /> : null}
      <span>{children}</span>
    </button>
  )
}