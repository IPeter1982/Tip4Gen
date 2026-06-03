# Tech Stack — Foci VB Tippjáték

This document describes the chosen technology stack for the World Cup tipping game, along with the reasoning behind each decision.

---

## Overview

A web-based football (soccer) World Cup tipping game. Users submit score predictions for every match, compete individually and in 3-person teams (optionally including one AI team member), and climb individual and team leaderboards. Tipping closes 1 hour before each kickoff; scoring uses a "best matching category" model with stage multipliers.

The architecture is a **decoupled SPA + API**: a Vite/React single-page app talking to an ASP.NET Core backend over REST and SignalR, backed by PostgreSQL.

---

## Architecture at a glance

```
                          ┌──────────┐
                          │  Auth0   │  (Authorization Code + PKCE → JWT)
                          └────┬─────┘
                               │ tokens
┌─────────────────────┐         REST (JSON)          ┌──────────────────────┐
│   React SPA (Vite)   │ ───────────────────────────▶ │   ASP.NET Core API    │
│  (Azure Static Web   │ ◀─────── SignalR (WS) ─────── │  (Azure App Service)  │
│      Apps)           │                              │  Domain logic         │
│  TanStack Query      │                              │  Hosted Services      │
│  Tailwind / shadcn   │                              │  EF Core              │
│  SignalR client      │                              │  JwtBearer (Auth0)    │
└─────────────────────┘                              └───────────┬───────────┘
                                                                 │
                                              ┌──────────────────┼──────────────────┐
                                              ▼                  ▼                  ▼
                                       ┌────────────┐    ┌──────────────┐   ┌──────────────┐
                                       │ PostgreSQL │    │ Football API │   │  AI provider │
                                       │ (Azure)    │    │ (results)    │   │  (AI tipper) │
                                       └────────────┘    └──────────────┘   └──────────────┘
```

Two clearly separated deployables, no blurring of where logic lives. The backend owns all business rules and is the single source of truth; the frontend renders state and submits actions. Auth0 is the identity provider — the API trusts its JWTs but stores no credentials.

---

## Frontend

| Concern | Choice | Notes |
|---|---|---|
| Build tool / dev server | **Vite** | Instant startup, near-instant HMR, minimal config. |
| UI framework | **React** | Component model fits the dashboard-style UI. |
| Language | **TypeScript** | Type safety across the app. |
| Routing | **React Router (v7)** | Plenty for this app; TanStack Router optional if full type-safe routes are wanted. |
| Server state | **TanStack Query** | Caching, refetching, invalidation for leaderboards, matches, tips. Pairs with SignalR: push event → invalidate query → auto-refetch. |
| Client UI state | **Zustand** | Lightweight; for filters, modals. No Redux — overkill here. |
| Styling | **Tailwind CSS + shadcn/ui** | The magazine-style design translates directly; shadcn components are copied into the repo, so we own them. |
| Forms & validation | **React Hook Form + Zod** | Zod mirrors backend validation rules and gives type-safe tip submission. |
| Auth | **@auth0/auth0-react** | Authorization Code + PKCE flow, token storage, silent renewal. Token also passed to SignalR via `accessTokenFactory`. |
| Realtime client | **@microsoft/signalr** | First-party SignalR client. Wrapped in a hook/context that fires TanStack Query invalidations on incoming events. |
| HTTP | **fetch / axios** in a typed API client | Optionally generate a TS client from the backend OpenAPI spec (`openapi-typescript` / NSwag) to kill frontend↔backend mismatch bugs. |
| Date/time | **date-fns-tz** (or Day.js + timezone plugin) | Display in Hungarian time; never trust the client clock for tipping deadlines. |

### Why Vite + React over Next.js

This is a behind-login, interactive dashboard, not a public content site — so SSR and SEO don't matter. Next.js's main advantages (SSR, server components, integrated API layer) would go unused because ASP.NET Core owns the API. Vite gives faster iteration and a simpler mental model, and a static bundle hosts cheaply anywhere.

---

## Backend

| Concern | Choice | Notes |
|---|---|---|
| Framework | **ASP.NET Core 10** (Web API) | .NET 10 is the current LTS (3-year support). Controller-based or minimal API. |
| ORM | **Entity Framework Core 10** (Npgsql) | Mature migrations; LINQ tooling suits the complex leaderboard / "best 3" queries. |
| Realtime | **SignalR** | Tip reveal at deadline, live leaderboard refresh. WebSocket fallback, groups, auto-reconnect out of the box. |
| Scheduled / background work | **Hosted Services** (`BackgroundService`) | Native to .NET — no external job platform needed. **Hangfire** optional if a job dashboard + retries are wanted. |
| Domain orchestration | **MediatR** or a plain service layer | For scoring and team-aggregation logic. |
| Validation | **FluentValidation** | Tip validation (deadline, format). |
| Logging | **Serilog** | Structured logging. |
| AI integration | **Anthropic.SDK** or **OpenAI** NuGet | For the AI team member's predictions + reasoning. |
| HTTP resilience | **IHttpClientFactory + Polly** | Retry / circuit breaker for the football data API. |

