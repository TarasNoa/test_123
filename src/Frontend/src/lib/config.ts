/// <reference types="vite/client" />

// Единственное место в проекте где хранятся URL.
// Все компоненты и сервисы берут URL только отсюда.
// Production static build uses relative URLs → nginx proxies /api/ to Gateway.
// Dev mode uses VITE env vars (or localhost defaults).
const isDev = import.meta.env.DEV;
export const config = {
  apiBaseUrl: isDev ? (import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000") : "",
  wsBaseUrl: isDev ? (import.meta.env.VITE_WS_BASE_URL ?? "ws://localhost:5000") : "",
  grpcBaseUrl: isDev ? (import.meta.env.VITE_GRPC_BASE_URL ?? "http://localhost:50051") : "",
} as const;
