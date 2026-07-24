import type { ReactNode } from 'react'
import { Download, FileSpreadsheet } from 'lucide-react'
import { REPORT_LABELS, t } from '../../lib/labels'
import type { ExportFormat } from '../../lib/tableExport'
import { AppButton } from './AppButton'

type ExportActionConfig = {
  label?: string
  loadingLabel?: string
  onClick?: () => void | Promise<void>
  loading?: boolean | undefined
  disabled?: boolean | undefined
  title?: string | undefined
}

type ExportMenuProps = {
  hasRows: boolean
  onExport?: (format: ExportFormat) => void | Promise<void>
  csv?: ExportActionConfig
  xlsx?: ExportActionConfig
  className?: string
  extraActions?: ReactNode
}

function runAction(action: ExportActionConfig | undefined) {
  return () => {
    if (action?.onClick) {
      void action.onClick()
    }
  }
}

export function ExportMenu({
  hasRows,
  onExport,
  csv,
  xlsx,
  className = '',
  extraActions,
}: ExportMenuProps) {
  if (!hasRows) {
    return null
  }

  const csvAction: ExportActionConfig | undefined = csv ?? (onExport
    ? {
        label: t(REPORT_LABELS.exportCsv),
        onClick: () => onExport('csv'),
      }
    : undefined)

  const xlsxAction: ExportActionConfig | undefined = xlsx ?? (onExport
    ? {
        label: t(REPORT_LABELS.exportExcel),
        loadingLabel: t(REPORT_LABELS.exportingExcel),
        onClick: () => onExport('xlsx'),
      }
    : undefined)

  return (
    <div className={`flex flex-wrap items-center gap-2 ${className}`.trim()}>
      {xlsxAction?.onClick && (
        <AppButton
          onClick={runAction(xlsxAction)}
          icon={FileSpreadsheet}
          variant="secondary"
          size="sm"
          loading={xlsxAction.loading ?? false}
          disabled={xlsxAction.disabled ?? false}
          {...(xlsxAction.title ? { title: xlsxAction.title } : {})}
        >
          {xlsxAction.loading
            ? (xlsxAction.loadingLabel ?? xlsxAction.label ?? t(REPORT_LABELS.exportingExcel))
            : (xlsxAction.label ?? t(REPORT_LABELS.exportExcel))}
        </AppButton>
      )}

      {csvAction?.onClick && (
        <AppButton
          onClick={runAction(csvAction)}
          icon={Download}
          variant="secondary"
          size="sm"
          loading={csvAction.loading ?? false}
          disabled={csvAction.disabled ?? false}
          {...(csvAction.title ? { title: csvAction.title } : {})}
        >
          {csvAction.label ?? t(REPORT_LABELS.exportCsv)}
        </AppButton>
      )}

      {extraActions}
    </div>
  )
}