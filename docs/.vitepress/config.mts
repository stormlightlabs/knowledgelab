import { defineConfig } from "vitepress";

export default defineConfig({
    title: "Stormlight Labs Note Taker",
    description:
        "Developer documentation & manual for the Stormlight Labs Note Taker",
    themeConfig: {
        nav: [
            { text: "Home", link: "/" },
            { text: "Examples", link: "/markdown-examples" },
        ],
        sidebar: [
            {
                text: "Examples",
                items: [
                    { text: "Markdown Examples", link: "/markdown-examples" },
                    { text: "Runtime API Examples", link: "/api-examples" },
                ],
            },
        ],
        socialLinks: [
            { icon: "github", link: "https://github.com/vuejs/vitepress" },
        ],
    },
    markdown: {
        theme: {
            light: "catppuccin-latte",
            dark: "catppuccin-mocha",
        },
    },
});
