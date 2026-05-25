# Implementation Plan — Foci VB Tippjáték

**Target launch:** 2026-06-11 (World Cup opener kickoff).
**Today:** 2026-05-25 → **17 days runway.**
**Scope:** 50–200 users, friends-and-extended-circle.
**Stack:** per `tech-stack.md` — Vite/React SPA + ASP.NET Core 9 API (planned 10, using 9 for speed) + PostgreSQL + Auth0 + Azure.

## Status snapshot

- **Phase 0:** ✅ done (committed `de66246`). Local Postgres 18 is in use instead of Docker.
- **Phase 1:** ✅ done end-to-end (commits `fe93917` + `f094978`). Auth0 tenant `dev-yifcd0c5p4s0wcj5.eu.auth0.com` live, real Google login → `/api/me` returns user row with `isAdmin: true` for `google-oauth2|115365131932488818447`.
- **Phase 2:** ✅ done (commits `e02496f` + `3763e11` + `370cd43`). Schema + `IFootballDataProvider` + api-football impl with `AddStandardResilienceHandler`. `POST /api/admin/fixtures/seed` verified end-to-end against real WC 2022 (1 tournament, 32 teams, 64 matches, correct stage breakdown). Quota-aware `FixturePoller` in Workers exercised live (`Live → Finished` transition + `MatchFinalized` dispatch). Latent Phase 1 admin-policy bug fixed in passing.
- **Phases 3–11:** not started.
- **Open decisions:** hosting swap (Fly.io+Vercel+Neon vs Azure) — see callouts. Auth decision is locked: **Auth0**.
- **Football data source:** api-football Free plan (100 req/day) verified. WC 2022 (64 fixtures, full results) accessible; WC 2026 is `coverage.fixtures=false` on Free → **dev against `season=2022`, swap to 2026 once we upgrade or change provider**. Admin manual-entry fallback in Phase 8 is now load-bearing, not optional. api-football's WC labels are matchday-style (`Group Stage - 1/2/3`) — group letter has to come from `/standings`, see Phase 2 follow-up below.
- **Next:** Phase 3 (tipping), unblocked.

## How to read this plan

Phases run roughly sequentially, but tasks within a phase are often parallelizable. Each task is sized to ~0.5–1 day of focused work. Tasks marked **[CUT-OK]** can be deferred to v1.1 if you're behind on launch day — the game still works without them. Tasks marked **[CRITICAL]** are blocking for tournament-day operation.

## Stack risk callouts before you start

Two stack choices will cost you days you don't have. Decide now whether to keep or swap:

1. **Auth0** — strong product, but setup + audience config + first-login user linking + Hungarian-language login UI customization is 1–2 days. **Alternative:** Better Auth (you have a Skill for it locally), Clerk, or Supabase Auth — all cheaper, faster to wire up, fine for 50–200 users.
2. **Azure end-to-end** — App Service + Static Web Apps + Postgres Flexible + Key Vault + Managed Identity wiring is 1–2 days. **Alternatives that ship in hours:** Fly.io or Railway for the API, Vercel/Netlify for the SPA, Neon/Supabase for Postgres. You lose the "all on Azure" tidiness, gain ~2 days.

If you're already comfortable with the Azure + Auth0 path, keep it. Otherwise, switch on day 1, not day 10.

---

# Phase 0 — Foundations (Day 1)

**Goal:** repo exists, both apps boot locally, deploy pipeline pushes a "hello world" to production.

**Local scaffolding — DONE 2026-05-24:**

- [x] Monorepo layout — `backend/` solution (Api / Domain / Infrastructure / Workers / Domain.Tests) + `web/` frontend
- [x] Backend skeleton: ASP.NET Core 9 (deferred 10), Serilog via `Host.UseSerilog` reading from config, `/api/health` returns ok+timestamp+env+version
- [x] Frontend skeleton: Vite + React 19 + TS + Tailwind v4 (via `@tailwindcss/vite`, no PostCSS) + React Router v7, single landing route at `/`
- [x] Vite dev proxy `/api` → `http://localhost:5050` (no CORS in dev)
- [x] Backend CORS policy for `http://localhost:5173` (covers non-proxied paths)
- [x] `.gitignore` covering .NET + Node + env/secrets + OS files
- [x] End-to-end verified: backend boots on `:5050`, frontend on `:5173`, landing page shows live `/api/health` JSON
- [x] Both apps build clean (`dotnet build backend/Tip4Gen.sln` + `npm run build --prefix web`)

