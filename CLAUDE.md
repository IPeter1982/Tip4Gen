# Tip4Gen

Hungarian-language Football World Cup tipping game web app. Target launch: **2026-06-11** (WC 2026 opener). Audience: 50–200 friends-and-extended-circle.

## Source-of-truth docs

- `Tip4Gen-guide.html` — **the rules.** Scoring, joker, team aggregation, tiebreakers, abandoned matches, AI fallback. When in doubt about behavior, read this — the §-numbered sections are referenced throughout the plan.
- `implementation-plan.md` — phased delivery plan (Phases 0–11), runway tracker, cut order, status snapshot at top.
- `tech-stack.md` — original stack proposal (read with skepticism: Auth0 + full Azure are flagged as overkill for this scope; see plan's stack callouts).

## Layout

```
backend/                    ASP.NET Core 9 solution (planned 10, on 9 for speed)
  src/Tip4Gen.Api           Controllers, Program.cs, Serilog, OpenAPI
  src/Tip4Gen.Domain        Pure domain types — no EF, no ASP.NET refs
  src/Tip4Gen.Infrastructure  EF Core, external clients (planned)
  src/Tip4Gen.Workers       BackgroundService host (planned)
  tests/Tip4Gen.Domain.Tests  xUnit
web/                        Vite + React 19 + TS frontend
  src/App.tsx               Landing page (Phase 0 health check)
  src/main.tsx              React Router v7 root
  src/index.css             Single line: @import "tailwindcss"
  vite.config.ts            Dev proxy: /api → http://localhost:5050
```

Dependency direction: **Api → Infrastructure → Domain**; **Workers → Infrastructure**; **Tests → Domain**. Don't add ASP.NET or EF to Domain.

## Dev commands (PowerShell)

```powershell
# Backend (port 5050)
dotnet run --project backend/src/Tip4Gen.Api

# Frontend (port 5173, proxies /api)
npm run dev --prefix web

# Build everything
dotnet build backend/Tip4Gen.sln
npm run build --prefix web

# Tests
dotnet test backend/Tip4Gen.sln
```

## Stack conventions

- **Tailwind v4** via `@tailwindcss/vite` plugin. **No PostCSS, no `tailwind.config.js`** — single `@import "tailwindcss"` in `index.css`. Configure via `@theme` in CSS, not JS.
- **React Router v7** — unified package `react-router` (not `react-router-dom`).
- **Serilog** wired via `Host.UseSerilog((ctx, _, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration))` — all config lives in `appsettings.json`.
- **CORS** policy applies to non-proxied paths only; in dev, Vite proxies `/api` so CORS rarely fires.
- **Time zones:** all deadlines and timestamps stored in **UTC**, displayed in **Europe/Budapest**.
- **Language:** UI copy is **Hungarian**. Code, identifiers, comments in English.

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

Single admin (the project owner). Gated by Auth0 `sub` claim matching `ADMIN_AUTH0_SUB` env var. Every `/api/admin/*` write must record a row in `admin_audit` in the same transaction (before/after JSON).

## Things not to do

- Don't introduce PostCSS or a `tailwind.config.js` — Tailwind v4 doesn't need them.
- Don't add EF Core or ASP.NET references to `Tip4Gen.Domain`.
- Don't fabricate dates from training data — today's date comes from the system context. WC 2026 starts **2026-06-11**.
- Don't widen scope mid-phase. If something doesn't fit in the current phase, note it in the plan and move on.
