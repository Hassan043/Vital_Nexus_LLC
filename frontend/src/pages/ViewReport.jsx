import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { api } from '../services/api'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, ReferenceLine, ResponsiveContainer, Legend
} from 'recharts'

const STATUS_COLORS = {
  low: '#dc3545',
  high: '#dc3545',
  inrange: '#28a745',
  unknown: '#6c757d'
}

export default function ViewReport() {
  const { reportId } = useParams()
  const [report, setReport] = useState(null)
  const [loading, setLoading] = useState(true)
  const [exporting, setExporting] = useState(false)
  const [allMarkerNames, setAllMarkerNames] = useState([])
  const [selectedMarkers, setSelectedMarkers] = useState([])
  const [trendData, setTrendData] = useState({})
  const [loadingTrends, setLoadingTrends] = useState(false)
  const navigate = useNavigate()

  useEffect(() => {
    loadReport()
    loadMarkerNames()
  }, [reportId])

  useEffect(() => {
    if (selectedMarkers.length > 0) fetchTrends()
  }, [selectedMarkers])

  const loadReport = async () => {
    try {
      const data = await api.getReport(reportId)
      setReport(data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const loadMarkerNames = async () => {
    try {
      const names = await api.getAllMarkerNames()
      setAllMarkerNames(names)
    } catch (err) {
      console.error(err)
    }
  }

  const fetchTrends = async () => {
    setLoadingTrends(true)
    try {
      const results = {}
      await Promise.all(
        selectedMarkers.map(async (name) => {
          const data = await api.getMarkerTrends(name)
          results[name] = data
        })
      )
      setTrendData(results)
    } catch (err) {
      console.error(err)
    } finally {
      setLoadingTrends(false)
    }
  }

  const toggleMarker = (name) => {
    setSelectedMarkers(prev =>
      prev.includes(name) ? prev.filter(m => m !== name) : [...prev, name]
    )
  }

  const buildChartData = (markerName) => {
    const points = trendData[markerName] || []
    return points.map(p => ({
      date: new Date(p.reportDate).toLocaleDateString(),
      value: parseFloat(p.value),
      status: p.status,
      referenceLow: p.referenceLow,
      referenceHigh: p.referenceHigh,
      unit: p.unit
    }))
  }

  const getLineColor = (markerName) => {
    const points = trendData[markerName] || []
    if (points.length === 0) return '#6c757d'
    const latest = points[points.length - 1]
    return STATUS_COLORS[latest.status.toLowerCase()] || '#6c757d'
  }

  const handleExportExcel = async () => {
    setExporting(true)
    try { await api.exportExcel(reportId) }
    catch (err) { alert('Export failed: ' + err.message) }
    finally { setExporting(false) }
  }

  const handleExportPdf = async () => {
    setExporting(true)
    try { await api.exportPdf(reportId) }
    catch (err) { alert('Export failed: ' + err.message) }
    finally { setExporting(false) }
  }

  if (loading) return <div className="container">Loading...</div>
  if (!report) return <div className="container">Report not found</div>

  return (
    <>
      <nav>
        <div className="container">
          <h1>VitalNexus</h1>
          <button onClick={() => navigate('/dashboard')} style={{ backgroundColor: '#6c757d' }}>
            Back to Dashboard
          </button>
        </div>
      </nav>

      <div className="container">
        <div className="card">
          {/* Report Header */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '20px' }}>
            <div>
              <h2>Lab Report</h2>
              <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginTop: '8px' }}>
                <strong>Report ID:</strong>{' '}
                <code style={{ fontSize: '13px', background: 'var(--background)', padding: '4px 8px', borderRadius: '4px', fontFamily: 'monospace' }}>
                  {report.reportPublicId}
                </code>
              </p>
              <p style={{ fontSize: '14px', marginTop: '4px' }}>
                <strong>Date:</strong> {new Date(report.reportDate).toLocaleDateString()}
              </p>
              {report.petProfile && (
                <p style={{ fontSize: '14px', marginTop: '4px' }}>
                  <strong>Pet:</strong> {report.petProfile.name} ({report.petProfile.species})
                </p>
              )}
            </div>
            <div style={{ display: 'flex', gap: '8px' }}>
              <button onClick={handleExportExcel} disabled={exporting} style={{ backgroundColor: '#28a745' }}>
                📊 Export Excel
              </button>
              <button onClick={handleExportPdf} disabled={exporting} style={{ backgroundColor: '#dc3545' }}>
                📄 Export PDF
              </button>
            </div>
          </div>

          {/* Disclaimer */}
          <div className="disclaimer">
            <strong>⚠️ EDUCATIONAL DISCLAIMER</strong>
            <p style={{ marginTop: '8px' }}>
              This report is for EDUCATIONAL PURPOSES ONLY. It is NOT medical advice, diagnosis, or treatment.
              Always consult your healthcare provider before making health decisions.
            </p>
          </div>

          {/* Lab Results Table */}
          <h3 style={{ marginTop: '32px', marginBottom: '16px' }}>Your Lab Results</h3>
          <table>
            <thead>
              <tr>
                <th>Marker</th>
                <th>Your Value</th>
                <th>Reference Range</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {report.labMarkers.map(marker => (
                <tr key={marker.id}>
                  <td><strong>{marker.markerName}</strong></td>
                  <td>{marker.value} {marker.unit}</td>
                  <td>
                    {marker.referenceLow && marker.referenceHigh
                      ? `${marker.referenceLow} - ${marker.referenceHigh} ${marker.unit}`
                      : 'N/A'}
                  </td>
                  <td className={`status-${marker.status.toLowerCase()}`}>{marker.status}</td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Focus Areas */}
          <div style={{ marginTop: '32px', padding: '20px', background: '#e7f3ff', borderRadius: '4px' }}>
            <h3>🎯 Top Focus Areas</h3>
            <ul style={{ marginTop: '12px' }}>
              {report.labMarkers
                .filter(m => m.status === 'Low' || m.status === 'High')
                .map(m => (
                  <li key={m.id} style={{ marginBottom: '8px' }}>
                    <strong>{m.markerName}</strong> is <strong className={`status-${m.status.toLowerCase()}`}>{m.status}</strong>
                  </li>
                ))}
              {report.labMarkers.every(m => m.status === 'InRange' || m.status === 'Unknown') && (
                <li>All markers in range! Focus on general wellness habits.</li>
              )}
            </ul>
          </div>

          {/* Next Steps */}
          <div style={{ marginTop: '24px', padding: '20px', background: '#fff3cd', borderRadius: '4px' }}>
            <h4>💡 Next Steps</h4>
            <ol style={{ marginTop: '12px' }}>
              <li>Export your PDF report for detailed explanations and food recommendations</li>
              <li>Export the Excel file for tracking templates</li>
              <li>Share this information with your healthcare provider</li>
              <li>Ask questions at your next appointment</li>
            </ol>
          </div>

          {/* Trend Analysis */}
          <div style={{ marginTop: '40px' }}>
            <h3>📈 Trend Analysis</h3>
            <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginTop: '4px', marginBottom: '20px' }}>
              Select markers below to compare your values across all reports over time.
            </p>

            {/* Marker Selector */}
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginBottom: '24px' }}>
              {allMarkerNames.map(name => (
                <button
                  key={name}
                  onClick={() => toggleMarker(name)}
                  style={{
                    padding: '6px 12px',
                    fontSize: '13px',
                    borderRadius: '20px',
                    border: '2px solid',
                    borderColor: selectedMarkers.includes(name) ? '#007bff' : '#dee2e6',
                    backgroundColor: selectedMarkers.includes(name) ? '#007bff' : 'white',
                    color: selectedMarkers.includes(name) ? 'white' : '#495057',
                    cursor: 'pointer',
                    transition: 'all 0.15s ease'
                  }}
                >
                  {name}
                </button>
              ))}
            </div>

            {/* Charts */}
            {loadingTrends && (
              <p style={{ color: 'var(--text-secondary)' }}>Loading trend data...</p>
            )}

            {!loadingTrends && selectedMarkers.length === 0 && (
              <div style={{ padding: '40px', textAlign: 'center', background: 'var(--background)', borderRadius: '8px', color: 'var(--text-secondary)' }}>
                Select one or more markers above to view trends
              </div>
            )}

            {!loadingTrends && selectedMarkers.map(markerName => {
              const chartData = buildChartData(markerName)
              const color = getLineColor(markerName)
              const firstPoint = chartData[0]

              return (
                <div key={markerName} style={{ marginBottom: '32px', padding: '20px', border: '1px solid #dee2e6', borderRadius: '8px' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
                    <h4 style={{ margin: 0 }}>{markerName}</h4>
                    {firstPoint && (
                      <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
                        Unit: {firstPoint.unit || 'N/A'}
                      </span>
                    )}
                  </div>

                  {chartData.length < 2 ? (
                    <div style={{ padding: '20px', textAlign: 'center', background: 'var(--background)', borderRadius: '4px', color: 'var(--text-secondary)', fontSize: '14px' }}>
                      Only 1 report found for this marker — upload more reports to see trends
                    </div>
                  ) : (
                    <ResponsiveContainer width="100%" height={250}>
                      <LineChart data={chartData} margin={{ top: 10, right: 20, left: 0, bottom: 0 }}>
                        <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                        <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                        <YAxis tick={{ fontSize: 12 }} />
                        <Tooltip
                          formatter={(value, name) => [`${value} ${firstPoint?.unit || ''}`, markerName]}
                          labelFormatter={(label) => `Date: ${label}`}
                        />
                        {firstPoint?.referenceLow && (
                          <ReferenceLine y={firstPoint.referenceLow} stroke="#ffc107" strokeDasharray="4 4" label={{ value: 'Low', fontSize: 11 }} />
                        )}
                        {firstPoint?.referenceHigh && (
                          <ReferenceLine y={firstPoint.referenceHigh} stroke="#ffc107" strokeDasharray="4 4" label={{ value: 'High', fontSize: 11 }} />
                        )}
                        <Line
                          type="monotone"
                          dataKey="value"
                          stroke={color}
                          strokeWidth={2}
                          dot={{ r: 5, fill: color }}
                          activeDot={{ r: 7 }}
                        />
                      </LineChart>
                    </ResponsiveContainer>
                  )}
                </div>
              )
            })}
          </div>
        </div>
      </div>
    </>
  )
}