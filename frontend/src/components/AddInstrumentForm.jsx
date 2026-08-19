import { useCallback, useMemo, useState } from 'react'
import { normalizeIdentifierInput, validateIdentifierByType } from '../utils/identifierValidation'

const INSTRUMENT_ID_REGEX = /^INS-\d{12}-\d{4}$/

function generateInstrumentId() {
  const now = new Date()
  const yy = String(now.getUTCFullYear()).slice(2)
  const MM = String(now.getUTCMonth() + 1).padStart(2, '0')
  const dd = String(now.getUTCDate()).padStart(2, '0')
  const HH = String(now.getUTCHours()).padStart(2, '0')
  const mm = String(now.getUTCMinutes()).padStart(2, '0')
  const ss = String(now.getUTCSeconds()).padStart(2, '0')
  return `INS-${yy}${MM}${dd}${HH}${mm}${ss}-0001`
}

function todayIso() {
  return new Date().toISOString().slice(0, 10)
}

function validate(fields, identifierValues) {
  const errors = {}

  if (!INSTRUMENT_ID_REGEX.test(fields.instrumentId.trim())) {
    errors.instrumentId = 'ID must follow the format INS-YYMMDDHHMMSS-NNNN (e.g. INS-260819143022-0001).'
  }

  if (!fields.name.trim()) {
    errors.name = 'Name is required.'
  } else if (fields.name.trim().length > 150) {
    errors.name = 'Name must be 150 characters or fewer.'
  }

  const isinUpper = normalizeIdentifierInput(fields.primaryIsin)
  if (!isinUpper) {
    errors.primaryIsin = 'Primary ISIN is required.'
  } else {
    const primaryIsinError = validateIdentifierByType('ISIN', isinUpper)
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

  if (!fields.effectiveDate) {
    errors.effectiveDate = 'Effective date is required.'
  }

  Object.entries(identifierValues ?? {}).forEach(([typeId, value]) => {
    const validationMessage = validateIdentifierByType(typeId, value)
    if (validationMessage) {
      errors[`identifier:${typeId}`] = validationMessage
    }
  })

  return errors
}

function AddInstrumentForm({ options, isSaving, error, serverErrors = {}, onSubmit, onCancel }) {
  const [instrumentId, setInstrumentId] = useState(() => generateInstrumentId())
  const [name, setName] = useState('')
  const [primaryIsin, setPrimaryIsin] = useState('')
  const [assetClassId, setAssetClassId] = useState(() => options?.assetClasses?.[0]?.assetClassId ?? '')
  const [sectorId, setSectorId] = useState(() => String(options?.sectors?.[0]?.sectorId ?? ''))
  const [exchangeId, setExchangeId] = useState(() => String(options?.exchanges?.[0]?.exchangeId ?? ''))
  const [currencyId, setCurrencyId] = useState(() => String(options?.currencies?.[0]?.currencyId ?? ''))
  const [issuerId, setIssuerId] = useState(() => String(options?.issuers?.[0]?.issuerId ?? ''))
  const [status, setStatus] = useState(() => options?.statuses?.[0]?.value ?? 'Active')
  const [effectiveDate, setEffectiveDate] = useState(todayIso)

  // Identifier values keyed by identifierTypeId (ISIN is derived from primaryIsin automatically)
  const editableIdentifierTypes = useMemo(
    () => (options?.identifierTypes ?? []).filter((t) => t.identifierTypeId !== 'ISIN'),
    [options],
  )
  const [identifierValues, setIdentifierValues] = useState(() =>
    Object.fromEntries(
      (options?.identifierTypes ?? [])
        .filter((t) => t.identifierTypeId !== 'ISIN')
        .map((t) => [t.identifierTypeId, '']),
    ),
  )

  const [touched, setTouched] = useState({})

  const fields = {
    instrumentId,
    name,
    primaryIsin,
    assetClassId,
    sectorId,
    exchangeId,
    currencyId,
    issuerId,
    status,
    effectiveDate,
  }

  const validationErrors = useMemo(() => validate(fields, identifierValues), [
    instrumentId,
    name,
    primaryIsin,
    assetClassId,
    sectorId,
    exchangeId,
    currencyId,
    issuerId,
    status,
    effectiveDate,
    identifierValues,
  ])

  const isFormValid = Object.keys(validationErrors).length === 0

  const handleBlur = (field) => setTouched((prev) => ({ ...prev, [field]: true }))

  const handleIdentifierChange = useCallback((typeId, value) => {
    setIdentifierValues((prev) => ({ ...prev, [typeId]: value }))
  }, [])

  const handleSubmit = async (event) => {
    event.preventDefault()

    // Mark all fields touched to surface all errors
    setTouched({
      instrumentId: true,
      name: true,
      primaryIsin: true,
      assetClassId: true,
      sectorId: true,
      exchangeId: true,
      currencyId: true,
      issuerId: true,
      status: true,
      effectiveDate: true,
      ...Object.fromEntries(editableIdentifierTypes.map((item) => [`identifier:${item.identifierTypeId}`, true])),
    })

    if (!isFormValid) {
      return
    }

    const additionalIdentifiers = Object.entries(identifierValues)
      .map(([identifierTypeId, value]) => ({
        identifierTypeId,
        identifierValue: normalizeIdentifierInput(value),
      }))
      .filter((item) => item.identifierValue.length > 0)

    await onSubmit({
      instrumentId: instrumentId.trim(),
      name: name.trim(),
      primaryIsin: primaryIsin.trim().toUpperCase(),
      assetClassId,
      sectorId: Number(sectorId),
      exchangeId: Number(exchangeId),
      currencyId: Number(currencyId),
      issuerId: Number(issuerId),
      status,
      effectiveDate,
      additionalIdentifiers,
    })
  }

  const fieldError = (field) => {
    const clientError = touched[field] ? validationErrors[field] : undefined
    if (clientError) {
      return clientError
    }

    return serverErrors[field]
  }

  return (
    <section className="table-panel" aria-label="Add Instrument">
      <div className="edit-header-row">
        <div>
          <p className="eyebrow">New instrument</p>
          <h2>Add Instrument</h2>
        </div>
        <button type="button" className="button button-secondary button-sm" onClick={onCancel}>
          ← Back
        </button>
      </div>

      <form className="edit-form" onSubmit={handleSubmit} noValidate>
        <div className="field-row">
          <label htmlFor="add-instrument-id">Instrument ID</label>
          <input
            id="add-instrument-id"
            value={instrumentId}
            onChange={(event) => setInstrumentId(event.target.value)}
            onBlur={() => handleBlur('instrumentId')}
            maxLength={40}
            placeholder="INS-YYMMDDHHMMSS-NNNN"
            required
            aria-describedby={fieldError('instrumentId') ? 'add-instrument-id-error' : undefined}
          />
          {fieldError('instrumentId') ? (
            <span id="add-instrument-id-error" className="field-error" role="alert">
              {fieldError('instrumentId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="add-name">Name</label>
          <input
            id="add-name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            onBlur={() => handleBlur('name')}
            maxLength={150}
            placeholder="e.g. Atlas Capital Common Stock (NASDAQ)"
            required
            aria-describedby={fieldError('name') ? 'add-name-error' : undefined}
          />
          {fieldError('name') ? (
            <span id="add-name-error" className="field-error" role="alert">
              {fieldError('name')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="add-asset-class">Asset Class</label>
          <select
            id="add-asset-class"
            value={assetClassId}
            onChange={(event) => setAssetClassId(event.target.value)}
            onBlur={() => handleBlur('assetClassId')}
            required
            aria-describedby={fieldError('assetClassId') ? 'add-asset-class-error' : undefined}
          >
            <option value="" disabled>Select asset class…</option>
            {options.assetClasses.map((item) => (
              <option key={item.assetClassId} value={item.assetClassId}>
                {item.name} ({item.assetClassId})
              </option>
            ))}
          </select>
          {fieldError('assetClassId') ? (
            <span id="add-asset-class-error" className="field-error" role="alert">
              {fieldError('assetClassId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="add-sector">Sector</label>
          <select
            id="add-sector"
            value={sectorId}
            onChange={(event) => setSectorId(event.target.value)}
            onBlur={() => handleBlur('sectorId')}
            required
            aria-describedby={fieldError('sectorId') ? 'add-sector-error' : undefined}
          >
            <option value="" disabled>Select sector…</option>
            {options.sectors.map((item) => (
              <option key={item.sectorId} value={String(item.sectorId)}>
                {item.name} ({item.sectorId})
              </option>
            ))}
          </select>
          {fieldError('sectorId') ? (
            <span id="add-sector-error" className="field-error" role="alert">
              {fieldError('sectorId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="add-exchange">Exchange</label>
          <select
            id="add-exchange"
            value={exchangeId}
            onChange={(event) => setExchangeId(event.target.value)}
            onBlur={() => handleBlur('exchangeId')}
            required
            aria-describedby={fieldError('exchangeId') ? 'add-exchange-error' : undefined}
          >
            <option value="" disabled>Select exchange…</option>
            {options.exchanges.map((item) => (
              <option key={item.exchangeId} value={String(item.exchangeId)}>
                {item.name} ({item.micCode})
              </option>
            ))}
          </select>
          {fieldError('exchangeId') ? (
            <span id="add-exchange-error" className="field-error" role="alert">
              {fieldError('exchangeId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="add-currency">Currency</label>
          <select
            id="add-currency"
            value={currencyId}
            onChange={(event) => setCurrencyId(event.target.value)}
            onBlur={() => handleBlur('currencyId')}
            required
            aria-describedby={fieldError('currencyId') ? 'add-currency-error' : undefined}
          >
            <option value="" disabled>Select currency…</option>
            {options.currencies.map((item) => (
              <option key={item.currencyId} value={String(item.currencyId)}>
                {item.name} ({item.currencyId})
              </option>
            ))}
          </select>
          {fieldError('currencyId') ? (
            <span id="add-currency-error" className="field-error" role="alert">
              {fieldError('currencyId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="add-issuer">Issuer</label>
          <select
            id="add-issuer"
            value={issuerId}
            onChange={(event) => setIssuerId(event.target.value)}
            onBlur={() => handleBlur('issuerId')}
            required
            aria-describedby={fieldError('issuerId') ? 'add-issuer-error' : undefined}
          >
            <option value="" disabled>Select issuer…</option>
            {options.issuers.map((item) => (
              <option key={item.issuerId} value={String(item.issuerId)}>
                {item.name} ({item.issuerId})
              </option>
            ))}
          </select>
          {fieldError('issuerId') ? (
            <span id="add-issuer-error" className="field-error" role="alert">
              {fieldError('issuerId')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="add-status">Status</label>
          <select
            id="add-status"
            value={status}
            onChange={(event) => setStatus(event.target.value)}
            onBlur={() => handleBlur('status')}
            required
            aria-describedby={fieldError('status') ? 'add-status-error' : undefined}
          >
            <option value="" disabled>Select status…</option>
            {options.statuses.map((item) => (
              <option key={item.value} value={item.value}>
                {item.value}
              </option>
            ))}
          </select>
          {fieldError('status') ? (
            <span id="add-status-error" className="field-error" role="alert">
              {fieldError('status')}
            </span>
          ) : null}
        </div>

        <div className="field-row">
          <label htmlFor="add-effective-date">Effective Date</label>
          <input
            id="add-effective-date"
            type="date"
            value={effectiveDate}
            onChange={(event) => setEffectiveDate(event.target.value)}
            onBlur={() => handleBlur('effectiveDate')}
            required
            aria-describedby={fieldError('effectiveDate') ? 'add-effective-date-error' : undefined}
          />
          {fieldError('effectiveDate') ? (
            <span id="add-effective-date-error" className="field-error" role="alert">
              {fieldError('effectiveDate')}
            </span>
          ) : null}
        </div>

        {/* ── Identifiers section ── */}
        <div className="add-form-section-heading">Identifiers</div>

        {/* ISIN is always auto-created from primaryIsin */}
        <div className="field-row">
          <label htmlFor="add-isin">
            ISIN <span className="field-label-required">*</span>
          </label>
          <input
            id="add-isin"
            value={primaryIsin}
            onChange={(event) => setPrimaryIsin(event.target.value.toUpperCase())}
            onBlur={() => handleBlur('primaryIsin')}
            maxLength={12}
            placeholder="e.g. US38259P5089"
            required
            aria-describedby={fieldError('primaryIsin') ? 'add-isin-error' : undefined}
          />
          <span className="field-hint">
            Becomes the Primary ISIN and is automatically registered as an ISIN identifier.
          </span>
          {fieldError('primaryIsin') ? (
            <span id="add-isin-error" className="field-error" role="alert">
              {fieldError('primaryIsin')}
            </span>
          ) : null}
        </div>

        {editableIdentifierTypes.map((idType) => (
          <div key={idType.identifierTypeId} className="field-row">
            <label htmlFor={`add-id-${idType.identifierTypeId}`}>
              {idType.name}{' '}
              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>(optional)</span>
            </label>
            <input
              id={`add-id-${idType.identifierTypeId}`}
              value={identifierValues[idType.identifierTypeId] ?? ''}
              onChange={(event) => handleIdentifierChange(idType.identifierTypeId, event.target.value.toUpperCase())}
              onBlur={() => handleBlur(`identifier:${idType.identifierTypeId}`)}
              maxLength={200}
              placeholder={`Enter ${idType.name} value`}
            />
            {fieldError(`identifier:${idType.identifierTypeId}`) ? (
              <span className="field-error" role="alert">
                {fieldError(`identifier:${idType.identifierTypeId}`)}
              </span>
            ) : null}
          </div>
        ))}

        {error ? (
          <p className="status-message error" style={{ margin: 0 }}>
            {error}
          </p>
        ) : null}

        <div className="actions">
          <button
            type="submit"
            className="button button-primary"
            disabled={isSaving}
          >
            {isSaving ? 'Creating…' : 'Create Instrument'}
          </button>
          <button type="button" className="button button-secondary" onClick={onCancel}>
            Cancel
          </button>
        </div>
      </form>
    </section>
  )
}

export default AddInstrumentForm
