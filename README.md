# Facilities Management Service

Angular frontend for the facilities management application. It provides login, dashboard views, maintenance request workflows, approvals, and spend reporting for an organisation-based service.

## Overview

This project is the SPA client for the backend API. It includes:

- user login with organisation, email, and password
- dashboard overview of requests
- maintenance request creation and listing
- approval / rejection / completion actions
- audit log viewing
- spend report generation

## Prerequisites

Before running the app, make sure you have:

- Node.js and npm installed
- a .NET SDK installed
- the backend API running locally

## Run the backend

From the backend project directory:

```bash
dotnet run --project src/FacilitiesMgmt.WebApi
```

The API is expected on http://localhost:5000, which matches the frontend API configuration.

> The frontend expects the backend on the default API port. Do not start the API with a different `--urls` value unless you also update the client configuration.

## Run the frontend

From this project root:

```bash
npm install
npm start
```

Or directly with Angular CLI:

```bash
npx ng serve
```

Then open:

http://localhost:4200

## Login

Sign-in requires:

- email
- password

The seeded accounts and shared password are documented in the repository setup notes.

## Project structure

```text
src/
  app/
    core/
      auth/
      interceptors/
      models/
    features/
      dashboard/
      login/
      maintenance-requests/
      spend-reports/
    shared/
```

The app uses standalone Angular components, lazy-loaded routes, and signal-based state.

## Notes

- Auth tokens are attached only to API requests to the configured backend base URL.
- Errors from the API are handled centrally by the HTTP interceptor.
- This app does not send tenant or organisation IDs from the client; the backend resolves identity from the validated JWT.
- The spend report range follows a half-open window: from inclusive, to exclusive.

## Development notes

- App configuration is in `src/app/app.config.ts`
- Routing is defined in `src/app/app.routes.ts`
- Auth guards and session handling are in `src/app/core/auth/`
- Models are maintained manually to mirror backend DTOs

## Related repositories

- Backend API: `backend/`
- Frontend app: this folder
