import type { ReactNode } from 'react'
import { ArrowLeft, ArrowRight, Check, X, Loader2 } from 'lucide-react'

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
  cancelLabel = 'Cancel',
  prevLabel = 'Previous',
  nextLabel = 'Continue',
}: AppWizardProps) {
  
  const handleStepClick = async (targetIndex: number) => {
    const targetStep = targetIndex + 1
    if (targetStep === currentStep) return

    // Going backwards is always allowed
    if (targetStep < currentStep) {
      onStepChange(targetStep)
      return
    }

    // Going forward requires sequential step validations
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
    // Validate final step before submission
    const currentStepConfig = steps[currentStep - 1]
    if (currentStepConfig?.validate) {
      const isValid = await currentStepConfig.validate()
      if (!isValid) return
    }
    await onSubmit()
  }

  const renderStepButton = (stepConfig: WizardStep, index: number) => {
    const stepNum = index + 1
    const isActive = currentStep === stepNum
    const isComplete = currentStep > stepNum

    return (
      <button
        key={stepConfig.label}
        type="button"
        onClick={() => void handleStepClick(index)}
        className={`flex min-w-28 sm:min-w-31 items-center gap-1.5 border px-2.5 py-1.5 text-left text-xxs font-extrabold uppercase tracking-wide rounded transition-all duration-150 cursor-pointer select-none ${
          isActive 
            ? 'border-blue-500 bg-blue-50/50 text-blue-700 shadow-3xs' 
            : isComplete 
              ? 'border-emerald-200 bg-emerald-50/40 text-emerald-700' 
              : 'border-slate-200 bg-white text-slate-400 hover:border-slate-300 hover:text-slate-600'
        }`}
        aria-current={isActive ? 'step' : undefined}
      >
        <span className={`flex h-4 w-4 items-center justify-center rounded-sm border text-[10px] font-mono shrink-0 ${
          isActive 
            ? 'border-blue-500 bg-blue-600 text-white' 
            : isComplete 
              ? 'border-emerald-500 bg-emerald-600 text-white' 
              : 'border-slate-200 bg-slate-50 text-slate-400'
        }`}>
          {stepNum}
        </span>
        <span className="truncate">{stepConfig.label}</span>
      </button>
    )
  }

  return (
    <div className="admin-grid-surface">
      <form onSubmit={handleSubmitForm} className="flex min-h-0 flex-1 flex-col gap-4">
        {/* Header with Title and Stepper Breadcrumbs */}
        <div className="flex flex-wrap items-center justify-between gap-3 shrink-0">
          <div>
            {eyebrow && (
              <div className="text-[10px] font-extrabold uppercase tracking-wider text-slate-400">
                {eyebrow}
              </div>
            )}
            <h1 className="text-base font-extrabold text-slate-800 tracking-tight leading-tight">
              {title}
            </h1>
            {description && (
              <p className="text-xxs font-semibold text-slate-400 mt-0.5 leading-normal">
                {description}
              </p>
            )}
          </div>
          <div className="flex flex-wrap items-center gap-1.5 select-none">
            {steps.map(renderStepButton)}
          </div>
        </div>

        {/* Content workspace wrapper with independent scroll */}
        <div className="min-h-0 flex-1 flex flex-col relative">
          <div className="overflow-y-auto custom-scrollbar flex-1 pr-1">
            {steps[currentStep - 1]?.render()}
          </div>

          {/* Inline Loading / Saving Overlay */}
          {isSubmitting && (
            <div className="absolute inset-0 bg-white/60 backdrop-blur-xs flex items-center justify-center z-50 rounded-lg animate-fade-in">
              <div className="flex flex-col items-center gap-2.5">
                <Loader2 className="h-7 w-7 animate-spin text-blue-600" />
                <span className="text-xs text-slate-500 font-bold tracking-wide uppercase animate-pulse">Processing...</span>
              </div>
            </div>
          )}
        </div>

        {/* Navigation Buttons Pinned Footer */}
        <div className="flex items-center justify-end gap-2.5 border-t border-slate-100 pt-3 shrink-0">
          <button 
            type="button" 
            onClick={onCancel} 
            className="admin-button admin-button--secondary text-xxs font-extrabold py-1.5 px-3 rounded shadow-3xs"
          >
            <X className="h-3.5 w-3.5" aria-hidden="true" />
            <span>{cancelLabel}</span>
          </button>

          {currentStep > 1 && (
            <button 
              type="button" 
              onClick={() => onStepChange(currentStep - 1)} 
              className="admin-button admin-button--secondary text-xxs font-extrabold py-1.5 px-3 rounded shadow-3xs"
            >
              <ArrowLeft className="h-3.5 w-3.5" aria-hidden="true" />
              <span>{prevLabel}</span>
            </button>
          )}

          {currentStep < steps.length ? (
            <button 
              type="button" 
              onClick={handleContinue} 
              className="admin-button admin-button--primary text-xxs font-extrabold py-1.5 px-3 rounded shadow-3xs"
            >
              <ArrowRight className="h-3.5 w-3.5" aria-hidden="true" />
              <span>{nextLabel}</span>
            </button>
          ) : (
            <button 
              type="submit" 
              disabled={isSubmitting} 
              className="admin-button admin-button--primary text-xxs font-extrabold py-1.5 px-3 rounded shadow-3xs disabled:opacity-55"
            >
              {isSubmitting ? (
                <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden="true" />
              ) : submitIcon ? (
                submitIcon
              ) : (
                <Check className="h-3.5 w-3.5" aria-hidden="true" />
              )}
              <span>{submitLabel}</span>
            </button>
          )}
        </div>
      </form>
    </div>
  )
}
