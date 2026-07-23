import { useCallback, useRef, useState, type ReactNode } from 'react'
import { AlertTriangle, X } from 'lucide-react'
import { Modal } from './Modal'
import { t, UI_LABELS } from '../../lib/labels'

/*
 * Standard confirmation dialog replacing window.confirm().
 *
 * Usage:
 *   const { confirm, confirmDialog } = useConfirm()
 *   ...
 *   if (!(await confirm({ title: 'Delete Course', message: '...', danger: true }))) return
 *   ...
 *   return (<>{...page}{confirmDialog}</>)
 */

export type ConfirmOptions = {
  title: string
  message: ReactNode
  confirmLabel?: string | undefined
  cancelLabel?: string | undefined
  /** Styles the confirm button red for destructive actions. */
  danger?: boolean | undefined
}

type PendingConfirm = ConfirmOptions & {
  resolve: (confirmed: boolean) => void
}

export function useConfirm() {
  const [pending, setPending] = useState<PendingConfirm | null>(null)
  const pendingRef = useRef<PendingConfirm | null>(null)

  const confirm = useCallback((options: ConfirmOptions) => {
    return new Promise<boolean>(resolve => {
      // Settle any dialog that is somehow still open before showing the next one.
      pendingRef.current?.resolve(false)
      const next = { ...options, resolve }
      pendingRef.current = next
      setPending(next)
    })
  }, [])

  const close = useCallback((confirmed: boolean) => {
    pendingRef.current?.resolve(confirmed)
    pendingRef.current = null
    setPending(null)
  }, [])

  const confirmDialog = pending ? (
    <ConfirmDialog
      title={pending.title}
      message={pending.message}
      confirmLabel={pending.confirmLabel}
      cancelLabel={pending.cancelLabel}
      danger={pending.danger}
      onConfirm={() => close(true)}
      onCancel={() => close(false)}
    />
  ) : null

  return { confirm, confirmDialog }
}

type ConfirmDialogProps = ConfirmOptions & {
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({
  title,
  message,
  confirmLabel,
  cancelLabel,
  danger = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const resolvedConfirmLabel = confirmLabel ?? t(UI_LABELS.confirm)
  const resolvedCancelLabel = cancelLabel ?? t(UI_LABELS.cancel)
  return (
    <Modal open onClose={onCancel} ariaLabel={title}>
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 select-none">
          <div className="flex items-center gap-2">
            <AlertTriangle className={`h-5 w-5 ${danger ? 'text-red-500' : 'text-amber-500'}`} />
            <h3 className="text-base font-extrabold text-slate-800 uppercase tracking-wide">{title}</h3>
          </div>
          <button
            type="button"
            onClick={onCancel}
            className="text-slate-400 hover:text-slate-600 hover:bg-slate-50 p-1.5 rounded-full transition cursor-pointer"
            aria-label="Close"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="px-6 py-5 text-sm text-slate-600 leading-relaxed">{message}</div>

        <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
          <button
            type="button"
            onClick={onCancel}
            className="px-4 py-2 text-sm font-bold text-slate-500 hover:text-slate-700 rounded-lg hover:bg-slate-100 transition cursor-pointer"
          >
            {resolvedCancelLabel}
          </button>
          <button
            type="button"
            autoFocus
            onClick={onConfirm}
            className={`px-5 py-2 text-white rounded-lg text-sm font-bold transition cursor-pointer shadow-xs ${
              danger ? 'bg-red-600 hover:bg-red-700' : 'bg-indigo-600 hover:bg-indigo-700'
            }`}
          >
            {resolvedConfirmLabel}
          </button>
        </div>
    </Modal>
  )
}
