import { useState, useCallback } from 'react'
import './BulkOperationsPanel.css'

function BulkOperationsPanel({
  selectedIds,
  onSelectAll,
  onClearSelection,
  onBulkDelete,
  onBulkEdit,
  isLoading,
  totalRows,
}) {
  const [showConfirmDialog, setShowConfirmDialog] = useState(false)
  const [operationType, setOperationType] = useState(null)

  const handleBulkAction = useCallback(
    (action) => {
      setOperationType(action)
      setShowConfirmDialog(true)
    },
    []
  )

  const handleConfirmAction = useCallback(() => {
    if (operationType === 'delete') {
      onBulkDelete()
    } else if (operationType === 'edit') {
      onBulkEdit()
    }
    setShowConfirmDialog(false)
    setOperationType(null)
  }, [operationType, onBulkDelete, onBulkEdit])

  if (selectedIds.length === 0) return null

  return (
    <>
      <div className="bulk-operations-banner">
        <div className="bulk-info">
          <span className="bulk-count">
            {selectedIds.length} instrument{selectedIds.length !== 1 ? 's' : ''} selected
          </span>
          {totalRows > selectedIds.length && (
            <button
              type="button"
              className="link-button"
              onClick={onSelectAll}
              disabled={isLoading}
            >
              Select all {totalRows}
            </button>
          )}
        </div>

        <div className="bulk-actions">
          <button
            type="button"
            className="button button-secondary button-sm"
            onClick={onClearSelection}
            disabled={isLoading}
          >
            Deselect
          </button>
          <button
            type="button"
            className="button button-primary button-sm"
            onClick={() => handleBulkAction('edit')}
            disabled={isLoading || selectedIds.length > 500}
            title={selectedIds.length > 500 ? 'Maximum 500 records per operation' : ''}
          >
            Edit Selected
          </button>
          <button
            type="button"
            className="button button-danger button-sm"
            onClick={() => handleBulkAction('delete')}
            disabled={isLoading || selectedIds.length > 500}
            title={selectedIds.length > 500 ? 'Maximum 500 records per operation' : ''}
          >
            Delete Selected
          </button>
        </div>
      </div>

      {showConfirmDialog && (
        <div className="confirm-dialog-overlay" onClick={() => setShowConfirmDialog(false)}>
          <div
            className="confirm-dialog"
            onClick={(e) => e.stopPropagation()}
          >
            <h3>Confirm {operationType === 'delete' ? 'Deletion' : 'Edit'}</h3>
            <p>
              {operationType === 'delete'
                ? `Are you sure you want to delete ${selectedIds.length} instrument${
                    selectedIds.length !== 1 ? 's' : ''
                  }? This action cannot be undone.`
                : `You are about to edit ${selectedIds.length} instrument${
                    selectedIds.length !== 1 ? 's' : ''
                  }. This will open a bulk edit form.`}
            </p>
            <div className="confirm-actions">
              <button
                type="button"
                className="button button-secondary"
                onClick={() => setShowConfirmDialog(false)}
                disabled={isLoading}
              >
                Cancel
              </button>
              <button
                type="button"
                className={`button ${operationType === 'delete' ? 'button-danger' : 'button-primary'}`}
                onClick={handleConfirmAction}
                disabled={isLoading}
              >
                {operationType === 'delete' ? 'Delete' : 'Edit'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}

export default BulkOperationsPanel
