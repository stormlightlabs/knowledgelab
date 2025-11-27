import { defineConfig } from "vite";
import tailwindcss from "@tailwindcss/vite";

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [tailwindcss()],
    clearScreen: false,
    server: {
        watch: {
            ignored: ["**/*.fs"],
        },
    },
});
