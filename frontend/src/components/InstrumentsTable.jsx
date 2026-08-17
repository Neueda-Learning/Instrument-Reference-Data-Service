import { formatDate, pickIdentifier } from '../utils/instrumentFormatters'

function InstrumentsTable({
  rows,
  totalRowsCount,
  sortBy,
  sortDirection,
  selectedInstrumentId,
  onSort,
  onSelectInstrument,
  onOpenMetadata,
}) {
  return (
    <>
      <div className="table-summary">
        <span>
          Showing {rows.length} of {totalRowsCount} records
        </span>
        <span>
          Sorted by {sortBy} ({sortDirection})
        </span>
      </div>

      <div className="table-scroll">
        <table>
          <thead>
            <tr>
              <th>
                <button type="button" className="sort-link" onClick={() => onSort('instrumentId')}>
                  Instrument ID
                </button>
              </th>
              <th>
                <button type="button" className="sort-link" onClick={() => onSort('name')}>
                  Name
                </button>
              </th>
              <th>ISIN</th>
              <th>CUSIP</th>
              <th>Asset Class</th>
              <th>Exchange</th>
              <th>Currency</th>
              <th>Status</th>
              <th>
                <button type="button" className="sort-link" onClick={() => onSort('lastUpdated')}>
                  Last Updated
                </button>
              </th>
              <th>Metadata</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => {
              const instrumentId = row.instrument.instrumentId
              const isSelected = selectedInstrumentId === instrumentId

              return (
                <tr
                  key={instrumentId}
                  className={isSelected ? 'table-row-selected' : ''}
                  onClick={() => onSelectInstrument(instrumentId)}
                >
                  <td className="mono">{instrumentId}</td>
                  <td>{row.instrument.name}</td>
                  <td className="mono">{pickIdentifier(row.identifiers, 'ISIN')}</td>
                  <td className="mono">{pickIdentifier(row.identifiers, 'CUSIP')}</td>
                  <td>{row.instrument.assetClassName}</td>
                  <td>{row.instrument.exchangeMicCode}</td>
                  <td>{row.instrument.currencyName}</td>
                  <td>
                    <span className={`status-pill status-${String(row.instrument.status).toLowerCase()}`}>
                      {row.instrument.status}
                    </span>
                  </td>
                  <td>{formatDate(row.instrument.lastUpdated)}</td>
                  <td>
                    <button
                      type="button"
                      className="view-meta-button"
                      onClick={(event) => {
                        event.stopPropagation()
                        onOpenMetadata(instrumentId)
                      }}
                    >
                      View Full Metadata
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
