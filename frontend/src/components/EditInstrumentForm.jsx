import { useCallback, useEffect, useMemo, useState } from 'react'
import { formatDate } from '../utils/instrumentFormatters'
import { normalizeIdentifierInput, validateIdentifierByType } from '../utils/identifierValidation'

function buildIdentifierValueMap(identifiers, identifierTypes) {
  const base = Object.fromEntries(
    (identifierTypes ?? [])
      .filter((item) => item.identifierTypeId !== 'ISIN')
      .map((item) => [item.identifierTypeId, '']),
  )

  if (!Array.isArray(identifiers)) {
    return base
  }

  identifiers.forEach((identifier) => {
    const typeId = String(identifier?.identifierTypeId ?? '').toUpperCase()
    if (!typeId || typeId === 'ISIN') {
      return
    }

    base[typeId] = String(identifier?.identifierValue ?? '')
  })

  return base
}

function validate(fields, identifierValues) {
  const errors = {}

  if (!fields.name.trim()) {
    errors.name = 'Name is required.'
  } else if (fields.name.trim().length > 150) {
    errors.name = 'Name must be 150 characters or fewer.'
  }

  const normalizedIsin = normalizeIdentifierInput(fields.primaryIsin)
  if (!normalizedIsin) {
    errors.primaryIsin = 'Primary ISIN is required.'
  } else {
    const primaryIsinError = validateIdentifierByType('ISIN', normalizedIsin)
    if (primaryIsinError) {
      errors.primaryIsin = primaryIsinError
    }
  }

  if (!fields.assetClassId) {
    errors.assetClassId = 'Asset class is required.'
  }

  if (!fields.sectorId) {
    errors.sectorId = 'Sector is required.'
  }

  if (!fields.exchangeId) {
    errors.exchangeId = 'Exchange is required.'
  }

  if (!fields.currencyId) {
    errors.currencyId = 'Currency is required.'
  }

  if (!fields.issuerId) {
    errors.issuerId = 'Issuer is required.'
  }

  if (!fields.status) {
    errors.status = 'Status is required.'
  }

  Object.entries(identifierValues ?? {}).forEach(([typeId, value]) => {
    const validationMessage = validateIdentifierByType(typeId, value)
    if (validationMessage) {
      errors[`identifier:${typeId}`] = validationMessage
    }
  })

  return errors
}

