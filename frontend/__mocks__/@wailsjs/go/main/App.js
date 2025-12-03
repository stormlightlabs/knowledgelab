/**
 * Mock Wails API for testing
 * */

export const CreateNewWorkspace = () =>
  Promise.resolve({
    Workspace: {
      Id: "new-workspace-id",
      Name: "New Workspace",
      RootPath: "/mock/new/workspace",
      IgnorePatterns: [".git"],
      CreatedAt: new Date().toISOString(),
      LastOpenedAt: new Date().toISOString(),
    },
    Config: {
      DailyNoteFormat: "2006-01-02",
      DailyNoteFolder: "",
      DefaultTags: [],
    },
    NoteCount: 1,
    TotalBlocks: 0,
  });

export const OpenWorkspace = (path) =>
  Promise.resolve({
    Workspace: {
      Id: "test-id",
      Name: "Test Workspace",
      RootPath: path,
      IgnorePatterns: [".git"],
      CreatedAt: new Date().toISOString(),
      LastOpenedAt: new Date().toISOString(),
    },
    Config: {
      DailyNoteFormat: "2006-01-02",
      DailyNoteFolder: "",
      DefaultTags: [],
    },
    NoteCount: 0,
    TotalBlocks: 0,
  });

export const ListNotes = () => Promise.resolve([]);

export const GetNote = (id) =>
  Promise.resolve({
    Id: id,
    Title: "Test Note",
    Path: id,
    Content: "# Test",
    Frontmatter: {},
    Blocks: [],
    Links: [],
    Tags: [],
    CreatedAt: new Date().toISOString(),
    ModifiedAt: new Date().toISOString(),
  });

export const SaveNote = () => Promise.resolve();

export const DeleteNote = () => Promise.resolve();

export const CreateNote = (title, folder) =>
  Promise.resolve({
    Id: `${folder}/${title}.md`,
    Title: title,
    Path: `${folder}/${title}.md`,
    Content: `# ${title}\n\n`,
    Frontmatter: {},
    Blocks: [],
    Links: [],
    Tags: [],
    CreatedAt: new Date().toISOString(),
    ModifiedAt: new Date().toISOString(),
  });

export const GetBacklinks = () => Promise.resolve([]);

export const GetGraph = () =>
  Promise.resolve({
    Nodes: [],
    Edges: [],
  });

export const Search = () => Promise.resolve([]);

export const GetNotesWithTag = () => Promise.resolve([]);

export const GetAllTags = () => Promise.resolve([]);

export const GetAllTagsWithCounts = () =>
  Promise.resolve([
    { Name: "test-tag", Count: 3, NoteIds: ["note1", "note2", "note3"] },
    { Name: "another-tag", Count: 1, NoteIds: ["note4"] },
  ]);

export const GetTagInfo = (tagName) =>
  Promise.resolve({
    Name: tagName,
    Count: 2,
    NoteIds: ["note1", "note2"],
  });

export const RenderMarkdown = (markdown) => Promise.resolve(`<p>${markdown}</p>`);

export const SelectDirectory = () => Promise.resolve("/mock/workspace/directory");

export const SelectFile = () => Promise.resolve("/mock/file.md");

export const SelectFiles = () => Promise.resolve(["/mock/file-a.md", "/mock/file-b.md"]);

export const SaveFile = () => Promise.resolve("/mock/saved.md");

export const LoadSettings = () =>
  Promise.resolve({
    General: {
      Theme: "auto",
      Language: "en",
      AutoSave: true,
      AutoSaveInterval: 30,
    },
    Editor: {
      FontFamily: "monospace",
      FontSize: 14,
      LineHeight: 1.6,
      TabSize: 2,
      VimMode: false,
      SpellCheck: true,
    },
  });

export const SaveSettings = () => Promise.resolve();

export const ShowMessage = () => Promise.resolve("ok");

