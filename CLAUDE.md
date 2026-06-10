# Tip4Gen

Hungarian-language Football World Cup tipping game web app. Target launch: **2026-06-11** (WC 2026 opener). Audience: 50–200 friends-and-extended-circle.

## Source-of-truth docs

- `Tip4Gen-guide.html` — **the rules.** Scoring, joker, team aggregation, tiebreakers, abandoned matches, AI fallback. When in doubt about behavior, read this — the §-numbered sections are referenced throughout the plan.
- `implementation-plan.md` — phased delivery plan (Phases 0–11), runway tracker, cut order, status snapshot at top.
- `tech-stack.md` — original stack proposal. Hosting section is **superseded** — see the Railway addendum at the bottom of that file and the "Prod hosting (Railway)" section below.

## Layout

```
backend/                    ASP.NET Core 9 solution (planned 10, on 9 for speed)
  src/Tip4Gen.Api           Controllers, Program.cs, Serilog, OpenAPI, Auth0
    Auth/                   Auth0Options, AuthExtensions, CurrentUserService
    Avatars/                DataUrlParser (decodes data:image/...;base64,...
                            payloads from the SPA canvas-resize pipeline)
    Controllers/            HealthController, MeController (+ avatar PUT/DELETE),
                            AdminController, FixturesAdminController,
                            ScoringAdminController,
                            TeamsAdminController (POST /lock + GET / list-all +
                              PUT /{id}/name + DELETE /{id} +
                              DELETE /{id}/members/{memberId}),
                            NationalTeamsController, MatchesController
                              (+ /tips returns per-tip Score after deadline),
                            TipsController, LongTipsController (+ GET /all
                              lists every other user's winner + top scorer
                              after tournament lock; 409 NotYetUnlocked before),
                            TeamsController (+ GET / browse all teams +
                              POST /{id}/join invite-less direct join),
                            LeaderboardController,
                            UsersController (avatar GET + closed-match tip history),
                            AiTipperAdminController (preview + manual
                              per-match run that bypasses the T-2h/T-1h policy),
                            AiAvatarController (public binary GET),
                            AiAvatarAdminController (admin PUT/DELETE),
                            MatchesAdminController (result/cancel/postpone),
                            AdminAuditController, LongTipsAdminController,
                            PlayersController (list for the SPA combobox),
                            PlayersAdminController (POST /import + GET /last-import)
  src/Tip4Gen.Domain        Pure domain types — no EF, no ASP.NET refs
    Users/User.cs           + UserRulesValidator (display name + avatar bytes)
    Settings/AiAvatarSetting.cs  Singleton entity (id = 1) for admin AI avatar
    Tournaments/            Stage + MatchStatus enums, Tournament (winner +
                            TopScorerPlayerId FKs), NationalTeam,
                            Match, StageMapper, MatchStatusMapper
    Tournaments/Events/     MatchFinalized record + IMatchFinalizedHandler
    Football/               IFootballDataProvider + ProviderFixture/Team/Status
    Players/                Player entity (Id, NationalTeamId FK, Name),
                            ParsedPlayer record (parser→importer wire type)
    Tipping/                Tip (UserId nullable; TeamMemberId nullable for AI tips;
                            IsAiFallback, Reasoning), TipRulesValidator,
                            LongTermTip (TargetTeamId XOR TargetPlayerId FKs)
                            + LongTermTipRulesValidator
    Scoring/                MatchResult, ScoreCategory, ScoringResult,
                            StageMultipliers, MatchScorer (pure),
                            ScoredTip (dual-key: UserId or TeamMemberId)
    Teams/                  Team (+ Unlock() flips Locked → Forming for the admin
                              remove-member auto-revert), TeamMember (UserId
                              nullable for AI slot), TeamInvite,
                            TeamStatus + AiMode enums,
                            TeamRulesValidator, TeamLockPolicy, TeamAggregator (pure)
    Leaderboard/            LeaderboardEntry, LeaderboardRanker (§9 tiebreakers,
                            shared placement), StreakCalculator (pure)
    Ai/                     AiTipResponse, AiTipResult tagged union, IAiTipper,
                            AiTipPromptBuilder (pure), AiTipResponseValidator (pure),
                            AiTipSchedulePolicy (pure), AiTipAttempt entity
    Admin/                  AdminAudit entity, AdminAuditAction enum
  src/Tip4Gen.Infrastructure  EF Core, external clients
    Persistence/AppDbContext.cs + Migrations/
    DependencyInjection.cs  AddInfrastructure(IConfiguration)
    Football/               WorldCup26IrProvider + Options + DTOs + JwtCache
                            (anonymous-first; lazy /auth/register+authenticate
                            on 401, token cached in memory)
    Tournaments/            FixtureSyncService (idempotent upsert + event dispatch)
    Tipping/                TipsService, LongTermTipsService (tagged-union results;
                              + GetAllPublicAsync for the post-lock reveal,
                              server-gated on tournament.StartsAtUtc),
                            UserTipHistoryService (closed-match drilldown from
                            individual leaderboard — LEFT JOIN scored_tips)
    Scoring/                MatchScoringService (idempotent re-score, dual-key),
                            MatchFinalizedScoringHandler (event handler)
    Teams/                  TeamsService (CRUD + invites + join, tagged-union),
                            TeamsAdminService (admin list/rename/delete +
                              remove-member with auto-revert-to-Forming +
                              last-human cascade-delete; audit per call),
                            TeamLockService (Forming → Locked/Disqualified pass),
                            TeamAggregationService (per-match sum-of-3 view)
    Leaderboard/            IndividualLeaderboardService (humans only)
                            + TeamLeaderboardService (humans + AI, dual-key)
    Ai/                     OpenAiOptions, OpenAiTipper (Chat Completions +
                            json_object mode; returns Disabled when ApiKey unset),
                            AiTippingService orchestrator
    Admin/                  IAdminAuditWriter + AdminAuditWriter (stage row in
                            same transaction as caller), MatchAdminService
                            (SetResult / Cancel / Postpone), LongTipOutcomesService
    Settings/               AiAvatarAdminService (singleton upsert/clear +
                            audit; same DataUrlParser/ValidateAvatar pipeline)
    Players/                WikipediaSquadsProviderOptions, IWikipediaSquadsProvider
                            + WikipediaSquadsProvider (MediaWiki parse API client),
                            WikipediaSquadsParser (pure HtmlAgilityPack walker),
                            PlayersImportService (idempotent insert + audit row)
    Workers/                BackgroundService host — FixturePoller (Phase 2)
                            + TeamLockJob (Phase 5) + AiTippingJob (Phase 6),
                            co-located in the API process (Phase 8.5 deploy)
      FixturePoller.cs        Calls FixtureSyncService when DB has active matches
      FixturePollerOptions.cs IntervalMinutes / ActiveWindowHours / LookaheadMinutes
      TeamLockJob.cs          Calls TeamLockService.LockAllAsync on a coarse cadence
      TeamLockJobOptions.cs   IntervalMinutes (default 5) / StartupDelaySeconds
      AiTippingJob.cs         Calls AiTippingService.RunOnceAsync on a 5-min tick
      AiTippingJobOptions.cs  IntervalMinutes (default 5) / StartupDelaySeconds
  tests/Tip4Gen.Domain.Tests  xUnit — StageMapper, MatchStatusMapper,
                              TipRulesValidator, LongTermTipRulesValidator,
                              UserRulesValidator (display name + avatar),
                              MatchScorer, StageMultipliers, TeamRulesValidator,
                              TeamAggregator, TeamLockPolicy, LeaderboardRanker,
                              StreakCalculator, AiTipResponseValidator,
                              AiTipSchedulePolicy, AiTipPromptBuilder
  tests/Tip4Gen.Infrastructure.Tests  xUnit — WikipediaSquadsParser golden tests
                              (only Infra-bound logic worth pure-testing; HTTP
                              providers stay untested here)
web/                        Vite + React 19 + TS frontend
  src/auth/                 AuthProvider, RequireAuth, useApi (typed + ApiError),
                            authConfig
  src/api/                  errors.ts (ApiError + ProblemDetails),
                            types.ts (shared response shapes),
                            hooks.ts (TanStack Query wrappers)
  src/lib/format.ts         Budapest TZ formatters + countdown + HU labels
  src/lib/imageResize.ts    Canvas square-crop → 128×128 JPEG q=0.85 → data URL
                            (shared by /me upload and /admin/ai-avatar)
  src/lib/teamFlag.ts       3-letter code → ISO 3166-1 alpha-2 (worldcup26.ir
                            uses real FIFA codes so most aliases are now dead
                            weight, kept as a buffer in case the upstream swaps;
                            ENG/WAL/SCO/NIR → gb-eng/gb-wls/gb-sct/gb-nir)
  src/lib/navIcons.tsx      NAV_ITEMS (single source of truth for Topbar
                            links + per-route icons); `requiresAuth` flag
                            hides links from logged-out users
  src/lib/teamReasons.ts    STATUS_LABEL + reasonMessage (ApiError.reason
                            → Hungarian) + errorMessage helpers, shared
                            by Team.tsx and TeamAll.tsx
  src/components/Topbar.tsx Filters NAV_ITEMS by `requiresAuth` + isAdmin;
                            inline nav on `md+`, Headless UI Popover-driven
                            hamburger menu under `md` (same `visibleItems` list)
  src/pages/                Home, Me, Matches, TipSubmit, LongTips (form
                              + post-lock `Mindenki tippje` reveal section),
                            Team, TeamAll (browse + direct-join), TeamJoin,
                            Leaderboard, UserTips (closed-match history for one player)
  src/pages/admin/          AdminMatches, AdminMatchEditor, AdminAudit,
                            AdminLongTips, AdminAiAvatar, AdminPlayers
                              (one-button idempotent Wikipedia squads importer),
                            AdminTeams (list-all + rename modal + delete-team
                              ConfirmDialog + per-member remove with contextual
                              warning when the team would drop below 3 or lose
                              its last human)
  src/auth/RequireAdmin     Gates admin routes; renders 403 panel for non-admins
  src/components/           Topbar, ConfirmDialog (modal w/ destructive variant),
                            Avatar (img-by-userId or letter-circle fallback;
                            isAi prop routes to the global /api/ai-avatar),
                            TeamFlag (flagcdn.com <img>, hides on null/error),
                            TeamLabel (flag + name flex wrapper),
                            TeamSelect (@headlessui/react Listbox so flags
                            render inside <option> rows too),
                            PlayerSelect (@headlessui/react Combobox — searchable
                            with a 3-char min-query gate; renders flag + name +
                            FIFA code on each option),
                            ThemeToggle (Sun/Moon button rendered in Topbar)
  src/theme/                ThemeProvider — class-based dark/light context
                            (`prefers-color-scheme` fallback + localStorage
                            persistence under `tip4gen.theme`), useTheme hook
  src/main.tsx              <ThemeProvider><AuthProvider><QueryClientProvider>…
  src/index.css             @import "tailwindcss" + @theme tokens (light) +
                            .dark overrides (navy/emerald stadium palette) +
                            @custom-variant dark + glow-accent / live-dot
                            utilities. No JS Tailwind config.
  index.html                Inline <head> script reads localStorage +
                            prefers-color-scheme and sets .dark before React
                            mounts (no first-paint flash). Don't remove it.
  vite.config.ts            port 5173, strictPort, dev proxy /api → :5050
  .env.local                VITE_AUTH0_* (gitignored)
```

