# CLAUDE.md

Scope: the Angular SPA in `frontend/`. The repository-wide agent rules in
[`../CLAUDE.md`](../CLAUDE.md) still apply — in particular scope discipline, the
fixed stack, and "hiding a button is not authorisation". Rationale for most
choices below is in [`../DECISIONS.md`](../DECISIONS.md).

## Commands

All from `frontend/`:

| Task | Command |
|---|---|
| Install | `npm install` |
| Dev server (→ <http://localhost:4200>) | `npm start` / `npx ng serve` |
| Production build (default config) | `npm run build` |
| Dev build, watching | `npm run watch` |
| Type-check templates and TS | `npx ng build` — there is no separate typecheck script |

**There is no linter.** No ESLint, no Prettier, no config for either. The only
static gate is the compiler: `tsconfig.json` sets `strict`,
`noPropertyAccessFromIndexSignature`, `noImplicitReturns`, and
`strictTemplates` + `strictInjectionParameters`.

**There are no frontend tests.** `ng test` (Karma/Jasmine) is wired up in
`angular.json` and the packages are installed, but no `.spec.ts` file exists and
every schematic in `angular.json` sets `skipTests: true`. This is deliberate —
`../DECISIONS.md` → "Things I chose not to build". Do not add a suite unless
asked; the rules worth testing are server-side, and the tests are there
(`cd backend; dotnet test`).

## Running against the backend

The API must be running or every screen shows an error. From `backend/`:

```bash
dotnet run --project src/FacilitiesMgmt.WebApi
```

`launchSettings.json` puts it on `http://localhost:5000`, which is what
`src/app/core/api.config.ts` points at. Two places in the repo still name the
old port **5080** — the run instructions in `../README.md` (which pass
`--urls http://localhost:5080`) and the offline message in
`core/interceptors/error.interceptor.ts`. Following the README as written starts
the API somewhere the SPA does not look. Either drop the `--urls` flag or change
`API_BASE_URL`.

CORS on the backend allows `http://localhost:4200` only
(`backend/.../appsettings.json` → `Cors:AllowedOrigins`), so serve on the
default port.

Sign-in needs an **organisation slug** as well as email and password, because
email is unique per organisation rather than globally. Seeded accounts and the
shared password are listed in `../README.md`.

## Architecture

Standalone components throughout, no `NgModule` anywhere. `main.ts` bootstraps
`AppComponent` with `appConfig` from `app.config.ts`, which provides the router
(with component input binding) and `HttpClient` with two functional
interceptors.

**Routing is lazy end to end.** `app.routes.ts` uses `loadComponent` for
`login`, `dashboard` and `reports`, and `loadChildren` into
`features/maintenance-requests/request.routes.ts`, which lazy-loads its own two
pages again. `authGuard` protects everything except `login`. An `approverGuard`
also exists in `core/auth/auth.guard.ts`.

**State is signals, no store library.** Components hold `signal()` /
`computed()` locally. The only cross-feature state is `AuthService`
(`core/auth/auth.service.ts`): one signal holding the whole `LoginResult`,
persisted to `localStorage` under `facilities-mgmt.session`, with an expired
token discarded on read so the user lands on `/login` instead of a wall of 401s.

**One API service per feature, co-located.** Every feature folder owns a
`<thing>-api.service.ts` next to the component that consumes it — `providedIn:
'root'`, injecting `HttpClient`, returning a raw `Observable<T>`. There is no
shared `ApiService` and no facade layer. Follow this when adding a feature
rather than centralising.

**The two interceptors, and why the order in `app.config.ts` matters.**
`authInterceptor` runs first on the way out and attaches the bearer token *only*
to URLs starting with `API_BASE_URL` — without that check a future third-party
call would leak the token. `errorInterceptor` is therefore outermost on the way
back: it flattens the API's RFC 9457 `ProblemDetails` (validation `errors`
first, then `detail`) into a plain `Error`, and clears the session + redirects
on 401. Consequence for components: subscribe with `error: (err: Error) =>
this.error.set(err.message)` and render that — no per-component error parsing.

**Models are hand-written mirrors of the backend DTOs** in `core/models/`. There
is no codegen and no OpenAPI client. Changing a DTO in
`backend/src/FacilitiesMgmt.Application/**/...Dtos.cs` means editing the
matching interface here by hand.

**Templates** use Angular's built-in control flow (`@if` / `@for`), live in
sibling `.html` files, and import pipes per component (`CurrencyPipe`,
`DatePipe`). No UI framework and no CSS library by choice — one global
`src/styles.css`.

## Invariants — do not "fix" these

- **No tenant or organisation header is ever sent.** The server resolves the
  tenant from validated JWT claims and ignores anything client-supplied. There
  is deliberately no `tenant.interceptor.ts`, even though the original spec's
  folder tree listed one (`../DECISIONS.md` §6).
- **No organisation id appears in any URL, query string, or body.** If you find
  yourself adding one, that is the bug.
- **Guards and the `canX` getters are presentation, not authorisation.** Every
  one is re-enforced server-side.
- **The approve button stays enabled on your own request.** It is left visible
  precisely so the server's 403 is observable rather than taken on trust
  (`request-actions.component.ts`). Do not hide or disable it.
- **A 404 may mean "another tenant's row", not "missing".** The message is
  deliberately identical in both cases; do not make it more specific.
- **The spend report range is half-open**: `fromUtc` inclusive, `toUtc`
  exclusive. Callers pass the start of the day *after* the last day they want
  included (`spend-report-api.service.ts`).

## API surface consumed

| Endpoint | Used by |
|---|---|
| `POST /api/auth/login` | `features/login/login-api.service.ts` |
| `GET /api/sites` | `request-create-api.service.ts` (site picker) |
| `GET /api/requests?status=&siteId=` | `request-list-api.service.ts`, `dashboard-api.service.ts` |
| `POST /api/requests` | `request-create-api.service.ts` |
| `POST /api/requests/{id}/approve` · `/reject` · `/complete` | `request-actions-api.service.ts` |
| `GET /api/requests/{id}/audit` | `audit-log-api.service.ts` |
| `GET /api/reports/spend?fromUtc=&toUtc=` | `spend-report-api.service.ts` |

Approve, reject and complete are Approver-only server-side; complete is also
where `ActualCost` is recorded. There is no update or delete endpoint for audit
entries for any role, by design.
