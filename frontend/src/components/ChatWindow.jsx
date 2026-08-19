import { useEffect, useRef, useState } from 'react'
import Markdown from 'react-markdown'
import './ChatWindow.css'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

export default function ChatWindow() {
  const [isOpen, setIsOpen] = useState(false)
  const [isMinimized, setIsMinimized] = useState(false)
  const [messages, setMessages] = useState([])
  const [input, setInput] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [error, setError] = useState('')
  const latestAssistantRef = useRef(null)
  const messagesContainerRef = useRef(null)

  // After a new assistant message lands, scroll so its top is visible
  useEffect(() => {
    if (!isOpen) return
    if (latestAssistantRef.current && messagesContainerRef.current) {
      const container = messagesContainerRef.current
      const el = latestAssistantRef.current
      container.scrollTop = el.offsetTop - container.offsetTop
    }
  }, [messages, isOpen])

  async function handleSend() {
    const text = input.trim()
    if (!text || isSending) return

    const nextMessages = [...messages, { role: 'user', content: text }]
    setMessages(nextMessages)
    setInput('')
    setIsSending(true)
    setError('')

    try {
      const response = await fetch(`${API_BASE_URL}/api/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messages: nextMessages }),
      })

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`)
      }

      const data = await response.json()
      setMessages((prev) => [...prev, { role: 'assistant', content: data.answer, isLatest: true }])
    } catch {
      setError('Failed to get a response. Please try again.')
    } finally {
      setIsSending(false)
    }
  }

  function handleKeyDown(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      handleSend()
    }
  }

  return (
    <div className="chat-widget" aria-label="AI Assistant">
      {isOpen && !isMinimized ? (
        <div className="chat-panel" role="dialog" aria-label="AI Financial Assistant">
          <div className="chat-header">
            <div className="chat-header-info">
              <span className="chat-header-dot" aria-hidden="true" />
              <span className="chat-header-title">AI Assistant</span>
            </div>
            <div className="chat-header-buttons">
              <button
                type="button"
                className="chat-minimize-btn"
                onClick={() => setIsMinimized(true)}
                aria-label="Minimize chat"
              >
                <svg width="12" height="12" viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
                  <path d="M2 6h8" />
                </svg>
              </button>
              <button
                type="button"
                className="chat-close-btn"
                onClick={() => {
                  setIsOpen(false)
                  setIsMinimized(false)
                  setMessages([])
                }}
                aria-label="Close chat and clear history"
              >
                <svg width="12" height="12" viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
                  <path d="M2 2l8 8M10 2L2 10" />
                </svg>
              </button>
            </div>
          </div>

          <div className="chat-messages" ref={messagesContainerRef} aria-live="polite" aria-label="Conversation">
            {messages.length === 0 ? (
              <p className="chat-empty">Ask me anything about financial instruments and reference data.</p>
            ) : null}

            {messages.map((msg, index) => {
              const isLatestAssistant = msg.role === 'assistant' && index === messages.length - 1
              return (
                <div
                  key={index}
                  ref={isLatestAssistant ? latestAssistantRef : null}
                  className={`chat-bubble-row ${msg.role === 'user' ? 'chat-bubble-row--user' : 'chat-bubble-row--assistant'}`}
                >
                  <div className={`chat-bubble ${msg.role === 'user' ? 'chat-bubble--user' : 'chat-bubble--assistant'}`}>
                    {msg.role === 'assistant'
                      ? <div className="chat-markdown"><Markdown>{msg.content}</Markdown></div>
                      : msg.content
                    }
                  </div>
                </div>
              )
            })}

            {isSending ? (
              <div className="chat-bubble-row chat-bubble-row--assistant">
                <div className="chat-bubble chat-bubble--assistant chat-bubble--typing" aria-label="Thinking">
                  <span /><span /><span />
                </div>
              </div>
            ) : null}

            {error ? <p className="chat-error">{error}</p> : null}
          </div>

          <div className="chat-input-row">
            <textarea
              className="chat-input"
              rows={1}
              placeholder="Ask about instruments, ISINs, asset classes…"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              disabled={isSending}
              aria-label="Chat message input"
            />
            <button
              type="button"
              className="chat-send-btn"
              onClick={handleSend}
              disabled={isSending || !input.trim()}
              aria-label="Send message"
            >
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M14 8H2M9 3l5 5-5 5" />
              </svg>
            </button>
          </div>
        </div>
      ) : null}

      <button
        type="button"
        className="chat-toggle-btn"
        onClick={() => {
          if (isMinimized) {
            setIsMinimized(false)
          } else {
            setIsOpen((prev) => !prev)
          }
        }}
        aria-label={isOpen && !isMinimized ? 'Hide AI Assistant' : 'Open AI Assistant'}
        aria-expanded={isOpen && !isMinimized}
      >
        {isOpen && !isMinimized ? (
          <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
            <path d="M4 4l12 12M16 4L4 16" />
          </svg>
        ) : (
          <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path d="M2 4a2 2 0 012-2h12a2 2 0 012 2v8a2 2 0 01-2 2H7l-4 3V4z" />
          </svg>
        )}
      </button>
    </div>
  )
}
