function InstrumentSearchForm({
  name = '',
  isin,
  cusip,
  isLoading,
  hasActiveFilters,
  lastQuery,
  onNameChange = () => {},
  onIsinChange,
  onCusipChange,
  onSearch,
  onReset,
}) {
  return (
    <section className="search-panel" aria-label="Instrument Search">
      <form onSubmit={onSearch} className="search-form">
        <div className="field-row">
          <label htmlFor="name-input">Name</label>
          <div className="form-input-wrap">
            <span className="form-input-icon" aria-hidden="true">
              <svg width="13" height="13" viewBox="0 0 13 13" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round">
                <circle cx="5.5" cy="5.5" r="4"/>
                <path d="M11.5 11.5L8.5 8.5"/>
              </svg>
            </span>
            <input
              id="name-input"
              className="has-icon"
              value={name}
              onChange={(event) => onNameChange(event.target.value)}
              placeholder="e.g. Apple"
              autoComplete="off"
            />
          </div>
        </div>

        <div className="field-row">
          <label htmlFor="isin-input">ISIN</label>
          <div className="form-input-wrap">
            <span className="form-input-icon" aria-hidden="true">
              <svg width="13" height="13" viewBox="0 0 13 13" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round">
                <circle cx="5.5" cy="5.5" r="4"/>
                <path d="M11.5 11.5L8.5 8.5"/>
              </svg>
            </span>
            <input
              id="isin-input"
              className="has-icon"
              value={isin}
              onChange={(event) => onIsinChange(event.target.value)}
              placeholder="e.g. US0378331005"
              maxLength={12}
              autoComplete="off"
            />
          </div>
        </div>

        <div className="field-row">
          <label htmlFor="cusip-input">CUSIP</label>
          <div className="form-input-wrap">
            <span className="form-input-icon" aria-hidden="true">
              <svg width="13" height="13" viewBox="0 0 13 13" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round">
                <circle cx="5.5" cy="5.5" r="4"/>
                <path d="M11.5 11.5L8.5 8.5"/>
              </svg>
            </span>
            <input
              id="cusip-input"
              className="has-icon"
              value={cusip}
              onChange={(event) => onCusipChange(event.target.value)}
              placeholder="e.g. 037833100"
              maxLength={9}
              autoComplete="off"
            />
          </div>
        </div>

        <div className="actions">
          <button type="submit" className="button button-primary" disabled={isLoading}>
            {isLoading ? 'Searching…' : 'Search'}
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
        {hasActiveFilters ? (
          <>
            Filtered by&nbsp;
            {lastQuery.isin ? (
              <span className="filter-chip">ISIN: {lastQuery.isin}</span>
            ) : null}
            {lastQuery.name ? (
              <span className="filter-chip">Name: {lastQuery.name}</span>
            ) : null}
            {lastQuery.cusip ? (
              <span className="filter-chip">CUSIP: {lastQuery.cusip}</span>
            ) : null}
          </>
        ) : (
          'Showing all instruments'
        )}
      </p>
    </section>
  )
}

export default InstrumentSearchForm
