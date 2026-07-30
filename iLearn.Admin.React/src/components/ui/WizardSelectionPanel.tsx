import type { ReactNode } from 'react'
import { Badge, type BadgeTone } from './Badge'

type WizardSelectionPanelProps = {
  title: ReactNode
  count?: number | undefined
  countTone?: BadgeTone | undefined
  actions?: ReactNode | undefined
  toolbar?: ReactNode | undefined
  children: ReactNode
  className?: string | undefined
  bodyClassName?: string | undefined
}

function joinClasses(...classes: Array<string | undefined>) {
  return classes.filter(Boolean).join(' ')
}

export function WizardSelectionPanel({
  title,
  count,
  countTone = 'neutral',
  actions,
  toolbar,
  children,
  className,
  bodyClassName,
}: WizardSelectionPanelProps) {
  return (
    <div className={joinClasses('flex-1 flex flex-col overflow-hidden rounded-lg border border-slate-200 bg-white min-h-0', className)}>
      <div className="p-2.5 bg-slate-50 border-b border-slate-200 flex items-center justify-between shrink-0 select-none">
        <span className="font-bold text-xs text-slate-400 uppercase tracking-wider">{title}</span>
        <div className="flex items-center gap-2">
          {count !== undefined && <Badge tone={countTone}>{count}</Badge>}
          {actions}
        </div>
      </div>
      {toolbar}
      <div className={joinClasses('flex-1 overflow-y-auto custom-scrollbar p-1.5 space-y-1.5 min-h-0', bodyClassName)}>
        {children}
      </div>
    </div>
  )
}