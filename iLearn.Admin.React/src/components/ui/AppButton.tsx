import { createElement, isValidElement, type ButtonHTMLAttributes, type ElementType, type ReactNode } from 'react'
import { Loader2 } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

type AppButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost'

type AppButtonIcon = LucideIcon | ReactNode

type AppButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children: ReactNode
  icon?: AppButtonIcon
  variant?: AppButtonVariant
  loading?: boolean
  size?: 'sm' | 'md'
}

const variantStyles: Record<AppButtonVariant, string> = {
  primary: 'bg-indigo-600 text-white hover:bg-indigo-700',
  secondary: 'border-slate-200 bg-white text-slate-900 hover:border-slate-300 hover:bg-slate-50',
  danger: 'bg-red-600 text-white hover:bg-red-700',
  ghost: 'border-transparent bg-transparent text-slate-500 hover:border-slate-300 hover:bg-slate-50',
}

const sizeStyles: Record<'sm' | 'md', string> = {
  md: 'min-h-[34px] px-3 text-xs sm:text-[13px]',
  sm: 'min-h-[28px] px-2.5 text-xs',
}

const renderIcon = (icon: AppButtonIcon | undefined) => {
  if (!icon) return null
  if (isValidElement(icon)) {
    return icon
  }

  // Covers function components and forwardRef/memo component objects.
  const isComponentType =
    typeof icon === 'function' ||
    (typeof icon === 'object' && icon !== null && '$$typeof' in icon)

  if (isComponentType) {
    return createElement(icon as ElementType, { 'aria-hidden': true })
  }

  return typeof icon === 'string' || typeof icon === 'number' ? icon : null
}

export function AppButton({
  children,
  icon,
  variant = 'secondary',
  loading = false,
  size = 'md',
  className = '',
  type = 'button',
  disabled,
  ...props
}: AppButtonProps) {
  const iconSlot = loading
    ? <Loader2 className="animate-spin" aria-hidden="true" />
    : renderIcon(icon)

  return (
    <button
      type={type}
      disabled={disabled || loading}
      aria-busy={loading ? true : undefined}
      className={`inline-flex items-center justify-center gap-[7px] rounded-md border border-transparent font-semibold cursor-pointer disabled:cursor-not-allowed disabled:opacity-55 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 [&_svg]:h-4 [&_svg]:w-4 ${sizeStyles[size]} ${variantStyles[variant]} ${className}`.trim()}
      {...props}
    >
      {iconSlot ? <span className="inline-flex items-center justify-center">{iconSlot}</span> : null}
      <span>{children}</span>
    </button>
  )
}