import { useState, useCallback } from 'react'
import './AdvancedSearch.css'

function AdvancedSearch({
  isLoading,
  assetClasses,
  sectors,
  exchanges,
  onSearch,
  onReset,
}) {
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedAssetClasses, setSelectedAssetClasses] = useState([])
  const [selectedSectors, setSelectedSectors] = useState([])
  const [selectedExchanges, setSelectedExchanges] = useState([])
  const [selectedStatuses, setSelectedStatuses] = useState([])

  const STATUSES = ['Active', 'Delisted', 'Suspended', 'Pending']

  const handleToggleFilter = useCallback((value, selected, setSelected) => {
    setSelected((prev) =>
      prev.includes(value) ? prev.filter((v) => v !== value) : [...prev, value]
    )
  }, [])

  const handleSearch = (e) => {
    e.preventDefault()
    onSearch({
      query: searchQuery.trim(),
      assetClasses: selectedAssetClasses,
      sectors: selectedSectors,
      exchanges: selectedExchanges,
      statuses: selectedStatuses.map((s) => s.toLowerCase()),
    })
  }

  const handleReset = () => {
    setSearchQuery('')
    setSelectedAssetClasses([])
    setSelectedSectors([])
    setSelectedExchanges([])
    setSelectedStatuses([])
    onReset()
  }

  const activeFiltersCount = [
    searchQuery ? 1 : 0,
    selectedAssetClasses.length,
    selectedSectors.length,
    selectedExchanges.length,
    selectedStatuses.length,
  ].reduce((a, b) => a + b, 0)

  return (
    <section className="advanced-search-panel" aria-label="Advanced Search">
      <form onSubmit={handleSearch} className="advanced-search-form">
        {/* Full-text search */}
        <div className="search-field-group">
          <label htmlFor="fts-input" className="field-label">
            Full-Text Search
          </label>
          <div className="form-input-wrap">
            <span className="form-input-icon" aria-hidden="true">
              <svg width="13" height="13" viewBox="0 0 13 13" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round">
                <circle cx="5.5" cy="5.5" r="4" />
                <path d="M11.5 11.5L8.5 8.5" />
              </svg>
            </span>
            <input
              id="fts-input"
              className="has-icon"
              type="search"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search by name, issuer, sector..."
              disabled={isLoading}
            />
          </div>
          <p className="field-hint">Searches instrument name, issuer, and sector</p>
        </div>

        {/* Faceted filters grid */}
        <div className="filters-grid">
          {/* Asset Class Filter */}
          {assetClasses && assetClasses.length > 0 && (
            <div className="filter-group">
              <span className="filter-group-title">Asset Class</span>
              <div className="filter-options">
                {assetClasses.map((assetClass) => (
                  <label key={assetClass.assetClassId} className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={selectedAssetClasses.includes(assetClass.assetClassId)}
                      onChange={() =>
                        handleToggleFilter(
                          assetClass.assetClassId,
                          selectedAssetClasses,
                          setSelectedAssetClasses
                        )
                      }
                      disabled={isLoading}
                    />
                    <span>{assetClass.name}</span>
                  </label>
                ))}
              </div>
            </div>
          )}

          {/* Sector Filter */}
          {sectors && sectors.length > 0 && (
            <div className="filter-group">
              <span className="filter-group-title">Sector</span>
              <div className="filter-options">
                {sectors.slice(0, 6).map((sector) => (
                  <label key={sector.sectorId} className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={selectedSectors.includes(sector.sectorId)}
                      onChange={() =>
                        handleToggleFilter(
                          sector.sectorId,
                          selectedSectors,
                          setSelectedSectors
                        )
                      }
                      disabled={isLoading}
                    />
                    <span>{sector.name}</span>
                  </label>
                ))}
                {sectors.length > 6 && (
                  <p className="filter-more-text">+ {sectors.length - 6} more</p>
                )}
              </div>
            </div>
          )}

          {/* Exchange Filter */}
          {exchanges && exchanges.length > 0 && (
            <div className="filter-group">
              <span className="filter-group-title">Exchange</span>
              <div className="filter-options">
                {exchanges.slice(0, 6).map((exchange) => (
                  <label key={exchange.exchangeId} className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={selectedExchanges.includes(exchange.exchangeId)}
                      onChange={() =>
                        handleToggleFilter(
                          exchange.exchangeId,
                          selectedExchanges,
                          setSelectedExchanges
                        )
                      }
                      disabled={isLoading}
                    />
                    <span>{exchange.name}</span>
                  </label>
                ))}
                {exchanges.length > 6 && (
                  <p className="filter-more-text">+ {exchanges.length - 6} more</p>
                )}
              </div>
            </div>
          )}

          {/* Status Filter */}
          <div className="filter-group">
            <span className="filter-group-title">Status</span>
            <div className="filter-options">
              {STATUSES.map((status) => (
                <label key={status} className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={selectedStatuses.includes(status)}
                    onChange={() =>
                      handleToggleFilter(status, selectedStatuses, setSelectedStatuses)
                    }
                    disabled={isLoading}
                  />
                  <span>{status}</span>
                </label>
              ))}
            </div>
          </div>
        </div>

        {/* Actions */}
        <div className="search-actions">
          <button
            type="submit"
            className="button button-primary"
            disabled={isLoading}
          >
            {isLoading ? 'Searching…' : 'Search'}
          </button>
          <button
            type="button"
            className="button button-secondary"
            onClick={handleReset}
            disabled={isLoading}
          >
            Clear All
          </button>
          {activeFiltersCount > 0 && (
            <span className="active-filters-badge">{activeFiltersCount} filter{activeFiltersCount !== 1 ? 's' : ''} active</span>
          )}
        </div>
      </form>
    </section>
  )
}

export default AdvancedSearch