**Done in Phase 1:**

- [x] Auth0 tenant + SPA app + API audience (see Phase 1 below)
- [x] First git commit (`de66246`)

**Deferred to a later phase:**

- [ ] `shadcn` init — pull in when an auth screen or admin form needs a component library (Phase 8 most likely)

**Blocked on external accounts — DO BEFORE PHASE 6 / 11:**

- [x] Football API account: api-football Free plan verified, key stored in user-secrets (`FootballApi:*`). League=1, dev season=2022 (2026 needs paid plan; admin manual entry fills the gap).
- [ ] AI provider key (Anthropic or OpenAI) — test one chat completion. **Needed for Phase 6.**
- [ ] Production env: provision DB + API host + SPA host (Azure path *or* the fallback stack from the callouts). **Needed for Phase 11.**
- [ ] CI: GitHub Actions deploys both apps on push to `main` (blocked on hosting choice). **Needed for Phase 11.**
- [ ] Secrets in prod: DB connection string + Auth0 audience + API keys from Key Vault (or platform env vars). **Needed for Phase 11.**

**Done when:** `git push` → live site shows "hello", backend `/health` returns 200, both reachable from your phone.

---

# Phase 1 — Auth + user model (Day 2) — ✅ DONE

**Shipped 2026-05-24 → 25:**

- [x] Frontend: `@auth0/auth0-react` provider (`AuthProvider`), login/logout buttons in Topbar, `RequireAuth` route guard, `useApi` hook attaches `Authorization: Bearer` automatically. Vite pinned to `--strictPort` so the callback URL never drifts.
- [x] Backend: `JwtBearer` middleware validating Auth0 tokens against JWKS via `services.AddAuth0(...)`. When Auth0 isn't configured, all `[Authorize]` endpoints return 401 (deny-all `SignatureValidator`) instead of 500.
- [x] DB: `users` table — `id` (UUID), `auth0_sub` (unique, max 255), `display_name` (max 120), `created_at` (timestamptz). EF migration `InitialUsers` applied to local Postgres 18.
- [x] `CurrentUserService.GetOrCreateAsync` — upserts user by `sub` claim, takes display name from `name`/`nickname`/`email`/`sub` in that order. Looks up sub under both `"sub"` and `ClaimTypes.NameIdentifier` (ASP.NET remaps by default).
- [x] `GET /api/me` returns id, displayName, auth0Sub, createdAt, isAdmin.
- [x] Frontend: Topbar shows `user.name`/`email`/`sub` when logged in; surfaces `useAuth0().error` so silent failures are visible.
- [x] Single admin: `Auth0:AdminSub` config key, `RequireAdmin` policy denies-all when unset. Currently set to `google-oauth2|115365131932488818447`.
- [x] `GET /api/admin/me` returns 200 for admin, 403 for non-admin, 401 unauthenticated. Verified.
- [x] Auth0 tenant configured: SPA app with callback/logout/web-origin `http://localhost:5173`, API with identifier `https://api.tip4gen.local`, "Allow Skipping User Consent" on, API authorized for the SPA via the Application → APIs toggle.
- [x] User-secrets set: `Auth0:Domain`, `Auth0:Audience`, `Auth0:AdminSub`, `ConnectionStrings:AppDb`. Frontend `web/.env.local` populated.

**Lessons banked (see commit `f094978` for the fixes):**

- Auth0's leading-/trailing-space typo in the API identifier is silent and unfixable (identifiers can't be edited) — delete and recreate.
- "Allow Skipping User Consent" alone isn't enough; the application also needs the API enabled in **Application → APIs** tab.
- ASP.NET JWT handler remaps `sub` → `nameidentifier` by default; either disable mapping or look up both names.
- Auth0Provider must not send an empty `audience` — Auth0 responds "Service not found".

