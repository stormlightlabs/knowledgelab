package service

import (
	"os"
	"path/filepath"
	"testing"
	"time"

	"notes/backend/domain"
)

func TestExtractTasks(t *testing.T) {
	tests := []struct {
		name          string
		content       string
		expectedCount int
		expectedTasks []struct {
			content     string
			isCompleted bool
		}
	}{
		{
			name: "single unchecked task",
			content: `# My Note

- [ ] Buy milk

Some text`,
			expectedCount: 1,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{
				{content: "Buy milk", isCompleted: false},
			},
		},
		{
			name: "single completed task",
			content: `# Tasks

- [x] Done task`,
			expectedCount: 1,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{
				{content: "Done task", isCompleted: true},
			},
		},
		{
			name: "mixed tasks",
			content: `# TODO

- [ ] Task 1
- [x] Task 2
- [ ] Task 3`,
			expectedCount: 3,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{
				{content: "Task 1", isCompleted: false},
				{content: "Task 2", isCompleted: true},
				{content: "Task 3", isCompleted: false},
			},
		},
		{
			name: "tasks with block IDs",
			content: `- [ ] Task with ID ^task-1
- [x] Another task ^task-2`,
			expectedCount: 2,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{
				{content: "Task with ID", isCompleted: false},
				{content: "Another task", isCompleted: true},
			},
		},
		{
			name:          "uppercase X completion",
			content:       `- [X] Task completed with uppercase X`,
			expectedCount: 1,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{
				{content: "Task completed with uppercase X", isCompleted: true},
			},
		},
		{
			name: "tasks with frontmatter",
			content: `---
title: My Tasks
tags: [work, important]
---

- [ ] Work task
- [x] Completed work`,
			expectedCount: 2,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{
				{content: "Work task", isCompleted: false},
				{content: "Completed work", isCompleted: true},
			},
		},
		{
			name: "non-task list items ignored",
			content: `# Lists

- Regular list item
* Another regular item
- [ ] Actual task
- Not a task either`,
			expectedCount: 1,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{
				{content: "Actual task", isCompleted: false},
			},
		},
		{
			name: "tasks with extra whitespace",
			content: `-  [  ]  Task with extra spaces
-  [x]  Another with spaces`,
			expectedCount: 2,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{
				{content: "Task with extra spaces", isCompleted: false},
				{content: "Another with spaces", isCompleted: true},
			},
		},
		{
			name:          "empty content",
			content:       ``,
			expectedCount: 0,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{},
		},
		{
			name: "no tasks",
			content: `# Just a note

Some content here.
No tasks at all.`,
			expectedCount: 0,
			expectedTasks: []struct {
				content     string
				isCompleted bool
			}{},
		},
	}

	tmpDir := filepath.Join(os.TempDir(), "test-extract-tasks")
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

	ns := NewNoteService(fs)

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			tasks := ns.ExtractTasks("test-note", "/test.md", []byte(tt.content))

			if len(tasks) != tt.expectedCount {
				t.Errorf("expected %d tasks, got %d", tt.expectedCount, len(tasks))
			}

			for i, expected := range tt.expectedTasks {
				if i >= len(tasks) {
					t.Fatalf("expected task at index %d not found", i)
				}

				task := tasks[i]
				if task.Content != expected.content {
					t.Errorf("task %d: expected content %q, got %q", i, expected.content, task.Content)
				}
				if task.IsCompleted != expected.isCompleted {
					t.Errorf("task %d: expected isCompleted %v, got %v", i, expected.isCompleted, task.IsCompleted)
				}
				if task.NoteID != "test-note" {
					t.Errorf("task %d: expected noteID 'test-note', got %q", i, task.NoteID)
				}
				if task.NotePath != "/test.md" {
					t.Errorf("task %d: expected notePath '/test.md', got %q", i, task.NotePath)
				}
				if task.ID == "" {
					t.Errorf("task %d: expected non-empty ID", i)
				}
				if task.BlockID != task.ID {
					t.Errorf("task %d: BlockID should equal ID", i)
				}
			}
		})
	}
}

