import { useEffect, useMemo, useState } from 'react'
import { formatDate } from '../utils/instrumentFormatters'

function EditInstrumentForm({
  detail,
  options,
  isSaving,
  error,
  success,
  onSubmit,
  onCancel,
}) {
  const instrument = detail?.instrument ?? null

  const [name, setName] = useState('')
  const [assetClassId, setAssetClassId] = useState('')
  const [sectorId, setSectorId] = useState('')
  const [exchangeId, setExchangeId] = useState('')
  const [currencyId, setCurrencyId] = useState('')
  const [issuerId, setIssuerId] = useState('')
  const [status, setStatus] = useState('')

  useEffect(() => {
    if (!instrument) {
      return
    }

    setName(instrument.name ?? '')
    setAssetClassId(instrument.assetClassId ?? '')
    setSectorId(String(instrument.sectorId ?? ''))
    setExchangeId(String(instrument.exchangeId ?? ''))
    setCurrencyId(String(instrument.currencyId ?? ''))
    setIssuerId(String(instrument.issuerId ?? ''))
    setStatus(instrument.status ?? '')
  }, [instrument])

  const isFormReady = useMemo(() => {
    return Boolean(
      instrument
      && name.trim()
      && assetClassId
      && sectorId
      && exchangeId
      && currencyId
      && issuerId
      && status,
    )
  }, [assetClassId, currencyId, exchangeId, instrument, issuerId, name, sectorId, status])

  const handleSubmit = async (event) => {
    event.preventDefault()
    if (!instrument) {
      return
    }

    await onSubmit({
      name: name.trim(),
      assetClassId,
      sectorId: Number(sectorId),
      exchangeId: Number(exchangeId),
      currencyId: Number(currencyId),
      issuerId: Number(issuerId),
      status,
      effectiveDate: instrument.effectiveDate,
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
          <p className="eyebrow">Edit Instrument</p>
          <h2>{instrument.instrumentId}</h2>
        </div>
        <button type="button" className="button button-secondary" onClick={onCancel}>
          Back to Home
        </button>
      </div>

      <form className="edit-form" onSubmit={handleSubmit}>
        <div className="field-row">
          <label htmlFor="edit-name">Name</label>
          <input
            id="edit-name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            maxLength={150}
            required
          />
        </div>

        <div className="field-row">
          <label htmlFor="edit-asset-class">Asset Class</label>
          <select
            id="edit-asset-class"
            value={assetClassId}
            onChange={(event) => setAssetClassId(event.target.value)}
            required
          >
            {options.assetClasses.map((item) => (
              <option key={item.assetClassId} value={item.assetClassId}>
                {item.name} ({item.assetClassId})
              </option>
            ))}
          </select>
        </div>

        <div className="field-row">
          <label htmlFor="edit-sector">Sector</label>
          <select
            id="edit-sector"
            value={sectorId}
            onChange={(event) => setSectorId(event.target.value)}
            required
          >
            {options.sectors.map((item) => (
              <option key={item.sectorId} value={String(item.sectorId)}>
                {item.name} ({item.sectorId})
              </option>
            ))}
          </select>
        </div>

        <div className="field-row">
          <label htmlFor="edit-exchange">Exchange</label>
          <select
            id="edit-exchange"
            value={exchangeId}
            onChange={(event) => setExchangeId(event.target.value)}
            required
          >
            {options.exchanges.map((item) => (
              <option key={item.exchangeId} value={String(item.exchangeId)}>
                {item.name} ({item.micCode})
              </option>
            ))}
          </select>
        </div>

        <div className="field-row">
          <label htmlFor="edit-currency">Currency</label>
          <select
            id="edit-currency"
            value={currencyId}
            onChange={(event) => setCurrencyId(event.target.value)}
            required
          >
            {options.currencies.map((item) => (
              <option key={item.currencyId} value={String(item.currencyId)}>
                {item.name} ({item.currencyId})
              </option>
            ))}
          </select>
        </div>

        <div className="field-row">
          <label htmlFor="edit-issuer">Issuer</label>
          <select
            id="edit-issuer"
            value={issuerId}
            onChange={(event) => setIssuerId(event.target.value)}
            required
          >
            {options.issuers.map((item) => (
              <option key={item.issuerId} value={String(item.issuerId)}>
                {item.name} ({item.issuerId})
              </option>
            ))}
          </select>
        </div>

        <div className="field-row">
          <label htmlFor="edit-status">Status</label>
          <select
            id="edit-status"
            value={status}
            onChange={(event) => setStatus(event.target.value)}
            required
          >
            {options.statuses.map((item) => (
              <option key={item.value} value={item.value}>
                {item.value}
              </option>
            ))}
          </select>
        </div>

        <div className="field-row">
          <label htmlFor="edit-primary-isin">Primary ISIN</label>
          <input id="edit-primary-isin" value={instrument.primaryIsin ?? ''} readOnly />
        </div>

        <div className="field-row">
          <label htmlFor="edit-effective-date">Effective Date</label>
          <input id="edit-effective-date" value={formatDate(instrument.effectiveDate)} readOnly />
        </div>

        <div className="field-row">
          <label htmlFor="edit-last-updated">Last Updated</label>
          <input id="edit-last-updated" value={formatDate(instrument.lastUpdated)} readOnly />
        </div>

        {error ? <p className="status-message error">{error}</p> : null}
        {success ? <p className="status-message">{success}</p> : null}

        <div className="actions">
          <button type="submit" className="button button-primary" disabled={!isFormReady || isSaving}>
            {isSaving ? 'Saving...' : 'Save Changes'}
          </button>
        </div>
      </form>
    </section>
  )
}

export default EditInstrumentForm
