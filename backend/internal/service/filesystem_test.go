package service

import (
	"os"
	"path/filepath"
	"testing"
	"time"
)

func TestFilesystemService_OpenWorkspace(t *testing.T) {
	tests := []struct {
		name    string
		setup   func() string
		wantErr bool
	}{
		{
			name: "create new workspace",
			setup: func() string {
				tmpDir := filepath.Join(os.TempDir(), "test-workspace-new")
				os.RemoveAll(tmpDir) // Ensure clean state
				return tmpDir
			},
			wantErr: false,
		},
		{
			name: "open existing workspace",
			setup: func() string {
				tmpDir := filepath.Join(os.TempDir(), "test-workspace-existing")
				os.MkdirAll(tmpDir, 0755)
				return tmpDir
			},
			wantErr: false,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			path := tt.setup()
			defer os.RemoveAll(path)

			fs, err := NewFilesystemService()
			if err != nil {
				t.Fatalf("NewFilesystemService() error = %v", err)
			}
			defer fs.Close()

			workspaceInfo, err := fs.OpenWorkspace(path)
			if (err != nil) != tt.wantErr {
				t.Errorf("OpenWorkspace() error = %v, wantErr %v", err, tt.wantErr)
				return
			}

			if !tt.wantErr {
				if workspaceInfo == nil {
					t.Error("OpenWorkspace() returned nil workspaceInfo")
					return
				}

				if workspaceInfo.Workspace.RootPath != path {
					t.Errorf("Workspace.RootPath = %v, want %v", workspaceInfo.Workspace.RootPath, path)
				}

				// Verify directory was created
				if _, err := os.Stat(path); os.IsNotExist(err) {
					t.Error("Workspace directory was not created")
				}
			}
		})
	}
}

func TestFilesystemService_LoadMarkdownFiles(t *testing.T) {
	// Create temp workspace with test files
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-load")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	os.MkdirAll(tmpDir, 0755)

	// Create test files
	testFiles := []string{
		"note1.md",
		"note2.markdown",
		"folder/note3.md",
		".obsidian/config.json", // Should be ignored
		"README.txt",            // Should be ignored (not markdown)
	}

	for _, file := range testFiles {
		fullPath := filepath.Join(tmpDir, file)
		os.MkdirAll(filepath.Dir(fullPath), 0755)
		os.WriteFile(fullPath, []byte("test content"), 0644)
	}

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	files, err := fs.LoadMarkdownFiles()
	if err != nil {
		t.Fatalf("LoadMarkdownFiles() error = %v", err)
	}

	// Should find 3 markdown files (note1, note2, folder/note3)
	expectedCount := 3
	if len(files) != expectedCount {
		t.Errorf("LoadMarkdownFiles() found %d files, want %d", len(files), expectedCount)
	}

	// Verify files are relative paths
	for _, file := range files {
		if filepath.IsAbs(file) {
			t.Errorf("LoadMarkdownFiles() returned absolute path: %s", file)
		}
	}
}

func TestFilesystemService_ReadWriteDelete(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-rwd")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	testPath := "test.md"
	testContent := []byte("# Test Note\n\nThis is a test.")

	// Test Write
	err = fs.WriteFile(testPath, testContent)
	if err != nil {
		t.Fatalf("WriteFile() error = %v", err)
	}

	// Test Read
	content, err := fs.ReadFile(testPath)
	if err != nil {
		t.Fatalf("ReadFile() error = %v", err)
	}

	if string(content) != string(testContent) {
		t.Errorf("ReadFile() content = %q, want %q", string(content), string(testContent))
	}

	// Test Delete
	err = fs.DeleteFile(testPath)
	if err != nil {
		t.Fatalf("DeleteFile() error = %v", err)
	}

	_, err = fs.ReadFile(testPath)
	if err == nil {
		t.Error("ReadFile() should error after delete")
	}
}

func TestFilesystemService_Events(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-events")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	// Create a file and wait for event
	testPath := "event-test.md"
	done := make(chan bool, 1)

	go func() {
		select {
		case event := <-fs.Events():
			if event.Path == testPath && event.Operation == FileOpCreate {
				done <- true
			}
		case <-time.After(2 * time.Second):
			done <- false
		}
	}()

	time.Sleep(100 * time.Millisecond) // Give watcher time to start
	fs.WriteFile(testPath, []byte("test"))

	if success := <-done; !success {
		t.Error("Did not receive create event")
	}
}

func TestFilesystemService_PathTraversal(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-security")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	// Try to access file outside workspace
	_, err = fs.ReadFile("../../etc/passwd")
	if err == nil {
		t.Error("ReadFile() should prevent path traversal")
	}

	// Try to write file outside workspace
	err = fs.WriteFile("../../malicious.md", []byte("bad"))
	if err == nil {
		t.Error("WriteFile() should prevent path traversal")
	}
}
