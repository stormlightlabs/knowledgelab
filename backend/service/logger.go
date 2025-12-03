package service

import (
	"context"
	"fmt"
	"time"

	"github.com/wailsapp/wails/v2/pkg/runtime"
)

// runtimeLogger wraps the Wails runtime logger with a safe fallback when the context has not been attached yet.
type runtimeLogger struct {
	ctx context.Context
}

// attach sets the runtime context used for logging.
func (l *runtimeLogger) attach(ctx context.Context) {
	l.ctx = ctx
}

func (l *runtimeLogger) Infof(format string, args ...any) {
	timestamp := time.Now().UTC().Format(time.RFC3339)
	message := fmt.Sprintf(format, args...)
	if l.ctx != nil {
		runtime.LogInfof(l.ctx, "[%s] INFO: %s", timestamp, message)
		return
	}
	fmt.Printf("[%s] INFO: %s\n", timestamp, message)
}

func (l *runtimeLogger) Debugf(format string, args ...any) {
	timestamp := time.Now().UTC().Format(time.RFC3339)
	message := fmt.Sprintf(format, args...)
	if l.ctx != nil {
		runtime.LogDebugf(l.ctx, "[%s] DEBUG: %s", timestamp, message)
		return
	}
	fmt.Printf("[%s] DEBUG: %s\n", timestamp, message)
}

func (l *runtimeLogger) Warnf(format string, args ...any) {
	timestamp := time.Now().UTC().Format(time.RFC3339)
	message := fmt.Sprintf(format, args...)
	if l.ctx != nil {
		runtime.LogWarningf(l.ctx, "[%s] WARN: %s", timestamp, message)
		return
	}
	fmt.Printf("[%s] WARN: %s\n", timestamp, message)
}

func (l *runtimeLogger) Errorf(format string, args ...any) {
	timestamp := time.Now().UTC().Format(time.RFC3339)
	message := fmt.Sprintf(format, args...)
	if l.ctx != nil {
		runtime.LogErrorf(l.ctx, "[%s] ERROR: %s", timestamp, message)
		return
	}
	fmt.Printf("[%s] ERROR: %s\n", timestamp, message)
}
