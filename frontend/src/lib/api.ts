/**
 * Thin fetch wrapper that:
 *  - Uses Next.js rewrites → /api/v1/* proxies to the Gateway.
 *  - Adds the Authorization header (Bearer) if an access token is stored.
 *  - Refreshes the access token via /api/v1/auth/refresh on 401 once.
 */

let accessToken: string | null = null
let refreshToken: string | null = null

const TOKEN_KEY_ACCESS = 'libr4.accessToken'
const TOKEN_KEY_REFRESH = 'libr4.refreshToken'

if (typeof window !== 'undefined') {
  accessToken = window.localStorage.getItem(TOKEN_KEY_ACCESS)
  refreshToken = window.localStorage.getItem(TOKEN_KEY_REFRESH)
}

export function setTokens(access: string | null, refresh: string | null) {
  accessToken = access
  refreshToken = refresh
  if (typeof window === 'undefined') return
  if (access) window.localStorage.setItem(TOKEN_KEY_ACCESS, access)
  else window.localStorage.removeItem(TOKEN_KEY_ACCESS)
  if (refresh) window.localStorage.setItem(TOKEN_KEY_REFRESH, refresh)
  else window.localStorage.removeItem(TOKEN_KEY_REFRESH)
}

export function getAccessToken() {
  return accessToken
}

export class ApiError extends Error {
  status: number
  code?: string
  constructor(message: string, status: number, code?: string) {
    super(message)
    this.status = status
    this.code = code
  }
}

async function refresh(): Promise<boolean> {
  if (!refreshToken) return false
  const res = await fetch('/api/v1/auth/refresh', {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
  if (!res.ok) {
    setTokens(null, null)
    return false
  }
  const json = await res.json()
  setTokens(json.accessToken, json.refreshToken)
  return true
}

export async function api<T = unknown>(
  path: string,
  init: RequestInit & { auth?: boolean } = {}
): Promise<T> {
  const { auth = true, ...rest } = init
  const headers = new Headers(rest.headers)
  headers.set('content-type', headers.get('content-type') ?? 'application/json')
  if (auth && accessToken) headers.set('authorization', `Bearer ${accessToken}`)

  const url = path.startsWith('/api/') ? path : `/api/v1${path.startsWith('/') ? path : `/${path}`}`

  let res = await fetch(url, { ...rest, headers })

  if (res.status === 401 && auth && refreshToken) {
    if (await refresh()) {
      headers.set('authorization', `Bearer ${accessToken}`)
      res = await fetch(url, { ...rest, headers })
    }
  }

  if (!res.ok) {
    let msg = res.statusText
    let code: string | undefined
    try {
      const j = await res.json()
      msg = j.detail ?? j.title ?? msg
      code = j.title
    } catch {}
    throw new ApiError(msg, res.status, code)
  }

  if (res.status === 204) return undefined as unknown as T
  return (await res.json()) as T
}
