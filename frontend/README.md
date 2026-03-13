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
│   ├── auth/
│   │   ├── auth.service.ts           # Signals: isAuthenticated, currentUser. Cookie-based login/logout + APP_INITIALIZER probe
│   │   ├── auth.guard.ts             # Functional CanActivateFn — redirects unauthenticated users to /login
│   │   ├── no-auth.guard.ts          # Functional CanActivateFn — redirects authenticated users away from /login
│   │   ├── auth.interceptor.ts       # HttpInterceptorFn — sets withCredentials: true on every request
│   │   └── csrf.interceptor.ts       # HttpInterceptorFn — reads XSRF-TOKEN cookie, adds X-XSRF-TOKEN header
│   ├── errors/
│   │   └── global-error-handler.ts   # ErrorHandler — catches unhandled errors, logs to console
│   ├── interceptors/
│   │   └── error.interceptor.ts      # HttpInterceptorFn — maps 4xx/5xx to user-facing toast messages
│   └── services/
│       └── toast.service.ts          # show(message, type) — manages toast signal array
├── shared/
│   └── ui/
│       ├── badge.component.ts        # Color-coded status badge (Todo/InProgress/Done)
│       ├── spinner.component.ts      # Animated loading indicator
│       ├── empty-state.component.ts  # Empty list placeholder with action button
│       ├── pagination.component.ts   # Page navigation — emits pageChange events
│       ├── toast-container.component.ts  # Renders active toast stack
│       └── date-picker.component.ts  # Custom date input with future-date validation
├── features/
│   ├── auth/
│   │   ├── login/                    # LoginComponent — reactive form, isSubmitting signal
│   │   └── register/                 # RegisterComponent — password strength + matching validators
│   ├── tasks/
│   │   ├── services/
│   │   │   └── task.service.ts       # Signals: tasks[], isLoading, totalCount, totalPages, currentPage
│   │   ├── components/
│   │   │   ├── task-list/            # Orchestrates list, form, empty state, spinner, pagination
│   │   │   ├── task-card/            # Individual task display with edit/delete actions
│   │   │   ├── task-form/            # Reactive form for create and edit modes
│   │   │   └── task-skeleton/        # Skeleton loading placeholder cards
│   │   └── tasks.routes.ts
│   └── shell/
│       └── shell.component.ts        # App shell — nav bar with logout action
├── validators/
│   ├── future-date.validator.ts      # ValidatorFn — rejects dates in the past
│   └── password-strength.validator.ts  # ValidatorFn — min 8 chars, upper, lower, digit, special
├── __fixtures__/
│   └── task.fixtures.ts              # Typed test data factories
├── app.routes.ts                     # / → /tasks (authGuard), lazy-loaded feature routes
├── app.config.ts                     # provideZonelessChangeDetection, HTTP client, interceptors
└── app.html                          # <router-outlet />
```

---

## Authentication Flow

1. **Bootstrap** — `APP_INITIALIZER` calls `AuthService.initialize()` which probes `GET /api/auth/me`
   - 200 → `isAuthenticated` signal set to `true` (existing cookie still valid)
   - 401/error → `isAuthenticated` set to `false` (no cookie or expired)
2. **Login** — `AuthService.login()` calls `POST /api/auth/login` with `withCredentials: true`
   - Backend sets `Set-Cookie: access_token=<jwt>; HttpOnly; SameSite=Lax/Strict`
   - Token is **never** returned in the response body or stored in JavaScript
3. **CSRF** — `csrfInterceptor` reads the `XSRF-TOKEN` cookie set by the backend and adds
   `X-XSRF-TOKEN` header to every mutating request (POST/PUT/DELETE). The backend validates this
   header via `UseAntiforgery()`.
4. **Requests** — `authInterceptor` clones every request with `withCredentials: true`; the browser
   attaches the `access_token` cookie automatically
5. **Guard** — `authGuard` reads `authService.isAuthenticated()` signal; unauthenticated users
   redirected to `/login`. `noAuthGuard` prevents authenticated users from hitting `/login`.
6. **Logout** — `POST /api/auth/logout` → backend clears cookie → signal set to `false` → navigate to `/login`

---

## State Management

All shared and component state uses Angular Signals:

```typescript
// TaskService — paginated list state
tasks = signal<Task[]>([]);
isLoading = signal<boolean>(false);
totalCount = signal<number>(0);
totalPages = signal<number>(0);
currentPage = signal<number>(1);
hasNextPage = computed(() => this.currentPage() < this.totalPages());

// Auth state — set by APP_INITIALIZER probe and login/logout
isAuthenticated = signal(false);

// Component-level state
showForm = signal(false);
editingTask = signal<Task | null>(null);
```

Every component uses `ChangeDetectionStrategy.OnPush`. No RxJS state stores, no `BehaviorSubject` for state management.

---

## Form Validators

| Validator | File | Description |
|---|---|---|
| `futureDateValidator` | `validators/future-date.validator.ts` | Rejects dates in the past |
| `passwordStrengthValidator` | `validators/password-strength.validator.ts` | Requires 8+ chars, upper, lower, digit, special char |

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
# Run all ~140 tests
npx vitest run

# With V8 coverage report
npx vitest run --coverage

# Watch mode (re-runs on file change)
npx vitest

# Type check only (strict, 0 errors required)
npx tsc --noEmit
```

Tests use `TestBed.configureTestingModule()`, `data-testid` selectors, and native async/await (no `fakeAsync`/`tick`). HTTP calls are mocked with `provideHttpClientTesting()` + `HttpTestingController`.

---

## Environment

API base URL is configured in `src/environments/environment.ts`:

```typescript
export const environment = {
  apiUrl: 'http://localhost:5000'
};
```
