package service

import (
	"crypto/md5"
	"fmt"
	"io/fs"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"notes/backend/internal/domain"

	"github.com/fsnotify/fsnotify"
)

// FilesystemService handles workspace filesystem operations.
// It manages opening workspaces, loading Markdown files, and watching for changes.
type FilesystemService struct {
	mu               sync.RWMutex
	currentWorkspace *domain.Workspace
	watcher          *fsnotify.Watcher
	eventChan        chan FileEvent
	stopChan         chan struct{}
}

// FileEvent represents a filesystem change event.
type FileEvent struct {
	Path      string
	Operation FileOperation
	Timestamp time.Time
}

// FileOperation categorizes filesystem change types.
type FileOperation string

const (
	FileOpCreate FileOperation = "create"
	FileOpModify FileOperation = "modify"
	FileOpDelete FileOperation = "delete"
	FileOpRename FileOperation = "rename"
)

// NewFilesystemService creates a new filesystem service.
func NewFilesystemService() (*FilesystemService, error) {
	watcher, err := fsnotify.NewWatcher()
	if err != nil {
		return nil, fmt.Errorf("failed to create filesystem watcher: %w", err)
	}

	return &FilesystemService{
		watcher:   watcher,
		eventChan: make(chan FileEvent, 100),
		stopChan:  make(chan struct{}),
	}, nil
}

// OpenWorkspace opens or creates a workspace at the specified path.
// Returns workspace information and begins filesystem watching.
func (s *FilesystemService) OpenWorkspace(path string) (*domain.WorkspaceInfo, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	// Validate path
	absPath, err := filepath.Abs(path)
	if err != nil {
		return nil, &domain.ErrInvalidPath{Path: path, Reason: err.Error()}
	}

	// Check if directory exists
	info, err := os.Stat(absPath)
	if err != nil {
		if os.IsNotExist(err) {
			// Create workspace directory
			if err := os.MkdirAll(absPath, 0755); err != nil {
				return nil, fmt.Errorf("failed to create workspace directory: %w", err)
			}
		} else {
			return nil, fmt.Errorf("failed to access workspace path: %w", err)
		}
	} else if !info.IsDir() {
		return nil, &domain.ErrInvalidPath{Path: path, Reason: "not a directory"}
	}

	// Create workspace object
	workspaceID := generateWorkspaceID(absPath)
	workspace := &domain.Workspace{
		ID:             workspaceID,
		Name:           filepath.Base(absPath),
		RootPath:       absPath,
		IgnorePatterns: defaultIgnorePatterns(),
		CreatedAt:      time.Now(),
		LastOpenedAt:   time.Now(),
	}

	// Count notes
	noteCount, err := s.countMarkdownFiles(absPath, workspace.IgnorePatterns)
	if err != nil {
		return nil, fmt.Errorf("failed to count notes: %w", err)
	}

	// Close existing workspace if any
	if s.currentWorkspace != nil {
		s.stopWatching()
	}

	s.currentWorkspace = workspace

	// Start watching filesystem
	if err := s.startWatching(absPath); err != nil {
		return nil, fmt.Errorf("failed to start filesystem watcher: %w", err)
	}

	return &domain.WorkspaceInfo{
		Workspace: *workspace,
		Config: domain.WorkspaceConfig{
			DailyNoteFormat: "2006-01-02",
			DailyNoteFolder: "",
			DefaultTags:     []string{},
		},
		NoteCount:   noteCount,
		TotalBlocks: 0, // Will be calculated during indexing
	}, nil
}

// GetCurrentWorkspace returns the currently open workspace, or error if none is open.
func (s *FilesystemService) GetCurrentWorkspace() (*domain.Workspace, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.currentWorkspace == nil {
		return nil, &domain.ErrWorkspaceNotOpen{}
	}

	return s.currentWorkspace, nil
}

