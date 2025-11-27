// Error types for domain operations.
package domain

import "fmt"

// ErrNotFound indicates that a requested resource was not found.
type ErrNotFound struct {
	Resource string // Resource type (e.g., "note", "workspace", "block")
	ID       string // Resource identifier
}

func (e *ErrNotFound) Error() string {
	return fmt.Sprintf("%s not found: %s", e.Resource, e.ID)
}

// ErrInvalidPath indicates an invalid or malformed file path.
type ErrInvalidPath struct {
	Path   string
	Reason string
}

func (e *ErrInvalidPath) Error() string {
	return fmt.Sprintf("invalid path %q: %s", e.Path, e.Reason)
}

// ErrWorkspaceNotOpen indicates an operation was attempted without an open workspace.
type ErrWorkspaceNotOpen struct{}

func (e *ErrWorkspaceNotOpen) Error() string {
	return "no workspace is currently open"
}

// ErrInvalidFrontmatter indicates frontmatter parsing failure.
type ErrInvalidFrontmatter struct {
	Path   string
	Reason string
}

func (e *ErrInvalidFrontmatter) Error() string {
	return fmt.Sprintf("invalid frontmatter in %q: %s", e.Path, e.Reason)
}

// ErrAlreadyExists indicates a resource already exists.
type ErrAlreadyExists struct {
	Resource string
	ID       string
}

func (e *ErrAlreadyExists) Error() string {
	return fmt.Sprintf("%s already exists: %s", e.Resource, e.ID)
}
