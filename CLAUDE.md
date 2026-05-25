# Tip4Gen

Hungarian-language Football World Cup tipping game web app. Target launch: **2026-06-11** (WC 2026 opener). Audience: 50–200 friends-and-extended-circle.

## Source-of-truth docs

- `Tip4Gen-guide.html` — **the rules.** Scoring, joker, team aggregation, tiebreakers, abandoned matches, AI fallback. When in doubt about behavior, read this — the §-numbered sections are referenced throughout the plan.
- `implementation-plan.md` — phased delivery plan (Phases 0–11), runway tracker, cut order, status snapshot at top.
- `tech-stack.md` — original stack proposal (read with skepticism: Auth0 + full Azure are flagged as overkill for this scope; see plan's stack callouts).

## Layout

```
backend/                    ASP.NET Core 9 solution (planned 10, on 9 for speed)
  src/Tip4Gen.Api           Controllers, Program.cs, Serilog, OpenAPI, Auth0
    Auth/                   Auth0Options, AuthExtensions, CurrentUserService
    Controllers/            HealthController, MeController, AdminController,
                            FixturesAdminController (POST /api/admin/fixtures/seed)
  src/Tip4Gen.Domain        Pure domain types — no EF, no ASP.NET refs
    Users/User.cs
    Tournaments/            Stage + MatchStatus enums, Tournament, NationalTeam,
                            Match, StageMapper, MatchStatusMapper
    Tournaments/Events/     MatchFinalized record + IMatchFinalizedHandler
    Football/               IFootballDataProvider + ProviderFixture/Team/Status
  src/Tip4Gen.Infrastructure  EF Core, external clients
    Persistence/AppDbContext.cs + Migrations/
    DependencyInjection.cs  AddInfrastructure(IConfiguration)
    Football/               ApiFootballProvider + Options + DTOs
    Tournaments/            FixtureSyncService (idempotent upsert + event dispatch)
  src/Tip4Gen.Workers       BackgroundService host — FixturePoller (Phase 2),
                            shares Api's UserSecretsId for shared dev config
    FixturePoller.cs        Calls FixtureSyncService when DB has active matches
    FixturePollerOptions.cs IntervalMinutes / ActiveWindowHours / LookaheadMinutes
  tests/Tip4Gen.Domain.Tests  xUnit — StageMapper + MatchStatusMapper (35 tests)
web/                        Vite + React 19 + TS frontend
  src/auth/                 AuthProvider, RequireAuth, useApi, authConfig
  src/components/Topbar.tsx
  src/pages/{Home,Me}.tsx
  src/main.tsx              <AuthProvider><BrowserRouter><App/>…
  src/index.css             Single line: @import "tailwindcss"
  vite.config.ts            port 5173, strictPort, dev proxy /api → :5050
  .env.local                VITE_AUTH0_* (gitignored)
```

Dependency direction: **Api → Infrastructure → Domain**; **Workers → Infrastructure**; **Tests → Domain**. Don't add ASP.NET or EF to Domain.

## Dev commands (PowerShell)

```powershell
# Backend (port 5050)
dotnet run --project backend/src/Tip4Gen.Api

# Workers (fixture poller — separate process, shares Api user-secrets)
dotnet run --project backend/src/Tip4Gen.Workers
# Quick tick for testing: --FixturePoller:IntervalMinutes=1 --FixturePoller:StartupDelaySeconds=2

# Frontend (port 5173, proxies /api)
npm run dev --prefix web

# Build everything
dotnet build backend/Tip4Gen.sln
npm run build --prefix web

# Tests
dotnet test backend/Tip4Gen.sln

# Apply EF migrations to local Postgres
dotnet ef database update --project backend/src/Tip4Gen.Infrastructure --startup-project backend/src/Tip4Gen.Api
```

## Stack conventions

- **Tailwind v4** via `@tailwindcss/vite` plugin. **No PostCSS, no `tailwind.config.js`** — single `@import "tailwindcss"` in `index.css`. Configure via `@theme` in CSS, not JS.
- **React Router v7** — unified package `react-router` (not `react-router-dom`).
- **Serilog** wired via `Host.UseSerilog((ctx, _, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration))` — all config lives in `appsettings.json`.
- **CORS** policy applies to non-proxied paths only; in dev, Vite proxies `/api` so CORS rarely fires.
- **Time zones:** all deadlines and timestamps stored in **UTC**, displayed in **Europe/Budapest**.
- **Language:** UI copy is **Hungarian**. Code, identifiers, comments in English.
- **Vite** must run with `strictPort: true` so the SPA always lives on `:5173` — Auth0 callback URLs are hard-coded to that port.

## Secrets

All dev credentials live in `dotnet user-secrets` for `backend/src/Tip4Gen.Api` — never in `appsettings.json`, never in the repo. The Workers project's csproj declares the same `UserSecretsId`, so both processes resolve the same secret store. Current keys:

- `ConnectionStrings:AppDb` — local Postgres 18 (`tip4gen_dev` DB, `tip4gen` role)
- `Auth0:Domain` / `Auth0:Audience` / `Auth0:AdminSub` — tenant `dev-yifcd0c5p4s0wcj5.eu.auth0.com`, audience `https://api.tip4gen.local`
- `FootballApi:Provider` / `ApiKey` / `BaseUrl` / `LeagueId` / `Season` — api-football Free plan, league=1, season=2022 (see Data caveat below)

Frontend env in `web/.env.local` (also gitignored): `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE`.

## Data caveat — WC 2026 fixtures

api-football's Free plan does NOT include WC 2026 fixture coverage (`coverage.fixtures=false`). We develop against **WC 2022** (`FootballApi:Season=2022`, 64 real matches with results) so the scoring engine can be tested with real data. To launch with real 2026 fixtures we must either upgrade api-football, swap to a different provider, OR rely on admin manual entry (Phase 8) — that fallback is **load-bearing**, not optional.

Schema must accommodate both formats: 2022 had 32 teams (no R32 round), 2026 has 48 teams with groups → R32 → R16 → QF → SF → bronze → final.

**Round labels are matchday-style, not group-letter-style.** api-football's WC fixtures use `Group Stage - 1/2/3` — the group letter (A–L) is *not* in `league.round`. `StageMapper` returns `(Stage.Group, null)` for those, so `matches.group_code` stays null until we enrich it from `/standings`. Deferred follow-up — schema already supports it, the seed/poller code doesn't yet. If you see code that assumes `group_code` is populated, check this first.

**Quota discipline.** api-football Free plan is 100 req/day. The seed endpoint costs 2 (one /fixtures + one /teams). The poller costs 1 per tick (only /fixtures), and only when there's a Live or imminent match — at default cadence it stays well under cap. Don't add a new poller without doing the same active-window check.

## Scoring rules — quick reference (full detail in guide §3–§9)

- Per-match: best-matching category among **10 / 5 / 3 / 1 / 0** points.
- Stage multipliers: group `1×` · R32 `1.5×` · R16 `1.5×` · QF `2×` · SF `2.5×` · bronze `2×` · final `3×`.
- **Joker** (max 3 per user, group stage only, one per match) **doubles after multiplier**.
- "One team's goal count matches" is strictly **home-to-home / away-to-away** (not swapped).
- Team aggregation: **best 3 of 4** member scores per match.
- AI fallback: auto **1–1** tip with `is_ai_fallback=true` if AI provider hasn't returned by **T-1h**.
- Tip deadline: **kickoff − 1h**, enforced server-side in UTC.
- Long-term tips (winner, top scorer) lock at **tournament-start kickoff**.

## Admin

Single admin (the project owner). Gated by Auth0 `sub` claim matching the `Auth0:AdminSub` user-secret. Every `/api/admin/*` write must record a row in `admin_audit` in the same transaction (before/after JSON).

## Auth gotchas (learned the hard way — see commits f094978, 3763e11)

- ASP.NET's JWT handler **remaps `sub` → `ClaimTypes.NameIdentifier`** by default. `CurrentUserService.Auth0Sub` looks under both names; don't "simplify" it back to one lookup.
- The same remapping bites **authorization policies**: `policy.RequireClaim("sub", AdminSub)` silently returns 403 even with the correct sub configured, because the claim ends up under `ClaimTypes.NameIdentifier`. `JsonWebTokenHandler.DefaultMapInboundClaims = false` does not stop it on the policy code path. Use `policy.RequireAssertion(...)` checking both names — see `AuthExtensions.AdminPolicy`.
- Auth0 silently rejects an empty `audience` parameter with "Service not found". The frontend `AuthProvider` only spreads `audience` when it's truthy — don't pass `audience: ''`.
- Auth0 API identifiers **cannot be edited after creation**. Whitespace typos require delete + recreate.
- Even with "Allow Skipping User Consent" on, the SPA must be **explicitly enabled** for the API under **Application → APIs** in the Auth0 dashboard.

## Things not to do

- Don't introduce PostCSS or a `tailwind.config.js` — Tailwind v4 doesn't need them.
- Don't add EF Core or ASP.NET references to `Tip4Gen.Domain`.
- Don't put credentials in `appsettings.json` or commit `web/.env.local` — use `dotnet user-secrets` and `.env.local` (both gitignored).
- Don't fabricate dates from training data — today's date comes from the system context. WC 2026 starts **2026-06-11**.
- Don't widen scope mid-phase. If something doesn't fit in the current phase, note it in the plan and move on.