Dependency direction: **Api → Infrastructure → Domain**; **Tip4Gen.Domain.Tests → Domain**; **Tip4Gen.Infrastructure.Tests → Infrastructure → Domain**. Don't add ASP.NET or EF to Domain. The only NuGet leak into Infrastructure outside the usual EF + HttpClient set is **HtmlAgilityPack** (used by `WikipediaSquadsParser`) — keep it confined to `Infrastructure/Players/`. Background services live in `Tip4Gen.Api/Workers/` and run in the API process (one deployable on Railway).

## Dev commands (PowerShell)

```powershell
# Backend (port 5050) — includes the three background services (FixturePoller,
# TeamLockJob, AiTippingJob). Override their cadence via config for quick ticks:
#   dotnet run --project backend/src/Tip4Gen.Api --FixturePoller:IntervalMinutes=1
dotnet run --project backend/src/Tip4Gen.Api

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

- **Tailwind v4** via `@tailwindcss/vite` plugin. **No PostCSS, no `tailwind.config.js`** — `@import "tailwindcss"` plus an `@theme` block in `index.css`. Configure via `@theme` in CSS, not JS. Colors are exposed as **semantic tokens** (`bg-surface`, `bg-elevated`, `bg-sunken`, `text-fg-default`/`-muted`/`-subtle`, `bg-accent`/`-soft`/`-strong`, `text-on-accent`, `text-danger`/`-success`/`-warning`, `bg-live`/`-soft`, `border-border-subtle`/`-strong`, `ring-ring-focus`). Don't use raw palette classes (`bg-stone-*`, `bg-emerald-500`, etc.) outside `index.css` — they bypass dark-mode token swaps.
- **Dark mode** is class-based (`<html class="dark">`) via `@custom-variant dark (&:where(.dark, .dark *));` in `index.css`. Light and dark share the same utility class names — only the CSS variables swap — so 99% of components have no `dark:` prefix. `ThemeProvider` (`src/theme/`) drives the toggle; the inline `<head>` script in `index.html` sets the class pre-hydration to avoid FOUC. Real `dark:` usage is reserved for *shape* differences (e.g., `TeamFlag` rings white flags more visibly on navy).
- **Icons** are `lucide-react`, imported directly per icon (no shim component) — Vite tree-shakes. Default sizes: `16` inline labels, `20` buttons, `28+` heroes. Color via `text-*` classes (lucide inherits `currentColor`).
- **React Router v7** — unified package `react-router` (not `react-router-dom`). No `ScrollRestoration` mounted; **submit→list flows use the focus-on-return pattern instead**: the submit page passes the item id via `navigate('/list', { state: { focusItemId: id } })`, the list page reads `location.state` in a `useEffect`, calls `scrollIntoView({ block: 'center' })` on `#item-${id}` inside a `requestAnimationFrame`, briefly flashes the existing live/highlight styling (1.5s + `clearTimeout` on cleanup), then `navigate(location.pathname + location.search, { replace: true, state: null })` so F5 / browser-back don't re-trigger. Live today on `TipSubmit` → `Matches`; reuse the same shape for any future submit→list pair instead of mounting `ScrollRestoration` (push navigation after a mutation wouldn't restore anyway — there's no prior scroll to restore *to*).
- **TanStack Query v5** + **react-hook-form** + **Zod** for data fetching and forms. `useApi` returns typed `get/put/post/del` helpers and parses ProblemDetails into `ApiError` (with `reason` extension).
- **@headlessui/react** powers two dropdowns and the mobile nav: `TeamSelect` (Listbox, fixed-size 48-team list), `PlayerSelect` (Combobox, ~600-row searchable), and the `Topbar` mobile menu (Popover + `CloseButton as={NavLink}` so a tap navigates *and* closes the panel). Pick **Listbox** when the list is small enough to scroll and you don't need text-search; pick **Combobox** when the user needs to type to narrow; pick **Popover** for menu-style triggers (outside-click + Escape close come free — no need for custom backdrop/state). Don't reach for any of these for plain text dropdowns.
- **Serilog** wired via `Host.UseSerilog((ctx, _, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration))` — all config lives in `appsettings.json`.
- **Enums on the wire are strings.** `Program.cs` registers `JsonStringEnumConverter` globally, so any enum returned from a controller serializes by name (`"Forming"`, not `0`). New DTOs can use enum types directly; don't sprinkle `.ToString()` at the controller boundary. Frontend `type` aliases (`type TeamStatus = 'Forming' | 'Locked' | …`) compare-by-string and rely on this. `MatchesController` still does manual `.ToString()` from before the global converter landed — fine, no behavior change, can be cleaned up later.
- **CORS** policy applies to non-proxied paths only; in dev, Vite proxies `/api` so CORS rarely fires.
- **Time zones:** all deadlines and timestamps stored in **UTC**, displayed in **Europe/Budapest**.
- **Language:** UI copy is **Hungarian**. Code, identifiers, comments in English.
- **Vite** must run with `strictPort: true` so the SPA always lives on `:5173` — Auth0 callback URLs are hard-coded to that port.

