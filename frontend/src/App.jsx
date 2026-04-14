import { useState, useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate, useNavigate } from 'react-router-dom'
import Login from './pages/Login'
import Register from './pages/Register'
import ForgotPassword from './pages/ForgotPassword'
import ResetPassword from './pages/ResetPassword'
import Dashboard from './pages/Dashboard'
import CreateReport from './pages/CreateReport'
import ViewReport from './pages/ViewReport'
import { api } from './services/api'

const TIMEOUT_MS = 30 * 60 * 1000

function InactivityGuard({ isAuthenticated, onLogout }) {
  const navigate = useNavigate()

  useEffect(() => {
    if (!isAuthenticated) return

    let timer

    const resetTimer = () => {
      clearTimeout(timer)
      timer = setTimeout(() => {
        api.logout()
        onLogout()
        navigate('/login')
      }, TIMEOUT_MS)
    }

    const events = ['mousemove', 'mousedown', 'keypress', 'scroll', 'touchstart', 'click']
    events.forEach(e => window.addEventListener(e, resetTimer))
    resetTimer()

    return () => {
      events.forEach(e => window.removeEventListener(e, resetTimer))
      clearTimeout(timer)
    }
  }, [isAuthenticated])

  return null
}

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false)

  useEffect(() => {
    setIsAuthenticated(!!api.getToken())
  }, [])

  return (
    <BrowserRouter>
      <InactivityGuard
        isAuthenticated={isAuthenticated}
        onLogout={() => setIsAuthenticated(false)}
      />
      <Routes>
        <Route path="/login" element={
          isAuthenticated ? <Navigate to="/dashboard" /> : <Login onLogin={() => setIsAuthenticated(true)} />
        } />
        <Route path="/register" element={
          isAuthenticated ? <Navigate to="/dashboard" /> : <Register onLogin={() => setIsAuthenticated(true)} />
        } />
        <Route path="/forgot-password" element={
          isAuthenticated ? <Navigate to="/dashboard" /> : <ForgotPassword />
        } />
        <Route path="/reset-password" element={
          isAuthenticated ? <Navigate to="/dashboard" /> : <ResetPassword />
        } />
        <Route path="/dashboard" element={
          isAuthenticated ? <Dashboard onLogout={() => setIsAuthenticated(false)} /> : <Navigate to="/login" />
        } />
        <Route path="/create-report" element={
          isAuthenticated ? <CreateReport /> : <Navigate to="/login" />
        } />
        <Route path="/report/:reportId" element={
          isAuthenticated ? <ViewReport /> : <Navigate to="/login" />
        } />
        <Route path="/" element={<Navigate to="/dashboard" />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App