func TestTaskService_IndexNote(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-task-service")
	defer os.RemoveAll(tmpDir)

	stores, err := NewStores("test-app", "test-workspace")
	if err != nil {
		t.Fatalf("failed to create stores: %v", err)
	}
	defer stores.Close()

	taskService := NewTaskService(stores.Task)

	noteID := "note-1"
	notePath := "/tasks.md"
	now := time.Now()

	tasks := []domain.Task{
		{
			ID:          "task-1",
			BlockID:     "task-1",
			NoteID:      noteID,
			NotePath:    notePath,
			Content:     "Buy groceries",
			IsCompleted: false,
			CreatedAt:   now,
			LineNumber:  5,
		},
		{
			ID:          "task-2",
			BlockID:     "task-2",
			NoteID:      noteID,
			NotePath:    notePath,
			Content:     "Finish report",
			IsCompleted: true,
			CreatedAt:   now,
			CompletedAt: &now,
			LineNumber:  6,
		},
	}

	err = taskService.IndexNote(noteID, notePath, tasks, now)
	if err != nil {
		t.Fatalf("failed to index note: %v", err)
	}

	noteTasks, err := taskService.GetTasksForNote(noteID)
	if err != nil {
		t.Fatalf("failed to get tasks for note: %v", err)
	}

	if len(noteTasks) != 2 {
		t.Errorf("expected 2 tasks, got %d", len(noteTasks))
	}

	foundTask1 := false
	foundTask2 := false
	for _, task := range noteTasks {
		if task.ID == "task-1" {
			foundTask1 = true
			if task.Content != "Buy groceries" {
				t.Errorf("task-1: expected content 'Buy groceries', got %q", task.Content)
			}
			if task.IsCompleted {
				t.Error("task-1: expected not completed")
			}
		}
		if task.ID == "task-2" {
			foundTask2 = true
			if task.Content != "Finish report" {
				t.Errorf("task-2: expected content 'Finish report', got %q", task.Content)
			}
			if !task.IsCompleted {
				t.Error("task-2: expected completed")
			}
		}
	}

	if !foundTask1 {
		t.Error("task-1 not found")
	}
	if !foundTask2 {
		t.Error("task-2 not found")
	}
}

func TestTaskService_GetAllTasks(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-task-getall")
	defer os.RemoveAll(tmpDir)

	stores, err := NewStores("test-app", "test-workspace-2")
	if err != nil {
		t.Fatalf("failed to create stores: %v", err)
	}
	defer stores.Close()

	taskService := NewTaskService(stores.Task)

	now := time.Now()
	yesterday := now.Add(-24 * time.Hour)

	tasks1 := []domain.Task{
		{
			ID:          "task-1",
			BlockID:     "task-1",
			NoteID:      "note-1",
			NotePath:    "/note1.md",
			Content:     "Task 1",
			IsCompleted: false,
			CreatedAt:   yesterday,
			LineNumber:  1,
		},
		{
			ID:          "task-2",
			BlockID:     "task-2",
			NoteID:      "note-1",
			NotePath:    "/note1.md",
			Content:     "Task 2",
			IsCompleted: true,
			CreatedAt:   now,
			CompletedAt: &now,
			LineNumber:  2,
		},
	}

	tasks2 := []domain.Task{
		{
			ID:          "task-3",
			BlockID:     "task-3",
			NoteID:      "note-2",
			NotePath:    "/note2.md",
			Content:     "Task 3",
			IsCompleted: false,
			CreatedAt:   now,
			LineNumber:  1,
		},
	}

	taskService.IndexNote("note-1", "/note1.md", tasks1, now)
	taskService.IndexNote("note-2", "/note2.md", tasks2, now)

	tests := []struct {
		name          string
		filter        domain.TaskFilter
		expectedCount int
	}{
		{
			name:          "no filter - all tasks",
			filter:        domain.TaskFilter{},
			expectedCount: 3,
		},
		{
			name: "filter pending tasks",
			filter: domain.TaskFilter{
				Status: ptrBool(false),
			},
			expectedCount: 2,
		},
		{
			name: "filter completed tasks",
			filter: domain.TaskFilter{
				Status: ptrBool(true),
			},
			expectedCount: 1,
		},
		{
			name: "filter by note ID",
			filter: domain.TaskFilter{
				NoteID: "note-1",
			},
			expectedCount: 2,
		},
		{
			name: "filter by created after",
			filter: domain.TaskFilter{
				CreatedAfter: &yesterday,
			},
			expectedCount: 2,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			taskInfo, err := taskService.GetAllTasks(tt.filter)
			if err != nil {
				t.Fatalf("failed to get all tasks: %v", err)
			}

			if taskInfo.TotalCount != tt.expectedCount {
				t.Errorf("expected %d tasks, got %d", tt.expectedCount, taskInfo.TotalCount)
			}
			if len(taskInfo.Tasks) != tt.expectedCount {
				t.Errorf("expected %d tasks in slice, got %d", tt.expectedCount, len(taskInfo.Tasks))
			}
		})
	}
}

