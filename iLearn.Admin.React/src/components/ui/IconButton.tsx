import { createElement, isValidElement } from 'react'
import type { ButtonHTMLAttributes, ElementType, ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

type IconButtonTone = 'neutral' | 'primary' | 'danger' | 'success'
type IconButtonSize = 'sm' | 'md' | 'lg'

type IconButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  icon: LucideIcon | ReactNode
  title: string // Mandatory for a11y
  tone?: IconButtonTone
  size?: IconButtonSize
}

const toneStyles: Record<IconButtonTone, string> = {
  neutral: 'text-slate-400 hover:text-slate-600 hover:bg-slate-100/80 active:bg-slate-200/50',
  primary: 'text-indigo-500 hover:text-indigo-700 hover:bg-indigo-50 active:bg-indigo-100/50',
  danger: 'text-red-500 hover:text-red-700 hover:bg-red-50 active:bg-red-100/50',
  success: 'text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50 active:bg-emerald-100/50',
}

const sizeStyles: Record<IconButtonSize, string> = {
  sm: 'h-7 w-7 p-1 [&_svg]:h-4 [&_svg]:w-4',
  md: 'h-[34px] w-[34px] p-1.5 [&_svg]:h-4.5 [&_svg]:w-4.5',
  lg: 'h-10 w-10 p-2 [&_svg]:h-5 [&_svg]:w-5',
}

const renderIcon = (icon: LucideIcon | ReactNode) => {
  if (!icon) return null
  if (isValidElement(icon)) {
    return icon
  }
  const isComponentType =
    typeof icon === 'function' ||
    (typeof icon === 'object' && icon !== null && '$$typeof' in icon)

  if (isComponentType) {
    return createElement(icon as ElementType, { 'aria-hidden': true })
  }
  return null
}

export function IconButton({
  icon,
  title,
  tone = 'neutral',
  size = 'md',
  className = '',
  type = 'button',
  ...props
}: IconButtonProps) {
  return (
    <button
      type={type}
      title={title}
      aria-label={title}
      className={`inline-flex items-center justify-center rounded-md border border-transparent transition cursor-pointer disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 ${sizeStyles[size]} ${toneStyles[tone]} ${className}`.trim()}
      {...props}
    >
      {renderIcon(icon)}
    </button>
  )
}