### Why C#

The scoring logic is the heart of this app and is non-trivial: stage multipliers, "best matching category" selection, joker doubling, team-level summation across 3 members. C# records, enums, and pattern matching (switch expressions over scoring categories) produce cleaner, safer, more testable code for this kind of business logic. Background work and realtime are first-party (`BackgroundService`, SignalR) rather than third-party add-ons.

---

## Database

| Concern | Choice | Notes |
|---|---|---|
| Engine | **PostgreSQL** | Cheaper hosting than SQL Server; JSON columns useful for storing AI prediction reasoning. |
| Hosting | **Azure Database for PostgreSQL — Flexible Server** | Managed; same region as the API. |

### Scoring & leaderboard strategy

Do **not** recompute points on every read. When a match closes, compute each tip's points **once** and store them. Serve the leaderboard from a materialized view or a cache table rebuilt on a schedule. This keeps the leaderboard instant even at 1,000+ users.

---

## Auth

**Auth0** as the identity provider. The backend does not manage passwords or user credentials — it validates Auth0-issued JWTs and authorizes requests.

| Concern | Choice | Notes |
|---|---|---|
| Identity provider | **Auth0** | Hosted login, social connections (Google, etc.), email/password, MFA if wanted. No credential storage in our DB. |
| Frontend integration | **@auth0/auth0-react** | SPA SDK; handles the Authorization Code + PKCE flow, token storage, and silent renewal. |
| Backend integration | **Microsoft.AspNetCore.Authentication.JwtBearer** | Validates Auth0 access tokens (issuer, audience, signature via Auth0 JWKS). |

### How it fits together

1. The React SPA uses `@auth0/auth0-react` to run the **Authorization Code flow with PKCE** (the recommended flow for SPAs — no client secret in the browser).
2. Auth0 issues an **access token** (JWT) scoped to our API's audience.
3. The SPA sends that token as a `Bearer` token to the ASP.NET Core API.
4. The API validates it with `JwtBearer` against Auth0's issuer and JWKS endpoint — stateless, no session store needed.
5. **SignalR** authenticates the same way: pass the access token via the `accessTokenFactory` option on the client connection.

### Notes & gotchas

- **Register an API in Auth0** (with an identifier/audience) so access tokens are JWTs validatable by the backend — not just opaque tokens for the userinfo endpoint.
- **Map the local user.** Auth0 owns identity, but the app still needs a `User` row keyed by the Auth0 `sub` claim, to attach tips, team membership, and jokers. Create/link this record on first authenticated request.
- **Token lifetime & renewal.** Keep access tokens short-lived; rely on Auth0 silent renewal / refresh token rotation (handled by the SDK) rather than rolling your own.
- **Free tier** covers a friends-and-family launch comfortably; check the active-user limits before a wider rollout.

---

## Hosting & deployment

> **Superseded — see the Railway addendum at the bottom of this section.** Originally Azure end-to-end, swapped to Railway on 2026-05-28 for faster setup and lower ops surface at the friends-and-family scale. The Azure analysis below is kept for historical context but does not describe the live deployment.

All on **Azure**, which is the most natural home for a .NET backend.

| Layer | Choice | Notes |
|---|---|---|
| Backend API | **Azure App Service** (Linux, .NET 10) | Simplest path; always-on, no cold starts. Step up to **Azure Container Apps** later if you want Docker-based scaling or to run the workers as separate containers. |
| Frontend | **Azure Static Web Apps** | Built for SPA bundles like a Vite build; global CDN, free tier, integrated GitHub Actions deploy. Can also reverse-proxy `/api` to the backend. |
| Database | **Azure Database for PostgreSQL — Flexible Server** | Managed Postgres, same region as the API to minimize latency. |
| Realtime (optional) | **Azure SignalR Service** | Offloads WebSocket connection management from App Service; worth adding if concurrent connections grow. App Service alone is fine to start. |
| Secrets | **Azure Key Vault** | Auth0 client config, football API key, AI provider key, DB connection string. Accessed via Managed Identity — no secrets in app settings. |
| CI/CD | **GitHub Actions** (or Azure DevOps) | Build/test/deploy both the SPA and the API. |
| Monitoring | **Application Insights** | Request tracing, exceptions, and a natural fit with Serilog. |

### Notes

- **Keep it always-on, not serverless.** A tipping game is a continuously-running workload, and .NET cold starts are slower than Node — App Service (or Container Apps with min replicas ≥ 1) fits better than Azure Functions.
- **Background services co-located vs. separate.** The hosted services (match polling, tip closing, AI tipping) can run inside the same App Service instance to start. If they grow heavy, split the **Workers** project into its own Azure Container App so a slow scoring cycle can't affect API latency.
- **Same region everywhere.** Put App Service, Postgres, and (if used) Azure SignalR Service in the same Azure region to keep latency and egress costs down.
- **Managed Identity over connection strings.** Let App Service authenticate to Key Vault and Postgres via Managed Identity where possible, rather than storing raw credentials.

