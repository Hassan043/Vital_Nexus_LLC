import { useState, useEffect } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../services/api'

export default function ResetPassword() {
  const [searchParams] = useSearchParams()
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  const token = searchParams.get('token')

  useEffect(() => {
    if (!token) {
      setError('Invalid or missing reset token')
    }
  }, [token])

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match')
      return
    }

    if (newPassword.length < 6) {
      setError('Password must be at least 6 characters')
      return
    }

    if (!token) {
      setError('Invalid reset token')
      return
    }

    setLoading(true)

    try {
      await api.resetPassword(token, newPassword)
      setSuccess(true)
      setTimeout(() => {
        navigate('/login')
      }, 3000)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      padding: '40px 20px'
    }}>
      <div style={{
        width: '100%',
        maxWidth: '440px',
        background: 'white',
        borderRadius: '12px',
        padding: '40px',
        boxShadow: '0 10px 25px rgba(0,0,0,0.1)'
      }}>
        <div style={{ marginBottom: '32px', textAlign: 'center' }}>
          <h1 style={{
            fontSize: '32px',
            fontWeight: 700,
            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            marginBottom: '8px'
          }}>
            VitalNexus
          </h1>
        </div>

        {success ? (
          <div style={{
            textAlign: 'center',
            padding: '20px'
          }}>
            <div style={{
              fontSize: '48px',
              marginBottom: '16px'
            }}>
              ✅
            </div>
            <h2 style={{
              fontSize: '24px',
              fontWeight: 700,
              marginBottom: '12px',
              color: 'var(--text-primary)'
            }}>
              Password reset successful
            </h2>
            <p style={{
              fontSize: '15px',
              color: 'var(--text-secondary)',
              marginBottom: '24px'
            }}>
              Redirecting to login...
            </p>
          </div>
        ) : (
          <>
            <h2 style={{
              fontSize: '24px',
              fontWeight: 700,
              marginBottom: '8px',
              color: 'var(--text-primary)'
            }}>
              Set new password
            </h2>
            <p style={{
              fontSize: '15px',
              color: 'var(--text-secondary)',
              marginBottom: '32px'
            }}>
              Enter your new password below.
            </p>

            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label>New password</label>
                <div style={{ position: 'relative' }}>
                  <input
                    type={showPassword ? 'text' : 'password'}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="••••••••"
                    required
                    minLength={6}
                    autoComplete="new-password"
                    style={{ paddingRight: '60px' }}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    style={{
                      position: 'absolute',
                      right: '12px',
                      top: '50%',
                      transform: 'translateY(-50%)',
                      background: 'none',
                      border: 'none',
                      color: 'var(--text-muted)',
                      cursor: 'pointer',
                      padding: '4px 8px',
                      fontSize: '13px',
                      fontWeight: 500,
                      boxShadow: 'none'
                    }}
                    onMouseOver={(e) => e.currentTarget.style.background = 'none'}
                    onMouseOut={(e) => e.currentTarget.style.background = 'none'}
                  >
                    {showPassword ? 'Hide' : 'Show'}
                  </button>
                </div>
                <p style={{ fontSize: '13px', color: 'var(--text-muted)', marginTop: '4px' }}>
                  Must be at least 6 characters
                </p>
              </div>

              <div className="form-group">
                <label>Confirm new password</label>
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  placeholder="••••••••"
                  required
                  minLength={6}
                  autoComplete="new-password"
                />
              </div>

              {error && (
                <div className="error" style={{ marginTop: '16px', marginBottom: '16px' }}>
                  {error}
                </div>
              )}

              <button
                type="submit"
                disabled={loading}
                style={{
                  width: '100%',
                  padding: '14px',
                  background: loading 
                    ? 'var(--text-muted)' 
                    : 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                  color: 'white',
                  border: 'none',
                  borderRadius: '8px',
                  fontSize: '16px',
                  fontWeight: 600,
                  cursor: loading ? 'not-allowed' : 'pointer',
                  marginTop: '16px',
                  marginBottom: '16px',
                  transition: 'all 0.2s ease',
                  boxShadow: loading ? 'none' : '0 4px 6px -1px rgba(102, 126, 234, 0.3)'
                }}
              >
                {loading ? 'Resetting...' : 'Reset password'}
              </button>

              <div style={{
                textAlign: 'center',
                fontSize: '14px',
                color: 'var(--text-secondary)'
              }}>
                <Link
                  to="/login"
                  style={{
                    color: 'var(--primary)',
                    fontWeight: 500,
                    textDecoration: 'none'
                  }}
                >
                  ← Back to login
                </Link>
              </div>
            </form>
          </>
        )}
      </div>
    </div>
  )
}