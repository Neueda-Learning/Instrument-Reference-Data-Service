import { formatDate, pickIdentifier } from '../utils/instrumentFormatters'

function SortIcon({ column, sortBy, sortDirection }) {
  const isActive = sortBy === column
  if (isActive) {
    return sortDirection === 'asc' ? (
      <svg width="10" height="10" viewBox="0 0 10 10" fill="currentColor" aria-hidden="true">
        <path d="M5 2L9 7H1L5 2Z"/>
      </svg>
    ) : (
      <svg width="10" height="10" viewBox="0 0 10 10" fill="currentColor" aria-hidden="true">
        <path d="M5 8L1 3H9L5 8Z"/>
      </svg>
    )
  }
  return (
    <svg width="10" height="10" viewBox="0 0 10 10" fill="currentColor" opacity="0.35" aria-hidden="true">
      <path d="M5 1.5L8 5H2L5 1.5ZM5 8.5L2 5H8L5 8.5Z"/>
    </svg>
  )
}

function InstrumentsTable({
  rows,
  totalRowsCount,
  sortBy,
  sortDirection,
  selectedInstrumentId,
  onSort,
  onSelectInstrument,
  onOpenMetadata,
  selectedIds,
  onToggleSelect,
  onSelectAllRows,
}) {
  return (
    <>
      <div className="table-scroll">
        <table>
          <thead>
            <tr>
              {selectedIds !== undefined && (
                <th className="checkbox-column">
                  <input
                    type="checkbox"
                    checked={selectedIds.length > 0 && selectedIds.length === rows.length}
                    indeterminate={selectedIds.length > 0 && selectedIds.length < rows.length}
                    onChange={() => onSelectAllRows(!selectedIds.length || selectedIds.length < rows.length)}
                    aria-label="Select all visible instruments"
                  />
                </th>
              )}
              <th>
                <button
                  type="button"
                  className={`sort-link ${sortBy === 'instrumentId' ? 'sort-active' : ''}`}
                  onClick={() => onSort('instrumentId')}
                >
                  Instrument ID
                  <SortIcon column="instrumentId" sortBy={sortBy} sortDirection={sortDirection} />
                </button>
              </th>
              <th>
                <button
                  type="button"
                  className={`sort-link ${sortBy === 'name' ? 'sort-active' : ''}`}
                  onClick={() => onSort('name')}
                >
                  Name
                  <SortIcon column="name" sortBy={sortBy} sortDirection={sortDirection} />
                </button>
              </th>
              <th>ISIN</th>
              <th>CUSIP</th>
              <th>Asset Class</th>
              <th>Exchange</th>
              <th>Currency</th>
              <th>Status</th>
              <th>
                <button
                  type="button"
                  className={`sort-link ${sortBy === 'lastUpdated' ? 'sort-active' : ''}`}
                  onClick={() => onSort('lastUpdated')}
                >
                  Last Updated
                  <SortIcon column="lastUpdated" sortBy={sortBy} sortDirection={sortDirection} />
                </button>
              </th>
              <th>Details</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => {
              const instrumentId = row.instrument.instrumentId
              const isSelected = selectedInstrumentId === instrumentId
              const isBulkSelected = selectedIds && selectedIds.includes(instrumentId)

              return (
                <tr
                  key={instrumentId}
                  className={`${isSelected ? 'table-row-selected' : ''} ${isBulkSelected ? 'table-row-bulk-selected' : ''}`}
                  onClick={() => onSelectInstrument(instrumentId)}
                >
                  {selectedIds !== undefined && (
                    <td className="checkbox-column" onClick={(e) => e.stopPropagation()}>
                      <input
                        type="checkbox"
                        checked={isBulkSelected}
                        onChange={() => onToggleSelect(instrumentId)}
                        aria-label={`Select ${row.instrument.name}`}
                      />
                    </td>
                  )}
                  <td className="mono">{instrumentId}</td>
                  <td style={{ fontWeight: 500 }}>{row.instrument.name}</td>
                  <td className="mono">{pickIdentifier(row.identifiers, 'ISIN')}</td>
                  <td className="mono">{pickIdentifier(row.identifiers, 'CUSIP')}</td>
                  <td>{row.instrument.assetClassName}</td>
                  <td className="mono">{row.instrument.exchangeMicCode}</td>
                  <td>{row.instrument.currencyName}</td>
                  <td>
                    <span className={`status-pill status-${String(row.instrument.status).toLowerCase()}`}>
                      {row.instrument.status}
                    </span>
                  </td>
                  <td style={{ color: 'var(--text-soft)' }}>{formatDate(row.instrument.lastUpdated)}</td>
                  <td>
                    <button
                      type="button"
                      className="view-meta-button"
                      onClick={(event) => {
                        event.stopPropagation()
                        onOpenMetadata(instrumentId)
                      }}
                    >
                      View Details
                      <svg width="10" height="10" viewBox="0 0 10 10" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                        <path d="M2 5h6M5 2l3 3-3 3"/>
                      </svg>
                    </button>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </>
  )
}

export default InstrumentsTable
