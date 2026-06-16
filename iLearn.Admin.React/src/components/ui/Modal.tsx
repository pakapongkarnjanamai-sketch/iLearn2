import type { CSSProperties, FormEventHandler, MouseEventHandler, ReactNode } from 'react'
import { X } from 'lucide-react'

type ModalSize = 'sm' | 'md' | 'lg'
type ModalAs = 'div' | 'form'

type ModalProps = {
  open: boolean
  onClose: () => void
  title?: ReactNode
  children: ReactNode
  size?: ModalSize
  as?: ModalAs
  onSubmit?: FormEventHandler<HTMLFormElement>
  windowClassName?: string
  windowStyle?: CSSProperties
  ariaLabel?: string
}

const sizeClassByValue: Record<ModalSize, string> = {
  sm: 'max-w-sm',
  md: '',
  lg: 'modal-window-lg',
}

export function Modal({
  open,
  onClose,
  title,
  children,
  size = 'md',
  as = 'div',
  onSubmit,
  windowClassName,
  windowStyle,
  ariaLabel,
}: ModalProps) {
  if (!open) return null

  const stopPropagation: MouseEventHandler = event => {
    event.stopPropagation()
  }

  const windowClass = ['modal-window', sizeClassByValue[size], 'relative', windowClassName].filter(Boolean).join(' ')

  const derivedAriaLabel =
    ariaLabel || (typeof title === 'string' && title.trim() ? title : 'Modal dialog')

  return (
    <div className="modal-overlay" onClick={onClose} role="dialog" aria-modal="true" aria-label={derivedAriaLabel}>
      {as === 'form' ? (
        <form className={windowClass} style={windowStyle} onClick={stopPropagation} onSubmit={onSubmit}>
          {title ? (
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <h3 className="text-base font-extrabold text-slate-800 uppercase tracking-wide">{title}</h3>
              <button
                type="button"
                onClick={onClose}
                className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer"
                aria-label="Close"
              >
                <X className="h-5 w-5" />
              </button>
            </div>
          ) : null}
          {children}
        </form>
      ) : (
        <div className={windowClass} style={windowStyle} onClick={stopPropagation}>
          {title ? (
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
              <h3 className="text-base font-extrabold text-slate-800 uppercase tracking-wide">{title}</h3>
              <button
                type="button"
                onClick={onClose}
                className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer"
                aria-label="Close"
              >
                <X className="h-5 w-5" />
              </button>
            </div>
          ) : null}
          {children}
        </div>
      )}
    </div>
  )
}
