import { defineConfig } from "@solidjs/start/config";

export default defineConfig({
  ssr: true,
  extensions: ["mdx"],
  server: {
    port: 3000,
    preset: "node",
  },
  vite: {
    build: {
      target: "esnext",
    },
  },
});
