// TODO: requires test coverage
package service

import (
	"fmt"
	"time"
)

// Timer tracks operation timing for performance logging.
// Start a timer with logger.StartTimer(), complete it with timer.Complete() to log duration.
type Timer struct {
	start     time.Time
	operation string
	logger    *runtimeLogger
}

// StartTimer begins timing an operation and returns a Timer for completion logging.
func (l *runtimeLogger) StartTimer(operation string) *Timer {
	return &Timer{
		start:     time.Now(),
		operation: operation,
		logger:    l,
	}
}

// Complete logs the operation completion with elapsed time and optional structured data.
// Duration is automatically calculated and appended as (Xms).
func (t *Timer) Complete(format string, args ...any) {
	elapsed := time.Since(t.start)
	message := fmt.Sprintf(format, args...)

	var duration string
	ms := elapsed.Milliseconds()
	if ms > 0 {
		duration = fmt.Sprintf("%dms", ms)
	} else {
		duration = fmt.Sprintf("%dµs", elapsed.Microseconds())
	}

	if message != "" {
		t.logger.Infof("%s (%s) %s", t.operation, duration, message)
	} else {
		t.logger.Infof("%s (%s)", t.operation, duration)
	}
}

// CompleteWithError logs operation completion with error details.
func (t *Timer) CompleteWithError(err error, format string, args ...any) {
	elapsed := time.Since(t.start)
	message := fmt.Sprintf(format, args...)

	var duration string
	ms := elapsed.Milliseconds()
	if ms > 0 {
		duration = fmt.Sprintf("%dms", ms)
	} else {
		duration = fmt.Sprintf("%dµs", elapsed.Microseconds())
	}

	if message != "" {
		t.logger.Errorf("%s failed (%s) %s: %v", t.operation, duration, message, err)
	} else {
		t.logger.Errorf("%s failed (%s): %v", t.operation, duration, err)
	}
}
