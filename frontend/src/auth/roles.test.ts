import { describe, expect, it } from 'vitest'
import { ApplicationRoles, hasRole, isAdmin } from './roles'

describe('roles', () => {
  it('detects admin role case-insensitively', () => {
    expect(isAdmin(['admin'])).toBe(true)
    expect(isAdmin(['User'])).toBe(false)
  })

  it('checks explicit role membership', () => {
    expect(hasRole(['User'], ApplicationRoles.User)).toBe(true)
    expect(hasRole(['User'], ApplicationRoles.Admin)).toBe(false)
  })
})
