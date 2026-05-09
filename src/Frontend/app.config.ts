import { defineConfig } from "@solidjs/start/config";

export default defineConfig({
  ssr: false,
  server: {
    port: 3000,
    preset: "node",
  },
  vite: {
    build: {
      target: "esnext",
    },
    envPrefix: "VITE_",
  },
});
