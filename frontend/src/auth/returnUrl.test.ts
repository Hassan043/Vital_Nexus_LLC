import { describe, expect, it, beforeEach } from 'vitest'
import {
  clearAuthReturnUrl,
  consumeAuthReturnUrl,
  peekAuthReturnUrl,
  sanitizeReturnUrl,
  saveAuthReturnUrl,
} from './returnUrl'

describe('returnUrl', () => {
  beforeEach(() => {
    clearAuthReturnUrl()
  })

  it('sanitizes protected app paths and rejects auth pages', () => {
    expect(sanitizeReturnUrl('/reports')).toBe('/reports')
    expect(sanitizeReturnUrl('/sign-in')).toBeNull()
    expect(sanitizeReturnUrl('https://evil.example')).toBeNull()
  })

  it('stores and consumes a return URL once', () => {
    saveAuthReturnUrl('/workspace?tab=labs')
    expect(peekAuthReturnUrl()).toBe('/workspace?tab=labs')
    expect(consumeAuthReturnUrl()).toBe('/workspace?tab=labs')
    expect(peekAuthReturnUrl()).toBeNull()
  })
})
