// Единственное место в проекте где хранятся URL.
// Все компоненты и сервисы берут URL только отсюда.
export const config = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000",
  wsBaseUrl: import.meta.env.VITE_WS_BASE_URL ?? "ws://localhost:5000",
  grpcBaseUrl: import.meta.env.VITE_GRPC_BASE_URL ?? "http://localhost:50051",
} as const;