## API error contract

Service layer returns tagged-union results (e.g. `TipUpsertResult.Success | MatchNotFound | Rejected`). Controllers translate `Rejected` into RFC 7807 ProblemDetails with:
- `title`: short English label
- `detail`: Hungarian user-facing message
- `reason`: machine-readable enum name (e.g. `DeadlinePassed`, `JokerNotAllowedOnKnockoutMatch`, `Locked`)

The SPA's `ApiError` lifts `reason` to a typed field so forms can map it to per-field errors without parsing detail strings. Keep the enum names stable — they're the contract.

## Secrets

All dev credentials live in `dotnet user-secrets` for `backend/src/Tip4Gen.Api` — never in `appsettings.json`, never in the repo. Current keys:

- `ConnectionStrings:AppDb` — local Postgres 18 (`tip4gen_dev` DB, `tip4gen` role)
- `Auth0:Domain` / `Auth0:Audience` / `Auth0:AdminSub` — tenant `dev-yifcd0c5p4s0wcj5.eu.auth0.com`, audience `https://api.tip4gen.local`
- `WorldCup26Ir:BaseUrl` / `LeagueId` / `Season` — worldcup26.ir (free, anonymous). Defaults baked into `WorldCup26IrOptions` so you don't need to set anything for local dev. `WorldCup26Ir:AuthEmail` / `:AuthPassword` are optional — the provider runs anonymous and only attempts a JWT register+login if the upstream ever returns 401.
- `OpenAi:ApiKey` — OpenAI project-scoped key (`sk-proj-…`). Optional: when unset, `OpenAiTipper` returns `AiTipResult.Disabled` and the schedule policy still writes the 1–1 fallback at T-1h. `OpenAi:Model` (default `gpt-4o-mini`), `OpenAi:Temperature` (0.7), `OpenAi:TimeoutSeconds` (15) are bindable from the same section.
- `WikipediaSquads:BaseUrl` / `:PageTitle` / `:TimeoutSeconds` / `:UserAgent` — Wikipedia squad importer config. All defaulted in `WikipediaSquadsProviderOptions` (`https://en.wikipedia.org` + `2026_FIFA_World_Cup_squads`). No credentials needed. Only override the UA if you fork; Wikipedia's ToS requires an identifiable string with a contact email.

Frontend env in `web/.env.local` (also gitignored): `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE`.

## Prod hosting (Railway)

Single Railway project (EU-West/Amsterdam), three services + the Postgres plugin. Auto-deploys on push to `main`. The SPA service's nginx reverse-proxies `/api/*` to the API over Railway's private network, so prod is same-origin and CORS-free (mirrors the dev Vite proxy).

```
SPA service (nginx)  ──/api private──►  API service (.NET)  ──private──►  Postgres plugin
```

- **API service** — `backend/Dockerfile` (multi-stage sdk:9.0 → aspnet:9.0). Listens on `$PORT`. Runs `db.Database.Migrate()` at startup. The three `BackgroundService` jobs (FixturePoller / TeamLockJob / AiTippingJob) live in this same process.
- **SPA service** — `web/Dockerfile` (node:22-alpine build → nginx:alpine serve). `web/nginx.conf` is templated with `envsubst` at container start so `$PORT`, `$API_HOST`, `$API_PORT` come from Railway env. Nginx-runtime variables (`$uri`, `$host`, etc.) are *not* in the envsubst allowlist and survive intact.
- **DB** — Railway Postgres plugin exposes `DATABASE_URL` in URI form. `Tip4Gen.Infrastructure/DependencyInjection.cs` translates it to Npgsql keyword/value form (with `SSL Mode=Require;Trust Server Certificate=true`) when `ConnectionStrings:AppDb` is not set.

**API env vars** (all `__` for nested keys):
- `ASPNETCORE_ENVIRONMENT=Production`
- `DATABASE_URL` (auto-injected from the Postgres plugin reference)
- `Auth0__Domain`, `Auth0__Audience`, `Auth0__AdminSub`
- `WorldCup26Ir__BaseUrl` (optional, defaults to `https://worldcup26.ir`), `WorldCup26Ir__AuthEmail` / `__AuthPassword` (optional — only used if the upstream ever 401s)
- `OpenAi__ApiKey` (optional — when unset, the tipper short-circuits and the 1–1 fallback still writes at T-1h)
- `WikipediaSquads__*` (all optional — defaults are baked in; no env var needs to be set for the importer to work in prod)
- `Cors__AllowedOrigin` = SPA public URL (defensive belt-and-braces; the reverse-proxy means the browser never CORS-talks to the API)

**SPA env vars**:
- *Runtime* — `API_HOST` (default `api.railway.internal`), `API_PORT` (default `8080`), `PORT` (auto-injected by Railway). Consumed by nginx for the reverse-proxy.
- *Build-time* — `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE`. Vite inlines these into the JS bundle when `vite build` runs, so they must be present during `docker build`. `web/Dockerfile` declares them as `ARG`s in the build stage; Railway auto-passes any service variable as `--build-arg` when a matching ARG exists in the Dockerfile, so set them on the Web service like any other env var.

The API base URL is *not* a `VITE_*` var — same-origin via nginx means `useApi` just calls `/api/...` as in dev.

