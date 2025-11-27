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
