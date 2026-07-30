import type { Cell, Sheet, SheetData } from 'write-excel-file/browser'
import { exportRowsAsCsv } from './csvExport'
import { downloadBlob } from './downloadBlob'

export type ExportFormat = 'csv' | 'xlsx'
export type ExportCell = string | number | null | undefined

export type ExportWorkbookSheet = {
  sheet: string
  header: string[]
  rows: ExportCell[][]
  columns?: number[] | undefined
}

function normalizeBaseFilename(filename: string) {
  return filename.replace(/\.(csv|xlsx)$/i, '')
}

function withExtension(filename: string, extension: ExportFormat) {
  return `${normalizeBaseFilename(filename)}.${extension}`
}

function toSheetCell(value: ExportCell): Cell {
  if (value === null || value === undefined) {
    return null
  }

  if (typeof value === 'number' && Number.isFinite(value)) {
    return value
  }

  return String(value)
}

function toSheetData(header: string[], rows: ExportCell[][]): SheetData {
  return [
    header.map((value) => ({ value, fontWeight: 'bold' })),
    ...rows.map((row) => row.map(toSheetCell)),
  ]
}

export async function exportRows(
  format: ExportFormat,
  filename: string,
  header: string[],
  rows: ExportCell[][],
): Promise<void> {
  if (format === 'csv') {
    exportRowsAsCsv(withExtension(filename, 'csv'), header, rows)
    return
  }

  const { default: writeXlsxFile } = await import('write-excel-file/browser')
  const blob = await writeXlsxFile(toSheetData(header, rows)).toBlob()
  downloadBlob(blob, withExtension(filename, 'xlsx'))
}

export async function exportWorkbook(filename: string, sheets: ExportWorkbookSheet[]): Promise<void> {
  const { default: writeXlsxFile } = await import('write-excel-file/browser')
  const workbookSheets: Array<Sheet<Blob>> = sheets.map((sheet) => ({
    sheet: sheet.sheet,
    data: toSheetData(sheet.header, sheet.rows),
    ...(sheet.columns ? { columns: sheet.columns.map((width) => ({ width })) } : {}),
  }))
  const blob = await writeXlsxFile(workbookSheets).toBlob()
  downloadBlob(blob, withExtension(filename, 'xlsx'))
}