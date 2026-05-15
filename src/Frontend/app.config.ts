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
