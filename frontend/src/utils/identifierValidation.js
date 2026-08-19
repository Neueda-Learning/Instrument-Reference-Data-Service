const IDENTIFIER_RULES = {
  CUSIP: {
    regex: /^[0-9A-Z]{9}$/,
    message: 'CUSIP must be 9 uppercase letters/digits (e.g. LS9BSD30F).',
  },
  ISIN: {
    regex: /^[A-Z]{2}[A-Z0-9]{9}[0-9]$/,
    message: 'ISIN must be 12 characters: 2 letters, 9 uppercase letters/digits, then 1 digit (e.g. DE28G3BV4SI0).',
  },
  RIC: {
    regex: /^[A-Z0-9]+(\.[A-Z0-9]+)?$/,
    message: "RIC must be uppercase letters/digits with an optional '.SUFFIX' (e.g. XETR.5NWWCP).",
  },
  SEDOL: {
    regex: /^[B-DF-HJ-NP-TV-Z0-9]{6}[0-9]$/,
    message: 'SEDOL must be 6 uppercase consonant/digit characters followed by 1 digit (e.g. BD5L398).',
  },
  TICKER: {
    regex: /^[A-Z0-9.\-/]{1,12}$/,
    message: "Ticker must be 1-12 chars using uppercase letters, digits, '.', '-', '/' (e.g. HELIYH27).",
  },
}

export function normalizeIdentifierInput(value) {
  return String(value ?? '').trim().toUpperCase()
}

export function validateIdentifierByType(typeId, value) {
  const normalizedTypeId = String(typeId ?? '').trim().toUpperCase()
  const normalizedValue = normalizeIdentifierInput(value)

  if (!normalizedValue) {
    return null
  }

  const rule = IDENTIFIER_RULES[normalizedTypeId]
  if (!rule) {
    return null
  }

  return rule.regex.test(normalizedValue) ? null : rule.message
}