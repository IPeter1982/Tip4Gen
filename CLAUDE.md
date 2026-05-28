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
                            FixturesAdminController, ScoringAdminController,
                            TeamsAdminController, NationalTeamsController,
                            MatchesController, TipsController, LongTipsController,
                            TeamsController, LeaderboardController,
                            AiTipperAdminController (preview endpoint),
                            MatchesAdminController (result/cancel/postpone),
                            AdminAuditController, LongTipsAdminController
  src/Tip4Gen.Domain        Pure domain types — no EF, no ASP.NET refs
    Users/User.cs
    Tournaments/            Stage + MatchStatus enums, Tournament, NationalTeam,
                            Match, StageMapper, MatchStatusMapper
    Tournaments/Events/     MatchFinalized record + IMatchFinalizedHandler
    Football/               IFootballDataProvider + ProviderFixture/Team/Status
    Tipping/                Tip (UserId nullable; TeamMemberId nullable for AI tips;
                            IsAiFallback, Reasoning), TipRulesValidator,
                            LongTermTip + LongTermTipRulesValidator
    Scoring/                MatchResult, ScoreCategory, ScoringResult,
                            StageMultipliers, MatchScorer (pure),
                            ScoredTip (dual-key: UserId or TeamMemberId)
    Teams/                  Team, TeamMember (UserId nullable for AI slot),
                            TeamInvite, TeamStatus + AiMode enums,
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
    Football/               ApiFootballProvider + Options + DTOs
    Tournaments/            FixtureSyncService (idempotent upsert + event dispatch)
    Tipping/                TipsService, LongTermTipsService (tagged-union results)
    Scoring/                MatchScoringService (idempotent re-score, dual-key),
                            MatchFinalizedScoringHandler (event handler)
    Teams/                  TeamsService (CRUD + invites + join, tagged-union),
                            TeamLockService (Forming → Locked/Disqualified pass),
                            TeamAggregationService (per-match best-3-of-4 view)
    Leaderboard/            IndividualLeaderboardService (humans only)
                            + TeamLeaderboardService (humans + AI, dual-key)
    Ai/                     OpenAiOptions, OpenAiTipper (Chat Completions +
                            json_object mode; returns Disabled when ApiKey unset),
                            AiTippingService orchestrator
    Admin/                  IAdminAuditWriter + AdminAuditWriter (stage row in
                            same transaction as caller), MatchAdminService
                            (SetResult / Cancel / Postpone), LongTipOutcomesService
  src/Tip4Gen.Workers       BackgroundService host — FixturePoller (Phase 2)
                            + TeamLockJob (Phase 5) + AiTippingJob (Phase 6),
                            shares Api's UserSecretsId for shared dev config
    FixturePoller.cs        Calls FixtureSyncService when DB has active matches
    FixturePollerOptions.cs IntervalMinutes / ActiveWindowHours / LookaheadMinutes
    TeamLockJob.cs          Calls TeamLockService.LockAllAsync on a coarse cadence
    TeamLockJobOptions.cs   IntervalMinutes (default 5) / StartupDelaySeconds
    AiTippingJob.cs         Calls AiTippingService.RunOnceAsync on a 5-min tick
    AiTippingJobOptions.cs  IntervalMinutes (default 5) / StartupDelaySeconds
  tests/Tip4Gen.Domain.Tests  xUnit — StageMapper, MatchStatusMapper,
                              TipRulesValidator, LongTermTipRulesValidator,
                              MatchScorer, StageMultipliers, TeamRulesValidator,
                              TeamAggregator, TeamLockPolicy, LeaderboardRanker,
                              StreakCalculator, AiTipResponseValidator,
                              AiTipSchedulePolicy, AiTipPromptBuilder (189 tests)
web/                        Vite + React 19 + TS frontend
  src/auth/                 AuthProvider, RequireAuth, useApi (typed + ApiError),
                            authConfig
  src/api/                  errors.ts (ApiError + ProblemDetails),
                            types.ts (shared response shapes),
                            hooks.ts (TanStack Query wrappers)
  src/lib/format.ts         Budapest TZ formatters + countdown + HU labels
  src/components/Topbar.tsx
  src/pages/                Home, Me, Matches, TipSubmit, LongTips, Team, TeamJoin,
                            Leaderboard
  src/pages/admin/          AdminMatches, AdminMatchEditor, AdminAudit, AdminLongTips
  src/auth/RequireAdmin     Gates admin routes; renders 403 panel for non-admins
  src/components/           Topbar, ConfirmDialog (modal w/ destructive variant)
  src/main.tsx              <AuthProvider><QueryClientProvider><BrowserRouter>…
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
- **TanStack Query v5** + **react-hook-form** + **Zod** for data fetching and forms. `useApi` returns typed `get/put/post/del` helpers and parses ProblemDetails into `ApiError` (with `reason` extension).
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

