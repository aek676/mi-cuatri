---
applyTo: '**'
---

# Git Commit Message Generator

Generate a conventional commit message in English following this exact format:

```
type(scope): message

Detailed description of changes made, including context,
reason for changes, and any important implementation details.
This can be multiple lines and much more comprehensive.

Files modified:
- path/to/file1.ext
- path/to/file2.ext
- path/to/file3.ext
```

## Requirements:

- **Types**: feat, fix, docs, style, refactor, perf, test, chore
- **Scopes**: core, operations, shared
- **Message**: imperative mood, lowercase, no period, **max 48 chars**
- **Description**: detailed explanation of changes, context, and reasoning
- **Files**: bulleted list of modified files with relative paths
- **Language**: English only

## Examples:

```
feat(core): add user authentication

Implemented JWT-based authentication system with login,
logout, and token refresh functionality. Added middleware
for route protection and user session management.

Files modified:
- apps/core/src/auth/auth.service.ts
- apps/core/src/auth/auth.controller.ts
- apps/core/src/middleware/auth.middleware.ts
```

```
fix(operations): resolve invoice calculation bug

Fixed rounding error in tax calculation that caused
discrepancies in invoice totals. Updated calculation
logic to use proper decimal precision.

Files modified:
- apps/operations/src/invoices/invoice.service.ts
- apps/operations/src/invoices/invoice.test.ts
```

Analyze the staged changes and generate ONE commit message following these rules exactly.
