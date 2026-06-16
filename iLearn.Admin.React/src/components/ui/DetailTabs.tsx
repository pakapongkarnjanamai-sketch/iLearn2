import type { ReactNode } from 'react'

type TabVariant = 'default' | 'compact'

type DetailTabItem<T extends string> = {
  key: T
  label: ReactNode
  disabled?: boolean
  title?: string
}

type DetailTabsProps<T extends string> = {
  tabs: DetailTabItem<T>[]
  active: T
  onChange: (key: T) => void
  variant?: TabVariant
  className?: string
}

const containerClassByVariant: Record<TabVariant, string> = {
  default: 'border-b border-slate-200 mb-6 flex gap-1',
  compact: 'flex gap-4 mt-2',
}

const buttonBaseClassByVariant: Record<TabVariant, string> = {
  default: 'pb-3 px-3 font-semibold text-sm transition relative cursor-pointer',
  compact: 'pb-1 font-bold text-xs uppercase tracking-wider transition relative cursor-pointer',
}

const activeClassByVariant: Record<TabVariant, string> = {
  default: 'text-indigo-600 font-bold border-b-2 border-indigo-500',
  compact: 'text-indigo-500 border-b-2 border-indigo-500',
}

const inactiveClassByVariant: Record<TabVariant, string> = {
  default: 'text-slate-400 hover:text-slate-700',
  compact: 'text-slate-500 hover:text-slate-700',
}

export function DetailTabs<T extends string>({
  tabs,
  active,
  onChange,
  variant = 'default',
  className,
}: DetailTabsProps<T>) {
  const containerClass = className
    ? `${containerClassByVariant[variant]} ${className}`
    : containerClassByVariant[variant]

  return (
    <div className={containerClass}>
      {tabs.map(tab => {
        const isActive = active === tab.key
        const stateClass = isActive ? activeClassByVariant[variant] : inactiveClassByVariant[variant]

        return (
          <button
            key={tab.key}
            type="button"
            onClick={() => onChange(tab.key)}
            disabled={tab.disabled}
            title={tab.title}
            aria-current={isActive ? 'page' : undefined}
            className={`${buttonBaseClassByVariant[variant]} ${stateClass} ${tab.disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
          >
            {tab.label}
          </button>
        )
      })}
    </div>
  )
}
