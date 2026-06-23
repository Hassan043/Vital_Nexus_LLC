import { describe, expect, it, beforeEach, vi } from 'vitest'
import {
  clearAuthReturnUrl,
  cleanAuthRedirectFromBrowserUrl,
  consumeAuthReturnUrl,
  formatReturnUrlForDisplay,
  peekAuthReturnUrl,
  sanitizeReturnUrl,
  saveAuthReturnUrl,
  stripAuthQueryParams,
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

  describe('stripAuthQueryParams', () => {
    it('removes MSAL/OAuth query parameters', () => {
      expect(stripAuthQueryParams('?state=abc&code=xyz&client_info=1')).toBe('')
      expect(stripAuthQueryParams('?tab=labs&state=abc')).toBe('?tab=labs')
      expect(stripAuthQueryParams('?error=access_denied&error_description=denied')).toBe('')
    })

    it('preserves non-auth query parameters', () => {
      expect(stripAuthQueryParams('?tab=labs&sort=asc')).toBe('?tab=labs&sort=asc')
    })
  })

  describe('sanitizeReturnUrl with auth params', () => {
    it('rejects root path with only OAuth noise', () => {
      expect(sanitizeReturnUrl('/?state=eyJpZCI6InRlc3QifQ')).toBeNull()
    })

    it('strips auth params from saved paths', () => {
      expect(sanitizeReturnUrl('/reports?state=abc&tab=summary')).toBe('/reports?tab=summary')
    })

    it('keeps root path when meaningful query remains after stripping', () => {
      expect(sanitizeReturnUrl('/?state=abc&tab=labs')).toBe('/?tab=labs')
    })
  })

  describe('formatReturnUrlForDisplay', () => {
    it('strips auth params for UI display', () => {
      expect(formatReturnUrlForDisplay('/workspace?state=abc&tab=labs')).toBe('/workspace?tab=labs')
      expect(formatReturnUrlForDisplay('/?state=abc')).toBe('/')
    })
  })

  describe('cleanAuthRedirectFromBrowserUrl', () => {
    it('replaces browser URL when auth query params are present', () => {
      window.history.replaceState({}, '', '/?state=abc&code=xyz')
      const replaceState = vi.spyOn(window.history, 'replaceState')

      cleanAuthRedirectFromBrowserUrl()

      expect(replaceState).toHaveBeenCalledWith(window.history.state, '', '/')
      replaceState.mockRestore()
    })

    it('does nothing when no auth query params are present', () => {
      window.history.replaceState({}, '', '/reports?tab=labs')
      const replaceState = vi.spyOn(window.history, 'replaceState')

      cleanAuthRedirectFromBrowserUrl()

      expect(replaceState).not.toHaveBeenCalled()
      replaceState.mockRestore()
    })
  })
})
