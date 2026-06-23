export const ApplicationRoles = {
  Admin: 'Admin',
  User: 'User',
} as const

export type ApplicationRole = (typeof ApplicationRoles)[keyof typeof ApplicationRoles]

export function hasRole(roles: readonly string[] | null | undefined, role: ApplicationRole): boolean {
  if (!roles?.length) {
    return false
  }

  return roles.some((value) => value.localeCompare(role, undefined, { sensitivity: 'accent' }) === 0)
}

export function isAdmin(roles: readonly string[] | null | undefined): boolean {
  return hasRole(roles, ApplicationRoles.Admin)
}
