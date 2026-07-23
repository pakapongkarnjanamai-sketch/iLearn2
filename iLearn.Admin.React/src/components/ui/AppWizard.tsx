import type { ReactNode } from 'react'
import { ArrowLeft, ArrowRight, Check, X, Loader2 } from 'lucide-react'
import { AppButton } from './AppButton'
import { t, UI_LABELS } from '../../lib/labels'

export type WizardStep = {
  label: string
  validate?: () => boolean | Promise<boolean>
  render: () => ReactNode
}

type AppWizardProps = {
  title: string
  description?: string
  eyebrow?: string
  steps: WizardStep[]
  currentStep: number
  onStepChange: (step: number) => void
  onCancel: () => void
  onSubmit: () => void | Promise<void>
  submitLabel: string
  isSubmitting?: boolean
  submitIcon?: ReactNode
  cancelLabel?: string
  prevLabel?: string
  nextLabel?: string
}

export function AppWizard({
  title,
  description,
  eyebrow,
  steps,
  currentStep,
  onStepChange,
  onCancel,
  onSubmit,
  submitLabel,
  isSubmitting = false,
  submitIcon,
  cancelLabel,
  prevLabel,
  nextLabel,
}: AppWizardProps) {
  const resolvedCancelLabel = cancelLabel ?? t(UI_LABELS.cancel)
  const resolvedPrevLabel = prevLabel ?? t(UI_LABELS.previous)
  const resolvedNextLabel = nextLabel ?? t(UI_LABELS.continue)
  
  const handleStepClick = async (targetIndex: number) => {
    const targetStep = targetIndex + 1
    if (targetStep === currentStep) return

    if (targetStep < currentStep) {
      onStepChange(targetStep)
      return
    }

    let stepToValidate = currentStep
    while (stepToValidate < targetStep) {
      const stepConfig = steps[stepToValidate - 1]
      if (stepConfig?.validate) {
        const isValid = await stepConfig.validate()
        if (!isValid) return
      }
      stepToValidate++
    }

    onStepChange(targetStep)
  }

  const handleContinue = async (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()

    const currentStepConfig = steps[currentStep - 1]
    if (currentStepConfig?.validate) {
      const isValid = await currentStepConfig.validate()
      if (!isValid) return
    }

    onStepChange(currentStep + 1)
  }

  const handleSubmitForm = async (e: React.FormEvent) => {
    e.preventDefault()
    const currentStepConfig = steps[currentStep - 1]
    if (currentStepConfig?.validate) {
      const isValid = await currentStepConfig.validate()
      if (!isValid) return
    }
    await onSubmit()
  }

  const isLastStep = currentStep === steps.length

  return (
    <div className="wizard-surface flex min-h-0 flex-1 flex-col overflow-hidden bg-white border border-slate-200/80 rounded-xl shadow-xs">
      <form onSubmit={handleSubmitForm} className="flex min-h-0 flex-1 flex-col">

        {/* ── Wizard Top Bar ── */}
        <div className="flex items-center gap-4 border-b border-slate-200 bg-white px-6 py-4 short:py-2 shrink-0">
          {/* Left: title cluster */}
          <div className="min-w-0">
            {eyebrow && (
              <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400">{eyebrow}</span>
            )}
            <h1 className="text-base font-bold text-slate-800 leading-tight truncate">{title}</h1>
          </div>

          {/* Center: step track */}
          <nav className="flex items-center gap-0.5 ml-auto" aria-label="Progress steps">
            {steps.map((step, i) => {
              const num = i + 1
              const isActive = currentStep === num
              const isComplete = currentStep > num
              return (
                <button
                  key={step.label}
                  type="button"
                  onClick={() => void handleStepClick(i)}
                  className={`flex items-center gap-1.5 rounded-md border-none bg-transparent px-3 py-1.5 text-xs font-bold uppercase tracking-wide cursor-pointer whitespace-nowrap transition-all duration-150 ${
                    isActive
                      ? 'bg-indigo-100 text-indigo-600'
                      : isComplete
                        ? 'text-emerald-600 hover:bg-slate-100'
                        : 'text-slate-400 hover:bg-slate-100 hover:text-slate-700'
                  }`}
                  aria-current={isActive ? 'step' : undefined}
                >
                  <span className={`inline-flex h-4.5 w-4.5 items-center justify-center rounded text-[10px] font-extrabold leading-none ${
                    isActive
                      ? 'bg-indigo-600 text-white'
                      : isComplete
                        ? 'bg-emerald-600 text-white'
                        : 'bg-slate-100 text-slate-500'
                  }`}>{isComplete ? '✓' : num}</span>
                  <span className="hidden md:inline">{step.label}</span>
                </button>
              )
            })}
          </nav>

          {/* Right: cancel shortcut */}
          <button
            type="button"
            onClick={onCancel}
            className="grid h-7 w-7 place-items-center ml-2 rounded-md border-none bg-transparent text-slate-400 cursor-pointer transition-all duration-150 hover:bg-red-50 hover:text-red-600"
            aria-label={resolvedCancelLabel}
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* ── Step content ── */}
        <div className="min-h-0 flex-1 flex flex-col relative bg-slate-50/60 border-b border-slate-100">
          <div className="overflow-y-auto custom-scrollbar flex-1 px-6 py-6 short:px-4 short:py-3">
            <div className="w-full h-full flex flex-col">
              {steps[currentStep - 1]?.render()}
            </div>
          </div>

          {isSubmitting && (
            <div className="absolute inset-0 bg-white/60 backdrop-blur-xs flex items-center justify-center z-50 rounded-lg animate-fade-in">
              <div className="flex flex-col items-center gap-2.5">
                <Loader2 className="h-7 w-7 animate-spin text-indigo-500" />
                <span className="text-xs text-slate-500 font-bold tracking-wide uppercase animate-pulse">Processing…</span>
              </div>
            </div>
          )}
        </div>

        {/* ── Pinned footer ── */}
        <div className="flex items-center justify-between gap-3 border-t border-slate-200 bg-white px-6 py-4 short:py-2 shrink-0">
          {/* Left: step indicator */}
          <span className="text-xs font-semibold text-slate-400">
            Step {currentStep} of {steps.length}
            {description && (
              <span className="hidden sm:inline text-slate-300 ml-2">— {description}</span>
            )}
          </span>

          {/* Right: action buttons */}
          <div className="flex items-center gap-2">
            {currentStep > 1 && (
              <AppButton
                type="button"
                onClick={() => onStepChange(currentStep - 1)}
                variant="secondary"
                icon={ArrowLeft}
                className="px-4 py-2 text-xs font-bold uppercase tracking-wide shadow-3xs"
              >
                {resolvedPrevLabel}
              </AppButton>
            )}

            {!isLastStep ? (
              <AppButton
                type="button"
                onClick={handleContinue}
                variant="primary"
                icon={ArrowRight}
                className="flex-row-reverse px-4 py-2 text-xs font-bold uppercase tracking-wide shadow-3xs"
              >
                {resolvedNextLabel}
              </AppButton>
            ) : (
              <AppButton
                type="submit"
                variant="primary"
                loading={isSubmitting}
                icon={submitIcon ?? Check}
                className="px-4 py-2 text-xs font-bold uppercase tracking-wide shadow-3xs"
              >
                {submitLabel}
              </AppButton>
            )}
          </div>
        </div>
      </form>
    </div>
  )
}
