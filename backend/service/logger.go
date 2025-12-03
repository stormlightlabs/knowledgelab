package service

import (
	"context"
	"fmt"

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
	if l.ctx != nil {
		runtime.LogInfof(l.ctx, format, args...)
		return
	}
	fmt.Printf("INFO: "+format+"\n", args...)
}

func (l *runtimeLogger) Debugf(format string, args ...any) {
	if l.ctx != nil {
		runtime.LogDebugf(l.ctx, format, args...)
		return
	}
	fmt.Printf("DEBUG: "+format+"\n", args...)
}

func (l *runtimeLogger) Warnf(format string, args ...any) {
	if l.ctx != nil {
		runtime.LogWarningf(l.ctx, format, args...)
		return
	}
	fmt.Printf("WARN: "+format+"\n", args...)
}

func (l *runtimeLogger) Errorf(format string, args ...any) {
	if l.ctx != nil {
		runtime.LogErrorf(l.ctx, format, args...)
		return
	}
	fmt.Printf("ERROR: "+format+"\n", args...)
}
