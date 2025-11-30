package paths

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"github.com/adrg/xdg"
)

// UserConfigDir returns the platform-appropriate user configuration directory for the application.
// Creates the directory with 0700 permissions if it doesn't exist.
func UserConfigDir(appName string) (string, error) {
	if appName == "" {
		return "", fmt.Errorf("appName cannot be empty")
	}

	configDir := filepath.Join(xdg.ConfigHome, appName)

	// Create directory with user-only permissions
	if err := os.MkdirAll(configDir, 0700); err != nil {
		return "", fmt.Errorf("failed to create user config directory: %w", err)
	}

	return configDir, nil
}

// WorkspaceConfigDir returns the workspace configuration directory path.
// This directory lives inside the workspace root and is intended for portable, version-controllable configuration.
// Creates the directory with 0755 permissions if it doesn't exist.
func WorkspaceConfigDir(workspaceRoot, appName string) (string, error) {
	if workspaceRoot == "" {
		return "", fmt.Errorf("workspaceRoot cannot be empty")
	}
	if appName == "" {
		return "", fmt.Errorf("appName cannot be empty")
	}

	lowerName := strings.ToLower(appName)
	dirName := lowerName
	if !strings.HasPrefix(lowerName, ".") {
		dirName = "." + lowerName
	}

	configDir := filepath.Join(workspaceRoot, dirName)

	if err := os.MkdirAll(configDir, 0755); err != nil {
		return "", fmt.Errorf("failed to create workspace config directory: %w", err)
	}

	return configDir, nil
}
