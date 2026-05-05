'use client'
import * as React from 'react'
import { api, setTokens, getAccessToken } from './api'

export interface AuthUser {
  id: string
  email: string
  displayName: string
  roles: string[]
  emailConfirmed: boolean
  twoFactorEnabled: boolean
  createdAt: string
  languages?: Array<{ code: string; proficiency: string }>
}

interface AuthTokens {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

interface AuthContextValue {
  user: AuthUser | null
  loading: boolean
  login: (email: string, password: string, twoFactorCode?: string) => Promise<void>
  register: (email: string, displayName: string, password: string) => Promise<void>
  logout: () => Promise<void>
  refreshUser: () => Promise<void>
}

const AuthContext = React.createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = React.useState<AuthUser | null>(null)
  const [loading, setLoading] = React.useState(true)

  const refreshUser = React.useCallback(async () => {
    if (!getAccessToken()) {
      setUser(null)
      return
    }
    try {
      const u = await api<AuthUser>('/auth/me')
      setUser(u)
    } catch {
      setUser(null)
    }
  }, [])

  React.useEffect(() => {
    ;(async () => {
      await refreshUser()
      setLoading(false)
    })()
  }, [refreshUser])

  async function login(email: string, password: string, twoFactorCode?: string) {
    const t = await api<AuthTokens>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, twoFactorCode }),
      auth: false,
    })
    setTokens(t.accessToken, t.refreshToken)
    await refreshUser()
  }

  async function register(email: string, displayName: string, password: string) {
    await api('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, displayName, password }),
      auth: false,
    })
    await login(email, password)
  }

  async function logout() {
    try {
      await api('/auth/logout', {
        method: 'POST',
        body: JSON.stringify({ refreshToken: window.localStorage.getItem('libr4.refreshToken') ?? '' }),
      })
    } catch {}
    setTokens(null, null)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout, refreshUser }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = React.useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}
