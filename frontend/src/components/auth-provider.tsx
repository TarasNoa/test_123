'use client'
import { useAuth as useCoreAuth, AuthProvider } from '@/lib/auth'

export { AuthProvider }

export function useAuth() {
  const auth = useCoreAuth()
  return {
    ...auth,
    isAuthenticated: Boolean(auth.user),
  }
}
