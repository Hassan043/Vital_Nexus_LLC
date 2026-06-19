import type { AccountInfo } from '@azure/msal-browser'

type WelcomeUserProps = {
  account: AccountInfo
}

export function WelcomeUser({ account }: WelcomeUserProps) {
  const displayName = account.name ?? account.username

  return (
    <div className="welcome-user">
      <p className="welcome-label">Signed in as</p>
      <p className="welcome-name">{displayName}</p>
      <p className="welcome-email">{account.username}</p>
    </div>
  )
}
