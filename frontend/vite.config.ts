import { loadEnv } from "vite";
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");

  return {
    plugins: [react()],
    test: { environment: "jsdom" },
    server: {
      proxy: {
        "/api": {
          target: env.VITE_DEV_API_TARGET || "http://localhost:5000",
          changeOrigin: true
        }
      }
    }
  };
});