func TestTaskService_UpdateTaskStatus(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-task-update")
	defer os.RemoveAll(tmpDir)

	stores, err := NewStores("test-app", "test-workspace-3")
	if err != nil {
		t.Fatalf("failed to create stores: %v", err)
	}
	defer stores.Close()

	taskService := NewTaskService(stores.Task)

	now := time.Now()
	tasks := []domain.Task{
		{
			ID:          "task-1",
			BlockID:     "task-1",
			NoteID:      "note-1",
			NotePath:    "/note.md",
			Content:     "Test task",
			IsCompleted: false,
			CreatedAt:   now,
			LineNumber:  1,
		},
	}

	taskService.IndexNote("note-1", "/note.md", tasks, now)

	err = taskService.UpdateTaskStatus("task-1", true)
	if err != nil {
		t.Fatalf("failed to update task status: %v", err)
	}

	noteTasks, err := taskService.GetTasksForNote("note-1")
	if err != nil {
		t.Fatalf("failed to get tasks: %v", err)
	}

	if len(noteTasks) != 1 {
		t.Fatalf("expected 1 task, got %d", len(noteTasks))
	}

	task := noteTasks[0]
	if !task.IsCompleted {
		t.Error("expected task to be completed")
	}
	if task.CompletedAt == nil {
		t.Error("expected CompletedAt to be set")
	}

	err = taskService.UpdateTaskStatus("task-1", false)
	if err != nil {
		t.Fatalf("failed to update task status: %v", err)
	}

	noteTasks, err = taskService.GetTasksForNote("note-1")
	if err != nil {
		t.Fatalf("failed to get tasks: %v", err)
	}

	task = noteTasks[0]
	if task.IsCompleted {
		t.Error("expected task to be incomplete")
	}
	if task.CompletedAt != nil {
		t.Error("expected CompletedAt to be nil")
	}
}

func TestTaskService_RemoveNote(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-task-remove")
	defer os.RemoveAll(tmpDir)

	stores, err := NewStores("test-app", "test-workspace-4")
	if err != nil {
		t.Fatalf("failed to create stores: %v", err)
	}
	defer stores.Close()

	taskService := NewTaskService(stores.Task)

	now := time.Now()
	tasks := []domain.Task{
		{
			ID:          "task-1",
			BlockID:     "task-1",
			NoteID:      "note-1",
			NotePath:    "/note.md",
			Content:     "Test task",
			IsCompleted: false,
			CreatedAt:   now,
			LineNumber:  1,
		},
	}

	taskService.IndexNote("note-1", "/note.md", tasks, now)

	noteTasks, _ := taskService.GetTasksForNote("note-1")
	if len(noteTasks) != 1 {
		t.Fatalf("expected 1 task before removal, got %d", len(noteTasks))
	}

	err = taskService.RemoveNote("note-1")
	if err != nil {
		t.Fatalf("failed to remove note: %v", err)
	}

	noteTasks, _ = taskService.GetTasksForNote("note-1")
	if len(noteTasks) != 0 {
		t.Errorf("expected 0 tasks after removal, got %d", len(noteTasks))
	}
}

// Helper function to create bool pointer
func ptrBool(b bool) *bool {
	return &b
}

// Helper function to create time pointer
func ptrTime(t time.Time) *time.Time {
	return &t
}

