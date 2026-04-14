import { useState } from 'react'

export default function FileUpload({ onFileProcessed, disabled }) {
  const [file, setFile] = useState(null)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState('')
  const [dragActive, setDragActive] = useState(false)

  const handleDrag = (e) => {
    e.preventDefault()
    e.stopPropagation()
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true)
    } else if (e.type === "dragleave") {
      setDragActive(false)
    }
  }

  const handleDrop = (e) => {
    e.preventDefault()
    e.stopPropagation()
    setDragActive(false)
    
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFile(e.dataTransfer.files[0])
    }
  }

  const handleChange = (e) => {
    e.preventDefault()
    if (e.target.files && e.target.files[0]) {
      handleFile(e.target.files[0])
    }
  }

  const handleFile = async (file) => {
    setError('')
    
    if (file.type !== 'application/pdf') {
      setError('Please upload a PDF file')
      return
    }

    if (file.size > 10 * 1024 * 1024) {
      setError('File size must be less than 10MB')
      return
    }

    setFile(file)
    setUploading(true)

    try {
      const formData = new FormData()
      formData.append('file', file)

      const token = localStorage.getItem('token')
      const response = await fetch('/api/uploads/parse-lab-report', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        },
        body: formData
      })

      if (!response.ok) {
        throw new Error('Failed to process file')
      }

      const data = await response.json()
      onFileProcessed(data)
    } catch (err) {
      setError(err.message || 'Failed to process file. Please enter values manually.')
    } finally {
      setUploading(false)
    }
  }

  const clearFile = () => {
    setFile(null)
    setError('')
  }

  return (
    <div style={{ marginBottom: '32px' }}>
      <div style={{
        padding: '24px',
        background: '#F9FAFB',
        border: '2px dashed var(--border)',
        borderRadius: '8px',
        textAlign: 'center',
        transition: 'all 0.2s ease',
        ...(dragActive && {
          borderColor: 'var(--primary)',
          background: 'var(--primary-light)'
        })
      }}
      onDragEnter={handleDrag}
      onDragLeave={handleDrag}
      onDragOver={handleDrag}
      onDrop={handleDrop}
      >
        {!file ? (
          <>
            <div style={{ fontSize: '48px', marginBottom: '16px' }}>📄</div>
            <h3 style={{ fontSize: '18px', fontWeight: 600, marginBottom: '8px', color: 'var(--text-primary)' }}>
              Upload Lab Report (Optional)
            </h3>
            <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '16px' }}>
              Drag and drop your PDF lab report here, or click to browse
            </p>
            <input
              type="file"
              accept=".pdf"
              onChange={handleChange}
              disabled={disabled || uploading}
              style={{ display: 'none' }}
              id="file-upload"
            />
            <label htmlFor="file-upload">
              <button
                type="button"
                onClick={() => document.getElementById('file-upload').click()}
                disabled={disabled || uploading}
                style={{
                  padding: '10px 20px',
                  background: 'white',
                  border: '1.5px solid var(--border)',
                  borderRadius: '6px',
                  color: 'var(--text-primary)',
                  fontWeight: 500,
                  cursor: disabled ? 'not-allowed' : 'pointer',
                  fontSize: '14px'
                }}
              >
                Choose File
              </button>
            </label>
            <p style={{ fontSize: '12px', color: 'var(--text-muted)', marginTop: '12px' }}>
              Supported format: PDF (max 10MB)
            </p>
          </>
        ) : uploading ? (
          <>
            <div style={{ fontSize: '48px', marginBottom: '16px' }}>⏳</div>
            <h3 style={{ fontSize: '18px', fontWeight: 600, marginBottom: '8px', color: 'var(--text-primary)' }}>
              Processing {file.name}...
            </h3>
            <p style={{ fontSize: '14px', color: 'var(--text-secondary)' }}>
              Extracting lab values from your report
            </p>
          </>
        ) : (
          <>
            <div style={{ fontSize: '48px', marginBottom: '16px' }}>✅</div>
            <h3 style={{ fontSize: '18px', fontWeight: 600, marginBottom: '8px', color: 'var(--text-primary)' }}>
              {file.name}
            </h3>
            <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '16px' }}>
              File processed. Review extracted values below.
            </p>
            <button
              type="button"
              onClick={clearFile}
              style={{
                padding: '8px 16px',
                background: 'white',
                border: '1.5px solid var(--border)',
                borderRadius: '6px',
                color: 'var(--text-secondary)',
                fontWeight: 500,
                cursor: 'pointer',
                fontSize: '13px'
              }}
            >
              Upload Different File
            </button>
          </>
        )}
      </div>

      {error && (
        <div className="error" style={{ marginTop: '16px' }}>
          {error}
        </div>
      )}

      <div style={{
        marginTop: '12px',
        padding: '12px',
        background: '#FEF3C7',
        border: '1px solid #FDE68A',
        borderRadius: '6px',
        fontSize: '13px',
        color: '#92400E'
      }}>
        <strong>⚠️ Important:</strong> Automated extraction may not be 100% accurate. 
        Always review and verify all values before submitting.
      </div>
    </div>
  )
}