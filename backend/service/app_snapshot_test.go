package service

import (
	"os"
	"path/filepath"
	"testing"
)

func TestDefaultAppSnapshot(t *testing.T) {
	snapshot := DefaultAppSnapshot()

	if snapshot.LastWorkspacePath != "" {
		t.Errorf("DefaultAppSnapshot().LastWorkspacePath = %q, want empty string", snapshot.LastWorkspacePath)
	}
}

func TestLoadAppSnapshot_NonExistent(t *testing.T) {
	tempDir := t.TempDir()
	nonExistentPath := filepath.Join(tempDir, "app.toml")

	snapshot, err := LoadAppSnapshot(nonExistentPath)
	if err != nil {
		t.Fatalf("LoadAppSnapshot() error = %v, want nil for non-existent file", err)
	}

	if snapshot.LastWorkspacePath != "" {
		t.Errorf("LoadAppSnapshot() returned non-default snapshot for non-existent file")
	}
}

func TestLoadAppSnapshot_Valid(t *testing.T) {
	tempDir := t.TempDir()
	snapshotPath := filepath.Join(tempDir, "app.toml")

	original := AppSnapshot{
		LastWorkspacePath: "/Users/test/workspace1",
	}

	if err := SaveAppSnapshot(snapshotPath, original); err != nil {
		t.Fatalf("SaveAppSnapshot() error = %v", err)
	}

	loaded, err := LoadAppSnapshot(snapshotPath)
	if err != nil {
		t.Fatalf("LoadAppSnapshot() error = %v", err)
	}

	if loaded.LastWorkspacePath != original.LastWorkspacePath {
		t.Errorf("LoadAppSnapshot().LastWorkspacePath = %q, want %q", loaded.LastWorkspacePath, original.LastWorkspacePath)
	}
}

func TestLoadAppSnapshot_InvalidTOML(t *testing.T) {
	tempDir := t.TempDir()
	snapshotPath := filepath.Join(tempDir, "app.toml")

	if err := os.WriteFile(snapshotPath, []byte("invalid toml content {{{"), 0644); err != nil {
		t.Fatalf("os.WriteFile() error = %v", err)
	}

	_, err := LoadAppSnapshot(snapshotPath)
	if err == nil {
		t.Fatal("LoadAppSnapshot() error = nil, want error for invalid TOML")
	}
}

func TestSaveAppSnapshot_CreatesFile(t *testing.T) {
	tempDir := t.TempDir()
	snapshotPath := filepath.Join(tempDir, "app.toml")

	snapshot := AppSnapshot{
		LastWorkspacePath: "/Users/test/workspace2",
	}

	err := SaveAppSnapshot(snapshotPath, snapshot)
	if err != nil {
		t.Fatalf("SaveAppSnapshot() error = %v", err)
	}

	if _, err := os.Stat(snapshotPath); os.IsNotExist(err) {
		t.Errorf("SaveAppSnapshot() did not create file at %q", snapshotPath)
	}

	loaded, err := LoadAppSnapshot(snapshotPath)
	if err != nil {
		t.Fatalf("LoadAppSnapshot() error = %v", err)
	}

	if loaded.LastWorkspacePath != snapshot.LastWorkspacePath {
		t.Errorf("Saved and loaded LastWorkspacePath mismatch: got %q, want %q", loaded.LastWorkspacePath, snapshot.LastWorkspacePath)
	}
}

func TestSaveAppSnapshot_Overwrite(t *testing.T) {
	tempDir := t.TempDir()
	snapshotPath := filepath.Join(tempDir, "app.toml")

	first := AppSnapshot{
		LastWorkspacePath: "/Users/test/workspace1",
	}

	if err := SaveAppSnapshot(snapshotPath, first); err != nil {
		t.Fatalf("SaveAppSnapshot() first save error = %v", err)
	}

	second := AppSnapshot{
		LastWorkspacePath: "/Users/test/workspace2",
	}

	if err := SaveAppSnapshot(snapshotPath, second); err != nil {
		t.Fatalf("SaveAppSnapshot() second save error = %v", err)
	}

	loaded, err := LoadAppSnapshot(snapshotPath)
	if err != nil {
		t.Fatalf("LoadAppSnapshot() error = %v", err)
	}

	if loaded.LastWorkspacePath != second.LastWorkspacePath {
		t.Errorf("LoadAppSnapshot() after overwrite = %q, want %q", loaded.LastWorkspacePath, second.LastWorkspacePath)
	}
}

func TestSaveAppSnapshot_InvalidPath(t *testing.T) {
	invalidPath := "/nonexistent/directory/app.toml"

	snapshot := AppSnapshot{
		LastWorkspacePath: "/Users/test/workspace",
	}

	err := SaveAppSnapshot(invalidPath, snapshot)
	if err == nil {
		t.Error("SaveAppSnapshot() with invalid path error = nil, want error")
	}
}

func TestAppSnapshot_RoundTrip(t *testing.T) {
	tests := []struct {
		name     string
		snapshot AppSnapshot
	}{
		{
			name: "empty path",
			snapshot: AppSnapshot{
				LastWorkspacePath: "",
			},
		},
		{
			name: "absolute Unix path",
			snapshot: AppSnapshot{
				LastWorkspacePath: "/Users/test/Documents/notes",
			},
		},
		{
			name: "absolute Windows path",
			snapshot: AppSnapshot{
				LastWorkspacePath: "C:\\Users\\test\\Documents\\notes",
			},
		},
		{
			name: "path with spaces",
			snapshot: AppSnapshot{
				LastWorkspacePath: "/Users/test/My Documents/my notes",
			},
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			tempDir := t.TempDir()
			snapshotPath := filepath.Join(tempDir, "app.toml")

			if err := SaveAppSnapshot(snapshotPath, tt.snapshot); err != nil {
				t.Fatalf("SaveAppSnapshot() error = %v", err)
			}

			loaded, err := LoadAppSnapshot(snapshotPath)
			if err != nil {
				t.Fatalf("LoadAppSnapshot() error = %v", err)
			}

			if loaded.LastWorkspacePath != tt.snapshot.LastWorkspacePath {
				t.Errorf("Round-trip LastWorkspacePath = %q, want %q", loaded.LastWorkspacePath, tt.snapshot.LastWorkspacePath)
			}
		})
	}
}