All dev credentials live in `dotnet user-secrets` for `backend/src/Tip4Gen.Api` — never in `appsettings.json`, never in the repo. The Workers project's csproj declares the same `UserSecretsId`, so both processes resolve the same secret store. Current keys:

- `ConnectionStrings:AppDb` — local Postgres 18 (`tip4gen_dev` DB, `tip4gen` role)
- `Auth0:Domain` / `Auth0:Audience` / `Auth0:AdminSub` — tenant `dev-yifcd0c5p4s0wcj5.eu.auth0.com`, audience `https://api.tip4gen.local`
- `FootballApi:Provider` / `ApiKey` / `BaseUrl` / `LeagueId` / `Season` — api-football Free plan, league=1, season=2022 (see Data caveat below)
- `OpenAi:ApiKey` — OpenAI project-scoped key (`sk-proj-…`). Optional: when unset, `OpenAiTipper` returns `AiTipResult.Disabled` and the schedule policy still writes the 1–1 fallback at T-1h. `OpenAi:Model` (default `gpt-4o-mini`), `OpenAi:Temperature` (0.7), `OpenAi:TimeoutSeconds` (15) are bindable from the same section.

Frontend env in `web/.env.local` (also gitignored): `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE`.

## Data caveat — WC 2026 fixtures

api-football's Free plan does NOT include WC 2026 fixture coverage (`coverage.fixtures=false`). We develop against **WC 2022** (`FootballApi:Season=2022`, 64 real matches with results) so the scoring engine can be tested with real data. To launch with real 2026 fixtures we must either upgrade api-football, swap to a different provider, OR rely on admin manual entry (Phase 8) — that fallback is **load-bearing**, not optional.

Schema must accommodate both formats: 2022 had 32 teams (no R32 round), 2026 has 48 teams with groups → R32 → R16 → QF → SF → bronze → final.

**Round labels are matchday-style, not group-letter-style.** api-football's WC fixtures use `Group Stage - 1/2/3` — the group letter (A–L) is *not* in `league.round`. `StageMapper` returns `(Stage.Group, null)` for those, so `matches.group_code` stays null until we enrich it from `/standings`. Deferred follow-up — schema already supports it, the seed/poller code doesn't yet. If you see code that assumes `group_code` is populated, check this first.

**Quota discipline.** api-football Free plan is 100 req/day. The seed endpoint costs 2 (one /fixtures + one /teams). The poller costs 1 per tick (only /fixtures), and only when there's a Live or imminent match — at default cadence it stays well under cap. Don't add a new poller without doing the same active-window check.

## Scoring rules — quick reference (full detail in guide §3–§9)

- Per-match categories (best wins): **Exact 10 · WinnerAndGoalDiff 5 · Winner 3 · OneTeamGoals 1 · Nothing 0**.
- Stage multipliers: group `1×` · R32 `1.5×` · R16 `1.5×` · QF `2×` · SF `2.5×` · bronze `2×` · final `3×`.
- **Joker** (max 3 per user, group stage only, one per match) **doubles after multiplier**.
- "One team's goal count matches" is strictly **home-to-home / away-to-away** (not swapped).
- Half-multiplier results round **away from zero** (`5 × 1.5 = 7.5 → 8`). `MatchScorer` is the single source of truth.
- Scoring is **idempotent**: `MatchScoringService.ScoreMatchAsync` deletes prior `scored_tips` for the match then re-inserts in one `SaveChanges`. Wired into `MatchFinalized` for auto-scoring and exposed via `POST /api/admin/matches/{id}/rescore` for manual re-runs.
- `scored_tips.user_id` and `scored_tips.team_member_id` are **denormalized** from `tips`. Exactly one is set per row, matching the source tip; leaderboard queries pick a side without joining through matches.
- Team aggregation: **best 3 of 4** member scores per match (`TeamAggregator.ForMatch`). Tiebreak on all-equal scores: drop the member with the largest id — deterministic, doesn't affect the total.
- AI fallback: auto **1–1** tip with `is_ai_fallback=true` if AI provider hasn't returned by **T-1h**.
- Tip deadline: **kickoff − 1h**, enforced server-side in UTC.
- Long-term tips (winner, top scorer) lock at **tournament-start kickoff**.

## Admin

Single admin (the project owner). Gated by Auth0 `sub` claim matching the `Auth0:AdminSub` user-secret. Every `/api/admin/*` write must record a row in `admin_audit` in the same transaction (before/after JSON). Audit lives in the service layer (not middleware) — each admin service injects `IAdminAuditWriter` and calls `RecordAsync` *before* the closing `SaveChangesAsync` so the audit row is part of the same EF transaction. The writer doesn't `SaveChanges` itself.

