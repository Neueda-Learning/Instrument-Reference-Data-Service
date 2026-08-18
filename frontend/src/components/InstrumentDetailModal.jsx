import { formatDate, formatDateTime } from '../utils/instrumentFormatters'
import './InstrumentDetailModal.css'

function InstrumentDetailModal({
  isOpen,
  instrument,
  identifiers,
  isLoading,
  error,
  onClose,
  onEdit,
  onDelete,
}) {
  if (!isOpen) return null

  if (isLoading) {
    return (
      <div className="modal-overlay" onClick={onClose}>
        <div className="modal-content master-detail-modal" onClick={(e) => e.stopPropagation()}>
          <p className="modal-loading">Loading instrument details...</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="modal-overlay" onClick={onClose}>
        <div className="modal-content master-detail-modal" onClick={(e) => e.stopPropagation()}>
          <div className="modal-header">
            <h2>Instrument Details</h2>
            <button className="modal-close-btn" onClick={onClose} aria-label="Close modal">
              ✕
            </button>
          </div>
          <p className="error-message">{error}</p>
        </div>
      </div>
    )
  }

  if (!instrument) return null

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        className="modal-content master-detail-modal"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header">
          <div>
            <h2>{instrument.name}</h2>
            <p className="modal-subtitle">{instrument.instrumentId}</p>
          </div>
          <button className="modal-close-btn" onClick={onClose} aria-label="Close modal">
            ✕
          </button>
        </div>

        <div className="modal-body master-detail-body">
          <div className="detail-section">
            <h3>Core Information</h3>
            <div className="detail-grid">
              <div className="detail-row">
                <span className="detail-label">Instrument ID</span>
                <span className="detail-value mono">{instrument.instrumentId}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Name</span>
                <span className="detail-value">{instrument.name}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Status</span>
                <span className={`status-pill status-${String(instrument.status).toLowerCase()}`}>
                  {instrument.status}
                </span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Asset Class</span>
                <span className="detail-value">{instrument.assetClassName}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Sector</span>
                <span className="detail-value">{instrument.sectorName || '-'}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Issuer</span>
                <span className="detail-value">{instrument.issuerName || '-'}</span>
              </div>
            </div>
          </div>

          <div className="detail-section">
            <h3>Market Information</h3>
            <div className="detail-grid">
              <div className="detail-row">
                <span className="detail-label">Exchange</span>
                <span className="detail-value mono">{instrument.exchangeMicCode}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Exchange Name</span>
                <span className="detail-value">{instrument.exchangeName || '-'}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Currency</span>
                <span className="detail-value">{instrument.currencyName}</span>
              </div>
            </div>
          </div>

          {identifiers && identifiers.length > 0 && (
            <div className="detail-section">
              <h3>Identifiers</h3>
              <div className="identifiers-table">
                <table>
                  <thead>
                    <tr>
                      <th>Type</th>
                      <th>Value</th>
                    </tr>
                  </thead>
                  <tbody>
                    {identifiers.map((id) => (
                      <tr key={id.identifierTypeId}>
                        <td className="identifier-type">{id.identifierTypeId}</td>
                        <td className="identifier-value mono">{id.identifierValue}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          <div className="detail-section">
            <h3>Audit Information</h3>
            <div className="detail-grid">
              <div className="detail-row">
                <span className="detail-label">Created</span>
                <span className="detail-value">{formatDateTime(instrument.createdDate)}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Last Updated</span>
                <span className="detail-value">{formatDateTime(instrument.lastUpdated)}</span>
              </div>
            </div>
          </div>
        </div>

        <div className="modal-footer">
          <button
            type="button"
            className="button button-secondary"
            onClick={onClose}
          >
            Close
          </button>
          <button
            type="button"
            className="button button-primary"
            onClick={() => onEdit(instrument.instrumentId)}
          >
            Edit
          </button>
          <button
            type="button"
            className="button button-danger"
            onClick={() => onDelete()}
          >
            Delete
          </button>
        </div>
      </div>
    </div>
  )
}

export default InstrumentDetailModal