---

# Phase 2 — Match data + fixtures (Day 3–4) — ✅ DONE

**Shipped 2026-05-25:**

- [x] Schema: `tournaments`, `teams_national`, `matches` (with `external_id`, CHECK constraints on `stage` and `status`, score-nullability + distinct-teams checks, unique `(tournament_id, external_id)`, indexes on `kickoff_utc` and `status`). Stages are a `Stage` enum in Domain (`Group/R32/R16/QF/SF/Bronze/Final`) — multipliers and Hungarian labels live in code per §4 of the guide.
- [x] `IFootballDataProvider` in Domain returning `ProviderFixture`/`ProviderTeam` records + normalized `ProviderStatus`. `ApiFootballProvider` in Infrastructure with typed `HttpClient`, `x-apisports-key` header, `AddStandardResilienceHandler` (Polly v8 via `Microsoft.Extensions.Http.Resilience`).
- [x] `StageMapper` pure parser handles both `Group A - N` (Euros-style) and `Group Stage - N` (FIFA WC matchday-style). Knockout labels: `Round of 32/16`, `1/16`, `1/8`, `Quarter-finals`, `Semi-finals`, `3rd Place Final`/`Bronze`, `Final`. 35 xUnit tests.
- [x] `FixtureSyncService.SyncAsync(includeTeamsRoster, ct)` — idempotent upsert keyed on `external_id`, dispatches `MatchFinalized` to every registered `IMatchFinalizedHandler` for matches that transitioned to `Finished` this run. Seed endpoint passes `true`, poller passes `false` to save a `/teams` call per tick.
- [x] `POST /api/admin/fixtures/seed` (admin-gated). Verified against real WC 2022: 1 tournament, 32 teams, 64 matches (48 group + 8 R16 + 4 QF + 2 SF + 1 bronze + 1 final), all `Finished`, idempotent second run returns `+0/~0/finalized 0`.
- [x] `FixturePoller` (`BackgroundService` in Tip4Gen.Workers) — wakes every `FixturePoller:IntervalMinutes` (default 15), but skips the API call unless DB has a Live match or a Scheduled one within `±ActiveWindowHours / +LookaheadMinutes` (default −4h / +1h). At max cadence that's 96 calls/day; on idle days it spends zero quota. Workers shares Api's `UserSecretsId` so config flows through.
- [x] `MatchFinalized` event (`Domain.Tournaments.Events`) + `IMatchFinalizedHandler` interface. Scoring service in Phase 4 will be the first handler.
- [x] Live verification: flipping the Final match to `Live` via psql triggered the next poll tick to call api-football, transition `Live → Finished` with score `2-2`, and dispatch 1 event.

**Lessons banked (see commit `3763e11`):**

- api-football's WC fixtures use **matchday-style round labels** (`Group Stage - 1/2/3`) — the group letter (A–L) is not in the label. To enrich `matches.group_code` we need a separate `/standings` call mapping teams to groups. See follow-up below.
- `RequireClaim("sub", ...)` in an authorization policy hits the same `sub → ClaimTypes.NameIdentifier` remapping that bit `CurrentUserService` in Phase 1 — `JsonWebTokenHandler.DefaultMapInboundClaims = false` does not stop it on the policy code path. Admin policy now mirrors the dual-name lookup. CLAUDE.md updated.

**Deferred follow-ups (NOT blocking Phase 3, valuable when convenient):**

- [ ] Group letter enrichment: pull api-football `/standings?league=…&season=…`, build team → group map, backfill `matches.group_code` so the UI can render "Group H — Argentina vs Saudi Arabia". ~1–2h. The seed endpoint can call /standings once after /fixtures; the poller doesn't need to re-pull (groups don't change mid-tournament).

---

# Phase 3 — Tipping (Day 4–6)

**Goal:** users can submit and edit match tips and long-term tips, deadlines enforced server-side.

