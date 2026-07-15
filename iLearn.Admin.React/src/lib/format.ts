const dateFormatter = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
})

const dateTimeFormatter = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

const numberFormatter = new Intl.NumberFormat('en-GB')
const fixedDigitsNumberFormatters = new Map<number, Intl.NumberFormat>()

const byteUnits = ['B', 'KB', 'MB', 'GB'] as const

function normalizeFractionDigits(value: number) {
  if (!Number.isFinite(value)) return 0
  const floored = Math.trunc(value)
  return Math.max(0, Math.min(20, floored))
}

function getFixedDigitsNumberFormatter(fractionDigits: number) {
  const normalized = normalizeFractionDigits(fractionDigits)
  const cached = fixedDigitsNumberFormatters.get(normalized)
  if (cached) return cached

  const formatter = new Intl.NumberFormat('en-GB', {
    minimumFractionDigits: normalized,
    maximumFractionDigits: normalized,
  })
  fixedDigitsNumberFormatters.set(normalized, formatter)
  return formatter
}

export const formatDate = (value: Date | string | null | undefined) => {
  if (!value) {
    return '-'
  }

  return dateFormatter.format(new Date(value))
}

export const formatDateTime = (value: Date | string | null | undefined) => {
  if (!value) {
    return '-'
  }

  return dateTimeFormatter.format(new Date(value))
}

export const formatNumber = (value: number | null | undefined, fractionDigits?: number) => {
  if (value === null || value === undefined || !Number.isFinite(value)) {
    return '-'
  }

  if (fractionDigits === undefined) {
    return numberFormatter.format(value)
  }

  return getFixedDigitsNumberFormatter(fractionDigits).format(value)
}

export const formatPercent = (value: number | null | undefined, fractionDigits = 0) => {
  if (value === null || value === undefined || !Number.isFinite(value)) {
    return '—'
  }

  return `${getFixedDigitsNumberFormatter(fractionDigits).format(value)}%`
}

export const formatBytes = (bytes: number | null | undefined) => {
  if (bytes === null || bytes === undefined || !Number.isFinite(bytes) || bytes <= 0) {
    return '—'
  }

  let size = bytes
  let unitIndex = 0

  while (size >= 1024 && unitIndex < byteUnits.length - 1) {
    size /= 1024
    unitIndex += 1
  }

  const fractionDigits = size >= 10 || unitIndex === 0 ? 0 : 1
  const sizeText = getFixedDigitsNumberFormatter(fractionDigits).format(size)

  return `${sizeText} ${byteUnits[unitIndex]}`
}

export const formatDuration = (totalSeconds: number | null | undefined): string => {
  if (totalSeconds === null || totalSeconds === undefined || !Number.isFinite(totalSeconds) || totalSeconds <= 0) {
    return '—'
  }
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)

  if (hours === 0 && minutes === 0) return '—'
  if (hours === 0) return `${minutes}m`
  return `${hours}h ${minutes}m`
}