export const LoadWorkspaceSnapshot = () =>
  Promise.resolve({
    UI: {
      ActivePage: "",
      SidebarVisible: true,
      SidebarWidth: 280,
      RightPanelVisible: false,
      RightPanelWidth: 300,
      PinnedPages: [],
      RecentPages: [],
      SearchHistory: [],
      LastWorkspacePath: "/mock/workspace/directory",
      GraphLayout: "force",
    },
  });

export const SaveWorkspaceSnapshot = () => Promise.resolve();

export const ClearRecentFiles = () =>
  Promise.resolve({
    UI: {
      ActivePage: "",
      SidebarVisible: true,
      SidebarWidth: 280,
      RightPanelVisible: false,
      RightPanelWidth: 300,
      PinnedPages: [],
      RecentPages: [],
      SearchHistory: [],
      LastWorkspacePath: "/mock/workspace/directory",
      GraphLayout: "force",
    },
  });

export const GetUserConfigDir = () => "/mock/user/config/KnowledgeLab";

export const InitWorkspaceConfigDir = (workspaceRoot) =>
  Promise.resolve(`${workspaceRoot}/.knowledgelab`);

export const GetAllTasks = () =>
  Promise.resolve({
    tasks: [],
    totalCount: 0,
    completedCount: 0,
    pendingCount: 0,
  });

export const GetTasksForNote = () => Promise.resolve([]);

export const ToggleTaskInNote = () => Promise.resolve();

export const ListThemes = () =>
  Promise.resolve(["iceberg", "nord", "solarized-light", "solarized-dark", "dracula"]);

export const LoadTheme = (slug) => {
  if (slug === "iceberg") {
    return Promise.resolve({
      system: "base16",
      name: "Iceberg",
      author: "cocopon",
      slug: "iceberg",
      variant: "dark",
      palette: {
        base00: "161821",
        base01: "1e2132",
        base02: "2a3158",
        base03: "6b7089",
        base04: "818596",
        base05: "c6c8d1",
        base06: "d2d4de",
        base07: "e8e9f0",
        base08: "e27878",
        base09: "e2a478",
        base0A: "e2a478",
        base0B: "b4be82",
        base0C: "89b8c2",
        base0D: "84a0c6",
        base0E: "a093c7",
        base0F: "b4a382",
      },
    });
  }

  return Promise.resolve({
    system: "base16",
    name: slug === "nord" ? "Nord" : "Solarized Light",
    author: slug === "nord" ? "arcticicestudio" : "Ethan Schoonover",
    slug: slug,
    variant: slug === "nord" ? "dark" : "light",
    palette: {
      base00: "2e3440",
      base01: "3b4252",
      base02: "434c5e",
      base03: "4c566a",
      base04: "d8dee9",
      base05: "e5e9f0",
      base06: "eceff4",
      base07: "8fbcbb",
      base08: "bf616a",
      base09: "d08770",
      base0A: "ebcb8b",
      base0B: "a3be8c",
      base0C: "88c0d0",
      base0D: "81a1c1",
      base0E: "b48ead",
      base0F: "5e81ac",
    },
  });
};

export const GetDefaultTheme = () =>
  Promise.resolve({
    system: "base16",
    name: "Iceberg",
    author: "cocopon",
    slug: "iceberg",
    variant: "dark",
    palette: {
      base00: "161821",
      base01: "1e2132",
      base02: "2a3158",
      base03: "6b7089",
      base04: "818596",
      base05: "c6c8d1",
      base06: "d2d4de",
      base07: "e8e9f0",
      base08: "e27878",
      base09: "e2a478",
      base0A: "e2a478",
      base0B: "b4be82",
      base0C: "89b8c2",
      base0D: "84a0c6",
      base0E: "a093c7",
      base0F: "b4a382",
    },
  });

export const SaveCustomTheme = (theme, suggestedPath) => {
  const filepath = suggestedPath || `/tmp/${theme.slug}-custom.yaml`;
  return Promise.resolve(filepath);
};