### Railway addendum (current — replaces the Azure plan above)

| Layer | Choice | Notes |
|---|---|---|
| Backend API | **Railway service** (Docker, `backend/Dockerfile`) | Multi-stage `sdk:9.0` → `aspnet:9.0`. Listens on `$PORT`. Includes the three `BackgroundService` jobs in-process (no separate Workers deployable). |
| Frontend | **Railway service** (Docker, `web/Dockerfile`) | `node:22-alpine` build → `nginx:alpine` serve. `web/nginx.conf` templated with `envsubst` at start; reverse-proxies `/api/*` to the API over Railway's private network (same-origin, no CORS in prod). |
| Database | **Railway Postgres plugin** | Exposes `DATABASE_URL` in URI form; `Tip4Gen.Infrastructure/DependencyInjection.cs` translates to Npgsql keyword/value form at startup. Private networking only — no public DB exposure. |
| Realtime (Phase 10 if shipped) | **API service WebSockets** | SignalR works on Railway services as-is. No equivalent of Azure SignalR Service offload at this scale; Phase 10 is `[CUT-OK]` and may be replaced with 30s polling. |
| Secrets | **Railway env vars** | Per-service env vars via the Railway dashboard. No Key Vault equivalent — fine at this scope. |
| CI/CD | **Railway auto-deploy on push to `main`** | Per-service builds; rollback via the dashboard. No GitHub Actions workflow needed for deploy. |
| Monitoring | **Railway logs + metrics** | Captures stdout/stderr (Serilog Console sink); built-in service-level metrics. Application Insights replaced. |
| Region | **EU-West (Amsterdam)** | Closest Railway region to the Hungarian audience. |
| Migrations | **API startup** | `db.Database.Migrate()` runs at boot; bad migrations stop boot loudly. Rollback path documented in CLAUDE.md. |
| Domain | **`*.up.railway.app` for launch** | Custom domain (e.g. `tip4gen.hu`) is a 5-minute swap post-launch + Auth0 callback-URL update. |

See CLAUDE.md "Prod hosting (Railway)" for the architecture diagram and env-var inventory, and implementation-plan.md Phase 8.5 for what code shipped.

---

## External services

| Service | Purpose | Candidates / Notes |
|---|---|---|
| Football data API | Fixtures, match status, final results | **API-Football** (RapidAPI), **Football-Data.org**, **SportMonks**. Test accuracy & latency before launch — it directly affects tip closing and auto-scoring. **Have a manual admin fallback** to enter results if the API lags. |
| AI provider | The AI team member's predictions | Claude or GPT. Called ~1–2h before kickoff with form / FIFA ranking / injuries; returns JSON `{ home_goals, away_goals, reasoning }` based on the configured "adventurousness" level. Cost is minimal per match. |
| Email | Notifications (deadline reminders, results) | **Resend**. |
| Web push (optional, v2) | Push reminders | **OneSignal** / **Knock**. |

---

## Recommended project structure (backend)

A clean layered split — not overengineering at this scale:

- **Api** — controllers, SignalR hubs, DI configuration.
- **Domain** — entities, value objects, the scoring logic. No database dependencies, so it's 100% unit-testable.
- **Infrastructure** — EF Core `DbContext`, external API clients (football data, AI), email.
- **Workers** — the hosted background services (pollers, schedulers).

Keep the scoring service pure: input = tip + result + stage + jokers; output = points. Fully unit-testable, so a rule change before the tournament won't quietly break.

---

## Cross-cutting concerns

- **Time zones — handle from day one.** Use `DateTimeOffset` (or NodaTime `Instant`) everywhere on the backend. Store the "1 hour before kickoff" deadline in UTC; display in Hungarian time. The backend is the source of truth for whether tipping is open — the frontend only renders the countdown.
- **Env config (Vite).** Only `VITE_`-prefixed vars reach the client and are public. The API base URL and the Auth0 domain + SPA client ID are fine to ship (they're meant to be public); secrets (API keys, Auth0 client *secret*, DB strings) never go in the frontend — they live in Azure Key Vault on the backend.
- **Resilience.** Wrap the football API behind `IHttpClientFactory` + Polly so a transient error can't stall a whole scoring cycle.

---

## Summary

**Frontend:** Vite + React + TypeScript + TanStack Query + Tailwind/shadcn + SignalR client
**Backend:** ASP.NET Core 10 (LTS) + EF Core 10 + SignalR + Hosted Services (+ optional Hangfire)
**Database:** PostgreSQL (managed)
**Auth:** Auth0 (Authorization Code + PKCE in the SPA; JwtBearer validation in the API)
**Hosting:** Azure end-to-end — Static Web Apps (frontend) + App Service (backend) + Database for PostgreSQL Flexible Server + Key Vault + Application Insights
**External:** Football data API (+ manual fallback), AI provider, Resend
