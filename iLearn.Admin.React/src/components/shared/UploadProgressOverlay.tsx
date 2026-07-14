import { useEffect, useState } from 'react'
import { CloudUpload, Loader2 } from 'lucide-react'
import { AppButton } from '../ui/AppButton'
import { ProgressBar } from '../ui/ProgressBar'
import { useConfirm } from '../ui/ConfirmDialog'
import { formatBytes, formatPercent } from '../../lib/format'
import type { UploadPhase } from '../../lib/apiClient'

type UploadProgressOverlayProps = {
  phase: UploadPhase
  loadedBytes: number
  totalBytes: number
  percent: number
  fileName: string
  onCancel: () => void
}

export function UploadProgressOverlay({
  phase,
  loadedBytes,
  totalBytes,
  percent,
  fileName,
  onCancel,
}: UploadProgressOverlayProps) {
  const { confirm, confirmDialog } = useConfirm()
  const [isCancelling, setIsCancelling] = useState(false)

  // beforeunload guard during active uploading/processing
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault()
      e.returnValue = 'Upload is in progress. Are you sure you want to leave?'
      return 'Upload is in progress. Are you sure you want to leave?'
    }
    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload)
    }
  }, [])

  const handleCancelClick = async () => {
    setIsCancelling(true)
    const confirmed = await confirm({
      title: 'Cancel Upload',
      message: 'Are you sure you want to cancel the SCORM package upload?',
      confirmLabel: 'Yes, Cancel',
      cancelLabel: 'No, Keep Uploading',
      danger: true,
    })
    setIsCancelling(false)
    if (confirmed) {
      onCancel()
    }
  }

  return (
    <>
      <div className="fixed inset-0 z-9999 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4 animate-fade-in select-none">
        <div className="bg-white border border-slate-100 rounded-xl shadow-2xl w-full max-w-md overflow-hidden flex flex-col p-6 items-center gap-4 animate-scale-up">
          {/* Header Icon */}
          <div className="flex items-center justify-center w-12 h-12 rounded-full bg-indigo-50 text-indigo-600">
            {phase === 'uploading' ? (
              <CloudUpload className="h-6 w-6 animate-pulse" />
            ) : (
              <Loader2 className="h-6 w-6 animate-spin" />
            )}
          </div>

          {/* Title and Metadata */}
          <div className="text-center w-full">
            <h3 className="text-sm font-extrabold text-slate-800 uppercase tracking-wide">
              {phase === 'uploading' ? 'Uploading Content' : 'Processing on Server'}
            </h3>
            <p className="text-xs text-slate-500 font-bold mt-1 max-w-full truncate px-4 select-all" title={fileName}>
              {fileName}
            </p>
          </div>

          {/* Progress Section */}
          <div className="w-full space-y-2 mt-2">
            <ProgressBar
              value={phase === 'processing' ? 100 : percent}
              completed={phase === 'processing'}
              maxWidthClass="w-full"
            />

            {phase === 'uploading' && (
              <div className="flex justify-between w-full text-xxs font-bold text-slate-400 select-none">
                <span>{formatBytes(loadedBytes)} / {formatBytes(totalBytes)}</span>
                <span>{formatPercent(percent)}</span>
              </div>
            )}

            {phase === 'processing' && (
              <p className="text-xs font-semibold text-indigo-500 text-center animate-pulse leading-relaxed">
                Processing on server — extracting & validating SCORM package...
              </p>
            )}
          </div>

          {/* Cancel Button */}
          {phase === 'uploading' && (
            <div className="mt-2 w-full flex justify-center">
              <AppButton
                variant="danger"
                size="sm"
                onClick={handleCancelClick}
                disabled={isCancelling}
              >
                Cancel Upload
              </AppButton>
            </div>
          )}

          {/* Caution Message */}
          <div className="text-center mt-1">
            <p className="text-[11px] font-semibold text-slate-400 leading-relaxed">
              Please do not close or refresh this page.
            </p>
          </div>
        </div>
      </div>
      {confirmDialog}
    </>
  )
}
