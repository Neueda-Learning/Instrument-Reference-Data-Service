import { useState, useEffect, useRef, useCallback } from 'react'
import './SearchAutocomplete.css'

function SearchAutocomplete({
  value,
  placeholder,
  onSelect,
  isLoading,
  suggestions,
  onSearchChange,
}) {
  const [isOpen, setIsOpen] = useState(false)
  const [highlightedIndex, setHighlightedIndex] = useState(-1)
  const inputRef = useRef(null)
  const containerRef = useRef(null)

  const handleInputChange = useCallback(
    (e) => {
      const query = e.target.value
      onSearchChange(query)
      setHighlightedIndex(-1)
    },
    [onSearchChange]
  )

  const handleSelectSuggestion = useCallback(
    (suggestion) => {
      onSelect(suggestion)
      setIsOpen(false)
      setHighlightedIndex(-1)
    },
    [onSelect]
  )

  const handleKeyDown = useCallback(
    (e) => {
      if (!isOpen) {
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
          setIsOpen(true)
        }
        return
      }

      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault()
          setHighlightedIndex((prev) =>
            prev < suggestions.length - 1 ? prev + 1 : prev
          )
          break
        case 'ArrowUp':
          e.preventDefault()
          setHighlightedIndex((prev) => (prev > 0 ? prev - 1 : -1))
          break
        case 'Enter':
          e.preventDefault()
          if (highlightedIndex >= 0) {
            handleSelectSuggestion(suggestions[highlightedIndex])
          }
          break
        case 'Escape':
          e.preventDefault()
          setIsOpen(false)
          break
        default:
          break
      }
    },
    [isOpen, suggestions, highlightedIndex, handleSelectSuggestion]
  )

  const handleFocus = () => {
    if (suggestions.length > 0) {
      setIsOpen(true)
    }
  }

  const handleBlur = () => {
    setTimeout(() => setIsOpen(false), 100)
  }

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (containerRef.current && !containerRef.current.contains(event.target)) {
        setIsOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  return (
    <div className="autocomplete-container" ref={containerRef}>
      <div className="form-input-wrap autocomplete-input-wrap">
        <input
          ref={inputRef}
          type="text"
          value={value}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          onFocus={handleFocus}
          onBlur={handleBlur}
          placeholder={placeholder}
          autoComplete="off"
          disabled={isLoading}
          className="autocomplete-input"
        />
        {isLoading && <span className="autocomplete-spinner" aria-hidden="true">⟳</span>}
      </div>

      {isOpen && suggestions.length > 0 && (
        <ul className="autocomplete-dropdown" role="listbox">
          {suggestions.map((suggestion, index) => (
            <li
              key={suggestion.id || index}
              className={`autocomplete-option ${index === highlightedIndex ? 'highlighted' : ''}`}
              onClick={() => handleSelectSuggestion(suggestion)}
              role="option"
              aria-selected={index === highlightedIndex}
            >
              <span className="option-primary">{suggestion.name || suggestion.label}</span>
              {suggestion.secondary && (
                <span className="option-secondary">{suggestion.secondary}</span>
              )}
            </li>
          ))}
        </ul>
      )}

      {isOpen && suggestions.length === 0 && value.trim().length > 0 && (
        <div className="autocomplete-empty">
          <p>No matching results</p>
        </div>
      )}
    </div>
  )
}

export default SearchAutocomplete
