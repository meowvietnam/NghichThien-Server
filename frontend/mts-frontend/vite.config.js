import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 3000,
    strictPort: false,
    allowedHosts: ["test.carrotcoder.fun"],
  },
  preview: {
    port: 8080,
    strictPort: false,
  },
});
