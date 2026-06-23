import { useState } from 'react'
import { Link } from 'react-router-dom'
import { AppLayout } from '../components/AppLayout'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

const signupSteps = [
  {
    title: 'Welcome',
    description: 'Create a secure clinic account for VitalNexus lab intelligence.',
  },
  {
    title: 'Your email',
    description: 'We use your work email as your sign-in name. VitalNexus never stores passwords.',
  },
  {
    title: 'Secure registration',
    description: 'Finish account creation on Microsoft’s hosted sign-up experience.',
  },
] as const

type WizardStep = 0 | 1 | 2

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function CreateAccountPage() {
  const { signUp, isLoading } = useVitalNexusAuth()
  const [step, setStep] = useState<WizardStep>(0)
  const [email, setEmail] = useState('')
  const [acceptedTerms, setAcceptedTerms] = useState(false)
  const [error, setError] = useState('')

  const emailIsValid = emailPattern.test(email.trim())

  async function handleCompleteRegistration() {
    if (!emailIsValid || !acceptedTerms) {
      setError('Enter a valid email and accept the terms to continue.')
      return
    }

    setError('')
    await signUp(email.trim())
  }

  return (
    <AppLayout>
      <div className="flow-header">
        <p className="eyebrow">Create account</p>
        <h1>Create your VitalNexus account</h1>
        <p className="lede">
          Functional medicine lab intelligence for your clinic. Registration is completed on
          Microsoft Entra External ID — we do not store passwords in VitalNexus.
        </p>
      </div>

      <ol className="step-indicator" aria-label="Registration progress">
        {signupSteps.map((item, index) => (
          <li key={item.title} className={index === step ? 'is-active' : index < step ? 'is-complete' : ''}>
            <span className="step-number">{index + 1}</span>
            <span className="step-label">{item.title}</span>
          </li>
        ))}
      </ol>

      <section className="auth-panel">
        {step === 0 ? (
          <>
            <h2>Clinic workspace access</h2>
            <p className="auth-status">
              You are creating a new <strong>Customer</strong> account (your organization). After Microsoft Entra
              registration, VitalNexus provisions one subscription, one dedicated Patients database, and assigns you as
              the sole <strong>Admin</strong>. You can invite additional staff as <strong>Users</strong> later.
            </p>
            <ul className="flow-list">
              <li>Use your clinic or work email address.</li>
              <li>Choose a password on Microsoft’s secure sign-up page.</li>
              <li>Verify your email and complete your profile there.</li>
            </ul>
            <div className="auth-actions">
              <button type="button" className="auth-button" onClick={() => setStep(1)}>
                Get started
              </button>
              <Link to="/sign-in" className="text-link">
                Already have an account?
              </Link>
            </div>
          </>
        ) : null}

        {step === 1 ? (
          <>
            <h2>Your work email</h2>
            <p className="auth-status">{signupSteps[1].description}</p>
            <label className="field-label" htmlFor="signup-email">
              Email address
            </label>
            <input
              id="signup-email"
              className="field-input"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="you@clinic.com"
            />
            <label className="checkbox-row">
              <input
                type="checkbox"
                checked={acceptedTerms}
                onChange={(event) => setAcceptedTerms(event.target.checked)}
              />
              <span>
                I agree that VitalNexus may create a customer identity for this email in Microsoft
                Entra External ID.
              </span>
            </label>
            {error ? <p className="field-error">{error}</p> : null}
            <div className="auth-actions">
              <button type="button" className="auth-button auth-button-secondary" onClick={() => setStep(0)}>
                Back
              </button>
              <button
                type="button"
                className="auth-button"
                disabled={!emailIsValid || !acceptedTerms}
                onClick={() => {
                  setError('')
                  setStep(2)
                }}
              >
                Continue
              </button>
            </div>
          </>
        ) : null}

        {step === 2 ? (
          <>
            <h2>Finish on Microsoft</h2>
            <p className="auth-status">
              We will open Microsoft’s registration page for <strong>{email.trim()}</strong>. Complete
              these steps there:
            </p>
            <ol className="numbered-flow-list">
              <li>Confirm your email address.</li>
              <li>Verify your inbox with the one-time code Microsoft sends.</li>
              <li>Create your password and profile details.</li>
              <li>Return to VitalNexus when registration completes.</li>
            </ol>
            <p className="flow-note">
              Email verification is required by Microsoft for new accounts. VitalNexus cannot change
              that step.
            </p>
            {error ? <p className="field-error">{error}</p> : null}
            <div className="auth-actions">
              <button type="button" className="auth-button auth-button-secondary" onClick={() => setStep(1)}>
                Back
              </button>
              <button
                type="button"
                className="auth-button"
                disabled={isLoading}
                onClick={() => void handleCompleteRegistration()}
              >
                {isLoading ? 'Redirecting…' : 'Continue to secure registration'}
              </button>
            </div>
          </>
        ) : null}
      </section>
    </AppLayout>
  )
}