func TestTaskService_AdvancedFiltering(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-task-advanced-filtering")
	defer os.RemoveAll(tmpDir)

	stores, err := NewStores("test-app", "test-advanced-filtering")
	if err != nil {
		t.Fatalf("failed to create stores: %v", err)
	}
	defer stores.Close()

	taskService := NewTaskService(stores.Task)

	baseTime := time.Date(2024, 1, 10, 12, 0, 0, 0, time.UTC)
	yesterday := baseTime.Add(-24 * time.Hour)
	twoDaysAgo := baseTime.Add(-48 * time.Hour)

	yesterdayCompleted := yesterday.Add(time.Hour)

	tasks1 := []domain.Task{
		{
			ID:          "task-1",
			BlockID:     "task-1",
			NoteID:      "note-1",
			NotePath:    "/note1.md",
			Content:     "Old pending task",
			IsCompleted: false,
			CreatedAt:   twoDaysAgo,
			LineNumber:  1,
		},
		{
			ID:          "task-2",
			BlockID:     "task-2",
			NoteID:      "note-1",
			NotePath:    "/note1.md",
			Content:     "Recently completed task",
			IsCompleted: true,
			CreatedAt:   yesterday,
			CompletedAt: ptrTime(baseTime),
			LineNumber:  2,
		},
		{
			ID:          "task-3",
			BlockID:     "task-3",
			NoteID:      "note-1",
			NotePath:    "/note1.md",
			Content:     "Old completed task",
			IsCompleted: true,
			CreatedAt:   twoDaysAgo,
			CompletedAt: ptrTime(yesterdayCompleted),
			LineNumber:  3,
		},
	}

	tasks2 := []domain.Task{
		{
			ID:          "task-4",
			BlockID:     "task-4",
			NoteID:      "note-2",
			NotePath:    "/note2.md",
			Content:     "New pending task",
			IsCompleted: false,
			CreatedAt:   baseTime,
			LineNumber:  1,
		},
	}

	taskService.IndexNote("note-1", "/note1.md", tasks1, yesterday)
	taskService.IndexNote("note-2", "/note2.md", tasks2, baseTime)

	tests := []struct {
		name          string
		filter        domain.TaskFilter
		expectedCount int
		description   string
	}{
		{
			name:          "no filter - all tasks",
			filter:        domain.TaskFilter{},
			expectedCount: 4,
			description:   "Should return all tasks",
		},
		{
			name: "filter by status - pending only",
			filter: domain.TaskFilter{
				Status: ptrBool(false),
			},
			expectedCount: 2,
			description:   "Should return pending tasks only",
		},
		{
			name: "filter by status - completed only",
			filter: domain.TaskFilter{
				Status: ptrBool(true),
			},
			expectedCount: 2,
			description:   "Should return completed tasks only",
		},
		{
			name: "filter by note ID",
			filter: domain.TaskFilter{
				NoteID: "note-1",
			},
			expectedCount: 3,
			description:   "Should return tasks from note-1 only",
		},
		{
			name: "combine status and note filters",
			filter: domain.TaskFilter{
				NoteID: "note-1",
				Status: ptrBool(false),
			},
			expectedCount: 1,
			description:   "Should return pending tasks from note-1 only",
		},
		// Date filtering tests
		// TODO: Add comprehensive edge case tests for boundary conditions (tasks created exactly at filter time, etc.)
		{
			name: "filter by created after",
			filter: domain.TaskFilter{
				CreatedAfter: ptrTime(yesterday.Add(-12 * time.Hour)),
			},
			expectedCount: 2,
			description:   "Should return tasks created after yesterday-12h (task-2 and task-4)",
		},
		{
			name: "filter by created before",
			filter: domain.TaskFilter{
				CreatedBefore: ptrTime(yesterday.Add(12 * time.Hour)),
			},
			expectedCount: 3,
			description:   "Should return tasks created before yesterday+12h (task-1, task-2, task-3)",
		},
		{
			name: "filter by date range - created between",
			filter: domain.TaskFilter{
				CreatedAfter:  ptrTime(twoDaysAgo.Add(12 * time.Hour)),
				CreatedBefore: ptrTime(yesterday.Add(12 * time.Hour)),
			},
			expectedCount: 1,
			description:   "Should return tasks created in the middle day (task-2)",
		},
		// TODO: Add comprehensive tests for CompletedAfter/CompletedBefore with various boundary conditions
		{
			name: "filter by completed after",
			filter: domain.TaskFilter{
				CompletedAfter: ptrTime(yesterday.Add(12 * time.Hour)),
			},
			expectedCount: 1,
			description:   "Should return tasks completed after yesterday+12h (task-2 only)",
		},
		{
			name: "combine status and date filters",
			filter: domain.TaskFilter{
				Status:       ptrBool(true),
				CreatedAfter: ptrTime(yesterday.Add(-12 * time.Hour)),
			},
			expectedCount: 1,
			description:   "Should return completed tasks created after yesterday-12h (task-2 only)",
		},
		// TODO: Add comprehensive tests for NoteModifiedAfter/NoteModifiedBefore with edge cases
		{
			name: "filter by note modified after",
			filter: domain.TaskFilter{
				NoteModifiedAfter: ptrTime(yesterday.Add(12 * time.Hour)),
			},
			expectedCount: 1,
			description:   "Should return tasks from notes modified recently (task-4 from note-2)",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			taskInfo, err := taskService.GetAllTasks(tt.filter)
			if err != nil {
				t.Fatalf("GetAllTasks() error = %v", err)
			}

			if taskInfo.TotalCount != tt.expectedCount {
				t.Errorf("%s: expected %d tasks, got %d", tt.description, tt.expectedCount, taskInfo.TotalCount)
			}
			if len(taskInfo.Tasks) != tt.expectedCount {
				t.Errorf("%s: expected %d tasks in slice, got %d", tt.description, tt.expectedCount, len(taskInfo.Tasks))
			}

			completedCount := 0
			pendingCount := 0
			for _, task := range taskInfo.Tasks {
				if task.IsCompleted {
					completedCount++
				} else {
					pendingCount++
				}
			}

			if completedCount+pendingCount != taskInfo.TotalCount {
				t.Errorf("count mismatch: completed(%d) + pending(%d) != total(%d)", completedCount, pendingCount, taskInfo.TotalCount)
			}
		})
	}
}