- [ ] DB schema: `tips` (user_id, match_id, home_goals, away_goals, joker, submitted_at), unique on (user_id, match_id)
- [ ] DB schema: `long_term_tips` (user_id, type [winner|top_scorer], target_id, locked_at)
- [ ] DB schema: `jokers_used` view or computed — 3 per user max, group-stage only
- [ ] API: `PUT /api/tips/{matchId}` — upsert tip; reject if past `kickoff_utc - 1h`
- [ ] API: `POST /api/long-tips` — accept only before tournament-start kickoff
- [ ] API: `GET /api/matches?phase=upcoming` — list with my tip + deadline
- [ ] API: `GET /api/matches/{id}/tips` — returns who-has-tipped (before deadline) or full tips (after deadline)
- [ ] Frontend: tip submission form (RHF + Zod), score inputs, joker checkbox, deadline countdown
- [ ] Frontend: tournament-eve long-term tip form (winner + top scorer), locks after first kickoff
- [ ] Frontend: match list page with status chips (open / closed / finished)
- [ ] Server-side joker validation: max 3 per user, group-stage matches only, one per match
- [ ] Time zones: all deadlines in UTC, display in Europe/Budapest

**Done when:** you can submit a tip from your phone, can't submit after the deadline (try it), and the long-term tips lock when first kickoff hits.

**[CRITICAL]**

---

# Phase 4 — Scoring engine (Day 6–7)

**Goal:** given a tip + result + stage + joker, compute points correctly. Pure, unit-tested, no DB.

- [ ] Domain: `Tip`, `MatchResult`, `Stage` enum, `Joker` flag — value objects in `Domain` project, no EF dependency
- [ ] `ScoringService.Score(tip, result, stage, joker) → int`
- [ ] Best-matching-category logic: 10 / 5 / 3 / 1 / 0 — exactly per §3 of the guide
- [ ] Stage multiplier: 1× / 1.5× / 1.5× / 2× / 2.5× / 2× / 3× for group / R32 / R16 / QF / SF / bronze / final
- [ ] Joker doubling applied **after** the multiplier
- [ ] "Egyik csapat gólszáma stimmel" — strictly home-to-home, away-to-away (not swapped)
- [ ] Unit tests: every category, every stage, joker on/off, edge cases (0–0 tip vs 0–0 result, joker on knockout match — should reject upstream)
- [ ] Service: `MatchScoringRunner` — on `MatchFinalized` event, loop tips for that match, persist `scored_tips` rows (`points`, `category`, `applied_multiplier`)
- [ ] Idempotency: re-running scoring for the same match overwrites cleanly (for admin re-score)

**Done when:** test suite green covering all rule combos, scoring a finished match is a single transaction.

**[CRITICAL]** — this is the heart of the app. Write the tests first, in Hungarian if it helps you reason about §3.

---

# Phase 5 — Teams (Day 7–9)

**Goal:** 4-person teams form before kickoff, lock at tournament start, score with best-3-of-4.

- [ ] DB schema: `teams` (id, name, ai_mode nullable, status [forming|locked|disqualified]), `team_members` (team_id, user_id, is_ai bool, ai_display_name nullable)
- [ ] API: `POST /api/teams` create a team, creator added as first member
- [ ] API: `POST /api/teams/{id}/invites` generate a join token; `POST /api/teams/join/{token}` to use it
- [ ] API: `POST /api/teams/{id}/leave` (only while `forming`)
- [ ] API: `PATCH /api/teams/{id}` name + AI mode (only while `forming`)
- [ ] API: `POST /api/teams/{id}/ai-member` add the AI slot with chosen mode (max 1, max 4 total)
- [ ] Validation: a user can only be in one team at a time; max 4 members, max 1 AI
- [ ] Background service `TeamLockJob`: at first match kickoff, flip every team with 4 members to `locked`, others to `disqualified`
- [ ] Team aggregation service: per match, sum the top 3 member points (sorted desc, drop lowest); long-term tips & jokers also follow "best 3" per the guide §7
- [ ] Frontend: team creation, invite link sharing, member list, AI slot configurator
- [ ] Frontend: team dashboard with current standing + per-match contribution view