## Auth gotchas (learned the hard way — see commits f094978, 3763e11)

- ASP.NET's JWT handler **remaps `sub` → `ClaimTypes.NameIdentifier`** by default. `CurrentUserService.Auth0Sub` looks under both names; don't "simplify" it back to one lookup.
- The same remapping bites **authorization policies**: `policy.RequireClaim("sub", AdminSub)` silently returns 403 even with the correct sub configured, because the claim ends up under `ClaimTypes.NameIdentifier`. `JsonWebTokenHandler.DefaultMapInboundClaims = false` does not stop it on the policy code path. Use `policy.RequireAssertion(...)` checking both names — see `AuthExtensions.AdminPolicy`.
- Auth0 silently rejects an empty `audience` parameter with "Service not found". The frontend `AuthProvider` only spreads `audience` when it's truthy — don't pass `audience: ''`.
- Auth0 API identifiers **cannot be edited after creation**. Whitespace typos require delete + recreate.
- Even with "Allow Skipping User Consent" on, the SPA must be **explicitly enabled** for the API under **Application → APIs** in the Auth0 dashboard.

## Teams schema gotchas

- `team_members.user_id` is **nullable**. Human members have a real `users.id`; the AI slot has `user_id IS NULL` + `is_ai = TRUE` + `ai_display_name` set. AI tips key on the team member itself (`tips.team_member_id`), never on a synthetic user — Tip and ScoredTip got nullable `user_id` + `team_member_id` (CHECK exactly-one) in Phase 6 so the personas stay out of the users table.
- "One team per user" is enforced by `UNIQUE(user_id)` on `team_members`. PostgreSQL treats NULLs as distinct in unique indexes, so multiple AI rows (one per team, each with NULL user_id) don't collide.
- "Max 1 AI per team" is a **partial unique index**: `UNIQUE(team_id) WHERE is_ai = TRUE`. The full table can have many AI rows; the partial index only sees the AI ones.
- Mutability gating is two-stage: tournament-start time **first** (so the error message points at the real cause), then team status. Both live in `TeamRulesValidator.ValidateMutable`.
- `TeamsService.LeaveAsync` cascades: when the last human leaves, the team and any AI member are removed too, so no orphan AI-only teams linger.

## Tips + ScoredTip schema gotchas (Phase 6)

- `tips.user_id` and `tips.team_member_id` are **both nullable** with a `CHECK ((user_id IS NOT NULL AND team_member_id IS NULL) OR (user_id IS NULL AND team_member_id IS NOT NULL))`. Same shape on `scored_tips`. Whichever side is set tells you whether the row is a human tip or an AI tip — don't write code that assumes `user_id` is always populated.
- Uniqueness is split into **two partial indexes**: `ux_tips_user_match` filters `user_id IS NOT NULL`, `ux_tips_member_match` filters `team_member_id IS NOT NULL`. Both can collide on a real concurrent double-write — catch `DbUpdateException` in any orchestrator path.
- `tips.is_ai_fallback` is `false` for human tips and AI successes; `true` only for the deterministic 1–1 row written at T-1h. `tips.reasoning` is null for human tips, optional for AI successes, and set to `"AI nem válaszolt időben."` for fallbacks.
- `ck_tips_ai_no_joker` enforces that AI tips never claim a joker (joker is human-only per §6). Don't relax this unless §6 changes.
- The leaderboard split is load-bearing: `IndividualLeaderboardService` filters `WHERE user_id IS NOT NULL` so AI rows never appear; `TeamLeaderboardService` and `TeamAggregationService` look up points by `UserId` (humans) OR `TeamMemberId` (AI). If you add a new aggregation, mirror the dual-key lookup or AI tips silently score zero.

## Admin schema + flow gotchas (Phase 8)

