import { useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../services/api'
import markersData from '../data/markers.json'
import FileUpload from '../components/FileUpload'

export default function CreateReport() {
  const [reportType, setReportType] = useState('human')
  const [reportDate, setReportDate] = useState(new Date().toISOString().split('T')[0])
  const [markerValues, setMarkerValues] = useState({})
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [searchTerm, setSearchTerm] = useState('')
  const [expandedCategories, setExpandedCategories] = useState({})
  const [uploadProcessed, setUploadProcessed] = useState(false)
  const navigate = useNavigate()

  const [petName, setPetName] = useState('')
  const [petSpecies, setPetSpecies] = useState('Dog')
  const [petBreed, setPetBreed] = useState('')
  const [petAge, setPetAge] = useState('')
  const [petWeight, setPetWeight] = useState('')

  const allMarkers = useMemo(() => {
    if (Array.isArray(markersData)) {
      return markersData
    } else if (markersData.markers && Array.isArray(markersData.markers)) {
      return markersData.markers
    }
    return []
  }, [])

  const normalizeSearchTerm = (term) => {
    return term.toLowerCase().trim()
  }

  const markerMatchesSearch = (marker, term) => {
    const normalizedTerm = normalizeSearchTerm(term)
    if (!normalizedTerm) return true

    const displayName = (marker.displayName || marker.MarkerName || marker.name || '').toLowerCase()
    const key = (marker.key || '').toLowerCase()
    const category = (marker.category || '').toLowerCase()

    if (displayName.includes(normalizedTerm)) return true
    if (key.includes(normalizedTerm)) return true

    if (normalizedTerm === 'a1c' && displayName.includes('hemoglobin a1c')) return true
    if (normalizedTerm === 'cbc' && category === 'cbc') return true

    return false
  }

  const categoryMatchesSearch = (categoryName, term) => {
    const normalizedTerm = normalizeSearchTerm(term)
    if (!normalizedTerm) return false
    return categoryName.toLowerCase().includes(normalizedTerm)
  }

  const filteredAndGroupedMarkers = useMemo(() => {
    const term = searchTerm.trim()
    const groups = {}

    allMarkers.forEach(marker => {
      const category = marker.category || 'Other'
      if (!groups[category]) {
        groups[category] = {
          categoryMatch: categoryMatchesSearch(category, term),
          markers: []
        }
      }

      if (!term || markerMatchesSearch(marker, term) || groups[category].categoryMatch) {
        groups[category].markers.push(marker)
      }
    })

    const result = {}
    Object.keys(groups).forEach(category => {
      if (groups[category].markers.length > 0) {
        result[category] = groups[category]
      }
    })

    return result
  }, [allMarkers, searchTerm])

  const categoryOrder = [
    'CBC',
    'Metabolic',
    'Lipids',
    'Thyroid',
    'Vitamins & Minerals',
    'Iron',
    'Liver',
    'Inflammation',
    'Other'
  ]

  const sortedCategories = useMemo(() => {
    const categories = Object.keys(filteredAndGroupedMarkers)
    return categories.sort((a, b) => {
      const indexA = categoryOrder.indexOf(a)
      const indexB = categoryOrder.indexOf(b)
      if (indexA === -1 && indexB === -1) return a.localeCompare(b)
      if (indexA === -1) return 1
      if (indexB === -1) return -1
      return indexA - indexB
    })
  }, [filteredAndGroupedMarkers])

  const toggleCategory = (category) => {
    setExpandedCategories(prev => ({
      ...prev,
      [category]: !prev[category]
    }))
  }

  const handleMarkerChange = (markerKey, field, value) => {
    setMarkerValues(prev => ({
      ...prev,
      [markerKey]: {
        ...prev[markerKey],
        [field]: value
      }
    }))
  }

  const handleFileProcessed = (extractedData) => {
    setUploadProcessed(true)
    
    if (extractedData.reportDate) {
      setReportDate(extractedData.reportDate)
    }

    if (extractedData.markers && Array.isArray(extractedData.markers)) {
      const newMarkerValues = {}
      
      extractedData.markers.forEach(extracted => {
        const marker = allMarkers.find(m => 
          m.key === extracted.key || 
          m.displayName === extracted.markerName ||
          (m.displayName || '').toLowerCase() === (extracted.markerName || '').toLowerCase()
        )
        
        if (marker) {
          const markerKey = marker.key || marker.displayName
          newMarkerValues[markerKey] = {
            value: extracted.value?.toString() || '',
            referenceLow: extracted.referenceLow?.toString() || '',
            referenceHigh: extracted.referenceHigh?.toString() || ''
          }
        } else {
          // Handle markers not in JSON (e.g., for AI generation testing)
          console.log(`Marker not found in JSON: ${extracted.markerName}, adding anyway`)
          newMarkerValues[extracted.markerName] = {
            value: extracted.value?.toString() || '',
            referenceLow: extracted.referenceLow?.toString() || '',
            referenceHigh: extracted.referenceHigh?.toString() || ''
          }
        }
      })
      
      setMarkerValues(newMarkerValues)
      
      const categoriesWithData = new Set()
      Object.keys(newMarkerValues).forEach(markerKey => {
        const marker = allMarkers.find(m => m.key === markerKey || m.displayName === markerKey)
        if (marker && marker.category) {
          categoriesWithData.add(marker.category)
        }
      })
      
      const newExpanded = {}
      categoriesWithData.forEach(cat => {
        newExpanded[cat] = true
      })
      setExpandedCategories(newExpanded)
    }
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      if (reportType === 'pet' && !petName.trim()) {
        setError('Please enter your pet\'s name')
        setLoading(false)
        return
      }

      let petProfileId = null
      if (reportType === 'pet') {
        const petData = {
          name: petName,
          species: petSpecies,
          breed: petBreed || null,
          age: petAge ? parseInt(petAge) : null,
          weight: petWeight ? parseFloat(petWeight) : null
        }

        const petResponse = await fetch('/api/pets', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${api.getToken()}`
          },
          body: JSON.stringify(petData)
        })

        if (!petResponse.ok) {
          throw new Error('Failed to create pet profile')
        }

        const petResult = await petResponse.json()
        petProfileId = petResult.id
      }

      const markersList = Object.entries(markerValues)
        .filter(([_, data]) => data.value)
        .map(([key, data]) => {
          const marker = allMarkers.find(m => m.key === key || m.displayName === key)
          const displayName = marker?.displayName || marker?.MarkerName || marker?.name || key
          return {
            markerName: displayName,
            value: parseFloat(data.value),
            unit: marker?.defaultUnit || marker?.unit || '',
            referenceLow: data.referenceLow ? parseFloat(data.referenceLow) : null,
            referenceHigh: data.referenceHigh ? parseFloat(data.referenceHigh) : null,
            testDate: reportDate
          }
        })

      if (markersList.length === 0) {
        setError('Please enter at least one marker value')
        setLoading(false)
        return
      }

      const report = await api.createLabReport({
        petProfileId: petProfileId,
        reportDate,
        markers: markersList
      })

      navigate(`/report/${report.id}`)
    } catch (err) {
      setError(err.message)
      setLoading(false)
    }
  }

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
          <h2>Create New Lab Report</h2>
          
          <div className="disclaimer">
            <strong>⚠️ Important</strong>
            <p style={{ marginTop: '8px' }}>
              Enter lab values exactly as shown on the lab report. 
              This tool provides educational information only - always consult your healthcare provider or veterinarian.
            </p>
          </div>

          <form onSubmit={handleSubmit}>
            <div style={{ marginBottom: '32px' }}>
              <h3 style={{ marginBottom: '16px' }}>Who is this report for?</h3>
              <div style={{ display: 'flex', gap: '16px' }}>
                <button
                  type="button"
                  onClick={() => setReportType('human')}
                  className={`report-type-btn ${reportType === 'human' ? 'active' : ''}`}
                >
                  <span style={{ fontSize: '32px' }}>👤</span>
                  <span>Myself (Human)</span>
                </button>
                <button
                  type="button"
                  onClick={() => setReportType('pet')}
                  className={`report-type-btn ${reportType === 'pet' ? 'active' : ''}`}
                >
                  <span style={{ fontSize: '32px' }}>🐾</span>
                  <span>My Pet</span>
                </button>
              </div>
            </div>

            {reportType === 'pet' && (
              <div className="pet-info-section">
                <h3 style={{ marginBottom: '16px' }}>Pet Information</h3>
                
                <div className="form-group">
                  <label>Pet Name *</label>
                  <input
                    type="text"
                    value={petName}
                    onChange={(e) => setPetName(e.target.value)}
                    placeholder="e.g., Max, Luna, Buddy"
                    required={reportType === 'pet'}
                  />
                </div>

                <div className="form-group">
                  <label>Species *</label>
                  <select
                    value={petSpecies}
                    onChange={(e) => setPetSpecies(e.target.value)}
                    required={reportType === 'pet'}
                  >
                    <option value="Dog">Dog</option>
                    <option value="Cat">Cat</option>
                  </select>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                  <div className="form-group">
                    <label>Breed (Optional)</label>
                    <input
                      type="text"
                      value={petBreed}
                      onChange={(e) => setPetBreed(e.target.value)}
                      placeholder="e.g., Labrador, Siamese"
                    />
                  </div>

                  <div className="form-group">
                    <label>Age (Optional)</label>
                    <input
                      type="number"
                      value={petAge}
                      onChange={(e) => setPetAge(e.target.value)}
                      placeholder="Age in years"
                      min="0"
                      max="30"
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label>Weight (Optional)</label>
                  <input
                    type="number"
                    step="0.1"
                    value={petWeight}
                    onChange={(e) => setPetWeight(e.target.value)}
                    placeholder="Weight in lbs"
                    min="0"
                  />
                </div>
              </div>
            )}

            <div className="form-group">
              <label>Lab Report Date *</label>
              <input
                type="date"
                value={reportDate}
                onChange={(e) => setReportDate(e.target.value)}
                required
              />
            </div>

            <FileUpload 
              onFileProcessed={handleFileProcessed}
              disabled={loading}
            />

            <h3 style={{ marginTop: '32px', marginBottom: '8px' }}>
              {uploadProcessed ? 'Review & Edit Extracted Values' : 'Lab Values'}
            </h3>
            <p style={{ marginBottom: '16px', color: 'var(--text-secondary)', fontSize: '14px' }}>
              {uploadProcessed 
                ? 'Verify the extracted values below. You can edit or add missing values manually.'
                : 'Enter the values from your lab report. Leave blank any markers you don\'t have. Use the lab-provided reference ranges when available.'}
            </p>

            <div className="marker-search">
              <input
                type="text"
                placeholder="Search markers (e.g., Vitamin D, LDL, TSH, CBC)"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="search-input"
              />
            </div>

            <div className="marker-categories">
              {sortedCategories.map(category => {
                const categoryData = filteredAndGroupedMarkers[category]
                const isExpanded = expandedCategories[category] || searchTerm.trim() !== ''
                const markers = categoryData.markers
                
                return (
                  <div key={category} className="marker-category">
                    <button
                      type="button"
                      className="category-header"
                      onClick={() => toggleCategory(category)}
                    >
                      <span className={`chevron ${isExpanded ? 'expanded' : ''}`}>▶</span>
                      <span className="category-name">{category}</span>
                      <span className="category-count">({markers.length})</span>
                    </button>
                    
                    {isExpanded && (
                      <div className="marker-grid">
                        <div className="marker-grid-header">
                          <div className="col-marker">Marker</div>
                          <div className="col-unit">Unit</div>
                          <div className="col-value">Value</div>
                          <div className="col-ref">Ref Low</div>
                          <div className="col-ref">Ref High</div>
                        </div>
                        
                        {markers.map(marker => {
                          const markerKey = marker.key || marker.displayName
                          const displayName = marker.displayName || marker.MarkerName || marker.name
                          const unit = marker.defaultUnit || marker.unit || ''
                          const hasValue = markerValues[markerKey]?.value
                          
                          return (
                            <div 
                              key={markerKey} 
                              className="marker-grid-row"
                              style={hasValue ? { background: '#F0FDF4' } : {}}
                            >
                              <div className="col-marker">
                                <strong>{displayName}</strong>
                                {hasValue && <span style={{ marginLeft: '8px', fontSize: '12px' }}>✓</span>}
                              </div>
                              <div className="col-unit">
                                <span className="unit-badge">{unit}</span>
                              </div>
                              <div className="col-value">
                                <input
                                  type="number"
                                  step="0.01"
                                  placeholder="—"
                                  value={markerValues[markerKey]?.value || ''}
                                  onChange={(e) => handleMarkerChange(markerKey, 'value', e.target.value)}
                                />
                              </div>
                              <div className="col-ref">
                                <input
                                  type="number"
                                  step="0.01"
                                  placeholder="—"
                                  value={markerValues[markerKey]?.referenceLow || ''}
                                  onChange={(e) => handleMarkerChange(markerKey, 'referenceLow', e.target.value)}
                                />
                              </div>
                              <div className="col-ref">
                                <input
                                  type="number"
                                  step="0.01"
                                  placeholder="—"
                                  value={markerValues[markerKey]?.referenceHigh || ''}
                                  onChange={(e) => handleMarkerChange(markerKey, 'referenceHigh', e.target.value)}
                                />
                              </div>
                            </div>
                          )
                        })}
                      </div>
                    )}
                  </div>
                )
              })}
            </div>

            {error && <div className="error">{error}</div>}

            <button type="submit" disabled={loading} style={{ width: '100%', marginTop: '24px' }}>
              {loading ? 'Creating Report...' : 'Create Report'}
            </button>
          </form>
        </div>
      </div>
    </>
  )
}