import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../services/api'

export default function ForgotPassword() {
  const [email, setEmail] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)

    try {
      await api.forgotPassword(email)
      setSubmitted(true)
    } catch (err) {
      setSubmitted(true)
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

        {!submitted ? (
          <>
            <h2 style={{
              fontSize: '24px',
              fontWeight: 700,
              marginBottom: '8px',
              color: 'var(--text-primary)'
            }}>
              Reset your password
            </h2>
            <p style={{
              fontSize: '15px',
              color: 'var(--text-secondary)',
              marginBottom: '32px'
            }}>
              Enter your email address and we'll send you a link to reset your password.
            </p>

            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label>Email address</label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="you@example.com"
                  required
                  autoComplete="email"
                />
              </div>

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
                  marginBottom: '16px',
                  transition: 'all 0.2s ease',
                  boxShadow: loading ? 'none' : '0 4px 6px -1px rgba(102, 126, 234, 0.3)'
                }}
              >
                {loading ? 'Sending...' : 'Send reset link'}
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
        ) : (
          <>
            <div style={{
              textAlign: 'center',
              padding: '20px'
            }}>
              <div style={{
                fontSize: '48px',
                marginBottom: '16px'
              }}>
                ✉️
              </div>
              <h2 style={{
                fontSize: '24px',
                fontWeight: 700,
                marginBottom: '12px',
                color: 'var(--text-primary)'
              }}>
                Check your email
              </h2>
              <p style={{
                fontSize: '15px',
                color: 'var(--text-secondary)',
                marginBottom: '24px'
              }}>
                If an account exists for {email}, we sent a password reset link.
              </p>
              <p style={{
                fontSize: '14px',
                color: 'var(--text-muted)',
                marginBottom: '32px'
              }}>
                Didn't receive an email? Check your spam folder or try again.
              </p>
              <button
                onClick={() => navigate('/login')}
                style={{
                  width: '100%',
                  padding: '14px',
                  background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                  color: 'white',
                  border: 'none',
                  borderRadius: '8px',
                  fontSize: '16px',
                  fontWeight: 600,
                  cursor: 'pointer'
                }}
              >
                Back to login
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}