**Done when:** you can create a team with 3 friends + the AI, see "Csapat zárul: {kickoff time}" countdown, and after lock the team has frozen membership.

**[CRITICAL]**

---

# Phase 6 — AI tipper (Day 9–10)

**Goal:** AI team members tip automatically, with fallback to 1–1 on failure.

- [ ] AI prompt template: takes form/ranking/recent results + adventurousness mode, returns strict JSON `{home_goals:int, away_goals:int, reasoning:string}`
- [ ] AI client wrapper: Anthropic or OpenAI SDK call with timeout + one retry
- [ ] Response validation: `home_goals` and `away_goals` ints in 0–15, reasoning ≤ 500 chars — anything else = treat as failure
- [ ] Scheduled job: for every upcoming match, at `kickoff - 2h`, run AI tips for all AI members on locked teams
- [ ] Retry at `kickoff - 90min` if first attempt failed
- [ ] Fallback at `kickoff - 1h`: insert 1–1 tip with `is_fallback = true`, reasoning "AI nem válaszolt időben"
- [ ] DB: `tips` gains `is_ai_fallback bool`, `reasoning text nullable`
- [ ] Frontend: AI badge on AI tips, additional "AI-fallback" chip when applicable, reasoning shown after deadline

**Done when:** for a test fixture, AI members get a tip without you touching anything; killing the AI API still produces a 1–1 fallback.

**[CUT-OK]** — if behind, ship without the AI feature and disable the AI checkbox at team creation. Add post-launch.

---

# Phase 7 — Leaderboards (Day 10–11)

**Goal:** individual and team rankings, fast and current.

- [ ] DB: `scored_tips` table (per-tip points after multiplier + joker)
- [ ] DB: `user_totals` view or cached table — sum of scored_tips + long-term tip points
- [ ] DB: `team_totals` — apply "best 3 of 4" per match + best-3 logic for long-term/jokers
- [ ] API: `GET /api/leaderboard/users` paginated, sorted desc
- [ ] API: `GET /api/leaderboard/teams` same
- [ ] Tiebreaker logic implemented exactly per §9 (including the "megosztott helyezés" terminator)
- [ ] Frontend: leaderboard pages, sortable, "me" highlighted, "my team" highlighted
- [ ] Recompute strategy: refresh totals when a match is scored (event-driven, not on every read)

**Done when:** after scoring 3 mock matches, both leaderboards show correct rankings and break ties per §9.

**[CRITICAL]** — but the materialized-view optimization from tech-stack.md is **[CUT-OK]** at 200 users; a plain query is fast enough.

---

# Phase 8 — Admin UI (Day 11–12)

**Goal:** you can fix anything during the tournament from your phone.

- [ ] Frontend: `/admin` route, hidden in nav for non-admins, guarded by `GET /api/admin/me`
- [ ] DB: `admin_audit` table (id, admin_sub, timestamp_utc, action, entity_type, entity_id, before_json, after_json, reason)
- [ ] Audit middleware: every `/api/admin/*` write captures before/after and inserts an audit row in the same transaction
- [ ] API: `PUT /api/admin/matches/{id}/result` — set/edit final score, triggers re-scoring
- [ ] API: `POST /api/admin/matches/{id}/cancel` — sets status, zeros all tips, refunds jokers per §11
- [ ] API: `POST /api/admin/matches/{id}/postpone` — new kickoff, new T-1h deadline
- [ ] API: `POST /api/admin/matches/{id}/rescore` — idempotent re-run of scoring
- [ ] API: `GET /api/admin/audit?match_id=…` paginated audit view
- [ ] Frontend pages: match list (with quick result-entry), single-match editor, audit log viewer
- [ ] Confirm-dialogs on cancel + postpone — destructive

**Done when:** you can mark a fake match as 2–1 from `/admin`, see the audit row, see every tipper's points update.

