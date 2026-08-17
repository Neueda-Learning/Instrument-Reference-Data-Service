import { formatDate, formatDateTime, pickIdentifier } from '../utils/instrumentFormatters'

function MetadataRow({ label, value, mono = false }) {
  return (
    <div className="metadata-row">
      <dt>{label}</dt>
      <dd className={mono ? 'mono' : ''}>{value || '-'}</dd>
    </div>
  )
}

function InstrumentMetadataPanel({ row }) {
  if (!row) {
    return (
      <section className="metadata-panel" aria-label="Instrument Metadata">
        <h2>Instrument Metadata</h2>
        <p className="metadata-empty">
          Select an instrument in the table to inspect its full reference data profile.
        </p>
      </section>
    )
  }

  const instrument = row.instrument
  const identifiers = Array.isArray(row.identifiers) ? row.identifiers : []
  const audits = Array.isArray(row.audits) ? row.audits : []

  return (
    <section className="metadata-panel" aria-label="Instrument Metadata">
      <div className="metadata-header">
        <div>
          <h2>Instrument Metadata</h2>
          <p className="metadata-subtitle">
            {instrument.name} ({instrument.instrumentId})
          </p>
        </div>
        <span className={`status-pill status-${String(instrument.status).toLowerCase()}`}>
          {instrument.status}
        </span>
      </div>

      <dl className="metadata-grid">
        <MetadataRow label="Instrument ID" value={instrument.instrumentId} mono />
        <MetadataRow label="Primary ISIN" value={instrument.primaryIsin || pickIdentifier(identifiers, 'ISIN')} mono />
        <MetadataRow label="Asset Class" value={`${instrument.assetClassName} (${instrument.assetClassId})`} />
        <MetadataRow label="Sector" value={`${instrument.sectorName} (${instrument.sectorId})`} />
        <MetadataRow label="Exchange" value={`${instrument.exchangeName} (${instrument.exchangeMicCode})`} />
        <MetadataRow label="Currency" value={`${instrument.currencyName} (${instrument.currencyId})`} />
        <MetadataRow label="Issuer" value={`${instrument.issuerName} (${instrument.issuerId})`} />
        <MetadataRow label="Effective Date" value={formatDate(instrument.effectiveDate)} />
        <MetadataRow label="Last Updated" value={formatDate(instrument.lastUpdated)} />
      </dl>

      <div className="metadata-block">
        <h3>Identifiers</h3>
        {identifiers.length === 0 ? (
          <p className="metadata-empty">No identifiers available.</p>
        ) : (
          <div className="metadata-table-wrap">
            <table className="metadata-table">
              <thead>
                <tr>
                  <th>Type</th>
                  <th>Value</th>
                  <th>Effective</th>
                  <th>Expiry</th>
                </tr>
              </thead>
              <tbody>
                {identifiers.map((identifier) => (
                  <tr key={identifier.identifierId}>
                    <td>{identifier.identifierTypeName}</td>
                    <td className="mono">{identifier.identifierValue}</td>
                    <td>{formatDate(identifier.effectiveDate)}</td>
                    <td>{formatDate(identifier.expiryDate)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="metadata-block">
        <h3>Audit Trail</h3>
        {audits.length === 0 ? (
          <p className="metadata-empty">No audit history available.</p>
        ) : (
          <div className="audit-list">
            {audits.slice(0, 12).map((audit) => (
              <article key={audit.auditId} className="audit-item">
                <p>
                  <strong>{audit.fieldName}</strong>: {audit.oldValue || '-'} {'->'} {audit.newValue || '-'}
                </p>
                <p>
                  {formatDateTime(audit.changedAt)} by {audit.changedBy} ({audit.changeSource})
                </p>
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
  )
}

export default InstrumentMetadataPanel
