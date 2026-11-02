# TeamFlow frontend

Angular 22 standalone SPA for the TeamFlow API, built with Angular Material.

## Prerequisites

- Node.js `^22.22.3`, `^24.15.0` or `>=26` (`.nvmrc` pins 22.22.3)
- npm 12+
- The TeamFlow API running at `https://localhost:7025` (see the [root README](../../README.md)).

## Local development

```powershell
npm install
npm start
```

Open `http://localhost:4200`. The development server forwards requests under `/api` to
`https://localhost:7025` (`src/proxy.conf.json`), so the app always calls the relative `/api/v1`
and needs no backend URL setting.

## Checks

```powershell
npm run format:check
npm test -- --watch=false   # Vitest
npm run build
```

CI does not run these yet; it covers the backend only.

## Architecture

```
src/app/
  core/        auth (AuthStore, guard, token storage, interceptor), http (error interceptor, 422 mapping), API config
  layout/      app shell (sidenav + toolbar) around the protected routes
  features/
    auth/      login, register
    projects/  projects, members, tasks, user search: APIs, models, permission policies, pages, dialogs
    imports/   FileImport component, ImportsApi, project and task import pages
    profile/
```

- Standalone components and lazy-loaded routes; no NgModules.
- One typed `*Api` class per backend resource (`ProjectsApi`, `TasksApi`, …). Only these classes use
  `HttpClient`; components never build URLs.
- State lives in component signals. `AuthStore` (session and current user) is the only global state.
- `authInterceptor` adds the JWT to `/api/v1` requests only. `errorInterceptor` handles 401 (clear the
  session, redirect to login), 429 and server/network failures, then rethrows. Features handle
  403/404/409/422 where they happen; 422 field errors are mapped onto forms with `applyServerValidationErrors`.
- Mutations happen in Material dialogs that call the API themselves and close with a result. The page then
  refreshes only the affected data.
- The project details page composes four requests (project, statistics, members, tasks). Each section loads
  and fails independently.
- UI permission checks live in `features/projects/project.policies.ts`. They only hide or disable UI; the API
  enforces the rules.
- HttpClient uses the XHR backend (`withXhr()`) because the fetch backend does not report upload progress.
