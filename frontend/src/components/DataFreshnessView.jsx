import { useEffect, useState } from 'react'
import { formatDate } from '../utils/instrumentFormatters'
import PaginationControls from './PaginationControls'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

function FreshnessTable({ title, rows, emptyMessage, onOpenMetadata }) {
  return (
    <article className="freshness-card">
      <h3>{title}</h3>
      {rows.length === 0 ? (
        <p className="metadata-empty">{emptyMessage}</p>
      ) : (
        <div className="freshness-table-wrap">
          <table className="freshness-table">
            <thead>
              <tr>
                <th>Instrument</th>
                <th>Name</th>
                <th>Last Updated</th>
                <th>Age (days)</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                return (
                  <tr key={row.instrumentId}>
                    <td className="mono">{row.instrumentId}</td>
                    <td>{row.name}</td>
                    <td>{formatDate(row.lastUpdated)}</td>
                    <td>{row.ageDays}</td>
                    <td>
                      <button
                        type="button"
                        className="view-meta-button"
                        onClick={() => onOpenMetadata(row.instrumentId)}
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
      )}
    </article>
  )
}

function DataFreshnessView({ filters, onOpenMetadata, activeQuickFilter, onApplyQuickFilter }) {
  const [staleAfterDays, setStaleAfterDays] = useState(30)
  const [recentWithinDays, setRecentWithinDays] = useState(7)
  const [monitorPageSize, setMonitorPageSize] = useState(8)
  const [stalePage, setStalePage] = useState(1)
  const [recentPage, setRecentPage] = useState(1)
  const [anomalyPage, setAnomalyPage] = useState(1)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')
  const [monitoringData, setMonitoringData] = useState({
    freshnessScore: 100,
    stale: { items: [], totalCount: 0, pageNumber: 1, pageSize: 8 },
    recent: { items: [], totalCount: 0, pageNumber: 1, pageSize: 8 },
    anomalies: { items: [], totalCount: 0, pageNumber: 1, pageSize: 8 },
  })

  useEffect(() => {
    setStalePage(1)
    setRecentPage(1)
    setAnomalyPage(1)
  }, [staleAfterDays, recentWithinDays, monitorPageSize, filters])

  useEffect(() => {
    const loadMonitoringData = async () => {
      setIsLoading(true)
      setError('')

      try {
        const query = new URLSearchParams()
        query.set('staleAfterDays', String(staleAfterDays))
        query.set('recentWithinDays', String(recentWithinDays))
        query.set('pageSize', String(monitorPageSize))
        query.set('stalePageNumber', String(stalePage))
        query.set('recentPageNumber', String(recentPage))
        query.set('anomalyPageNumber', String(anomalyPage))

        if (filters?.isin) {
          query.set('isin', filters.isin)
        }
        if (filters?.cusip) {
          query.set('cusip', filters.cusip)
        }
        if (filters?.name) {
          query.set('name', filters.name)
        }

        const response = await fetch(`${API_BASE_URL}/api/instruments/monitoring?${query.toString()}`)
        if (!response.ok) {
          throw new Error(`Request failed with status ${response.status}`)
        }

        const data = await response.json()
        setMonitoringData({
          freshnessScore: Number.isFinite(data.freshnessScore) ? data.freshnessScore : 100,
          stale: data.stale ?? { items: [], totalCount: 0, pageNumber: 1, pageSize: monitorPageSize },
          recent: data.recent ?? { items: [], totalCount: 0, pageNumber: 1, pageSize: monitorPageSize },
          anomalies: data.anomalies ?? { items: [], totalCount: 0, pageNumber: 1, pageSize: monitorPageSize },
        })
      } catch {
        setError('Unable to load monitoring data right now.')
        setMonitoringData({
          freshnessScore: 100,
          stale: { items: [], totalCount: 0, pageNumber: 1, pageSize: monitorPageSize },
          recent: { items: [], totalCount: 0, pageNumber: 1, pageSize: monitorPageSize },
          anomalies: { items: [], totalCount: 0, pageNumber: 1, pageSize: monitorPageSize },
        })
      } finally {
        setIsLoading(false)
      }
    }

    loadMonitoringData()
  }, [staleAfterDays, recentWithinDays, monitorPageSize, stalePage, recentPage, anomalyPage, filters])

  return (
    <section className="freshness-panel" aria-label="Stale and Recently Changed Instruments">
      <div className="freshness-toolbar">
        <div className="freshness-controls">
          <label>
            Rows per section
            <input
              type="number"
              min="4"
              max="50"
              value={monitorPageSize}
              onChange={(event) => setMonitorPageSize(Math.min(50, Math.max(4, Number(event.target.value) || 8)))}
            />
          </label>

          <label>
            Stale after (days)
            <input
              type="number"
              min="1"
              value={staleAfterDays}
              onChange={(event) => setStaleAfterDays(Number(event.target.value) || 1)}
            />
          </label>

          <label>
            Recent within (days)
            <input
              type="number"
              min="1"
              value={recentWithinDays}
              onChange={(event) => setRecentWithinDays(Number(event.target.value) || 1)}
            />
          </label>
        </div>

        <div className="freshness-quick-filters">
          <button
            type="button"
            className={`button ${activeQuickFilter === 'stale' ? 'button-primary' : 'button-secondary'}`}
            onClick={() => onApplyQuickFilter('stale', { staleAfterDays, recentWithinDays })}
          >
            Stale Only to Main Table
          </button>

          <button
            type="button"
            className={`button ${activeQuickFilter === 'recent' ? 'button-primary' : 'button-secondary'}`}
            onClick={() => onApplyQuickFilter('recent', { staleAfterDays, recentWithinDays })}
          >
            Recent Only to Main Table
          </button>

          <button
            type="button"
            className={`button ${activeQuickFilter === 'all' ? 'button-primary' : 'button-secondary'}`}
            onClick={() => onApplyQuickFilter('all')}
          >
            Reset Table Filter
          </button>
        </div>
      </div>

      <div className="freshness-overview-cards">
        <div className="freshness-stat freshness-stat-score">
          <span>Freshness Score</span>
          <strong>{monitoringData.freshnessScore}%</strong>
          <small>Higher is better — based on stale vs total</small>
        </div>
        <div className="freshness-stat">
          <span>Stale</span>
          <strong>{monitoringData.stale.totalCount}</strong>
        </div>
        <div className="freshness-stat freshness-stat-recent">
          <span>Recently Changed</span>
          <strong>{monitoringData.recent.totalCount}</strong>
        </div>
        <div className="freshness-stat freshness-stat-anomaly">
          <span>Anomalies</span>
          <strong>{monitoringData.anomalies.totalCount}</strong>
        </div>
      </div>

      {isLoading ? <p className="status-message">Loading monitoring data...</p> : null}
      {error ? <p className="status-message error">{error}</p> : null}

      <div className="freshness-grid">
        <div className="freshness-section">
          <FreshnessTable
            title={`Stale Instruments (> ${staleAfterDays} days)`}
            rows={monitoringData.stale.items}
            emptyMessage="No stale instruments for the selected threshold."
            onOpenMetadata={onOpenMetadata}
          />
          <PaginationControls
            label="Stale Instruments"
            currentPage={stalePage}
            totalItems={monitoringData.stale.totalCount}
            pageSize={monitorPageSize}
            pageSizeOptions={[4, 8, 12, 20]}
            onPageChange={setStalePage}
            onPageSizeChange={(value) => {
              setMonitorPageSize(value)
              setStalePage(1)
              setRecentPage(1)
              setAnomalyPage(1)
            }}
          />
        </div>

        <div className="freshness-section">
          <FreshnessTable
            title={`Recently Changed Instruments (<= ${recentWithinDays} days)`}
            rows={monitoringData.recent.items}
            emptyMessage="No recently changed instruments for the selected threshold."
            onOpenMetadata={onOpenMetadata}
          />
          <PaginationControls
            label="Recently Changed"
            currentPage={recentPage}
            totalItems={monitoringData.recent.totalCount}
            pageSize={monitorPageSize}
            pageSizeOptions={[4, 8, 12, 20]}
            onPageChange={setRecentPage}
            onPageSizeChange={(value) => {
              setMonitorPageSize(value)
              setStalePage(1)
              setRecentPage(1)
              setAnomalyPage(1)
            }}
          />
        </div>
      </div>

      <article className="freshness-card">
        <h3>Anomalies</h3>
        {monitoringData.anomalies.totalCount === 0 ? (
          <p className="metadata-empty">No anomalies detected in Last Updated values.</p>
        ) : (
          <ul className="anomaly-list">
            {monitoringData.anomalies.items.map((item) => (
              <li key={item.instrumentId}>
                <span className="mono">{item.instrumentId}</span>
                <span>{item.name}</span>
                <span>{item.reason}</span>
                <span>{formatDate(item.lastUpdated)}</span>
                <button
                    type="button"
                    className="view-meta-button"
                    onClick={() => onOpenMetadata(item.instrumentId)}
                >
                  View Details
                  <svg width="10" height="10" viewBox="0 0 10 10" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                    <path d="M2 5h6M5 2l3 3-3 3"/>
                  </svg>
                </button>
              </li>
            ))}
          </ul>
        )}

        <PaginationControls
          label="Anomalies"
          currentPage={anomalyPage}
          totalItems={monitoringData.anomalies.totalCount}
          pageSize={monitorPageSize}
          pageSizeOptions={[4, 8, 12, 20]}
          onPageChange={setAnomalyPage}
          onPageSizeChange={(value) => {
            setMonitorPageSize(value)
            setStalePage(1)
            setRecentPage(1)
            setAnomalyPage(1)
          }}
        />
      </article>
    </section>
  )
}

export default DataFreshnessView
