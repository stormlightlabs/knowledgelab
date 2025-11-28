# Status Update

Write a comprehensive status update to `STATUS.md` at the root of the repository.

## Purpose

This command prepares the repository for a new agent/contributor by documenting the project's current state. It should be run before clearing agent memory so the next session can quickly understand the vision and technical state.

## Status Update Structure

The status update should be organized into three vision layers:

### 1. Long-Term Vision

The original inspiration and ultimate goal:

- Why this project exists
- What problem it solves (Open Source alternative to Obsidian.md, with LogSeq inspired features)
- The dream state of the application

### 2. Medium-Term Vision

Where we're heading in the next few months:

- Core features to build
- Technical milestones
- User experience improvements

### 3. Short-Term Vision

Immediate next steps and current priorities:

- Active work streams
- Known issues to fix
- Performance improvements
- Documentation needs

## Technical State

Document the current implementation:

### Architecture

- Backend: language, framework, hosting, database
- Frontend: framework, runtime, hosting, styling approach
- Storage: where markdown files live
- Deployment: CI/CD setup, cross-platform builds

### What's Working

- List functioning features
- Note deployment status
- Highlight successful integrations

### What's In Progress

- Active development areas
- Features being built
- Refactors underway

### Known Issues

- Technical debt
- Bugs
- Performance problems
- Missing features

### Technical Decisions

Why certain choices were made:

- Why F#/Wails+Go vs alternatives (e.g., TS/Tauri+Rust)
- Why current deployment architecture
- Trade-offs accepted
- Future flexibility considerations

## Instructions

1. Read the current `README.md`, `AGENTS.md`, `CONTRIBUTING.md`, recent status updates in `sandbox/`, and key source files
2. Check `git status` and recent commits to understand current work
3. Scan the codebase structure (`backend/`, `app.go`, `main.go`, `frontend/`)
4. Review the tech stack in `go.mod`, `wails.json`, `frontend/package.json`
5. Synthesize into a clear, chronological narrative
6. Write to `STATUS.md` in the root directory
7. Update this timestamp in the file: `Status as of: YYYY-MM-DD HH:MM UTC` (ensure this is correct using the unix date util)

## Tone

- Direct, technical, and succinct
- Honest about limitations and trade-offs
- Optimistic about vision
- Practical about current state
- Useful for someone with no prior context
