package paths

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestUserConfigDir(t *testing.T) {
	tests := []struct {
		name        string
		appName     string
		shouldError bool
		checkFunc   func(t *testing.T, result string)
	}{
		{
			name:        "empty app name",
			appName:     "",
			shouldError: true,
		},
		{
			name:        "normal operation",
			appName:     "testapp",
			shouldError: false,
			checkFunc: func(t *testing.T, result string) {
				if !strings.HasSuffix(result, "testapp") {
					t.Errorf("expected path to end with 'testapp', got %s", result)
				}
				if !filepath.IsAbs(result) {
					t.Errorf("expected absolute path, got %s", result)
				}
			},
		},
		{
			name:        "different app name",
			appName:     "KnowledgeLab",
			shouldError: false,
			checkFunc: func(t *testing.T, result string) {
				if !strings.HasSuffix(result, "KnowledgeLab") {
					t.Errorf("expected path to end with 'KnowledgeLab', got %s", result)
				}
			},
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result, err := UserConfigDir(tt.appName)

			if tt.shouldError {
				if err == nil {
					t.Errorf("expected error, got nil")
				}
				return
			}

			if err != nil {
				t.Fatalf("unexpected error: %v", err)
			}

			if tt.checkFunc != nil {
				tt.checkFunc(t, result)
			}

			if _, err := os.Stat(result); os.IsNotExist(err) {
				t.Errorf("directory was not created: %s", result)
			}

			info, err := os.Stat(result)
			if err != nil {
				t.Fatalf("failed to stat directory: %v", err)
			}
			mode := info.Mode().Perm()
			if mode != 0700 {
				t.Errorf("expected permissions 0700, got %o", mode)
			}
		})
	}
}

func TestUserConfigDir_Idempotent(t *testing.T) {
	appName := "testapp"

	result1, err := UserConfigDir(appName)
	if err != nil {
		t.Fatalf("first call failed: %v", err)
	}

	result2, err := UserConfigDir(appName)
	if err != nil {
		t.Fatalf("second call failed: %v", err)
	}

	if result1 != result2 {
		t.Errorf("results differ: %s != %s", result1, result2)
	}

	info, err := os.Stat(result2)
	if err != nil {
		t.Fatalf("directory doesn't exist after second call: %v", err)
	}
	if info.Mode().Perm() != 0700 {
		t.Errorf("permissions changed after second call: %o", info.Mode().Perm())
	}
}

func TestWorkspaceConfigDir(t *testing.T) {
	tests := []struct {
		name          string
		workspaceRoot string
		appName       string
		shouldError   bool
		expectedDir   string
	}{
		{
			name:          "empty workspace root",
			workspaceRoot: "",
			appName:       "testapp",
			shouldError:   true,
		},
		{
			name:          "empty app name",
			workspaceRoot: t.TempDir(),
			appName:       "",
			shouldError:   true,
		},
		{
			name:          "normal operation",
			workspaceRoot: t.TempDir(),
			appName:       "KnowledgeLab",
			shouldError:   false,
			expectedDir:   ".knowledgelab",
		},
		{
			name:          "lowercase conversion",
			workspaceRoot: t.TempDir(),
			appName:       "MyApp",
			shouldError:   false,
			expectedDir:   ".myapp",
		},
		{
			name:          "already has dot prefix",
			workspaceRoot: t.TempDir(),
			appName:       ".myapp",
			shouldError:   false,
			expectedDir:   ".myapp", // Already has dot, don't add another
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result, err := WorkspaceConfigDir(tt.workspaceRoot, tt.appName)

			if tt.shouldError {
				if err == nil {
					t.Errorf("expected error, got nil")
				}
				return
			}

			if err != nil {
				t.Fatalf("unexpected error: %v", err)
			}

			if tt.expectedDir != "" {
				expectedPath := filepath.Join(tt.workspaceRoot, tt.expectedDir)
				if result != expectedPath {
					t.Errorf("expected %s, got %s", expectedPath, result)
				}
			}

			if _, err := os.Stat(result); os.IsNotExist(err) {
				t.Errorf("directory was not created: %s", result)
			}

			info, err := os.Stat(result)
			if err != nil {
				t.Fatalf("failed to stat directory: %v", err)
			}
			mode := info.Mode().Perm()
			if mode != 0755 {
				t.Errorf("expected permissions 0755, got %o", mode)
			}

			relPath, err := filepath.Rel(tt.workspaceRoot, result)
			if err != nil {
				t.Fatalf("failed to get relative path: %v", err)
			}
			if filepath.IsAbs(relPath) || strings.HasPrefix(relPath, "..") {
				t.Errorf("result is not under workspace root: %s", result)
			}
		})
	}
}

