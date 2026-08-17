export function formatDate(value) {
  if (!value) {
    return '-'
  }

  const date = parseDateValue(value)
  if (!date) {
    return value
  }

  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
  }).format(date)
}

export function parseDateValue(value) {
  if (!value) {
    return null
  }

  const normalizedValue = /^\d{4}-\d{2}-\d{2}$/.test(value)
    ? `${value}T00:00:00`
    : value

  const date = new Date(normalizedValue)
  return Number.isNaN(date.valueOf()) ? null : date
}

export function formatDateTime(value) {
  if (!value) {
    return '-'
  }

  const date = new Date(value)
  if (Number.isNaN(date.valueOf())) {
    return value
  }

  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}

export function pickIdentifier(identifiers, typeId) {
  if (!Array.isArray(identifiers)) {
    return '-'
  }

  const match = identifiers.find(
    (item) => item?.identifierTypeId?.toUpperCase() === typeId,
  )

  return match?.identifierValue ?? '-'
}
