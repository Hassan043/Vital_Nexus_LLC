import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../services/api'

export default function Register({ onLogin }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [passwordErrors, setPasswordErrors] = useState([])
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  const validatePassword = (pwd) => {
    const errors = []
    if (pwd.length < 8) errors.push('At least 8 characters')
    if (!/[A-Z]/.test(pwd)) errors.push('One uppercase letter')
    if (!/[a-z]/.test(pwd)) errors.push('One lowercase letter')
    if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>/?]/.test(pwd)) errors.push('One special character')
    return errors
  }

  const handlePasswordChange = (e) => {
    const pwd = e.target.value
    setPassword(pwd)
    setPasswordErrors(validatePassword(pwd))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')

    if (password !== confirmPassword) {
      setError('Passwords do not match')
      return
    }

    const errors = validatePassword(password)
    if (errors.length > 0) {
      setError('Password does not meet requirements')
      return
    }

    setLoading(true)

    try {
      await api.register(email, password)
      await api.login(email, password)
      onLogin()
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
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'
    }}>
      <div style={{
        flex: 1,
        display: 'none',
        flexDirection: 'column',
        justifyContent: 'center',
        padding: '80px 60px',
        color: 'white'
      }}
      className="branding-panel">
        <div style={{ maxWidth: '500px' }}>
          <h1 style={{
            fontSize: '48px',
            fontWeight: 700,
            marginBottom: '24px',
            color: 'white',
            letterSpacing: '-1px'
          }}>
            VitalNexus
          </h1>
          <p style={{
            fontSize: '20px',
            lineHeight: '1.8',
            opacity: 0.95,
            fontWeight: 400
          }}>
            Understand your lab results with simple explanations, personalized wellness guidance, and actionable insights.
          </p>
          <div style={{
            marginTop: '48px',
            padding: '20px 24px',
            background: 'rgba(255,255,255,0.1)',
            borderRadius: '12px',
            backdropFilter: 'blur(10px)',
            border: '1px solid rgba(255,255,255,0.2)'
          }}>
            <p style={{ fontSize: '14px', opacity: 0.9, fontWeight: 500 }}>
              🎓 Educational wellness guidance based on your lab markers
            </p>
          </div>
        </div>
      </div>

      <div style={{
        flex: 1,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '40px 20px',
        background: 'white'
      }}>
        <div style={{
          width: '100%',
          maxWidth: '440px'
        }}>
          <div style={{ marginBottom: '32px', textAlign: 'center' }} className="mobile-logo">
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
            <p style={{
              fontSize: '14px',
              color: 'var(--text-secondary)'
            }}>
              Lab wellness insights, simplified
            </p>
          </div>

          <div style={{
            background: '#EEF2FF',
            border: '1px solid #C7D2FE',
            borderRadius: '8px',
            padding: '10px 14px',
            marginBottom: '32px',
            fontSize: '13px',
            color: '#4338CA',
            textAlign: 'center'
          }}>
            📚 Educational use only · Not medical advice
          </div>

          <h2 style={{
            fontSize: '28px',
            fontWeight: 700,
            marginBottom: '8px',
            color: 'var(--text-primary)'
          }}>
            Create your account
          </h2>
          <p style={{
            fontSize: '15px',
            color: 'var(--text-secondary)',
            marginBottom: '32px'
          }}>
            Start tracking your wellness journey
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

            <div className="form-group">
              <label>Password</label>
              <div style={{ position: 'relative' }}>
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={handlePasswordChange}
                  placeholder="••••••••"
                  required
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
              
              {password && (
                <div style={{ marginTop: '8px', fontSize: '13px' }}>
                  <p style={{ 
                    color: passwordErrors.length === 0 ? '#10B981' : '#6B7280',
                    fontWeight: 500,
                    marginBottom: '4px'
                  }}>
                    Password must include:
                  </p>
                  <ul style={{ margin: 0, paddingLeft: '20px' }}>
                    <li style={{ color: password.length >= 8 ? '#10B981' : '#EF4444' }}>
                      At least 8 characters {password.length >= 8 && '✓'}
                    </li>
                    <li style={{ color: /[A-Z]/.test(password) ? '#10B981' : '#EF4444' }}>
                      One uppercase letter {/[A-Z]/.test(password) && '✓'}
                    </li>
                    <li style={{ color: /[a-z]/.test(password) ? '#10B981' : '#EF4444' }}>
                      One lowercase letter {/[a-z]/.test(password) && '✓'}
                    </li>
                    <li style={{ color: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>/?]/.test(password) ? '#10B981' : '#EF4444' }}>
                      One special character (!@#$%^&*) {/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>/?]/.test(password) && '✓'}
                    </li>
                  </ul>
                </div>
              )}
            </div>

            <div className="form-group">
              <label>Confirm password</label>
              <input
                type={showPassword ? 'text' : 'password'}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                required
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
              disabled={loading || passwordErrors.length > 0}
              style={{
                width: '100%',
                padding: '14px',
                background: (loading || passwordErrors.length > 0)
                  ? 'var(--text-muted)' 
                  : 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                color: 'white',
                border: 'none',
                borderRadius: '8px',
                fontSize: '16px',
                fontWeight: 600,
                cursor: (loading || passwordErrors.length > 0) ? 'not-allowed' : 'pointer',
                marginBottom: '20px',
                marginTop: '16px',
                transition: 'all 0.2s ease',
                transform: 'none',
                boxShadow: (loading || passwordErrors.length > 0) ? 'none' : '0 4px 6px -1px rgba(102, 126, 234, 0.3)'
              }}
              onMouseOver={(e) => {
                if (!loading && passwordErrors.length === 0) {
                  e.currentTarget.style.transform = 'translateY(-1px)';
                  e.currentTarget.style.boxShadow = '0 6px 12px -2px rgba(102, 126, 234, 0.4)';
                }
              }}
              onMouseOut={(e) => {
                e.currentTarget.style.transform = 'none';
                e.currentTarget.style.boxShadow = (loading || passwordErrors.length > 0) ? 'none' : '0 4px 6px -1px rgba(102, 126, 234, 0.3)';
              }}
            >
              {loading ? 'Creating account...' : 'Create account'}
            </button>

            <div style={{
              textAlign: 'center',
              fontSize: '14px',
              color: 'var(--text-secondary)'
            }}>
              Already have an account?{' '}
              <Link
                to="/login"
                style={{
                  color: 'var(--primary)',
                  fontWeight: 600,
                  textDecoration: 'none'
                }}
              >
                Sign in
              </Link>
            </div>
          </form>
        </div>
      </div>

      <style>{`
        @media (min-width: 768px) {
          .branding-panel {
            display: flex !important;
          }
          .mobile-logo {
            display: none !important;
          }
        }
      `}</style>
    </div>
  )
}