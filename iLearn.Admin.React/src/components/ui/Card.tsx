import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { SectionHeader } from './SectionHeader'

type CardProps = {
  /** Title for the card header. If present, SectionHeader with variant="card" is rendered. */
  title?: ReactNode | undefined
  /** Icon to show in the card header. */
  icon?: LucideIcon | undefined
  /** Custom header actions (buttons, controls) displayed on the right. */
  actions?: ReactNode | undefined
  /** The content of the card. */
  children: ReactNode
  /** Additional CSS classes for the outer <section> element. */
  className?: string | undefined
  /** Additional CSS classes for the inner body wrapper <div>. Default has no padding to allow full-width tables. */
  bodyClassName?: string | undefined
}

export function Card({
  title,
  icon,
  actions,
  children,
  className = '',
  bodyClassName = '',
}: CardProps) {
  return (
    <section className={`overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs ${className}`.trim()}>
      {title && (
        <SectionHeader icon={icon} variant="card" actions={actions}>
          {title}
        </SectionHeader>
      )}
      <div className={bodyClassName}>
        {children}
      </div>
    </section>
  )
}
