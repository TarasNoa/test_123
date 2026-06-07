import { defineConfig } from "@solidjs/start/config";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  ssr: false,
  server: {
    preset: "static",
  },
  vite: {
    build: {
      target: "esnext",
    },
    envPrefix: "VITE_",
    plugins: [tailwindcss()],
    optimizeDeps: {
      exclude: ['monaco-editor'],
    },
    worker: {
      format: 'es',
    },
    server: {
      headers: {
        "Content-Security-Policy": "default-src 'self'; script-src 'self' 'unsafe-eval' 'unsafe-inline' http: https:; connect-src 'self' ws: wss: http: https: localhost:5000 localhost:5001 localhost:5002 localhost:5004 localhost:5007 localhost:3000 localhost:3099; img-src 'self' data: blob: http: https:; style-src 'self' 'unsafe-inline'; font-src 'self' data:;",
      },
      proxy: {
        "/api": {
          target: "http://localhost:5000",
          changeOrigin: true,
          secure: false,
        },
        "/hubs": {
          target: "http://localhost:5000",
          changeOrigin: true,
          ws: true,
          secure: false,
        },
        "/ws": {
          target: "http://localhost:5000",
          changeOrigin: true,
          ws: true,
          secure: false,
        },
      },
    },
  },
});