**[CRITICAL]** — without this, a single delayed/abandoned match wedges the tournament.

---

# Phase 9 — Notifications (Day 12–13)

**Goal:** people get reminded so they don't forget to tip.

- [ ] Resend account + sender domain verified
- [ ] DB: `user_preferences` — `email_reminders_enabled bool` (default true)
- [ ] Background jobs:
  - `T-24h`: email users who haven't tipped a match in the next 24h
  - `T-2h`: last-call email to users still without a tip
  - Post-match: per-user summary of points earned today
- [ ] Email templates (Hungarian) — keep them short and link directly to the tip page
- [ ] Frontend: preferences page to toggle reminders
- [ ] Deduplication: don't email twice for the same match/event (track in DB)

**Done when:** trigger a test match with kickoff in 24h, get the email, click through, tip, get no further reminders.

**[CUT-OK]** — if launching behind, ship with one daily digest instead of per-match reminders. Add per-match later.

---

# Phase 10 — Realtime + polish (Day 13–15)

**Goal:** the app feels alive during matches.

- [ ] SignalR hub: subscribe to a tournament channel
- [ ] Server pushes: `MatchFinalized`, `LeaderboardUpdated`, `MatchStatusChanged`
- [ ] Frontend: SignalR client wired to TanStack Query invalidations
- [ ] Live score banner during in-progress matches
- [ ] **[CUT-OK alternative]** if SignalR is fighting you: refetch leaderboard and match list every 30s with TanStack Query — invisible to users at this scale
- [ ] Onboarding flow: "1. log in → 2. join or create team → 3. submit long-term tips → 4. tip first match"
- [ ] Empty states everywhere (no tips yet, no teams yet, leaderboard empty)
- [ ] Mobile pass: every page tested on a phone (90% of tipping will happen mobile)
- [ ] Loading + error states on every async surface
- [ ] Production logging dialed in — Serilog + Application Insights (or platform log viewer)

**[CUT-OK]** — the SignalR work is optional. Everything else here is polish that affects user trust.

---

# Phase 11 — Beta + launch (Day 15–19)

**Goal:** real people use it before the World Cup opens.

- [ ] Pick 5–10 friends for closed beta, give them accounts
- [ ] Run a fake/test match — admin enters a result, verify scoring + leaderboard + notifications all work end-to-end
- [ ] Have testers form 2 real teams and submit long-term tips
- [ ] Bug-fix sprint based on beta feedback (reserve at least 2 days for this — there will be issues)
- [ ] Final deploy + smoke test
- [ ] Lock all configuration — Auth0 callback URLs, CORS origins, env vars, admin sub claim
- [ ] Tournament-eve checklist:
  - [ ] All 104 fixtures loaded with correct kickoffs
  - [ ] Football API quota check — will it survive a full tournament's polling?
  - [ ] AI tipper end-to-end test on a real upcoming fixture
  - [ ] Admin UI verified from your phone (you'll be on the go during matches)
  - [ ] Backup admin contact / manual workaround documented for if you're unreachable
  - [ ] Announce to users with a deadline reminder for long-term tips

**Done when:** opening match scored correctly, top of leaderboard makes sense, you slept through the night.

---

# Critical path & cut order if you slip

The bare minimum for tournament-day operation:

> **Phases 0–5, 7, 8, plus beta from Phase 11.**

If you're behind on Day 12, cut in this order:
1. Realtime (Phase 10) → replace with 30s polling
2. AI tipper (Phase 6) → ship without, hide the AI checkbox
3. Email reminders (Phase 9) → ship without, post one announcement in your group chat instead
4. Audit log UI within Phase 8 → audit table still writes, but skip the viewer (you can query it manually)

The non-negotiables: auth, fixtures + scoring engine + tipping + teams + leaderboard + admin-result-entry. Lose any of those and the tournament can't run.

# Reserve days

Build in **at least 2 days of unallocated buffer** by Day 17. There will be a thing you didn't plan for — a third-party outage, a scoring edge case nobody tested, an Auth0 callback misconfig. Treat the buffer as sacred.
