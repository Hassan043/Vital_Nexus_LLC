import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../services/api'

export default function Dashboard({ onLogout }) {
  const [reports, setReports] = useState([])
  const [loading, setLoading] = useState(true)
  const [deleteModal, setDeleteModal] = useState({ open: false, reportId: null, reportPublicId: '' })
  const [deleting, setDeleting] = useState(false)
  const navigate = useNavigate()

  useEffect(() => {
    loadReports()
  }, [])

  const loadReports = async () => {
    try {
      const data = await api.getReports()
      setReports(data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = () => {
    api.logout()
    onLogout()
  }

  const openDeleteModal = (reportId, reportPublicId) => {
    setDeleteModal({ open: true, reportId, reportPublicId })
  }

  const closeDeleteModal = () => {
    setDeleteModal({ open: false, reportId: null, reportPublicId: '' })
  }

  const handleDelete = async () => {
    setDeleting(true)
    try {
      await api.deleteReport(deleteModal.reportId)
      setReports(reports.filter(r => r.id !== deleteModal.reportId))
      closeDeleteModal()
    } catch (err) {
      console.error('Failed to delete report:', err)
      alert('Failed to delete report. Please try again.')
    } finally {
      setDeleting(false)
    }
  }

  return (
    <>
      <nav>
        <div className="container">
          <h1>VitalNexus</h1>
          <div>
            <button onClick={() => navigate('/create-report')}>
              + New Report
            </button>
            <button onClick={handleLogout} style={{ backgroundColor: '#6c757d' }}>
              Logout
            </button>
          </div>
        </div>
      </nav>

      <div className="container">
        <div className="card">
          <h2>Your Lab Reports</h2>
          
          {loading ? (
            <p>Loading...</p>
          ) : reports.length === 0 ? (
            <div>
              <p>No reports yet. Create your first report to get started!</p>
              <button onClick={() => navigate('/create-report')} style={{ marginTop: '16px' }}>
                Create First Report
              </button>
            </div>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Report ID</th>
                  <th>Date</th>
                  <th>Type</th>
                  <th>Markers</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {reports.map(report => (
                  <tr key={report.id}>
                    <td>
                      <code style={{ 
                        fontSize: '13px', 
                        background: 'var(--background)', 
                        padding: '4px 8px', 
                        borderRadius: '4px',
                        fontFamily: 'monospace'
                      }}>
                        {report.reportPublicId}
                      </code>
                    </td>
                    <td>{new Date(report.reportDate).toLocaleDateString()}</td>
                    <td>{report.petProfile ? `Pet: ${report.petProfile.name}` : 'Human'}</td>
                    <td>{report.labMarkers.length} markers</td>
                    <td style={{ display: 'flex', gap: '8px' }}>
                      <button
                        onClick={() => navigate(`/report/${report.id}`)}
                        style={{ fontSize: '12px', padding: '6px 12px' }}
                      >
                        View Details
                      </button>
                      <button
                        onClick={() => openDeleteModal(report.id, report.reportPublicId)}
                        style={{ 
                          fontSize: '12px', 
                          padding: '6px 12px',
                          backgroundColor: '#dc3545',
                          color: 'white',
                          border: 'none',
                          borderRadius: '6px',
                          cursor: 'pointer'
                        }}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {deleteModal.open && (
        <div style={{
          position: 'fixed',
          inset: 0,
          backgroundColor: 'rgba(0, 0, 0, 0.5)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000
        }}>
          <div style={{
            background: 'white',
            borderRadius: '12px',
            padding: '32px',
            maxWidth: '440px',
            width: '90%',
            boxShadow: '0 20px 60px rgba(0,0,0,0.3)'
          }}>
            <div style={{ 
              width: '52px', 
              height: '52px', 
              borderRadius: '50%', 
              background: '#FEE2E2',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '24px',
              marginBottom: '20px'
            }}>
              🗑️
            </div>

            <h3 style={{ 
              fontSize: '20px', 
              fontWeight: 700, 
              marginBottom: '8px',
              color: '#111'
            }}>
              Delete Report?
            </h3>

            <p style={{ 
              color: '#6B7280', 
              marginBottom: '8px',
              fontSize: '15px',
              lineHeight: '1.5'
            }}>
              Are you sure you want to delete this report? This action cannot be undone.
            </p>

            <code style={{
              display: 'block',
              background: '#F3F4F6',
              padding: '8px 12px',
              borderRadius: '6px',
              fontSize: '13px',
              color: '#374151',
              marginBottom: '24px'
            }}>
              {deleteModal.reportPublicId}
            </code>

            <div style={{ display: 'flex', gap: '12px' }}>
              <button
                onClick={closeDeleteModal}
                disabled={deleting}
                style={{
                  flex: 1,
                  padding: '12px',
                  background: 'white',
                  color: '#374151',
                  border: '1px solid #D1D5DB',
                  borderRadius: '8px',
                  fontSize: '15px',
                  fontWeight: 600,
                  cursor: 'pointer'
                }}
              >
                Cancel
              </button>
              <button
                onClick={handleDelete}
                disabled={deleting}
                style={{
                  flex: 1,
                  padding: '12px',
                  background: deleting ? '#FCA5A5' : '#DC2626',
                  color: 'white',
                  border: 'none',
                  borderRadius: '8px',
                  fontSize: '15px',
                  fontWeight: 600,
                  cursor: deleting ? 'not-allowed' : 'pointer'
                }}
              >
                {deleting ? 'Deleting...' : 'Yes, Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}