// LoadMarkdownFiles scans the workspace and returns all Markdown file paths.
func (s *FilesystemService) LoadMarkdownFiles() ([]string, error) {
	workspace, err := s.GetCurrentWorkspace()
	if err != nil {
		return nil, err
	}

	var files []string
	err = filepath.WalkDir(workspace.RootPath, func(path string, d fs.DirEntry, err error) error {
		if err != nil {
			return err
		}

		// Skip ignored patterns
		if s.shouldIgnore(path, workspace.IgnorePatterns) {
			if d.IsDir() {
				return filepath.SkipDir
			}
			return nil
		}

		// Include only Markdown files
		if !d.IsDir() && isMarkdownFile(path) {
			// Store relative path
			relPath, err := filepath.Rel(workspace.RootPath, path)
			if err != nil {
				return err
			}
			files = append(files, relPath)
		}

		return nil
	})

	if err != nil {
		return nil, fmt.Errorf("failed to load markdown files: %w", err)
	}

	return files, nil
}

// ReadFile reads a file from the workspace.
func (s *FilesystemService) ReadFile(relativePath string) ([]byte, error) {
	workspace, err := s.GetCurrentWorkspace()
	if err != nil {
		return nil, err
	}

	fullPath := filepath.Join(workspace.RootPath, relativePath)

	// Validate that path is within workspace (prevent directory traversal)
	if !strings.HasPrefix(fullPath, workspace.RootPath) {
		return nil, &domain.ErrInvalidPath{Path: relativePath, Reason: "path outside workspace"}
	}

	content, err := os.ReadFile(fullPath)
	if err != nil {
		if os.IsNotExist(err) {
			return nil, &domain.ErrNotFound{Resource: "file", ID: relativePath}
		}
		return nil, fmt.Errorf("failed to read file: %w", err)
	}

	return content, nil
}

// WriteFile writes content to a file in the workspace.
func (s *FilesystemService) WriteFile(relativePath string, content []byte) error {
	workspace, err := s.GetCurrentWorkspace()
	if err != nil {
		return err
	}

	fullPath := filepath.Join(workspace.RootPath, relativePath)

	// Validate that path is within workspace
	if !strings.HasPrefix(fullPath, workspace.RootPath) {
		return &domain.ErrInvalidPath{Path: relativePath, Reason: "path outside workspace"}
	}

	// Ensure directory exists
	dir := filepath.Dir(fullPath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("failed to create directory: %w", err)
	}

	// Write file
	if err := os.WriteFile(fullPath, content, 0644); err != nil {
		return fmt.Errorf("failed to write file: %w", err)
	}

	return nil
}

// DeleteFile removes a file from the workspace.
func (s *FilesystemService) DeleteFile(relativePath string) error {
	workspace, err := s.GetCurrentWorkspace()
	if err != nil {
		return err
	}

	fullPath := filepath.Join(workspace.RootPath, relativePath)

	// Validate that path is within workspace
	if !strings.HasPrefix(fullPath, workspace.RootPath) {
		return &domain.ErrInvalidPath{Path: relativePath, Reason: "path outside workspace"}
	}

	if err := os.Remove(fullPath); err != nil {
		if os.IsNotExist(err) {
			return &domain.ErrNotFound{Resource: "file", ID: relativePath}
		}
		return fmt.Errorf("failed to delete file: %w", err)
	}

	return nil
}

// Events returns the channel for filesystem events.
func (s *FilesystemService) Events() <-chan FileEvent {
	return s.eventChan
}

// Close stops the filesystem service and releases resources.
func (s *FilesystemService) Close() error {
	s.mu.Lock()
	defer s.mu.Unlock()

	s.stopWatching()

	if s.watcher != nil {
		return s.watcher.Close()
	}

	return nil
}

// startWatching begins filesystem watching for the workspace.
func (s *FilesystemService) startWatching(rootPath string) error {
	// Add root directory to watcher
	if err := s.addWatchRecursive(rootPath); err != nil {
		return err
	}

	// Start event processing goroutine
	go s.processEvents()

	return nil
}

