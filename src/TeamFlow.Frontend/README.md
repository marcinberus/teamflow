# TeamFlow frontend

Angular 22 standalone SPA for the TeamFlow API.

## Prerequisites

- Node.js 22+
- npm 12+
- The TeamFlow API running at `https://localhost:7025`.

## Local development

```powershell
npm install
npm start
```

Open `http://localhost:4200`. The Angular development server forwards requests under
`/api` to `https://localhost:7025`; browser code can therefore call `/api/v1` without a
development-only backend URL.

## Checks

```powershell
npm run format:check
npm test -- --watch=false
npm run build
```
