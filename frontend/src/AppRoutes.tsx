import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthenticatedAppShell } from './components/AuthenticatedAppShell'
import { RequireAuth } from './components/RequireAuth'
import { RequireGuest } from './components/RequireGuest'
import { CreateAccountPage } from './pages/CreateAccountPage'
import { HomePage } from './pages/HomePage'
import { SignInPage } from './pages/SignInPage'

export function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<RequireGuest />}>
          <Route path="/sign-in" element={<SignInPage />} />
          <Route path="/create-account" element={<CreateAccountPage />} />
        </Route>

        <Route element={<RequireAuth />}>
          <Route element={<AuthenticatedAppShell />}>
            <Route path="/" element={<HomePage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
