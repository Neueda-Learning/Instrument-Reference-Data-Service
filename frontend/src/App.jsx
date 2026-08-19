import { useCallback, useEffect, useState } from 'react'
import './App.css'
import InstrumentSearchForm from './components/InstrumentSearchForm'
import AdvancedSearch from './components/AdvancedSearch'
import InstrumentsTable from './components/InstrumentsTable'
import InstrumentMetadataPanel from './components/InstrumentMetadataPanel'
import BulkOperationsPanel from './components/BulkOperationsPanel'
import DataFreshnessView from './components/DataFreshnessView'
import PaginationControls from './components/PaginationControls'
import EditInstrumentForm from './components/EditInstrumentForm'
import AddInstrumentForm from './components/AddInstrumentForm'
import ThemeToggle from './components/ThemeToggle'
import ChatWindow from './components/ChatWindow'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

function pickIdentifierValue(identifiers, typeId) {
  if (!Array.isArray(identifiers)) {
    return ''
  }

  const match = identifiers.find(
    (item) => item?.identifierTypeId?.toUpperCase() === typeId,
  )

  return String(match?.identifierValue ?? '')
}

function sortInstrumentRows(items, sortBy, sortDirection) {
  const direction = sortDirection === 'desc' ? -1 : 1

  return [...items].sort((left, right) => {
    const leftInstrument = left.instrument ?? {}
    const rightInstrument = right.instrument ?? {}

    let leftValue
    let rightValue

    if (sortBy === 'name') {
      leftValue = String(leftInstrument.name ?? '').toUpperCase()
      rightValue = String(rightInstrument.name ?? '').toUpperCase()
    } else if (sortBy === 'lastUpdated') {
      leftValue = String(leftInstrument.lastUpdated ?? '')
      rightValue = String(rightInstrument.lastUpdated ?? '')
    } else {
      leftValue = String(leftInstrument.instrumentId ?? '').toUpperCase()
      rightValue = String(rightInstrument.instrumentId ?? '').toUpperCase()
    }

    if (leftValue < rightValue) {
      return -1 * direction
    }

    if (leftValue > rightValue) {
      return 1 * direction
    }

    return 0
  })
}

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
  const [addOptions, setAddOptions] = useState(null)
  const [isLoadingAdd, setIsLoadingAdd] = useState(false)
  const [isSavingAdd, setIsSavingAdd] = useState(false)
  const [addSubmitError, setAddSubmitError] = useState('')
  const [isEditSuccessPopupOpen, setIsEditSuccessPopupOpen] = useState(false)
  const [editSuccessPopupMessage, setEditSuccessPopupMessage] = useState('')
  const [homePage, setHomePage] = useState(1)
  const [homePageSize, setHomePageSize] = useState(15)
  const [monitorQuickFilter, setMonitorQuickFilter] = useState({
    type: 'all',
    staleAfterDays: 30,
    recentWithinDays: 7,
  })

  // New state for advanced search and bulk operations
  const [isDarkMode, setIsDarkMode] = useState(() => {
    if (typeof localStorage !== 'undefined') {
      return localStorage.getItem('irds-dark-mode') === 'true'
    }
    return false
  })
  const [selectedBulkIds, setSelectedBulkIds] = useState([])
  const [useAdvancedSearch, setUseAdvancedSearch] = useState(false)
  const [advancedFilters, setAdvancedFilters] = useState(null)
  const [assetClasses, setAssetClasses] = useState([])
  const [sectors, setSectors] = useState([])
  const [exchanges, setExchanges] = useState([])

  const loadHomeInstruments = useCallback(async () => {
    setIsLoadingHome(true)
    setHomeError('')

    try {
      // Advanced search: fetch all, filter client-side
      if (advancedFilters) {
        const response = await fetch(`${API_BASE_URL}/api/instruments`)
        if (!response.ok) throw new Error(`Request failed with status ${response.status}`)
        const allItemsPayload = await response.json()
        const allItems = Array.isArray(allItemsPayload) ? allItemsPayload : []

        const q = advancedFilters.query?.toLowerCase() ?? ''
        const filtered = allItems.filter((item) => {
          const inst = item.instrument ?? {}
          if (q) {
            const haystack = [inst.name, inst.issuerName, inst.sectorName, inst.assetClassName]
              .map((v) => (v ?? '').toLowerCase())
              .join(' ')
            if (!haystack.includes(q)) return false
          }
          if (advancedFilters.assetClasses?.length && !advancedFilters.assetClasses.includes(inst.assetClassId)) return false
          if (advancedFilters.sectors?.length && !advancedFilters.sectors.includes(inst.sectorId)) return false
          if (advancedFilters.exchanges?.length && !advancedFilters.exchanges.includes(inst.exchangeId)) return false
          if (advancedFilters.statuses?.length && !advancedFilters.statuses.includes((inst.status ?? '').toLowerCase())) return false
          return true
        })

        const sortedItems = sortInstrumentRows(filtered, sortBy, sortDirection)
        const startIndex = (homePage - 1) * homePageSize
        const pagedItems = sortedItems.slice(startIndex, startIndex + homePageSize)
        setRows(pagedItems)
        setHomeTotalCount(sortedItems.length)
        if (pagedItems.length === 0) setSelectedInstrumentId('')
        return
      }

      const hasIdentifierSearch = Boolean(appliedFilters.isin || appliedFilters.cusip)

      if (hasIdentifierSearch) {
        const response = await fetch(`${API_BASE_URL}/api/instruments`)
        if (!response.ok) {
          throw new Error(`Request failed with status ${response.status}`)
        }

        const allItemsPayload = await response.json()
        const allItems = Array.isArray(allItemsPayload) ? allItemsPayload : []

        const filteredItems = allItems.filter((item) => {
          const isinValue = pickIdentifierValue(item.identifiers, 'ISIN').toUpperCase()
          const cusipValue = pickIdentifierValue(item.identifiers, 'CUSIP').toUpperCase()

          const isinMatches = !appliedFilters.isin || isinValue.includes(appliedFilters.isin)
          const cusipMatches = !appliedFilters.cusip || cusipValue.includes(appliedFilters.cusip)

          return isinMatches && cusipMatches
        })

        const sortedItems = sortInstrumentRows(filteredItems, sortBy, sortDirection)
        const startIndex = (homePage - 1) * homePageSize
        const pagedItems = sortedItems.slice(startIndex, startIndex + homePageSize)

        setRows(pagedItems)
        setHomeTotalCount(sortedItems.length)

        if (pagedItems.length === 0) {
          setSelectedInstrumentId('')
        }

        return
      }

      const query = new URLSearchParams()

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
  }, [advancedFilters, appliedFilters, homePage, homePageSize, sortBy, sortDirection, monitorQuickFilter])

  useEffect(() => {
    loadHomeInstruments()
  }, [loadHomeInstruments])

  useEffect(() => {
    setAppliedFilters({
      isin: isin.trim().toUpperCase(),
      cusip: cusip.trim().toUpperCase(),
    })
    setHomePage(1)
  }, [isin, cusip])

  const handleSearch = async (event) => {
    event.preventDefault()
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
    localStorage.setItem('irds-dark-mode', isDarkMode)
    if (isDarkMode) {
      document.documentElement.classList.add('dark-mode')
    } else {
      document.documentElement.classList.remove('dark-mode')
    }
  }, [isDarkMode])

  // Load edit options (assetClasses, sectors, exchanges)
  useEffect(() => {
    const loadEditOptions = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/instruments/options`)
        if (!response.ok) throw new Error('Failed to load options')
        const data = await response.json()
        setAssetClasses(data.assetClasses || [])
        setSectors(data.sectors || [])
        setExchanges(data.exchanges || [])
      } catch (error) {
        console.error('Failed to load edit options:', error)
      }
    }
    loadEditOptions()
  }, [])

  // Scroll to top on page change
  useEffect(() => {
    window.scrollTo(0, 0)
  }, [currentPage])

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

  // Bulk operations handlers
  const handleToggleBulkSelect = useCallback(
    (instrumentId) => {
      setSelectedBulkIds((prev) =>
        prev.includes(instrumentId)
          ? prev.filter((id) => id !== instrumentId)
          : [...prev, instrumentId]
      )
    },
    []
  )

  const handleSelectAllBulk = useCallback(
    (shouldSelectAll) => {
      if (shouldSelectAll) {
        setSelectedBulkIds(rows.map((row) => row.instrument.instrumentId))
      } else {
        setSelectedBulkIds([])
      }
    },
    [rows]
  )

  const handleClearBulkSelection = useCallback(() => {
    setSelectedBulkIds([])
  }, [])

  const handleBulkDelete = useCallback(async () => {
    if (!selectedBulkIds.length) return
    if (!window.confirm(`Delete ${selectedBulkIds.length} instruments? This cannot be undone.`)) return

    try {
      for (const id of selectedBulkIds) {
        await fetch(`${API_BASE_URL}/api/instruments/${id}`, { method: 'DELETE' })
      }
      setSelectedBulkIds([])
      await loadHomeInstruments()
    } catch (error) {
      console.error('Bulk delete failed:', error)
      alert('Some deletions failed. Please try again.')
    }
  }, [selectedBulkIds, loadHomeInstruments])

  const handleAdvancedSearch = useCallback((filters) => {
    setAdvancedFilters(filters)
    setHomePage(1)
  }, [])

  const toggleTheme = useCallback(() => {
    setIsDarkMode((prev) => !prev)
  }, [])

  const handleOpenAddPage = useCallback(async () => {
    setAddSubmitError('')
    setIsLoadingAdd(true)
    setCurrentPage('add')

    try {
      const response = await fetch(`${API_BASE_URL}/api/instruments/options`)
      if (!response.ok) throw new Error('Failed to load options')
      const data = await response.json()
      setAddOptions(data)
    } catch {
      setAddOptions(null)
    } finally {
      setIsLoadingAdd(false)
    }
  }, [])

  const handleSaveAdd = async (payload) => {
    setAddSubmitError('')
    setIsSavingAdd(true)

    try {
      const response = await fetch(`${API_BASE_URL}/api/instruments`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })

      if (response.status === 409) {
        const message = await response.text()
        setAddSubmitError(message || 'An instrument with this ID or ISIN already exists.')
        return
      }

      if (!response.ok) {
        const message = await response.text()
        setAddSubmitError(message || `Create failed with status ${response.status}.`)
        return
      }

      await loadHomeInstruments()
      setEditSuccessPopupMessage(`Instrument "${payload.name}" (${payload.instrumentId}) was created successfully.`)
      setIsEditSuccessPopupOpen(true)
      setCurrentPage('home')
    } catch {
      setAddSubmitError('Unable to create instrument. Please verify your input and try again.')
    } finally {
      setIsSavingAdd(false)
    }
  }

  const handleCancelAdd = () => {
    setCurrentPage('home')
    setAddSubmitError('')
  }

  const isQuickFilterActive = monitorQuickFilter.type !== 'all'
  const isHomePage = currentPage === 'home'
  const isMonitoringPage = currentPage === 'monitoring'
  const isEditPage = currentPage === 'edit'
  const isAddPage = currentPage === 'add'

  return (
    <div className="app-shell">
      <nav className="top-nav" aria-label="Primary">
        <div className="nav-inner">
          <div className="nav-left">
            <div className="nav-brand" aria-label="IRDS Home">
              <svg width="28" height="28" viewBox="0 0 28 28" fill="none" aria-hidden="true">
                <rect width="28" height="28" rx="7" fill="#2563eb"/>
                <path d="M7 18.5L11 13.5L14.5 16.5L18.5 10.5L21.5 13.5" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="7" cy="18.5" r="1.75" fill="white"/>
                <circle cx="11" cy="13.5" r="1.75" fill="white"/>
                <circle cx="14.5" cy="16.5" r="1.75" fill="white"/>
                <circle cx="18.5" cy="10.5" r="1.75" fill="white"/>
                <circle cx="21.5" cy="13.5" r="1.75" fill="white"/>
              </svg>
              <div className="nav-brand-text">
                <span className="nav-brand-name">IRDS</span>
                <span className="nav-brand-sub">Reference Data Workbench</span>
              </div>
            </div>

            <div className="nav-divider" aria-hidden="true" />

            <div className="top-nav-links">
              <button
                type="button"
                className={`top-nav-link ${isHomePage ? 'active' : ''}`}
                onClick={() => setCurrentPage('home')}
              >
                Instruments
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
              {isAddPage ? (
                <button type="button" className="top-nav-link active">
                  Add Instrument
                </button>
              ) : null}
            </div>
          </div>

          <div className="nav-right">
            <ThemeToggle isDarkMode={isDarkMode} onToggle={toggleTheme} />
            <div className="api-status-badge" aria-label="API connected">
              <span className="api-status-dot" aria-hidden="true" />
              API Connected
            </div>
          </div>
        </div>
      </nav>

      <main className="page-main">
        <div className="page-container">

      {isHomePage ? (
        <>
          <header className="page-header">
            <p className="page-breadcrumb">
              <span>IRDS</span>
              <span className="page-breadcrumb-sep" aria-hidden="true">›</span>
              <span className="page-breadcrumb-current">Instruments</span>
            </p>
            <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: '1rem', flexWrap: 'wrap' }}>
              <div>
                <h1 className="page-title">Instrument Search &amp; Reference Table</h1>
                <p className="page-description">
                  Search financial instruments by identifier and review the complete reference
                  dataset in one fast, analyst-focused view.
                </p>
              </div>
              <button
                type="button"
                className="button button-primary"
                style={{ marginTop: '0.25rem', whiteSpace: 'nowrap' }}
                onClick={handleOpenAddPage}
              >
                + Add Instrument
              </button>
            </div>
          </header>

          {!useAdvancedSearch && (
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
          )}

          {useAdvancedSearch && (
            <AdvancedSearch
              isLoading={isLoadingHome}
              assetClasses={assetClasses}
              sectors={sectors}
              exchanges={exchanges}
              onSearch={handleAdvancedSearch}
              onReset={() => {
                setAdvancedFilters(null)
                handleReset()
                setUseAdvancedSearch(false)
              }}
            />
          )}

          <div className="search-mode-toggle" style={{ marginBottom: '1rem' }}>
            <button
              type="button"
              className="button button-primary"
              onClick={() => setUseAdvancedSearch(!useAdvancedSearch)}
            >
              {useAdvancedSearch ? '← Back to Simple Search' : 'Advanced Search →'}
            </button>
          </div>

          <BulkOperationsPanel
            selectedIds={selectedBulkIds}
            onSelectAll={handleSelectAllBulk}
            onClearSelection={handleClearBulkSelection}
            onBulkDelete={handleBulkDelete}
            onBulkEdit={() => alert('Bulk edit coming soon')}
            isLoading={isLoadingHome}
            totalRows={homeTotalCount}
          />

          <section className="table-panel" aria-label="Instrument Table">
            {isQuickFilterActive ? (
              <div className="quick-filter-banner">
                <span>
                  Main table filtered by: <strong>{monitorQuickFilter.type}</strong>
                </span>
                <button
                  type="button"
                  className="button button-secondary button-sm"
                  onClick={() => handleApplyMonitorQuickFilter('all')}
                >
                  Show All
                </button>
              </div>
            ) : null}

            {!homeError && homeTotalCount > 0 ? (
              <div className="table-panel-header">
                <span className="table-panel-title">Instruments</span>
                <div className="table-panel-meta">
                  <span className="count-badge">{homeTotalCount.toLocaleString()} records</span>
                  <span className="sort-badge">Sorted by {sortBy} · {sortDirection === 'asc' ? '↑' : '↓'}</span>
                </div>
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
                  selectedIds={selectedBulkIds}
                  onToggleSelect={handleToggleBulkSelect}
                  onSelectAllRows={handleSelectAllBulk}
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
          <header className="page-header">
            <p className="page-breadcrumb">
              <span>IRDS</span>
              <span className="page-breadcrumb-sep" aria-hidden="true">›</span>
              <span className="page-breadcrumb-current">Monitoring</span>
            </p>
            <h1 className="page-title">Stale &amp; Recently Changed Instruments</h1>
            <p className="page-description">
              Monitor update freshness, surface outlier date behaviour, and quickly jump to
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

      {isAddPage ? (
        <>
          <header className="page-header">
            <p className="page-breadcrumb">
              <span>IRDS</span>
              <span className="page-breadcrumb-sep" aria-hidden="true">›</span>
              <span>Instruments</span>
              <span className="page-breadcrumb-sep" aria-hidden="true">›</span>
              <span className="page-breadcrumb-current">Add Instrument</span>
            </p>
            <h1 className="page-title">Add Instrument</h1>
            <p className="page-description">
              Fill in the details below to create a new instrument. The Instrument ID is
              auto-generated following the <code>INS-YYMMDDHHMMSS-NNNN</code> format; you
              may edit it if needed. The Primary ISIN must be a valid 12-character code.
            </p>
          </header>

          {isLoadingAdd ? <p className="status-message">Loading form…</p> : null}
          {!isLoadingAdd && addOptions ? (
            <AddInstrumentForm
              options={addOptions}
              isSaving={isSavingAdd}
              error={addSubmitError}
              onSubmit={handleSaveAdd}
              onCancel={handleCancelAdd}
            />
          ) : null}
          {!isLoadingAdd && !addOptions ? (
            <p className="status-message error">Unable to load form options. Please try again.</p>
          ) : null}
        </>
      ) : null}

      {isEditPage ? (
        <>
          <header className="page-header">
            <p className="page-breadcrumb">
              <span>IRDS</span>
              <span className="page-breadcrumb-sep" aria-hidden="true">›</span>
              <span>Instruments</span>
              <span className="page-breadcrumb-sep" aria-hidden="true">›</span>
              <span className="page-breadcrumb-current">Edit Instrument</span>
            </p>
            <h1 className="page-title">Edit Instrument</h1>
            <p className="page-description">
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

        </div>
      </main>

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
                aria-label="Close metadata panel"
              >
                <svg width="14" height="14" viewBox="0 0 14 14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
                  <path d="M2 2l10 10M12 2L2 12"/>
                </svg>
              </button>
              {!isLoadingMetadata && !metadataError && selectedInstrumentDetail ? (
                <div className="metadata-action-group">
                  <button
                    type="button"
                    className="button button-secondary button-sm"
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
                <div className="metadata-header-left">
                  <h2 className="metadata-title">Edit Successful</h2>
                </div>
              </div>
              <div className="metadata-body">
                <p className="status-message" style={{ margin: 0 }}>{editSuccessPopupMessage || 'Instrument edit was successful.'}</p>
                <div className="actions" style={{ marginTop: '0.75rem' }}>
                  <button
                    type="button"
                    className="button button-primary"
                    onClick={handleCloseEditSuccessPopup}
                  >
                    Done
                  </button>
                </div>
              </div>
            </section>
          </div>
        </div>
      ) : null}

      <ChatWindow />
    </div>
  )
}

export default App
