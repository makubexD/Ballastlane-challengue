# BallastLane — Frontend

Angular 20 single-page application for task management. Built with standalone components, Signals, TailwindCSS 4, and vitest 4. Zoneless by default — no zone.js.

---

## Tech Stack

| Component | Technology |
|---|---|
| Framework | Angular 20 (standalone, zoneless) |
| Language | TypeScript 5.9 (strict) |
| State | Angular Signals (`signal`, `computed`) |
| Styling | TailwindCSS 4 (CSS-first configuration) |
| Testing | vitest 4 + Angular TestBed |
| HTTP | Angular `HttpClient` with `HttpInterceptorFn` |

---

## Prerequisites

| Tool | Version |
|---|---|
| Node.js | 22 LTS |
| Angular CLI | 20.x (`npm i -g @angular/cli`) |

---

## Setup & Run

```bash
npm install
ng serve
```

App is available at `http://localhost:4200`.

The backend API must be running on `http://localhost:5000` — see [../backend/README.md](../backend/README.md).

---

## Project Structure

```
src/app/
├── core/
│   └── auth/
│       ├── auth.service.ts       # Signals: _token, isAuthenticated, currentUser
│       ├── auth.guard.ts         # Functional CanActivateFn — redirects to /login
│       └── auth.interceptor.ts   # HttpInterceptorFn — attaches Bearer token
├── shared/
│   └── ui/
│       ├── badge.component.ts    # Color-coded status badge (Todo/InProgress/Done)
│       ├── spinner.component.ts  # Animated loading indicator
│       └── empty-state.component.ts  # Empty list placeholder with action button
├── features/
│   ├── auth/
│   │   ├── login/                # LoginComponent — reactive form, isSubmitting signal
│   │   └── register/             # RegisterComponent — password strength validator
│   └── tasks/
│       ├── services/
│       │   └── task.service.ts   # Signals: tasks[], isLoading. CRUD + optimistic delete
│       ├── components/
│       │   ├── task-list/        # Orchestrates list, form, empty state, spinner
│       │   ├── task-card/        # Individual task display with edit/delete actions
│       │   └── task-form/        # Reactive form for create and edit modes
│       └── tasks.routes.ts
├── __fixtures__/
│   └── task.fixtures.ts          # Typed test data
├── app.routes.ts                 # / → /tasks, lazy-loaded with authGuard
├── app.config.ts                 # provideZonelessChangeDetection, HTTP client
└── app.html                      # <router-outlet />
```

---

## Authentication Flow

1. User submits credentials on `/login` → `AuthService.login()` calls `POST /api/auth/login`
2. JWT token stored in `localStorage` and a `signal<string | null>()`
3. `isAuthenticated = computed(() => _token() !== null)` drives the UI reactively
4. `authInterceptor` attaches `Authorization: Bearer {token}` to every outgoing API request
5. `authGuard` protects `/tasks` — unauthenticated users are redirected to `/login`
6. `AuthService.logout()` clears localStorage and resets the signal

---

## State Management

All shared and component state uses Angular Signals:

```typescript
// Service-level state
tasks = signal<Task[]>([]);
isLoading = signal<boolean>(false);

// Derived state
isAuthenticated = computed(() => this._token() !== null);

// Component-level state
showForm = signal(false);
editingTask = signal<Task | null>(null);
```

Every component uses `ChangeDetectionStrategy.OnPush`. No RxJS state stores, no `BehaviorSubject` for state management.

---

## TailwindCSS 4

CSS-first configuration — no `tailwind.config.js` for theming:

```css
/* styles.css */
@import "tailwindcss";

/* Custom theme via @theme directive */
@theme {
  --color-primary: ...;
}
```

PostCSS plugin: `@tailwindcss/postcss` (configured in `.postcssrc.json`). Do not use the legacy `tailwind` plugin.

---

## Running Tests

```bash
# Run all 29 tests
npx vitest run

# With V8 coverage report
npx vitest run --coverage

# Watch mode (re-runs on file change)
npx vitest

# Type check only (strict, 0 errors required)
npx tsc --noEmit
```

Tests use `TestBed.configureTestingModule()`, `data-testid` selectors, and native async/await (no `fakeAsync`/`tick`). HTTP calls are mocked with `HttpClientTestingModule` + `HttpTestingController`.

---

## Environment

API base URL is configured in `src/environments/environment.ts`:

```typescript
export const environment = {
  apiUrl: 'http://localhost:5000'
};
```