function EditInstrumentForm({
  detail,
  options,
  isSaving,
  error,
  success,
  serverErrors = {},
  onSubmit,
  onCancel,
}) {
  const instrument = detail?.instrument ?? null
  const editableIdentifierTypes = useMemo(
    () => (options?.identifierTypes ?? []).filter((item) => item.identifierTypeId !== 'ISIN'),
    [options],
  )

  const [name, setName] = useState('')
  const [primaryIsin, setPrimaryIsin] = useState('')
  const [assetClassId, setAssetClassId] = useState('')
  const [sectorId, setSectorId] = useState('')
  const [exchangeId, setExchangeId] = useState('')
  const [currencyId, setCurrencyId] = useState('')
  const [issuerId, setIssuerId] = useState('')
  const [status, setStatus] = useState('')
  const [identifierValues, setIdentifierValues] = useState({})
  const [touched, setTouched] = useState({})

  useEffect(() => {
    if (!instrument) {
      return
    }

    setName(instrument.name ?? '')
    setPrimaryIsin(instrument.primaryIsin ?? '')
    setAssetClassId(instrument.assetClassId ?? '')
    setSectorId(String(instrument.sectorId ?? ''))
    setExchangeId(String(instrument.exchangeId ?? ''))
    setCurrencyId(String(instrument.currencyId ?? ''))
    setIssuerId(String(instrument.issuerId ?? ''))
    setStatus(instrument.status ?? '')
    setIdentifierValues(buildIdentifierValueMap(detail?.identifiers, options?.identifierTypes))
    setTouched({})
  }, [detail?.identifiers, instrument, options?.identifierTypes])

  const handleBlur = (field) => {
    setTouched((previous) => ({ ...previous, [field]: true }))
  }

  const handleIdentifierChange = useCallback((typeId, value) => {
    setIdentifierValues((previous) => ({
      ...previous,
      [typeId]: value,
    }))
  }, [])

  const fields = {
    name,
    primaryIsin,
    assetClassId,
    sectorId,
    exchangeId,
    currencyId,
    issuerId,
    status,
  }

  const validationErrors = useMemo(() => validate(fields, identifierValues), [
    assetClassId,
    currencyId,
    exchangeId,
    identifierValues,
    issuerId,
    name,
    primaryIsin,
    sectorId,
    status,
  ])

  const fieldError = (field) => {
    const clientError = touched[field] ? validationErrors[field] : undefined
    if (clientError) {
      return clientError
    }

    return serverErrors[field]
  }

  const isFormReady = useMemo(() => {
    return Boolean(instrument) && Object.keys(validationErrors).length === 0
  }, [instrument, validationErrors])

  const handleSubmit = async (event) => {
    event.preventDefault()
    if (!instrument) {
      return
    }

    setTouched({
      name: true,
      primaryIsin: true,
      assetClassId: true,
      sectorId: true,
      exchangeId: true,
      currencyId: true,
      issuerId: true,
      status: true,
      ...Object.fromEntries(editableIdentifierTypes.map((item) => [`identifier:${item.identifierTypeId}`, true])),
    })

    if (!isFormReady) {
      return
    }

    const additionalIdentifiers = Object.entries(identifierValues)
      .map(([identifierTypeId, value]) => ({
        identifierTypeId,
        identifierValue: normalizeIdentifierInput(value),
      }))
      .filter((item) => item.identifierValue.length > 0)

    await onSubmit({
      name: name.trim(),
      primaryIsin: normalizeIdentifierInput(primaryIsin),
      assetClassId,
      sectorId: Number(sectorId),
      exchangeId: Number(exchangeId),
      currencyId: Number(currencyId),
      issuerId: Number(issuerId),
      status,
      effectiveDate: instrument.effectiveDate,
      additionalIdentifiers,
    })
  }

  if (!instrument) {
    return (
      <section className="table-panel" aria-label="Edit Instrument">
        <p className="status-message error">Instrument details are unavailable for editing.</p>
      </section>
    )
  }

  return (
    <section className="table-panel" aria-label="Edit Instrument">
      <div className="edit-header-row">
        <div>
          <p className="eyebrow">Editing instrument</p>
          <h2>{instrument.name}</h2>
          <p style={{ marginTop: '0.125rem', fontSize: '0.8125rem', color: 'var(--text-muted)' }}>{instrument.instrumentId}</p>
        </div>
        <button type="button" className="button button-secondary button-sm" onClick={onCancel}>
          ← Back
        </button>
      </div>

      <form className="edit-form" onSubmit={handleSubmit} noValidate>
        <div className="field-row">
          <label htmlFor="edit-name">Name</label>
          <input
            id="edit-name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            onBlur={() => handleBlur('name')}
            maxLength={150}
            required
            aria-describedby={fieldError('name') ? 'edit-name-error' : undefined}
          />
          {fieldError('name') ? (
            <span id="edit-name-error" className="field-error" role="alert">
              {fieldError('name')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="edit-primary-isin">Primary ISIN</label>
          <input
            id="edit-primary-isin"
            value={primaryIsin}
            onChange={(event) => setPrimaryIsin(event.target.value.toUpperCase())}
            onBlur={() => handleBlur('primaryIsin')}
            maxLength={12}
            required
            aria-describedby={fieldError('primaryIsin') ? 'edit-primary-isin-error' : undefined}
          />
          {fieldError('primaryIsin') ? (
            <span id="edit-primary-isin-error" className="field-error" role="alert">
              {fieldError('primaryIsin')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="edit-asset-class">Asset Class</label>
          <select
            id="edit-asset-class"
            value={assetClassId}
            onChange={(event) => setAssetClassId(event.target.value)}
            onBlur={() => handleBlur('assetClassId')}
            required
            aria-describedby={fieldError('assetClassId') ? 'edit-asset-class-error' : undefined}
          >
            {options.assetClasses.map((item) => (
              <option key={item.assetClassId} value={item.assetClassId}>
                {item.name} ({item.assetClassId})
              </option>
            ))}
          </select>
          {fieldError('assetClassId') ? (
            <span id="edit-asset-class-error" className="field-error" role="alert">
              {fieldError('assetClassId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="edit-sector">Sector</label>
          <select
            id="edit-sector"
            value={sectorId}
            onChange={(event) => setSectorId(event.target.value)}
            onBlur={() => handleBlur('sectorId')}
            required
            aria-describedby={fieldError('sectorId') ? 'edit-sector-error' : undefined}
          >
            {options.sectors.map((item) => (
              <option key={item.sectorId} value={String(item.sectorId)}>
                {item.name} ({item.sectorId})
              </option>
            ))}
          </select>
          {fieldError('sectorId') ? (
            <span id="edit-sector-error" className="field-error" role="alert">
              {fieldError('sectorId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="edit-exchange">Exchange</label>
          <select
            id="edit-exchange"
            value={exchangeId}
            onChange={(event) => setExchangeId(event.target.value)}
            onBlur={() => handleBlur('exchangeId')}
            required
            aria-describedby={fieldError('exchangeId') ? 'edit-exchange-error' : undefined}
          >
            {options.exchanges.map((item) => (
              <option key={item.exchangeId} value={String(item.exchangeId)}>
                {item.name} ({item.micCode})
              </option>
            ))}
          </select>
          {fieldError('exchangeId') ? (
            <span id="edit-exchange-error" className="field-error" role="alert">
              {fieldError('exchangeId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="edit-currency">Currency</label>
          <select
            id="edit-currency"
            value={currencyId}
            onChange={(event) => setCurrencyId(event.target.value)}
            onBlur={() => handleBlur('currencyId')}
            required
            aria-describedby={fieldError('currencyId') ? 'edit-currency-error' : undefined}
          >
            {options.currencies.map((item) => (
              <option key={item.currencyId} value={String(item.currencyId)}>
                {item.name} ({item.currencyId})
              </option>
            ))}
          </select>
          {fieldError('currencyId') ? (
            <span id="edit-currency-error" className="field-error" role="alert">
              {fieldError('currencyId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="edit-issuer">Issuer</label>
          <select
            id="edit-issuer"
            value={issuerId}
            onChange={(event) => setIssuerId(event.target.value)}
            onBlur={() => handleBlur('issuerId')}
            required
            aria-describedby={fieldError('issuerId') ? 'edit-issuer-error' : undefined}
          >
            {options.issuers.map((item) => (
              <option key={item.issuerId} value={String(item.issuerId)}>
                {item.name} ({item.issuerId})
              </option>
            ))}
          </select>
          {fieldError('issuerId') ? (
            <span id="edit-issuer-error" className="field-error" role="alert">
              {fieldError('issuerId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="edit-status">Status</label>
          <select
            id="edit-status"
            value={status}
            onChange={(event) => setStatus(event.target.value)}
            onBlur={() => handleBlur('status')}
            required
            aria-describedby={fieldError('status') ? 'edit-status-error' : undefined}
          >
            {options.statuses.map((item) => (
              <option key={item.value} value={item.value}>
                {item.value}
              </option>
            ))}
          </select>
          {fieldError('status') ? (
            <span id="edit-status-error" className="field-error" role="alert">
              {fieldError('status')}
            </span>
          ) : null}
        </div>

        <div className="add-form-section-heading">Identifiers</div>

        {editableIdentifierTypes.map((item) => (
          <div key={item.identifierTypeId} className="field-row">
            <label htmlFor={`edit-id-${item.identifierTypeId}`}>
              {item.name}{' '}
              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>(optional)</span>
            </label>
            <input
              id={`edit-id-${item.identifierTypeId}`}
              value={identifierValues[item.identifierTypeId] ?? ''}
              onChange={(event) => handleIdentifierChange(item.identifierTypeId, event.target.value.toUpperCase())}
              onBlur={() => handleBlur(`identifier:${item.identifierTypeId}`)}
              maxLength={200}
              placeholder={`Enter ${item.name} value`}
              aria-describedby={fieldError(`identifier:${item.identifierTypeId}`) ? `edit-id-${item.identifierTypeId}-error` : undefined}
            />
            {fieldError(`identifier:${item.identifierTypeId}`) ? (
              <span id={`edit-id-${item.identifierTypeId}-error`} className="field-error" role="alert">
                {fieldError(`identifier:${item.identifierTypeId}`)}
              </span>
            ) : null}
          </div>
        ))}

        <div className="field-row">
          <label htmlFor="edit-effective-date">Effective Date</label>
          <input id="edit-effective-date" value={formatDate(instrument.effectiveDate)} readOnly />
        </div>

        <div className="field-row">
          <label htmlFor="edit-last-updated">Last Updated</label>
          <input id="edit-last-updated" value={formatDate(instrument.lastUpdated)} readOnly />
        </div>

        {error ? <p className="status-message error" style={{ margin: 0 }}>{error}</p> : null}
        {success ? <p className="status-message" style={{ margin: 0 }}>{success}</p> : null}

        <div className="actions">
          <button type="submit" className="button button-primary" disabled={!isFormReady || isSaving}>
            {isSaving ? 'Saving…' : 'Save Changes'}
          </button>
          <button type="button" className="button button-secondary" onClick={onCancel}>
            Cancel
          </button>
        </div>
      </form>
    </section>
  )
}

export default EditInstrumentForm