**Auth0 callback URLs**: add the SPA's `*.up.railway.app` URL to Allowed Callback URLs, Allowed Logout URLs, Allowed Web Origins (alongside the existing `http://localhost:5173`). API audience identifier unchanged.

**Deploy + rollback**: push to `main` triggers per-service builds. Failed releases → "Redeploy" a previous build via the Railway dashboard. Bad migration → revert locally, push, then `railway run dotnet ef migrations remove --project backend/src/Tip4Gen.Infrastructure --startup-project backend/src/Tip4Gen.Api`.

## Data caveat — WC 2026 fixtures (worldcup26.ir)

We pull fixtures and teams from **worldcup26.ir** — a free, community-run (one maintainer: rezarahiminia) Node/Express + MongoDB API. 104 matches sourced from Wikipedia. Anonymous access works for `GET /get/games` and `GET /get/teams` despite the Swagger UI claiming JWT is required; the provider runs anonymous and only tries `/auth/register` + `/auth/authenticate` lazily on a 401. Rate limit is 500 req/min (effectively infinite for our 5-min poller cadence).

**Risks to know:**

- **Single-maintainer free project.** If it goes offline mid-tournament, admin manual entry (Phase 8 — `/api/admin/matches/{id}/result|cancel|postpone`) is the only fallback.
- **Timezone is US Eastern, hard-coded.** `local_date` is `MM/dd/yyyy HH:mm` with no explicit TZ; `WorldCup26IrProvider.ParseEasternToUtc` treats it as `America/New_York` (Eastern). Mexico City / Vancouver / LA matches will drift 1–3h if the upstream is not actually Eastern — spot-check the Estadio Azteca opener (`06/11/2026 13:00` → `2026-06-11 17:00 UTC` → 11:00 CDT, matches FIFA's published kickoff) and one West-Coast match before trusting blindly. If wrong: extend the provider with a stadium_id → IANA TZ map.
- **No Live / Postponed / Cancelled status.** The upstream only exposes `finished: "TRUE"|"FALSE"`. The provider maps TRUE → Finished, FALSE → Scheduled — there is no Live transition. The FixturePoller's "live or imminent" guard still works because it queries our local `MatchStatus` (Scheduled + within window). Cancellations and postponements go through admin.
- **Knockout placeholders.** Until the bracket fills, `home_team_id` / `away_team_id` are `"0"`. The provider drops those rows; the poller picks them up once real team IDs land.
- **Group code is populated from day one.** `WorldCupGame.group` carries the A–L letter directly for `type=="group"`. The historical "matches.group_code stays null" gotcha is gone.

**StageMapper** now exposes a single `FromWorldCupType(string)` for inputs `"group" | "r32" | "r16" | "qf" | "sf" | "third" | "final"`. The old api-football `Group Stage - N` parser is gone.

## Scoring rules — quick reference (full detail in guide §3–§9)

- Per-match categories (best wins): **Exact 10 · WinnerAndGoalDiff 5 · Winner 3 · OneTeamGoals 1 · Nothing 0**.
- Stage multipliers: group `1×` · R32 `1.5×` · R16 `1.5×` · QF `2×` · SF `2.5×` · bronze `2×` · final `3×`.
- **Joker** (max 3 per user, group stage only, one per match) **doubles after multiplier**.
- "One team's goal count matches" is strictly **home-to-home / away-to-away** (not swapped).
- Half-multiplier results round **away from zero** (`5 × 1.5 = 7.5 → 8`). `MatchScorer` is the single source of truth.
- Scoring is **idempotent**: `MatchScoringService.ScoreMatchAsync` deletes prior `scored_tips` for the match then re-inserts in one `SaveChanges`. Wired into `MatchFinalized` for auto-scoring and exposed via `POST /api/admin/matches/{id}/rescore` for manual re-runs.
- `scored_tips.user_id` and `scored_tips.team_member_id` are **denormalized** from `tips`. Exactly one is set per row, matching the source tip; leaderboard queries pick a side without joining through matches.
- Team aggregation: **sum of all 3** member scores per match (`TeamAggregator.ForMatch`) — every tipper counts, no dropping, no tiebreak. A non-tipping member's 0 directly hurts the team total.
- AI fallback: auto **1–1** tip with `is_ai_fallback=true` if AI provider hasn't returned by **T-1h**.
- Tip deadline: **kickoff − 1h**, enforced server-side in UTC.
- Long-term tips (winner, top scorer) lock at **tournament-start kickoff**. Both are stored as FKs — top scorer is `target_player_id` into the `players` table (no free text), so correctness checks are strict GUID equality.

## Admin

Single admin (the project owner). Gated by Auth0 `sub` claim matching the `Auth0:AdminSub` user-secret. Every `/api/admin/*` write must record a row in `admin_audit` in the same transaction (before/after JSON). Audit lives in the service layer (not middleware) — each admin service injects `IAdminAuditWriter` and calls `RecordAsync` *before* the closing `SaveChangesAsync` so the audit row is part of the same EF transaction. The writer doesn't `SaveChanges` itself.

## Auth gotchas (learned the hard way — see commits f094978, 3763e11)

- ASP.NET's JWT handler **remaps `sub` → `ClaimTypes.NameIdentifier`** by default. `CurrentUserService.Auth0Sub` looks under both names; don't "simplify" it back to one lookup.
- The same remapping bites **authorization policies**: `policy.RequireClaim("sub", AdminSub)` silently returns 403 even with the correct sub configured, because the claim ends up under `ClaimTypes.NameIdentifier`. `JsonWebTokenHandler.DefaultMapInboundClaims = false` does not stop it on the policy code path. Use `policy.RequireAssertion(...)` checking both names — see `AuthExtensions.AdminPolicy`.
- Auth0 silently rejects an empty `audience` parameter with "Service not found". The frontend `AuthProvider` only spreads `audience` when it's truthy — don't pass `audience: ''`.
- Auth0 API identifiers **cannot be edited after creation**. Whitespace typos require delete + recreate.
- Even with "Allow Skipping User Consent" on, the SPA must be **explicitly enabled** for the API under **Application → APIs** in the Auth0 dashboard.
- **Login providers are dashboard-config, not code.** `loginWithRedirect()` is paramless at all 3 callsites (`Topbar.tsx`, `Home.tsx` GuestPanel, `RequireAuth.tsx`) — Universal Login shows whatever connections the tenant + SPA app have enabled (Authentication → Database/Social, plus Application → Connections). Backend keys on `sub` and works for any prefix (`google-oauth2|...`, `auth0|...`, `facebook|...`, …); `CurrentUserService.GetOrCreateAsync` falls back name → nickname → email → sub for the display name, so an email/password signup lands as the email local-part (editable on `/me`). To pre-select a provider from the SPA (skip the chooser), pass `{ authorizationParams: { connection: 'X' } }` — but the default paramless flow is intentional. **Same email via two providers = two separate `users` rows** (no account linking by default).
- **"Invalid state" from `useAuth0().error` is recoverable, not a real error.** Stale `?code=&state=` params on the URL (back nav, interrupted login, StrictMode race) leave the SDK without a matching transaction. `AuthProvider.tsx` has an explicit `onRedirectCallback` that strips the params via `history.replaceState` after handling, and `Topbar.tsx` filters this specific error message out so users don't see a scary red banner next to a button that just works. Other Auth0 errors (config, network) still surface — don't widen the filter.

## Teams schema gotchas

- `team_members.user_id` is **nullable**. Human members have a real `users.id`; the AI slot has `user_id IS NULL` + `is_ai = TRUE` + `ai_display_name` set. AI tips key on the team member itself (`tips.team_member_id`), never on a synthetic user — Tip and ScoredTip got nullable `user_id` + `team_member_id` (CHECK exactly-one) in Phase 6 so the personas stay out of the users table.
- "One team per user" is enforced by `UNIQUE(user_id)` on `team_members`. PostgreSQL treats NULLs as distinct in unique indexes, so multiple AI rows (one per team, each with NULL user_id) don't collide.
- "Max 1 AI per team" is a **partial unique index**: `UNIQUE(team_id) WHERE is_ai = TRUE`. The full table can have many AI rows; the partial index only sees the AI ones.
- Mutability is **status-only**: `TeamRulesValidator.ValidateMutable(status)` returns Ok for `Forming`, rejects `Locked` / `Disqualified`. There is no tournament-start gate — under-sized teams stay Forming after the start so new members can still join (and `TeamsService.CreateAsync` allows new teams mid-tournament). A team auto-locks via `TeamLockJob` once `status==Forming && now>=tournamentStart && memberCount>=Team.MaxMembers` (`TeamLockPolicy.Decide`). `Disqualified` is no longer reached automatically — kept only as a manual/legacy state.
- `TeamsService.LeaveAsync` cascades: when the last human leaves, the team and any AI member are removed too, so no orphan AI-only teams linger.
- **Admin remove-member mirrors the leave cascade with an extra auto-revert.** `TeamsAdminService.RemoveMemberAsync` is the only path that can pull a member out of a `Locked` team (the user-facing `LeaveAsync` rejects via `ValidateMutable`). Logic in order: (1) if removing this row would leave **zero humans**, cascade-delete the team + any AI member just like `LeaveAsync`; (2) otherwise remove only that member; (3) if the team was `Locked` and the surviving member count is now `< Team.MaxMembers`, call `team.Unlock()` so it flips back to `Forming` and the existing direct-join path can refill it (the `TeamLockJob` re-locks once it's full). DB cascades take care of the rest: `team_members.team_id` / `team_invites.team_id` have `ON DELETE CASCADE`; `tips.team_member_id` / `scored_tips.team_member_id` cascade too, so the removed AI's tips disappear automatically. Human tips/scored_tips persist (FK is `user_id`), which is intentional — the user stays on the individual board with their historical points. **No re-scoring call needed**: `TeamLeaderboardService` recomputes on read, so the team's next page-load reflects the new roster (or returns 0 points while it's under-sized).
- **Two parallel join paths.** `JoinByTokenAsync` (invite-token redemption, hits `team_invites`) and `JoinDirectlyAsync` (browse `/team/all` → `POST /api/teams/{id}/join`, no invite involved) both call into the same `TeamRulesValidator.ValidateMutable` + `ValidateAddMember` chain and produce the same `TeamLocked` / `TeamFull` / `AlreadyInTeam` reasons, so the SPA's `reasonMessage` (now in `web/src/lib/teamReasons.ts`) handles both flows with one map. `team_invites` is **only** touched by the invite-token path; the direct-join path is a plain `INSERT INTO team_members`. If you add a new join-style endpoint (e.g. magic-link, QR), funnel it through the same validator pair so the error contract stays single-source.

## Tips + ScoredTip schema gotchas (Phase 6)

- `tips.user_id` and `tips.team_member_id` are **both nullable** with a `CHECK ((user_id IS NOT NULL AND team_member_id IS NULL) OR (user_id IS NULL AND team_member_id IS NOT NULL))`. Same shape on `scored_tips`. Whichever side is set tells you whether the row is a human tip or an AI tip — don't write code that assumes `user_id` is always populated.
- Uniqueness is split into **two partial indexes**: `ux_tips_user_match` filters `user_id IS NOT NULL`, `ux_tips_member_match` filters `team_member_id IS NOT NULL`. Both can collide on a real concurrent double-write — catch `DbUpdateException` in any orchestrator path.
- `tips.is_ai_fallback` is `false` for human tips and AI successes; `true` only for the deterministic 1–1 row written at T-1h. `tips.reasoning` is null for human tips, optional for AI successes, and set to `"AI nem válaszolt időben."` for fallbacks.
- `ck_tips_ai_no_joker` enforces that AI tips never claim a joker (joker is human-only per §6). Don't relax this unless §6 changes.
- The leaderboard split is load-bearing: `IndividualLeaderboardService` filters `WHERE user_id IS NOT NULL` so AI rows never appear; `TeamLeaderboardService` and `TeamAggregationService` look up points by `UserId` (humans) OR `TeamMemberId` (AI). If you add a new aggregation, mirror the dual-key lookup or AI tips silently score zero.
- **Per-tip score shape is shared.** Two endpoints surface scoring details for a tip: `GET /api/users/{userId}/tips` (UserTips page, humans only) and `GET /api/matches/{matchId}/tips` (TipSubmit's `Mindenki tippje` panel — only after the deadline). Both LEFT-JOIN `scored_tips` so cancelled / not-yet-scored matches return `score: null` — the SPA already handles that branch. On the SPA, reuse the `UserTipScore` / `ScoreCategoryName` aliases in `web/src/api/types.ts` and `CATEGORY_LABEL_HU` in `web/src/lib/format.ts` instead of rolling new ones — they're the single source for the `{category} · ×{multiplier} · ×2 (joker)` rendering.

## Admin schema + flow gotchas (Phase 8)

- **Audit pattern**: `IAdminAuditWriter.RecordAsync` only stages the `AdminAudit` row on the DbContext — never calls SaveChanges. The admin service that called it owns the transaction. If you write a new admin endpoint and forget the audit call, `grep` is your only defence (no middleware will catch it).
- **`before` / `after` JSON shape is per-action.** Don't dump full aggregates. `MatchSetResult` snapshots `{status, homeGoals, awayGoals}`; `MatchCancel` adds `scoredTipsCleared` / `jokersRefunded` to the after-shape; `MatchPostpone` snapshots `{kickoffUtc, status}`; `AiTipperManualRun` records `before={aiMembers, alreadyHadTip}` / `after={attempted, written, fallbacks}`; `LongTipOutcomesSet` snapshots `{winnerTeamId, topScorerPlayerId}` on both sides (FKs only — no denormalized names); `PlayersImported` writes a null `before` and `after={added, skipped, unmatchedTeams, totalAfter, parsedTotal}`; `TeamRenamed` is `{name}` on both sides; `TeamDeleted` snapshots `{name, status, memberCount, humanMembers, aiMembers}` and writes null `after`; `TeamMemberRemoved` records `before={memberId, displayName, isAi, teamName, teamStatus}` / `after={remainingMembers, statusAfter, teamCascadeDeleted}` — `statusAfter` is null when the whole team cascaded. Match the relevant slice and nothing more.
- **`MatchStatus.Awarded` vs `Finished`**: distinct status for FIFA-decided outcomes per §11. `MatchScoringService` treats both as scorable so scoring is identical; the distinction is for UI badges + audit context. Use `Match.AwardResult(home, away)` (not `SetFinalScore`) when the source is admin-entered.
- **Cancel mechanics**: `MatchAdminService.CancelAsync` does four things in one `SaveChanges` — `match.ClearScore() + UpdateStatus(Cancelled)`, `ExecuteDeleteAsync` on `scored_tips`, `ExecuteUpdateAsync` flipping `tips.joker = false`, then the audit row. The `ExecuteUpdate/Delete` calls bypass the change tracker but run inside the implicit ambient transaction — the audit row's `SaveChanges` commits the lot atomically.
- **Joker refund** is implicit via `TipRulesValidator`: setting `joker = false` on a cancelled match's tips makes the user's `otherJokerCountForUser` query drop by one on next validation. Tip rows themselves stay as a historical record (§11 doesn't require deletion).
- **Postpone never refunds jokers** (§11 explicit). Existing tips remain editable up to the new deadline; the `TipRulesValidator` deadline check is driven by `kickoff_utc` alone, so the new `kickoff - 1h` is automatic.
- **Postpone validation**: `newKickoffUtc` must be ≥ `now + 1h + 5min` so the resulting deadline isn't already in the past. The 5-min buffer is intentional to avoid edge-case clock skew rejections.
- **Tournament outcomes are editable**, not locked. Re-calling `PUT /api/admin/long-tips/outcomes` overwrites and writes a new audit row. The individual leaderboard recomputes on every fetch (no cache) so corrections take effect immediately.
- **Top-scorer match is now strict FK equality** (`tip.TargetPlayerId == tournament.TopScorerPlayerId`) — both sides come from the curated `players` table, so typo tolerance moved to the SPA dropdown (PlayerSelect's text search). The earlier case-insensitive trimmed-string comparison is gone. If you re-introduce free text, also re-introduce the trim/lowercase compare in `IndividualLeaderboardService.ComputeLongTipCorrectness`.
- **`Tournament.WinnerTeamId` and `Tournament.TopScorerPlayerId`** both have `OnDelete(Restrict)` (to `teams_national` and `players` respectively). Deleting a national team or player that's recorded as a tournament outcome is blocked — safer than nulling the field silently. If a re-seed is needed mid-tournament, clear outcomes via the admin endpoint first.
- **Long-term tip reveal is server-gated, not just SPA-gated.** `GET /api/long-tips/all` (rendered as the *Mindenki tippje* section on `/long-tips`) returns `409 NotYetUnlocked` + `lockUtc` extension until `DateTimeOffset.UtcNow >= tournament.StartsAtUtc` — same gate as `LongTermTipRulesValidator`. Don't move the visibility check to the SPA alone: a hostile client would otherwise scrape everyone's picks pre-lock and skew the tournament. The SPA's `useAllLongTips(enabled)` hook also `enabled`-gates on `longTips.data?.locked` so the network tab stays clean of expected 409s. If you ever add a second post-lock reveal endpoint (e.g. team-aggregated long tips), reuse the same `LongTermTipPublicListResult.NotYetUnlocked` shape so the SPA's reason-mapping stays single-source.
- **Public reveal filters the caller out** (`UserId != currentUserId` in `LongTermTipsService.GetAllPublicAsync`) and drops users who tipped neither (winner *and* top scorer both null). Sort is `OrderBy(DisplayName, StringComparer.OrdinalIgnoreCase)` — change deliberately, the list is rendered as-is. The caller's own pick is already shown by the form above the list, so re-including them would just be visual duplication. If you ever need the full roster (admin tooling, exports), add a separate endpoint rather than relaxing the self-filter on this one.

## AI tipper gotchas (Phase 6)

- `OpenAiTipper` short-circuits to `AiTipResult.Disabled` when `OpenAi:ApiKey` is empty. Don't add `[Required]` to `OpenAiOptions.ApiKey` — the disabled path is intentional so the scaffold runs without a key, and the schedule policy still writes the 1–1 fallback at T-1h.
- `AiTipSchedulePolicy.Decide` is the only place the T-2h/T-90m/T-1h windows are encoded. Tests in `Domain.Tests/Ai/AiTipSchedulePolicyTests.cs` cover every boundary; if you tweak a window, update both.
- `AiTippingService` writes one `ai_tip_attempts` row per IAiTipper call (success or failure). The schedule policy reads that count to decide retry vs. skip — without persistent counting, a process restart in the [T-2h, T-1h] window would re-burn quota.
- The OpenAI prompt currently passes team names as-is. Hungarian team names like "Magyarország" can confuse the model into hallucinating a wrong opponent — production data from worldcup26.ir uses English names so this doesn't bite. If you ever localize team names in the DB, pass both English + Hungarian into `AiTipPromptBuilder.Build`.
- `OperationCanceledException` catch in `OpenAiTipper.GenerateAsync` uses `when (!ct.IsCancellationRequested)` to distinguish HttpClient timeout (our own deadline → return `Timeout`) from caller-cancel (let it propagate). Don't simplify that guard.
- **Manual per-match force run** lives at `POST /api/admin/ai-tipper/run/{matchId}` (`AiTippingService.RunForMatchAsync`). It **bypasses `AiTipSchedulePolicy`** entirely — fires the tipper for every Locked-team AI member that doesn't yet have a tip on the match, and on any AI failure (Disabled/Timeout/ProviderError/InvalidResponse) writes the deterministic 1–1 fallback so the slot never stays empty. One `admin_audit` row per call (`AdminAuditAction.AiTipperManualRun`). Eligibility: match status `Scheduled` + kickoff in the future (409 `MatchNotEligible` otherwise); 404 `MatchNotFound` if the id doesn't exist. SPA button lives on `/admin/matches/{id}` (`AiTipperPanel` in `AdminMatchEditor.tsx`) and auto-disables when ineligible.

## Avatar gotchas

- **No server-side image library.** The SPA's `web/src/lib/imageResize.ts` does the square-crop + resize to 128×128 JPEG q=0.85 on a canvas before posting. Server only validates content-type prefix (`image/jpeg|png|webp`) + 50 KB byte cap (`UserRulesValidator.ValidateAvatar`). If you ever need server-side processing (e.g. EXIF strip, auto-orient), add SixLabors.ImageSharp — don't roll your own.
- **GET endpoints are anonymous.** `<img src>` can't carry Bearer tokens, so `GET /api/users/{id}/avatar` and `GET /api/ai-avatar` are `[AllowAnonymous]`. User IDs are GUIDs (unguessable) and only surface inside authenticated pages, so leakage risk is acceptable for a friend-group app. Don't put avatars behind `[Authorize]` "to be safe" — it breaks `<img>` rendering silently.
- **Cache headers are aggressive on purpose.** Both endpoints return `Cache-Control: public, max-age=86400, immutable` plus an `ETag` derived from the version. The avatar URL carries `?v={version}` — the version is the first 8 chars of `SHA256(bytes)` in lowercase. A new upload changes the version → URL changes → browser refetches. The `v` query param is ignored server-side.
- **`MeResponse.aiAvatarVersion` drives the AI branch of `<Avatar />`.** The component is hook-aware (`useMe()` inside); only render it inside a tree where `/api/me` is fetched. AI team members never have `userId`, so the order of checks in `Avatar.tsx` matters: AI branch first, then human-with-version, then letter-circle. `useSetAiAvatar` / `useDeleteAiAvatar` invalidate `['me']` so the version propagates everywhere automatically.
- **`ai_avatar_setting` is a singleton.** `CHECK (id = 1)` on the table + `SingletonId = 1` const in the entity. `AiAvatarAdminService.SetAsync` does upsert via Add-or-Replace; `ClearAsync` deletes. Don't write code that assumes >1 row.
- **Audit payload never contains the bytes.** `AdminAuditAction.AiAvatarSet` / `AiAvatarDeleted` before/after carry `{version, contentType}` only. Dumping the bytea into `admin_audit.jsonb` would blow up the row and the audit-log UI. Mirror this shape if you add more avatar admin actions.
- **`DataUrlParser` is shared.** Both `MeController.SetAvatar` and `AiAvatarAdminController.Set` call `Tip4Gen.Api.Avatars.DataUrlParser.TryParse`. Pre-decode length cap is `User.MaxAvatarBytes * 4/3 + 256` — cheap O(1) guard before allocating a `byte[]` from a hostile body. If you tweak the limit, update both.
- **Topbar shows local display name, not Auth0's.** `Topbar.tsx` prefers `me.data?.displayName` over `user?.name` from the Auth0 SDK, falling back only while `me` is loading. The avatar URL requires `me.data.id`, so the rendering already depends on `useMe()` resolving.
- **Logged-out nav is filtered, not just route-gated.** `Topbar.tsx` chains two filters over `NAV_ITEMS` into a single `visibleItems` const: `!i.requiresAuth || isAuthenticated`, then `!i.path.startsWith('/admin') || me.data?.isAdmin`. Logged-out users only see `/` (Főoldal) and `/szabalyzat` (Szabályzat); admin-only entries (`/admin`, `/admin/players`, …) vanish from the menu for non-admins, not just from the routes. **Both the desktop inline `<nav>` and the mobile hamburger `PopoverPanel` map over the same `visibleItems` list** — add new filters to that const, not to either map call, or the two surfaces will silently diverge. When adding a new nav link in `lib/navIcons.tsx`, set `requiresAuth: true` unless the page itself is public (no `RequireAuth` wrapper in `App.tsx`). Any path that begins with `/admin` automatically counts as admin-only via the prefix check — don't add a new flag. Loading-state trade-off: during `useAuth0().isLoading` the auth-gated links stay hidden — a logged-in user sees a brief "bevillan" on cold load, which is the intended cost of never leaking the protected menu to logged-out visitors.

## Theme + dark-mode gotchas

- **Tailwind v4 dark syntax differs from v3.** v3's `darkMode: 'class'` JS setting **does nothing** in v4. The correct incantation is `@custom-variant dark (&:where(.dark, .dark *));` — `@variant dark` (no `custom`) silently no-ops. `:where()` keeps specificity at 0 so utilities aren't bumped over your overrides.
- **Token-only colors.** Every color in the SPA goes through `--color-*` variables declared in `@theme` and overridden under `.dark`. Adding a new color = add a new token in both blocks, not a one-off `bg-purple-500`. If you reintroduce raw palette classes, dark mode silently regresses for those elements. Allowed exceptions today are all in `Leaderboard.tsx` and all theme-independent medal colours: `text-amber-400 / text-zinc-300 / text-orange-700` (the `MEDAL_COLOR` constant used by `TeamRow`) and the brand-hex literals `text-[#F5C842] / text-[#D8D8E0] / text-[#C87941]` (gold/silver/bronze rank-icon + number on `UserRowCard`). The hex variants are static literal strings on purpose — Tailwind v4's JIT scanner skips template-interpolated arbitrary-value classes, so don't refactor them into `bg-[${VAR}]`. Comment any future exception inline so the next person doesn't "fix" it.
- **No first-paint flash.** The inline `<script>` in `web/index.html` `<head>` reads `localStorage['tip4gen.theme']` + `prefers-color-scheme` and toggles `.dark` *before* React hydrates. Don't move it into a module, don't remove it — without it the page renders light then snaps to dark on every cold load.
- **`ThemeProvider` must be outermost** in `main.tsx` so the class is set before any consumer (Auth0 redirect screens, error boundaries) renders. Order in `main.tsx`: `<ThemeProvider><AuthProvider><QueryClientProvider><BrowserRouter><App/></...>`.
- **Avatar HSL letter circles** use `hsl(${hue}, 65%, 50%)` — a single value that's legible in both themes. The border switched from `border-2 border-stone-900` to `ring-1 ring-white/10` so a dark-mode container doesn't swallow the edge. Don't reintroduce a black border.
- **`<TeamFlag>` is the one place `dark:` shows up.** Flagcdn SVGs include white-backgrounded flags (JP, England) that blend into navy. The component uses `ring-1 ring-border-subtle dark:ring-white/15` — a *shape* difference, not a color swap, which is why a semantic token can't fix it.
- **Admin pages intentionally stay brutalist.** Token-swap only — keep `border-2` widths and the dense mono labels. The visual register tells the admin "you're in the tools area" without a banner. If you redesign an admin page, don't accidentally soften its borders to plain `border`.
- **Headless UI Listbox** in `TeamSelect` uses `data-[focus]:bg-accent-soft data-[focus]:text-accent-strong`. `data-[focus]` is the v2 hook (already in code) — keep it; just retarget colors via tokens. Don't touch the `anchor="bottom start"` or focus management.
- **`glow-accent` + `live-dot`** are custom utilities declared in `index.css` via `@utility`. `glow-accent` paints a 1px accent ring + soft 24px glow (used on live match cards in `Matches.tsx` and on the current user's team row in `TeamRow`). The individual `UserRowCard` deliberately does **not** use `glow-accent` for `isMe` or for the leader — current-user is marked by `border-accent` alone, and the top-3 rank gets its medal tint on the icon + number only. Don't reintroduce the glow there. `live-dot` is a 1.4s opacity pulse (used on the Radio icon in `LiveMatchBanner`). Add new theme-aware utilities the same way — don't inline `box-shadow` with hard-coded rgba.
- **TipSubmit joker** is a `<button type="button">` with `aria-pressed`, NOT a checkbox. The Star pill toggles `setValue('joker', !joker, { shouldDirty: true })` on click, and an `<input type="hidden">` carries the value through RHF. If you reintroduce a `<input type="checkbox">` you'll lose the pill styling — and the form will submit the wrong shape unless you re-add the register call.

## National-team flag gotchas

- **Team codes are real FIFA now (worldcup26.ir).** The upstream returns honest FIFA 3-letter codes (`IRN`, `NED`, `ESP`, `JPN`, `KSA`, `SUI`, `MAR`, etc.). `web/src/lib/teamFlag.ts` *also* still holds the older api-football aliases (`IRA`, `NET`, `SPA`, `JAP`, `SAU`, `SER`, `SWI`, `CAM`, `COS`, `MOR`) — they're harmless dead-weight under the current provider and act as a buffer in case we ever swap upstream again. If you're staring at a missing flag, dump `teams_national.code` first — don't trust your memory of the FIFA spec.
- **Flag images are runtime CDN.** `<TeamFlag code={...} />` renders `<img src="https://flagcdn.com/{iso}.svg">` with `onError` → render-nothing fallback. No flag assets in the repo. flagcdn supports UK subdivisions (`gb-eng` / `gb-wls` / `gb-sct` / `gb-nir`) so each home nation gets its own flag — that's why `ENG → gb-eng` in the map, not `gb`.
- **`<TeamLabel team={...} />` is the default for every team-name render site.** Plain `{team.name}` was replaced everywhere (Matches list, TipSubmit header + score-input labels, LiveMatchBanner, AdminMatches, AdminMatchEditor). If you add a new site that renders a team name, use `<TeamLabel />` — don't reinvent the flex layout.
- **Native `<select>` can't render images in `<option>`.** The two long-tip dropdowns split:
  - **Winner team** (48 teams) → `<TeamSelect />` (Headless UI Listbox). Open-and-scroll UX.
  - **Top scorer** (~600 players) → `<PlayerSelect />` (Headless UI Combobox). Type-to-search with a `MIN_QUERY_LENGTH = 3` gate — under that, the dropdown shows a "Gépelj legalább 3 karaktert" hint instead of the full list (avoids a 600-row scroll wall).
  In `LongTips`, both are wrapped in RHF `Controller` and map `value=''` ↔ `null` at the boundary so existing form-state shapes (empty-string = no selection) still work. Mirror this if you need either inside another RHF form.

## Players + Wikipedia squads importer gotchas

- **`players` is admin-import-only, not user-editable.** The only writer is `PlayersImportService` behind `POST /api/admin/players/import`; there is no `POST /api/admin/players` for hand-entry. If a player is missing (late call-up, injury substitution after import), admin re-runs the importer. Don't add a manual-create endpoint without also widening `AdminAuditAction` and the SPA flow — the FK contract on long-term tips assumes every row in `players` came through the audited path.
- **The importer is idempotent and never updates names.** Algorithm: parse Wikipedia → resolve country → if `(NationalTeamId, Name)` already exists, `skipped++`; else `Added`. Names are immutable on a re-import even if Wikipedia changes spacing, because existing `long_term_tips.target_player_id` FKs would silently start pointing at a "renamed" person. If Wikipedia removes a player, the row stays — users who tipped them keep their pick (and lose the bet, fairly).
- **Country resolution: code → name fallback, both case-insensitive.** `PlayersImportService` builds two dictionaries from `teams_national`: by `code` (FIFA 3-letter) and by `name`. The Wikipedia parser today emits country *names* only (no codes — the H3 id like `Czech_Republic` doesn't carry a FIFA code), so the name path does the heavy lifting; the code path is kept for robustness in case a future parser revision can mine the flag image for `IRN` / `KSA`. If `unmatchedTeams > 0` in the import response, dump the warning logs — usually it's a spelling mismatch (`"Czech Republic"` vs `"Czechia"`).
- **Parser lives in Infrastructure, not Domain.** `WikipediaSquadsParser` is a pure static class but takes a hard dependency on `HtmlAgilityPack`, which would violate the Domain-stays-pure rule. The shared wire type `ParsedPlayer` lives in `Tip4Gen.Domain/Players/` so Infrastructure → Domain is the only dep direction the importer needs. Golden tests for the parser live in `Tip4Gen.Infrastructure.Tests/Players/`, not `Domain.Tests/`.
- **MediaWiki parse API, not raw page scrape.** `WikipediaSquadsProvider` GETs `/w/api.php?action=parse&page={PageTitle}&prop=text&format=json&formatversion=2`. This gives a clean, skin-free HTML fragment that's more stable than the user-facing page. Wikipedia's ToS demands an identifiable UA — `WikipediaSquadsProviderOptions.UserAgent` defaults to `Tip4Gen/1.0 (contact: ispanpeter82@gmail.com)`. Don't blank it.
- **HTML shape the parser depends on**: country `<h3 id="Czech_Republic">…</h3>` followed (after the `div.mw-heading` wrapper) by `<table class="sortable wikitable plainrowheaders">` whose `<tr class="nat-fs-player">` rows have the player name in `<th scope="row">`. The parser keys on the `id` attribute for country (not InnerText — that would also pick up the `[edit]` link), and on `nat-fs-player` for rows (skips header/legend rows). If Wikipedia restructures, the parser tests are the canary.
- **Migration `AddPlayersAndPlayerFks` deletes pre-existing `TopScorer` rows from `long_term_tips`.** No production tip data existed when this migration shipped (pre-launch). The migration explicitly runs `DELETE FROM long_term_tips WHERE type = 'TopScorer'` before swapping `target_player_name` for `target_player_id`, because the new CHECK constraint requires `target_player_id IS NOT NULL` for that type. If you ever re-apply this migration on a DB with live tip data, that data is lost — don't.
- **Last-import card reads from `admin_audit`.** `/api/admin/players/last-import` queries the latest `admin_audit` row with `Action = PlayersImported`, returning `{occurredAt, afterJson}`. The SPA `AdminPlayers` page parses `afterJson` for the `{added, skipped, unmatchedTeams, totalAfter}` summary. If you change the audit shape, also update `AdminPlayers.tsx`'s `parseLastSummary`.
- **Importing is the admin's prod-bootstrap step.** Railway doesn't auto-run the importer; first time you deploy a fresh DB, log in as admin and click "Importálás futtatása" before anyone can tip a top scorer. The migration creates the empty `players` table — the rows are entirely the importer's responsibility.

## Forms gotcha — Zod + react-hook-form

`z.coerce.number()` makes Zod's **input** type `unknown` while the output is `number`. `zodResolver` then produces a `Resolver<output, ctx, input>` whose two sides don't line up with the single `TFieldValues` RHF expects, causing a TS2322 mismatch. Two options:
- Keep `z.number()` (no coerce) and pass `{ valueAsNumber: true }` to `register` so the DOM string is converted before validation. This is what `TipSubmit.tsx` does.
- Or use the three-generic `useForm<Input, Ctx, Output>(...)`. More ceremony, same outcome.

## Things not to do

- Don't introduce PostCSS or a `tailwind.config.js` — Tailwind v4 doesn't need them.
- Don't use raw palette classes (`bg-stone-*`, `bg-emerald-500`, `text-red-700`, etc.) outside `index.css`. Use the semantic tokens (`bg-elevated`, `bg-accent`, `text-danger`, …) so dark mode swaps for free.
- Don't reintroduce `dark:` prefixes anywhere except `TeamFlag`. The variable-swap approach is the whole point — sprinkled `dark:` classes will diverge and rot.
- Don't remove the inline theme script from `web/index.html` `<head>` — it's what prevents the first-paint light flash on dark loads.
- Don't add EF Core or ASP.NET references to `Tip4Gen.Domain`.
- Don't put credentials in `appsettings.json` or commit `web/.env.local` — use `dotnet user-secrets` and `.env.local` (both gitignored).
- Don't fabricate dates from training data — today's date comes from the system context. WC 2026 starts **2026-06-11**.
- Don't widen scope mid-phase. If something doesn't fit in the current phase, note it in the plan and move on.
- Don't break the `reason` enum in ProblemDetails responses — the frontend maps it to per-field errors. Adding a new variant is fine; renaming an existing one needs a coordinated SPA change.
- Don't hand-write rows into `players` (SQL, EF seed, anywhere). Every row must come through `PlayersImportService` so the audit trail stays the source of truth; a smuggled row that a user then tips on creates a tip pointing at an unauditable player.
