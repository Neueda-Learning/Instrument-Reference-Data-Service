function InstrumentSearchForm({
  isin,
  cusip,
  isLoading,
  hasActiveFilters,
  lastQuery,
  onIsinChange,
  onCusipChange,
  onSearch,
  onReset,
}) {
  return (
    <section className="search-panel" aria-label="Instrument Search">
      <form onSubmit={onSearch} className="search-form">
        <div className="field-row">
          <label htmlFor="isin-input">ISIN</label>
          <input
            id="isin-input"
            value={isin}
            onChange={(event) => onIsinChange(event.target.value)}
            placeholder="US0378331005"
            maxLength={12}
            autoComplete="off"
          />
        </div>

        <div className="field-row">
          <label htmlFor="cusip-input">CUSIP</label>
          <input
            id="cusip-input"
            value={cusip}
            onChange={(event) => onCusipChange(event.target.value)}
            placeholder="037833100"
            maxLength={9}
            autoComplete="off"
          />
        </div>

        <div className="actions">
          <button type="submit" className="button button-primary" disabled={isLoading}>
            {isLoading ? 'Searching...' : 'Search'}
          </button>
          <button
            type="button"
            className="button button-secondary"
            onClick={onReset}
            disabled={isLoading}
          >
            Reset
          </button>
        </div>
      </form>

      <p className="query-info">
        {hasActiveFilters
          ? `Filtered by ${lastQuery.isin ? `ISIN: ${lastQuery.isin}` : ''}${lastQuery.isin && lastQuery.cusip ? ' | ' : ''}${lastQuery.cusip ? `CUSIP: ${lastQuery.cusip}` : ''}`
          : 'Showing all instruments'}
      </p>
    </section>
  )
}

export default InstrumentSearchForm
