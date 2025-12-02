package service

import (
	"sync"
	"time"

	"notes/backend/domain"
)

// TaskService manages task indexing, querying, and persistence.
// Maintains in-memory indexes for efficient filtering and provides SQLite persistence.
type TaskService struct {
	mu sync.RWMutex
	// tasks maps task ID to task
	tasks map[string]*domain.Task
	// byNoteID maps note ID to list of task IDs
	byNoteID map[string][]string
	// byStatus maps completion status to list of task IDs
	byStatus map[bool][]string
	// noteModified tracks note modification times for filtering
	noteModified map[string]time.Time
	// store handles SQLite persistence
	store *TaskStore
}

// NewTaskService creates a new task service with SQLite persistence.
func NewTaskService(store *TaskStore) *TaskService {
	return &TaskService{
		tasks:        make(map[string]*domain.Task),
		byNoteID:     make(map[string][]string),
		byStatus:     make(map[bool][]string),
		noteModified: make(map[string]time.Time),
		store:        store,
	}
}

// IndexNote parses tasks from a note and updates the index.
// Removes old tasks for the note and indexes new ones. Persists to SQLite and loads existing metadata.
func (s *TaskService) IndexNote(noteID string, notePath string, tasks []domain.Task, modifiedAt time.Time) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	s.removeNoteFromIndexes(noteID)

	s.noteModified[noteID] = modifiedAt

	existingTasks, err := s.store.GetTasksForNote(noteID)
	if err != nil {
		existingTasks = []domain.Task{}
	}

	existingByID := make(map[string]*domain.Task)
	for i := range existingTasks {
		existingByID[existingTasks[i].ID] = &existingTasks[i]
	}

	for i := range tasks {
		task := &tasks[i]

		if existing, ok := existingByID[task.ID]; ok {
			task.CreatedAt = existing.CreatedAt
			if !existing.IsCompleted && task.IsCompleted {
				now := time.Now()
				task.CompletedAt = &now
			} else if existing.IsCompleted && !task.IsCompleted {

				task.CompletedAt = nil
			} else {

				task.CompletedAt = existing.CompletedAt
			}
		} else {
			if task.CreatedAt.IsZero() {
				task.CreatedAt = time.Now()
			}

			if task.IsCompleted && task.CompletedAt == nil {
				now := time.Now()
				task.CompletedAt = &now
			}
		}

		s.tasks[task.ID] = task
		s.byNoteID[noteID] = append(s.byNoteID[noteID], task.ID)
		s.byStatus[task.IsCompleted] = append(s.byStatus[task.IsCompleted], task.ID)

		if err := s.store.SaveTask(task); err != nil {
			return err
		}
	}

	return nil
}

// RemoveNote removes all tasks associated with a note from indexes and database.
func (s *TaskService) RemoveNote(noteID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	s.removeNoteFromIndexes(noteID)
	delete(s.noteModified, noteID)

	return s.store.DeleteTasksForNote(noteID)
}

// GetAllTasks returns all tasks matching the filter criteria.
func (s *TaskService) GetAllTasks(filter domain.TaskFilter) (domain.TaskInfo, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	var matchedTasks []domain.Task

	for _, task := range s.tasks {
		if s.matchesFilter(task, filter) {
			matchedTasks = append(matchedTasks, *task)
		}
	}

	totalCount := len(matchedTasks)
	completedCount := 0
	pendingCount := 0

	for _, task := range matchedTasks {
		if task.IsCompleted {
			completedCount++
		} else {
			pendingCount++
		}
	}

	return domain.TaskInfo{
		Tasks:          matchedTasks,
		TotalCount:     totalCount,
		CompletedCount: completedCount,
		PendingCount:   pendingCount,
	}, nil
}

// GetTasksForNote returns all tasks in a specific note.
func (s *TaskService) GetTasksForNote(noteID string) ([]domain.Task, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	taskIDs, ok := s.byNoteID[noteID]
	if !ok {
		return []domain.Task{}, nil
	}

	tasks := make([]domain.Task, 0, len(taskIDs))
	for _, id := range taskIDs {
		if task, ok := s.tasks[id]; ok {
			tasks = append(tasks, *task)
		}
	}

	return tasks, nil
}

// UpdateTaskStatus toggles the completion status of a task.
// Updates both the index and database.
func (s *TaskService) UpdateTaskStatus(taskID string, isCompleted bool) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	task, ok := s.tasks[taskID]
	if !ok {
		return &domain.ErrNotFound{Resource: "task", ID: taskID}
	}

	oldStatus := task.IsCompleted
	task.IsCompleted = isCompleted

	if isCompleted && !oldStatus {
		now := time.Now()
		task.CompletedAt = &now
	} else if !isCompleted && oldStatus {
		task.CompletedAt = nil
	}

	s.removeTaskFromStatusIndex(taskID, oldStatus)
	s.byStatus[isCompleted] = append(s.byStatus[isCompleted], taskID)
	return s.store.SaveTask(task)
}

// removeNoteFromIndexes removes all tasks for a note from in-memory indexes.
// Does not affect database - caller must handle persistence.
func (s *TaskService) removeNoteFromIndexes(noteID string) {
	taskIDs, ok := s.byNoteID[noteID]
	if !ok {
		return
	}

	for _, taskID := range taskIDs {
		if task, ok := s.tasks[taskID]; ok {
			s.removeTaskFromStatusIndex(taskID, task.IsCompleted)
			delete(s.tasks, taskID)
		}
	}

	delete(s.byNoteID, noteID)
}

// removeTaskFromStatusIndex removes a task ID from the status index.
func (s *TaskService) removeTaskFromStatusIndex(taskID string, status bool) {
	statusList := s.byStatus[status]
	for i, id := range statusList {
		if id == taskID {
			s.byStatus[status] = append(statusList[:i], statusList[i+1:]...)
			break
		}
	}
}

// matchesFilter checks if a task matches the filter criteria.
func (s *TaskService) matchesFilter(task *domain.Task, filter domain.TaskFilter) bool {
	if filter.Status != nil && task.IsCompleted != *filter.Status {
		return false
	}

	if filter.NoteID != "" && task.NoteID != filter.NoteID {
		return false
	}

	if filter.CreatedAfter != nil && !task.CreatedAt.After(*filter.CreatedAfter) {
		return false
	}
	if filter.CreatedBefore != nil && !task.CreatedAt.Before(*filter.CreatedBefore) {
		return false
	}

	if filter.CompletedAfter != nil {
		if task.CompletedAt == nil || !task.CompletedAt.After(*filter.CompletedAfter) {
			return false
		}
	}
	if filter.CompletedBefore != nil {
		if task.CompletedAt == nil || !task.CompletedAt.Before(*filter.CompletedBefore) {
			return false
		}
	}

	if filter.NoteModifiedAfter != nil || filter.NoteModifiedBefore != nil {
		noteModTime, ok := s.noteModified[task.NoteID]
		if !ok {
			return false
		}

		if filter.NoteModifiedAfter != nil && noteModTime.Before(*filter.NoteModifiedAfter) {
			return false
		}
		if filter.NoteModifiedBefore != nil && noteModTime.After(*filter.NoteModifiedBefore) {
			return false
		}
	}

	return true
}
