import { useState } from 'react'
import { Link } from 'react-router-dom'
import { AppLayout } from '../components/AppLayout'
import { AuthCard } from '../components/AuthCard'
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

const stepCopy: Record<WizardStep, { title: string; subtitle: string }> = {
  0: {
    title: 'Create your VitalNexus account',
    subtitle:
      'Functional medicine lab intelligence for your clinic. Registration is completed on Microsoft Entra External ID.',
  },
  1: {
    title: 'Your work email',
    subtitle: signupSteps[1].description,
  },
  2: {
    title: 'Finish on Microsoft',
    subtitle: 'Complete registration on Microsoft’s secure sign-up page, then return here.',
  },
}

export function CreateAccountPage() {
  const { signUp, isLoading } = useVitalNexusAuth()
  const [step, setStep] = useState<WizardStep>(0)
  const [email, setEmail] = useState('')
  const [acceptedTerms, setAcceptedTerms] = useState(false)
  const [error, setError] = useState('')

  const emailIsValid = emailPattern.test(email.trim())
  const { title, subtitle } = stepCopy[step]

  async function handleCompleteRegistration() {
    if (!emailIsValid || !acceptedTerms) {
      setError('Enter a valid email and accept the terms to continue.')
      return
    }

    setError('')
    await signUp(email.trim())
  }

  return (
    <AppLayout variant="auth">
      <AuthCard eyebrow="Create account" title={title} subtitle={subtitle} wide>
        <ol className="auth-card-steps" aria-label="Registration progress">
          {signupSteps.map((item, index) => (
            <li
              key={item.title}
              className={
                index === step ? 'auth-card-step is-active' : index < step ? 'auth-card-step is-complete' : 'auth-card-step'
              }
            >
              <span className="auth-card-step-number">{index + 1}</span>
              <span className="auth-card-step-label">{item.title}</span>
            </li>
          ))}
        </ol>

        {step === 0 ? (
          <>
            <p className="auth-card-copy">
              You are creating a new <strong>Customer</strong> account (your organization). After Microsoft Entra
              registration, VitalNexus provisions one subscription, one dedicated Patients database, and assigns you as
              the sole <strong>Admin</strong>. You can invite additional staff as <strong>Users</strong> later.
            </p>
            <ul className="auth-card-list">
              <li>Use your clinic or work email address.</li>
              <li>Choose a password on Microsoft’s secure sign-up page.</li>
              <li>Verify your email and complete your profile there.</li>
            </ul>
            <div className="auth-card-actions">
              <button type="button" className="auth-button auth-button-block" onClick={() => setStep(1)}>
                Get started
              </button>
              <p className="auth-card-footer">
                Already have an account?{' '}
                <Link to="/sign-in" className="text-link">
                  Sign in
                </Link>
              </p>
            </div>
          </>
        ) : null}

        {step === 1 ? (
          <>
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
            <label className="checkbox-row auth-card-checkbox">
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
            <div className="auth-card-actions auth-card-actions-split">
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
            <p className="auth-card-notice">
              We will open Microsoft’s registration page for <strong>{email.trim()}</strong>.
            </p>
            <ol className="auth-card-list auth-card-list-numbered">
              <li>Confirm your email address.</li>
              <li>Verify your inbox with the one-time code Microsoft sends.</li>
              <li>Create your password and profile details.</li>
              <li>Return to VitalNexus when registration completes.</li>
            </ol>
            <p className="auth-card-hint">
              Email verification is required by Microsoft for new accounts. VitalNexus cannot change that step.
            </p>
            {error ? <p className="field-error">{error}</p> : null}
            <div className="auth-card-actions auth-card-actions-split">
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
      </AuthCard>
    </AppLayout>
  )
}
