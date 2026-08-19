import './TrendReportModal.css'

function TrendReportModal({ isOpen, trends, isLoading, onClose }) {
  if (!isOpen) return null

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        className="modal-content trend-modal"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header">
          <h2>Instruments by Sector & Asset Class</h2>
          <button className="modal-close-btn" onClick={onClose} aria-label="Close modal">
            ✕
          </button>
        </div>

        {isLoading && <p className="modal-loading">Loading trend data...</p>}

        {!isLoading && trends && (
          <div className="modal-body trend-body">
            <div className="trends-grid">
              {trends.map((trend) => (
                <div key={trend.id} className="trend-card">
                  <div className="trend-header">
                    <h4>{trend.name}</h4>
                    <span className="trend-type">{trend.type}</span>
                  </div>
                  <div className="trend-value">{trend.count}</div>
                  <p className="trend-subtitle">instruments</p>
                  {trend.changePercent && (
                    <p
                      className={`trend-change ${
                        trend.changePercent > 0 ? 'positive' : 'negative'
                      }`}
                    >
                      {trend.changePercent > 0 ? '↑' : '↓'} {Math.abs(trend.changePercent)}%
                      last 30 days
                    </p>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="modal-footer">
          <button type="button" className="button button-primary" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  )
}

export default TrendReportModal

