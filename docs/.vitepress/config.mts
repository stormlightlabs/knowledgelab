import { defineConfig } from "vitepress";

export default defineConfig({
  title: "Stormlight Labs Note Taker",
  description: "Developer documentation & manual for the Stormlight Labs Note Taker",
  themeConfig: {
    nav: [
      { text: "Home", link: "/" },
      { text: "Overview", link: "/overview" },
      { text: "Markdown Syntax", link: "/markdown-dialect" },
    ],
    sidebar: [
      {
        text: "Getting Started",
        items: [
          { text: "Overview", link: "/overview" },
          { text: "Configuration", link: "/configuration" },
          { text: "Configuration Layout", link: "/config-layout" },
        ],
      },
      {
        text: "Writing Notes",
        items: [
          { text: "Markdown Dialect", link: "/markdown-dialect" },
          { text: "Markdown Examples", link: "/md-examples" },
          { text: "Daily Notes", link: "/daily-notes" },
        ],
      },
      {
        text: "Productivity",
        items: [
          { text: "Task Management", link: "/task-management" },
          { text: "Keyboard Shortcuts", link: "/keyboard-shortcuts" },
        ],
      },
      {
        text: "Knowledge Graph",
        items: [
          { text: "Graph Database", link: "/graph" },
          { text: "Search", link: "/search" },
        ],
      },
      {
        text: "Importing",
        items: [{ text: "Import Guides", link: "/importing" }],
      },
    ],
    socialLinks: [{ icon: "github", link: "https://github.com/stormlightlabs/notes" }],
  },
  markdown: {
    theme: {
      light: "catppuccin-latte",
      dark: "catppuccin-mocha",
    },
  },
});
