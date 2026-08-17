function PaginationControls({
  label,
  currentPage,
  totalItems,
  pageSize,
  pageSizeOptions,
  onPageChange,
  onPageSizeChange,
}) {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize))
  const isFirstPage = currentPage <= 1
  const isLastPage = currentPage >= totalPages

  const maxVisibleButtons = 5
  const halfWindow = Math.floor(maxVisibleButtons / 2)
  const rangeStart = Math.max(1, Math.min(currentPage - halfWindow, totalPages - maxVisibleButtons + 1))
  const rangeEnd = Math.min(totalPages, rangeStart + maxVisibleButtons - 1)

  const visiblePages = []
  for (let page = rangeStart; page <= rangeEnd; page += 1) {
    visiblePages.push(page)
  }

  const startIndex = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1
  const endIndex = Math.min(currentPage * pageSize, totalItems)

  return (
    <div className="pagination-bar" aria-label={`${label} Pagination`}>
      <div className="pagination-meta">
        <span>{label}</span>
        <span>
          Showing {startIndex}-{endIndex} of {totalItems}
        </span>
      </div>

      <div className="pagination-actions">
        <label className="pagination-size-label">
          Rows
          <select
            value={pageSize}
            onChange={(event) => onPageSizeChange(Number(event.target.value))}
          >
            {pageSizeOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>

        <button
          type="button"
          className="button button-secondary"
          disabled={isFirstPage}
          onClick={() => onPageChange(currentPage - 1)}
        >
          Previous
        </button>

        {rangeStart > 1 ? (
          <>
            <button
              type="button"
              className="pagination-number-button"
              onClick={() => onPageChange(1)}
            >
              1
            </button>
            {rangeStart > 2 ? <span className="pagination-ellipsis">...</span> : null}
          </>
        ) : null}

        {visiblePages.map((page) => (
          <button
            key={page}
            type="button"
            className={`pagination-number-button ${page === currentPage ? 'active' : ''}`}
            onClick={() => onPageChange(page)}
          >
            {page}
          </button>
        ))}

        {rangeEnd < totalPages ? (
          <>
            {rangeEnd < totalPages - 1 ? <span className="pagination-ellipsis">...</span> : null}
            <button
              type="button"
              className="pagination-number-button"
              onClick={() => onPageChange(totalPages)}
            >
              {totalPages}
            </button>
          </>
        ) : null}

        <span className="pagination-page">Page {currentPage} / {totalPages}</span>

        <button
          type="button"
          className="button button-secondary"
          disabled={isLastPage}
          onClick={() => onPageChange(currentPage + 1)}
        >
          Next
        </button>
      </div>
    </div>
  )
}

export default PaginationControls
