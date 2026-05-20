import type { ButtonHTMLAttributes, ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

type AppButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost'

type AppButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children: ReactNode
  icon?: LucideIcon
  variant?: AppButtonVariant
}

export function AppButton({ children, icon, variant = 'secondary', className = '', type = 'button', ...props }: AppButtonProps) {
  const Icon = icon

  return (
    <button type={type} className={`admin-button admin-button--${variant} ${className}`.trim()} {...props}>
      {Icon ? <Icon aria-hidden="true" /> : null}
      <span>{children}</span>
    </button>
  )
}