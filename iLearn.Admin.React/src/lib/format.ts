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

export const formatBoolean = (value: boolean | null | undefined) => {
  if (value === undefined || value === null) {
    return '-'
  }

  return value ? 'Yes' : 'No'
}