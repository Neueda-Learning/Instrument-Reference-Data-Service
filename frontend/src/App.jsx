import { useCallback, useEffect, useState } from 'react'
import './App.css'
import InstrumentSearchForm from './components/InstrumentSearchForm'
import InstrumentsTable from './components/InstrumentsTable'
import InstrumentMetadataPanel from './components/InstrumentMetadataPanel'
import DataFreshnessView from './components/DataFreshnessView'
import PaginationControls from './components/PaginationControls'
import EditInstrumentForm from './components/EditInstrumentForm'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

function App() {
  const [currentPage, setCurrentPage] = useState('home')
  const [isin, setIsin] = useState('')
  const [cusip, setCusip] = useState('')
  const [appliedFilters, setAppliedFilters] = useState({ isin: '', cusip: '' })
  const [rows, setRows] = useState([])
  const [homeTotalCount, setHomeTotalCount] = useState(0)
  const [isLoadingHome, setIsLoadingHome] = useState(false)
  const [homeError, setHomeError] = useState('')
  const [sortBy, setSortBy] = useState('instrumentId')
  const [sortDirection, setSortDirection] = useState('asc')
  const [selectedInstrumentId, setSelectedInstrumentId] = useState('')
  const [isMetadataModalOpen, setIsMetadataModalOpen] = useState(false)
  const [selectedInstrumentDetail, setSelectedInstrumentDetail] = useState(null)
  const [isLoadingMetadata, setIsLoadingMetadata] = useState(false)
  const [metadataError, setMetadataError] = useState('')
  const [editInstrumentId, setEditInstrumentId] = useState('')
  const [editInstrumentDetail, setEditInstrumentDetail] = useState(null)
  const [editOptions, setEditOptions] = useState(null)
  const [isLoadingEdit, setIsLoadingEdit] = useState(false)
  const [isSavingEdit, setIsSavingEdit] = useState(false)
  const [editError, setEditError] = useState('')
  const [editSubmitError, setEditSubmitError] = useState('')
  const [editSuccess, setEditSuccess] = useState('')
  const [isEditSuccessPopupOpen, setIsEditSuccessPopupOpen] = useState(false)
  const [editSuccessPopupMessage, setEditSuccessPopupMessage] = useState('')
  const [homePage, setHomePage] = useState(1)
  const [homePageSize, setHomePageSize] = useState(15)
  const [monitorQuickFilter, setMonitorQuickFilter] = useState({
    type: 'all',
    staleAfterDays: 30,
    recentWithinDays: 7,
  })

  const loadHomeInstruments = useCallback(async () => {
    setIsLoadingHome(true)
    setHomeError('')

    try {
      const query = new URLSearchParams()
      if (appliedFilters.isin) {
        query.set('isin', appliedFilters.isin)
      }
      if (appliedFilters.cusip) {
        query.set('cusip', appliedFilters.cusip)
      }

      query.set('pageNumber', String(homePage))
      query.set('pageSize', String(homePageSize))
      query.set('sortBy', sortBy)
      query.set('sortDirection', sortDirection)

      if (monitorQuickFilter.type !== 'all') {
        query.set('freshnessFilter', monitorQuickFilter.type)
        query.set('staleAfterDays', String(monitorQuickFilter.staleAfterDays))
        query.set('recentWithinDays', String(monitorQuickFilter.recentWithinDays))
      }

      const response = await fetch(`${API_BASE_URL}/api/instruments/paged?${query.toString()}`)
      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`)
      }

      const data = await response.json()
      const items = Array.isArray(data.items) ? data.items : []
      setRows(items)
      setHomeTotalCount(Number.isFinite(data.totalCount) ? data.totalCount : 0)

      if (items.length === 0) {
        setSelectedInstrumentId('')
      }
    } catch {
      setRows([])
      setHomeTotalCount(0)
      setSelectedInstrumentId('')
      setHomeError('Unable to load instruments right now. Please verify the API is running.')
    } finally {
      setIsLoadingHome(false)
    }
  }, [appliedFilters, homePage, homePageSize, sortBy, sortDirection, monitorQuickFilter])

  useEffect(() => {
    loadHomeInstruments()
  }, [loadHomeInstruments])

  const handleSearch = async (event) => {
    event.preventDefault()
    const nextFilters = {
      isin: isin.trim().toUpperCase(),
      cusip: cusip.trim().toUpperCase(),
    }

    setAppliedFilters(nextFilters)
    setMonitorQuickFilter({
      type: 'all',
      staleAfterDays: 30,
      recentWithinDays: 7,
    })
    setHomePage(1)
  }

  const handleReset = async () => {
    setIsin('')
    setCusip('')
    setAppliedFilters({ isin: '', cusip: '' })
    setMonitorQuickFilter({
      type: 'all',
      staleAfterDays: 30,
      recentWithinDays: 7,
    })
    setHomePage(1)
  }

  const handleSort = (column) => {
    if (sortBy === column) {
      setSortDirection((value) => (value === 'asc' ? 'desc' : 'asc'))
      setHomePage(1)
      return
    }

    setSortBy(column)
    setSortDirection('asc')
    setHomePage(1)
  }

  const hasActiveFilters = Boolean(appliedFilters.isin || appliedFilters.cusip)

  useEffect(() => {
    if (!selectedInstrumentId && rows.length > 0) {
      setSelectedInstrumentId(rows[0].instrument.instrumentId)
      return
    }

    if (selectedInstrumentId && !rows.some((row) => row.instrument.instrumentId === selectedInstrumentId)) {
      setSelectedInstrumentId(rows[0]?.instrument.instrumentId ?? '')
    }
  }, [rows, selectedInstrumentId])

  useEffect(() => {
    const totalPages = Math.max(1, Math.ceil(homeTotalCount / homePageSize))
    if (homePage > totalPages) {
      setHomePage(totalPages)
    }
  }, [homeTotalCount, homePage, homePageSize])

  useEffect(() => {
    if (!isMetadataModalOpen) {
      return undefined
    }

    const handleEscape = (event) => {
      if (event.key === 'Escape') {
        setIsMetadataModalOpen(false)
      }
    }

    window.addEventListener('keydown', handleEscape)

    return () => {
      window.removeEventListener('keydown', handleEscape)
    }
  }, [isMetadataModalOpen])

  const handleOpenMetadataModal = async (instrumentId) => {
    setSelectedInstrumentId(instrumentId)
    setSelectedInstrumentDetail(null)
    setMetadataError('')
    setIsLoadingMetadata(true)
    setIsMetadataModalOpen(true)

    try {
      const response = await fetch(`${API_BASE_URL}/api/instruments/${instrumentId}`)
      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`)
      }

      const detail = await response.json()
      setSelectedInstrumentDetail(detail)
    } catch {
      setSelectedInstrumentDetail(null)
      setMetadataError('Unable to load full instrument metadata.')
    } finally {
      setIsLoadingMetadata(false)
    }
  }

  const handleCloseMetadataModal = () => {
    setIsMetadataModalOpen(false)
  }

  const loadEditContext = useCallback(async (instrumentId) => {
    setIsLoadingEdit(true)
    setEditError('')
    setEditSubmitError('')
    setEditSuccess('')

    try {
      const [detailResponse, optionsResponse] = await Promise.all([
        fetch(`${API_BASE_URL}/api/instruments/${instrumentId}`),
        fetch(`${API_BASE_URL}/api/instruments/options`),
      ])

      if (!detailResponse.ok || !optionsResponse.ok) {
        throw new Error('Unable to load edit data')
      }

      const [detailPayload, optionsPayload] = await Promise.all([
        detailResponse.json(),
        optionsResponse.json(),
      ])

      setEditInstrumentDetail(detailPayload)
      setEditOptions(optionsPayload)
      setEditInstrumentId(instrumentId)
    } catch {
      setEditInstrumentDetail(null)
      setEditOptions(null)
      setEditError('Unable to load editable instrument data and option lists.')
    } finally {
      setIsLoadingEdit(false)
    }
  }, [])

  const handleOpenEditPage = useCallback(async (instrumentId) => {
    setIsMetadataModalOpen(false)
    setCurrentPage('edit')
    await loadEditContext(instrumentId)
  }, [loadEditContext])

  const handleSaveEdit = async (payload) => {
    if (!editInstrumentId) {
      return
    }

    setEditError('')
    setEditSubmitError('')
    setEditSuccess('')
    setIsSavingEdit(true)

    try {
      const response = await fetch(`${API_BASE_URL}/api/instruments/${editInstrumentId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      })

      if (!response.ok) {
        throw new Error(`Update failed with status ${response.status}`)
      }

      await loadEditContext(editInstrumentId)
      await loadHomeInstruments()
      setEditSuccessPopupMessage('Instrument edit was successful.')
      setIsEditSuccessPopupOpen(true)
      setCurrentPage('home')
    } catch {
      setEditSubmitError('Unable to update instrument. Please review your selections and try again.')
    } finally {
      setIsSavingEdit(false)
    }
  }

  const handleCancelEdit = () => {
    setCurrentPage('home')
    setEditError('')
    setEditSubmitError('')
    setEditSuccess('')
  }

  const handleCloseEditSuccessPopup = () => {
    setIsEditSuccessPopupOpen(false)
    setEditSuccessPopupMessage('')
  }

  const handleDeleteInstrument = async () => {
    if (!selectedInstrumentDetail) return
    const { instrumentId, name } = selectedInstrumentDetail.instrument
    if (!window.confirm(`Are you sure you want to delete "${name}" (${instrumentId})? This action cannot be undone.`)) {
      return
    }

    try {
      const response = await fetch(`${API_BASE_URL}/api/instruments/${instrumentId}`, {
        method: 'DELETE',
      })
      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`)
      }
      setIsMetadataModalOpen(false)
      loadHomeInstruments()
    } catch {
      setMetadataError('Failed to delete the instrument. Please try again.')
    }
  }

  const handleApplyMonitorQuickFilter = (type, context = {}) => {
    if (type === 'all') {
      setMonitorQuickFilter({
        type: 'all',
        staleAfterDays: 30,
        recentWithinDays: 7,
      })
      setHomePage(1)
      setCurrentPage('home')
      return
    }

    setMonitorQuickFilter({
      type,
      staleAfterDays: context.staleAfterDays ?? 30,
      recentWithinDays: context.recentWithinDays ?? 7,
    })
    setHomePage(1)
    setCurrentPage('home')
  }

  const isQuickFilterActive = monitorQuickFilter.type !== 'all'
  const isHomePage = currentPage === 'home'
  const isMonitoringPage = currentPage === 'monitoring'
  const isEditPage = currentPage === 'edit'

  return (
    <div className="page-shell">
      <nav className="top-nav" aria-label="Primary">
        <div className="top-nav-links">
          <button
            type="button"
            className={`top-nav-link ${isHomePage ? 'active' : ''}`}
            onClick={() => setCurrentPage('home')}
          >
            Home
          </button>
          <button
            type="button"
            className={`top-nav-link ${isMonitoringPage ? 'active' : ''}`}
            onClick={() => setCurrentPage('monitoring')}
          >
            Monitoring
          </button>
          {isEditPage ? (
            <button type="button" className="top-nav-link active">
              Edit Instrument
            </button>
          ) : null}
        </div>

        <div className="top-nav-brand">
          <span className="top-nav-kicker">Instrument Reference Data Service</span>
          <strong>Reference Data Workbench</strong>
        </div>
      </nav>

      {isHomePage ? (
        <>
          <header className="hero-panel">
            <p className="eyebrow">Instrument Reference Data Service</p>
            <h1>Instrument Search & Reference Table</h1>
            <p className="hero-copy">
              Search financial instruments by identifier and review the complete reference
              dataset in one fast, analyst-focused view.
            </p>
          </header>

          <InstrumentSearchForm
            isin={isin}
            cusip={cusip}
            isLoading={isLoadingHome}
            hasActiveFilters={hasActiveFilters}
            lastQuery={appliedFilters}
            onIsinChange={setIsin}
            onCusipChange={setCusip}
            onSearch={handleSearch}
            onReset={handleReset}
          />

          <section className="table-panel" aria-label="Instrument Table">
            {isQuickFilterActive ? (
              <div className="quick-filter-banner">
                <span>
                  Main table filtered by freshness monitor: <strong>{monitorQuickFilter.type}</strong>
                </span>
                <button
                  type="button"
                  className="button button-secondary"
                  onClick={() => handleApplyMonitorQuickFilter('all')}
                >
                  Show All Instruments
                </button>
              </div>
            ) : null}

            {!homeError && isLoadingHome ? <p className="status-message">Loading instruments...</p> : null}

            {homeError ? <p className="status-message error">{homeError}</p> : null}

            {!homeError && !isLoadingHome && homeTotalCount === 0 ? (
              <p className="status-message">
                {isQuickFilterActive
                  ? `No instruments matched the \"${monitorQuickFilter.type}\" monitoring filter.`
                  : 'No instruments found for the selected criteria.'}
              </p>
            ) : null}

            {!homeError && homeTotalCount > 0 ? (
              <>
                <InstrumentsTable
                  rows={rows}
                  totalRowsCount={homeTotalCount}
                  sortBy={sortBy}
                  sortDirection={sortDirection}
                  selectedInstrumentId={selectedInstrumentId}
                  onSort={handleSort}
                  onSelectInstrument={setSelectedInstrumentId}
                  onOpenMetadata={handleOpenMetadataModal}
                />
                <PaginationControls
                  label="Home Table"
                  currentPage={homePage}
                  totalItems={homeTotalCount}
                  pageSize={homePageSize}
                  pageSizeOptions={[10, 15, 25, 50]}
                  onPageChange={setHomePage}
                  onPageSizeChange={(nextPageSize) => {
                    setHomePageSize(nextPageSize)
                    setHomePage(1)
                  }}
                />
              </>
            ) : null}
          </section>
        </>
      ) : null}

      {isMonitoringPage ? (
        <>
          <header className="hero-panel">
            <p className="eyebrow">Monitoring</p>
            <h1>Stale & Recently Changed Instruments</h1>
            <p className="hero-copy">
              Monitor update freshness, surface outlier date behavior, and quickly jump to
              affected records for deeper investigation.
            </p>
          </header>

          {!homeError ? (
            <DataFreshnessView
              filters={appliedFilters}
              onOpenMetadata={handleOpenMetadataModal}
              activeQuickFilter={monitorQuickFilter.type}
              onApplyQuickFilter={handleApplyMonitorQuickFilter}
            />
          ) : <p className="status-message error">Unable to load monitoring data because the API is unavailable.</p>}
        </>
      ) : null}

      {isEditPage ? (
        <>
          <header className="hero-panel">
            <p className="eyebrow">Edit Workflow</p>
            <h1>Edit Instrument</h1>
            <p className="hero-copy">
              Name can be edited directly. Asset class, sector, exchange, currency, issuer,
              and status can only be selected from backend-defined lists.
            </p>
          </header>

          {isLoadingEdit ? <p className="status-message">Loading edit form...</p> : null}
          {!isLoadingEdit && editError ? <p className="status-message error">{editError}</p> : null}
          {!isLoadingEdit && !editError && editInstrumentDetail && editOptions ? (
            <EditInstrumentForm
              detail={editInstrumentDetail}
              options={editOptions}
              isSaving={isSavingEdit}
              error={editSubmitError}
              success={editSuccess}
              onSubmit={handleSaveEdit}
              onCancel={handleCancelEdit}
            />
          ) : null}
        </>
      ) : null}

      {isMetadataModalOpen ? (
        <div
          className="metadata-modal-overlay"
          role="button"
          tabIndex={0}
          onClick={handleCloseMetadataModal}
          onKeyDown={(event) => {
            if (event.key === 'Enter' || event.key === ' ') {
              handleCloseMetadataModal()
            }
          }}
        >
          <div
            className="metadata-modal-content"
            role="dialog"
            aria-modal="true"
            aria-label="Instrument Full Metadata"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="metadata-modal-actions">
              <button
                type="button"
                className="metadata-modal-close"
                onClick={handleCloseMetadataModal}
                aria-label="Close metadata modal"
              >
                Close
              </button>
              {!isLoadingMetadata && !metadataError && selectedInstrumentDetail ? (
                <div className="metadata-action-group">
                  <button
                    type="button"
                    className="button button-secondary"
                    onClick={() => handleOpenEditPage(selectedInstrumentDetail.instrument.instrumentId)}
                  >
                    Edit Instrument
                  </button>
                  <button
                    type="button"
                    className="button button-danger"
                    onClick={handleDeleteInstrument}
                  >
                    Delete Instrument
                  </button>
                </div>
              ) : null}
            </div>
            {isLoadingMetadata ? <p className="status-message">Loading metadata...</p> : null}
            {!isLoadingMetadata && metadataError ? <p className="status-message error">{metadataError}</p> : null}
            {!isLoadingMetadata && !metadataError ? <InstrumentMetadataPanel row={selectedInstrumentDetail} /> : null}
          </div>
        </div>
      ) : null}

      {isEditSuccessPopupOpen ? (
        <div
          className="metadata-modal-overlay"
          role="button"
          tabIndex={0}
          onClick={handleCloseEditSuccessPopup}
          onKeyDown={(event) => {
            if (event.key === 'Enter' || event.key === ' ') {
              handleCloseEditSuccessPopup()
            }
          }}
        >
          <div
            className="metadata-modal-content"
            role="dialog"
            aria-modal="true"
            aria-label="Edit Success"
            onClick={(event) => event.stopPropagation()}
          >
            <section className="metadata-panel" aria-label="Edit Success Message">
              <div className="metadata-header">
                <h2>Edit Successful</h2>
              </div>
              <p className="status-message">{editSuccessPopupMessage || 'Instrument edit was successful.'}</p>
              <div className="actions" style={{ marginTop: '0.8rem' }}>
                <button
                  type="button"
                  className="button button-primary"
                  onClick={handleCloseEditSuccessPopup}
                >
                  OK
                </button>
              </div>
            </section>
          </div>
        </div>
      ) : null}
    </div>
  )
}

export default App
