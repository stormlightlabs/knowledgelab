/**
 * Mock Wails API for testing
 * */

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
      LastWorkspacePath: "/mock/workspace/directory",
      GraphLayout: "force",
    },
  });
