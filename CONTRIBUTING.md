# Contributing Guidelines

## Code Quality

*Aim* for >90% test coverage

In both Go & F# code, use documentation comments for public/exported symbols.
If you feel that a private symbol is complex enough for context, the doc comments are okay.

Take advantage of F# constructs, like higher-order functions.

Be mindful of React gotchas (`key` prop in rendered lists, incorrect dependency arrays, etc.)

Use your best judgement when adding source files, but in general when a file is >2000 lines, its time to split it up.

## JSON IPC Integration

- Always route `@wailsjs/go` responses through the decoders in `frontend/src/Json.fs`; add new decoders there before extending `Api.fs`.
- When the Go backend can emit `null` for slices/maps, decode with `Decode.oneOf` + `Decode.nil []/{}` to force safe defaults and cover the case in `frontend/tests/Json.Test.fs`.
- Keep `frontend/__mocks__/@wailsjs/go/main/App.js` in sync with every imported API.