- **Audit pattern**: `IAdminAuditWriter.RecordAsync` only stages the `AdminAudit` row on the DbContext — never calls SaveChanges. The admin service that called it owns the transaction. If you write a new admin endpoint and forget the audit call, `grep` is your only defence (no middleware will catch it).
- **`before` / `after` JSON shape is per-action.** Don't dump full aggregates. `MatchSetResult` snapshots `{status, homeGoals, awayGoals}`; `MatchCancel` adds `scoredTipsCleared` / `jokersRefunded` to the after-shape; `MatchPostpone` snapshots `{kickoffUtc, status}`. Match the relevant slice and nothing more.
- **`MatchStatus.Awarded` vs `Finished`**: distinct status for FIFA-decided outcomes per §11. `MatchScoringService` treats both as scorable so scoring is identical; the distinction is for UI badges + audit context. Use `Match.AwardResult(home, away)` (not `SetFinalScore`) when the source is admin-entered.
- **Cancel mechanics**: `MatchAdminService.CancelAsync` does four things in one `SaveChanges` — `match.ClearScore() + UpdateStatus(Cancelled)`, `ExecuteDeleteAsync` on `scored_tips`, `ExecuteUpdateAsync` flipping `tips.joker = false`, then the audit row. The `ExecuteUpdate/Delete` calls bypass the change tracker but run inside the implicit ambient transaction — the audit row's `SaveChanges` commits the lot atomically.
- **Joker refund** is implicit via `TipRulesValidator`: setting `joker = false` on a cancelled match's tips makes the user's `otherJokerCountForUser` query drop by one on next validation. Tip rows themselves stay as a historical record (§11 doesn't require deletion).
- **Postpone never refunds jokers** (§11 explicit). Existing tips remain editable up to the new deadline; the `TipRulesValidator` deadline check is driven by `kickoff_utc` alone, so the new `kickoff - 1h` is automatic.
- **Postpone validation**: `newKickoffUtc` must be ≥ `now + 1h + 5min` so the resulting deadline isn't already in the past. The 5-min buffer is intentional to avoid edge-case clock skew rejections.
- **Tournament outcomes are editable**, not locked. Re-calling `PUT /api/admin/long-tips/outcomes` overwrites and writes a new audit row. The individual leaderboard recomputes on every fetch (no cache) so corrections take effect immediately.
- **Top-scorer match is case-insensitive on trimmed strings** so admin typos like " Lionel Messi " don't dock users who tipped "lionel messi". If you change this, update `IndividualLeaderboardService.ComputeLongTipCorrectness` and document why.
- **`Tournament.WinnerTeamId`** has `OnDelete(Restrict)` to `teams_national`. Deleting a national team that's recorded as the winner is blocked — safer than nulling the field silently. If a re-seed is needed mid-tournament, clear outcomes via the admin endpoint first.

## AI tipper gotchas (Phase 6)

- `OpenAiTipper` short-circuits to `AiTipResult.Disabled` when `OpenAi:ApiKey` is empty. Don't add `[Required]` to `OpenAiOptions.ApiKey` — the disabled path is intentional so the scaffold runs without a key, and the schedule policy still writes the 1–1 fallback at T-1h.
- `AiTipSchedulePolicy.Decide` is the only place the T-2h/T-90m/T-1h windows are encoded. Tests in `Domain.Tests/Ai/AiTipSchedulePolicyTests.cs` cover every boundary; if you tweak a window, update both.
- `AiTippingService` writes one `ai_tip_attempts` row per IAiTipper call (success or failure). The schedule policy reads that count to decide retry vs. skip — without persistent counting, a process restart in the [T-2h, T-1h] window would re-burn quota.
- The OpenAI prompt currently passes team names as-is. Hungarian team names like "Magyarország" can confuse the model into hallucinating a wrong opponent — production data from api-football uses English names so this doesn't bite. If you ever localize team names in the DB, pass both English + Hungarian into `AiTipPromptBuilder.Build`.
- `OperationCanceledException` catch in `OpenAiTipper.GenerateAsync` uses `when (!ct.IsCancellationRequested)` to distinguish HttpClient timeout (our own deadline → return `Timeout`) from caller-cancel (let it propagate). Don't simplify that guard.

## Forms gotcha — Zod + react-hook-form

`z.coerce.number()` makes Zod's **input** type `unknown` while the output is `number`. `zodResolver` then produces a `Resolver<output, ctx, input>` whose two sides don't line up with the single `TFieldValues` RHF expects, causing a TS2322 mismatch. Two options:
- Keep `z.number()` (no coerce) and pass `{ valueAsNumber: true }` to `register` so the DOM string is converted before validation. This is what `TipSubmit.tsx` does.
- Or use the three-generic `useForm<Input, Ctx, Output>(...)`. More ceremony, same outcome.

## Things not to do

- Don't introduce PostCSS or a `tailwind.config.js` — Tailwind v4 doesn't need them.
- Don't add EF Core or ASP.NET references to `Tip4Gen.Domain`.
- Don't put credentials in `appsettings.json` or commit `web/.env.local` — use `dotnet user-secrets` and `.env.local` (both gitignored).
- Don't fabricate dates from training data — today's date comes from the system context. WC 2026 starts **2026-06-11**.
- Don't widen scope mid-phase. If something doesn't fit in the current phase, note it in the plan and move on.
- Don't break the `reason` enum in ProblemDetails responses — the frontend maps it to per-field errors. Adding a new variant is fine; renaming an existing one needs a coordinated SPA change.