// stopWatching stops filesystem watching.
func (s *FilesystemService) stopWatching() {
	if s.stopChan != nil {
		close(s.stopChan)
		s.stopChan = make(chan struct{})
	}
}

// addWatchRecursive adds watches for directory and all subdirectories.
func (s *FilesystemService) addWatchRecursive(root string) error {
	return filepath.WalkDir(root, func(path string, d fs.DirEntry, err error) error {
		if err != nil {
			return err
		}

		if d.IsDir() {
			// Skip ignored directories
			if s.shouldIgnore(path, s.currentWorkspace.IgnorePatterns) {
				return filepath.SkipDir
			}

			if err := s.watcher.Add(path); err != nil {
				return fmt.Errorf("failed to watch %s: %w", path, err)
			}
		}

		return nil
	})
}

// processEvents handles filesystem events from fsnotify.
func (s *FilesystemService) processEvents() {
	for {
		select {
		case <-s.stopChan:
			return
		case event, ok := <-s.watcher.Events:
			if !ok {
				return
			}

			// Convert fsnotify event to our event type
			var op FileOperation
			switch {
			case event.Op&fsnotify.Create == fsnotify.Create:
				op = FileOpCreate
				// If a new directory was created, watch it
				if info, err := os.Stat(event.Name); err == nil && info.IsDir() {
					s.addWatchRecursive(event.Name)
				}
			case event.Op&fsnotify.Write == fsnotify.Write:
				op = FileOpModify
			case event.Op&fsnotify.Remove == fsnotify.Remove:
				op = FileOpDelete
			case event.Op&fsnotify.Rename == fsnotify.Rename:
				op = FileOpRename
			default:
				continue
			}

			// Only emit events for Markdown files
			if isMarkdownFile(event.Name) {
				relPath, err := filepath.Rel(s.currentWorkspace.RootPath, event.Name)
				if err == nil {
					s.eventChan <- FileEvent{
						Path:      relPath,
						Operation: op,
						Timestamp: time.Now(),
					}
				}
			}

		case err, ok := <-s.watcher.Errors:
			if !ok {
				return
			}
			// Log error (in production, would use proper logging)
			fmt.Printf("Filesystem watcher error: %v\n", err)
		}
	}
}

// shouldIgnore checks if a path matches any ignore patterns.
func (s *FilesystemService) shouldIgnore(path string, patterns []string) bool {
	base := filepath.Base(path)
	for _, pattern := range patterns {
		if matched, _ := filepath.Match(pattern, base); matched {
			return true
		}
		// Also check if the base name starts with the pattern (for dotfiles)
		if strings.HasPrefix(base, pattern) {
			return true
		}
	}
	return false
}

// countMarkdownFiles counts Markdown files in a directory.
func (s *FilesystemService) countMarkdownFiles(root string, ignorePatterns []string) (int, error) {
	count := 0
	err := filepath.WalkDir(root, func(path string, d fs.DirEntry, err error) error {
		if err != nil {
			return err
		}

		if s.shouldIgnore(path, ignorePatterns) {
			if d.IsDir() {
				return filepath.SkipDir
			}
			return nil
		}

		if !d.IsDir() && isMarkdownFile(path) {
			count++
		}

		return nil
	})

	return count, err
}

// isMarkdownFile checks if a file has a Markdown extension.
func isMarkdownFile(path string) bool {
	ext := strings.ToLower(filepath.Ext(path))
	return ext == ".md" || ext == ".markdown"
}

// generateWorkspaceID creates a unique identifier for a workspace based on its path.
func generateWorkspaceID(path string) string {
	hash := md5.Sum([]byte(path))
	return fmt.Sprintf("%x", hash)
}

// defaultIgnorePatterns returns common patterns to ignore in workspaces.
func defaultIgnorePatterns() []string {
	return []string{
		".git",
		".obsidian",
		".logseq",
		"node_modules",
		".DS_Store",
		"*.tmp",
	}
}