func TestWorkspaceConfigDir_Idempotent(t *testing.T) {
	workspaceRoot := t.TempDir()
	appName := "testapp"

	result1, err := WorkspaceConfigDir(workspaceRoot, appName)
	if err != nil {
		t.Fatalf("first call failed: %v", err)
	}

	result2, err := WorkspaceConfigDir(workspaceRoot, appName)
	if err != nil {
		t.Fatalf("second call failed: %v", err)
	}

	if result1 != result2 {
		t.Errorf("results differ: %s != %s", result1, result2)
	}

	info, err := os.Stat(result2)
	if err != nil {
		t.Fatalf("directory doesn't exist after second call: %v", err)
	}
	if info.Mode().Perm() != 0755 {
		t.Errorf("permissions changed after second call: %o", info.Mode().Perm())
	}
}

func TestWorkspaceConfigDir_NestedWorkspaces(t *testing.T) {
	tempDir := t.TempDir()

	outer := filepath.Join(tempDir, "outer-workspace")
	inner := filepath.Join(outer, "notes", "inner-workspace")

	if err := os.MkdirAll(inner, 0755); err != nil {
		t.Fatalf("failed to create nested directories: %v", err)
	}

	outerConfig, err := WorkspaceConfigDir(outer, "testapp")
	if err != nil {
		t.Fatalf("failed to create outer config: %v", err)
	}

	innerConfig, err := WorkspaceConfigDir(inner, "testapp")
	if err != nil {
		t.Fatalf("failed to create inner config: %v", err)
	}

	if outerConfig == innerConfig {
		t.Errorf("outer and inner configs should be different")
	}

	expectedOuter := filepath.Join(outer, ".testapp")
	if outerConfig != expectedOuter {
		t.Errorf("outer config: expected %s, got %s", expectedOuter, outerConfig)
	}

	expectedInner := filepath.Join(inner, ".testapp")
	if innerConfig != expectedInner {
		t.Errorf("inner config: expected %s, got %s", expectedInner, innerConfig)
	}
}

func TestUserConfigDir_UsesXDGPackage(t *testing.T) {
	appName := "testapp"

	result, err := UserConfigDir(appName)
	if err != nil {
		t.Fatalf("UserConfigDir failed: %v", err)
	}

	if !filepath.IsAbs(result) {
		t.Errorf("expected absolute path from xdg package, got %s", result)
	}

	if !strings.HasSuffix(result, appName) {
		t.Errorf("expected path to end with %s, got %s", appName, result)
	}

	if _, err := os.Stat(result); os.IsNotExist(err) {
		t.Errorf("directory not created: %s", result)
	}
}

func TestWorkspaceConfigDir_CaseConversion(t *testing.T) {
	tests := []struct {
		input    string
		expected string
	}{
		{"KnowledgeLab", ".knowledgelab"},
		{"UPPERCASE", ".uppercase"},
		{"MixedCase", ".mixedcase"},
		{"alreadylower", ".alreadylower"},
	}

	for _, tt := range tests {
		t.Run(tt.input, func(t *testing.T) {
			workspaceRoot := t.TempDir()
			result, err := WorkspaceConfigDir(workspaceRoot, tt.input)
			if err != nil {
				t.Fatalf("WorkspaceConfigDir failed: %v", err)
			}

			expectedPath := filepath.Join(workspaceRoot, tt.expected)
			if result != expectedPath {
				t.Errorf("expected %s, got %s", expectedPath, result)
			}
		})
	}
}
