import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// The API runs on :8080 in local dev; in production (Docker) nginx proxies
// /api to the api container on the same origin, so calls always use "/api".
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:8080",
        changeOrigin: true,
      },
    },
  },
});
