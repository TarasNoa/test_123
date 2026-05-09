// app.config.ts
import { defineConfig } from "@solidjs/start/config";
var app_config_default = defineConfig({
  ssr: false,
  server: {
    port: 3e3,
    preset: "node"
  },
  vite: {
    build: {
      target: "esnext"
    },
    envPrefix: "VITE_"
  }
});
export {
  app_config_default as default
};
