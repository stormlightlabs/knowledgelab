import { defineConfig } from "vite";
import tailwindcss from "@tailwindcss/vite";
import path from "path";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [tailwindcss()],
  clearScreen: false,
  resolve: {
    alias: {
      "@wailsjs": path.resolve(__dirname, "wailsjs"),
    },
  },
  server: {
    watch: {
      ignored: ["**/*.fs"],
    },
  },
});
