import { useAuth0 } from '@auth0/auth0-react'
import { useEffect, useMemo, useState, type ComponentType } from 'react'
import { Link } from 'react-router'
import {
  ArrowRight,
  CheckCircle2,
  ChevronRight,
  House,
  LockKeyhole,
  LogIn,
  Shield,
  Target,
  Trophy,
  Users,
  type LucideProps,
} from 'lucide-react'
import { useLongTips, useMatches, useMyTeam } from '../api/hooks'
import { isAuthConfigured } from '../auth/authConfig'
import { formatBudapest } from '../lib/format'

export function Home() {
  const { isAuthenticated, isLoading } = useAuth0()

  return (
    <div className="min-h-[calc(100vh-4rem)] bg-gradient-to-b from-accent/5 via-surface to-surface dark:from-[#0b1438] dark:via-surface dark:to-surface">
      <div className="max-w-4xl mx-auto px-6 py-12 space-y-10">
        <Hero />

        {!isAuthConfigured && (
          <section className="rounded-2xl border border-warning/40 bg-warning/10 p-5 font-mono text-sm text-warning">
            Auth0 nincs beállítva — másold a <code>web/.env.local.example</code> fájlt
            <code> web/.env.local</code>-ra, töltsd ki a <code>VITE_AUTH0_*</code> értékeket, és
            indítsd újra a dev szervert.
          </section>
        )}

        {isAuthConfigured && isLoading && (
          <p className="font-mono text-fg-subtle text-center">betöltés…</p>
        )}

        {isAuthConfigured && !isLoading && !isAuthenticated && <GuestPanel />}
        {isAuthConfigured && !isLoading && isAuthenticated && <OnboardingPanel />}
      </div>
    </div>
  )
}

function Hero() {
  const longTips = useLongTips()
  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(id)
  }, [])

  const kickoff = longTips.data?.lockUtc
  const locked = longTips.data?.locked ?? false
  const countdown = useMemo(() => splitCountdown(kickoff, now), [kickoff, now])

  return (
    <header className="text-center space-y-6 pt-4">
      <div className="inline-flex items-center gap-2 text-xs font-mono uppercase tracking-[0.25em] text-accent">
        <House size={14} />
        Foci VB 2026 · Tippjáték
      </div>
      <h1 className="text-5xl sm:text-7xl font-bold tracking-tight text-fg-default">Tip4Gen</h1>
      <p className="font-mono text-sm text-fg-muted max-w-xl mx-auto">
        2026-os labdarúgó-világbajnokság · barátok közötti tippjáték
      </p>

      {kickoff && countdown && (
        <div className="space-y-2">
          <p className="text-[11px] font-mono uppercase tracking-[0.25em] text-fg-subtle">
            {locked ? 'A torna elkezdődött' : 'Kezdésig hátra'}
          </p>
          {locked ? (
            <p className="inline-flex items-center gap-2 text-xl font-bold text-accent">
              <span className="inline-block h-2 w-2 rounded-full bg-live live-dot" />
              ÉL
            </p>
          ) : (
            <div className="inline-flex gap-2 sm:gap-3">
              <TimeBlock value={countdown.days} unit="nap" />
              <TimeBlock value={countdown.hours} unit="óra" />
              <TimeBlock value={countdown.minutes} unit="perc" />
              <TimeBlock value={countdown.seconds} unit="mp" />
            </div>
          )}
          <p className="text-[11px] font-mono uppercase tracking-[0.15em] text-fg-subtle">
            nyitány: {formatBudapest(kickoff)}
          </p>
        </div>
      )}
    </header>
  )
}

function TimeBlock({ value, unit }: { value: number; unit: string }) {
  return (
    <div className="rounded-xl border border-accent/30 bg-elevated/70 backdrop-blur px-3 sm:px-5 py-3 min-w-[64px] sm:min-w-[80px]">
      <p className="text-2xl sm:text-4xl font-mono font-bold tabular-nums text-fg-default leading-none">
        {value.toString().padStart(2, '0')}
      </p>
      <p className="mt-1 text-[10px] font-mono uppercase tracking-[0.2em] text-fg-subtle">{unit}</p>
    </div>
  )
}

function splitCountdown(target: string | undefined, now: Date) {
  if (!target) return null
  const diff = new Date(target).getTime() - now.getTime()
  if (diff <= 0) return { days: 0, hours: 0, minutes: 0, seconds: 0 }
  const days = Math.floor(diff / 86400000)
  const hours = Math.floor((diff % 86400000) / 3600000)
  const minutes = Math.floor((diff % 3600000) / 60000)
  const seconds = Math.floor((diff % 60000) / 1000)
  return { days, hours, minutes, seconds }
}

function GuestPanel() {
  const { loginWithRedirect } = useAuth0()
  return (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2">
        <RuleCard
          icon={Target}
          title="Tippelj minden meccsre"
          body="0–15 gól. Csoportkörben joker (max 3×, dupla pont)."
        />
        <RuleCard
          icon={Trophy}
          title="Végső győztes"
          body="Torna győztese + gólkirály — a nyitómérkőzésig zárul."
        />
        <RuleCard
          icon={Users}
          title="3 fős csapat"
          body="Csapat opcionális AI taggal. A torna kezdetekor zárul."
        />
        <RuleCard
          icon={Shield}
          title="Ranglista"
          body="Egyéni és csapat ranglista a torna alatt és után."
        />
      </div>
      <button
        type="button"
        onClick={() => loginWithRedirect()}
        className="w-full inline-flex items-center justify-center gap-2 rounded-xl bg-accent text-on-accent py-4 text-sm font-mono uppercase tracking-[0.2em] font-bold transition hover:bg-accent-strong focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring-focus"
      >
        <LogIn size={16} />
        Belépés / Regisztráció
      </button>
    </div>
  )
}

function RuleCard({
  icon: Icon,
  title,
  body,
}: {
  icon: ComponentType<LucideProps>
  title: string
  body: string
}) {
  return (
    <article className="rounded-2xl border border-border-subtle bg-elevated p-5 space-y-2 transition hover:-translate-y-0.5 hover:border-accent">
      <Icon size={22} className="text-accent" />
      <h3 className="text-sm font-bold text-fg-default">{title}</h3>
      <p className="font-mono text-xs text-fg-muted leading-relaxed">{body}</p>
    </article>
  )
}

function OnboardingPanel() {
  const longTips = useLongTips()
  const myTeam = useMyTeam()
  const allMatches = useMatches('all')

  const hasTeam = !!myTeam.data
  const hasAnyLongTip =
    !!longTips.data && (!!longTips.data.winnerTeamId || !!longTips.data.topScorerPlayerId)
  const hasAnyTip = (allMatches.data ?? []).some((m) => m.myTip !== null)
  const longTipsLocked = longTips.data?.locked ?? false

  return (
    <div className="space-y-6">
      <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle text-center">
        Indítólista
      </h2>

      <div className="grid gap-4 md:grid-cols-2">
        <Step
          n={1}
          icon={Users}
          title="Csapat"
          description={
            hasTeam
              ? `Csapat: ${myTeam.data!.name} (${myTeam.data!.members.length}/3)`
              : 'Hozz létre 3 fős csapatot vagy fogadd el egy meghívót — opcionális AI taggal.'
          }
          done={hasTeam}
          loading={myTeam.isLoading}
          actionLabel={hasTeam ? 'Csapat kezelése' : 'Csapat létrehozása'}
          actionHref="/team"
        />

        <Step
          n={2}
          icon={Trophy}
          title="Végső győztes"
          description={
            hasAnyLongTip
              ? longTipsLocked
                ? 'A tippjeid véglegesek — nézd meg a ranglistán.'
                : 'A tippjeid mentve — a nyitányig módosíthatóak.'
              : longTipsLocked
                ? 'Lezárult — a torna már elkezdődött.'
                : 'Tippeld meg a torna győztesét és a gólkirályt — a nyitányig.'
          }
          done={hasAnyLongTip}
          locked={longTipsLocked && !hasAnyLongTip}
          loading={longTips.isLoading}
          actionLabel={hasAnyLongTip ? 'Megnéz / módosít' : 'Végső győztes'}
          actionHref="/long-tips"
        />

        <Step
          n={3}
          icon={Target}
          title="Első tipp"
          description={
            hasAnyTip
              ? 'Tippeltél már — folytasd a soron következő meccseknél.'
              : 'Tippeld meg az első mérkőzést — kezdés előtt 1 órával lezárul.'
          }
          done={hasAnyTip}
          loading={allMatches.isLoading}
          actionLabel={hasAnyTip ? 'Mérkőzések' : 'Első tipp'}
          actionHref="/matches"
        />

        <Step
          n={4}
          icon={Shield}
          title="Ranglista"
          description="Egyéni és csapat ranglista — nézd meg, hogy állsz."
          actionLabel="Ranglista"
          actionHref="/leaderboard"
        />
      </div>

      <section className="rounded-2xl border border-border-subtle bg-elevated p-5">
        <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-3">
          Gyors hivatkozások
        </p>
        <div className="flex flex-wrap gap-2">
          <QuickLink to="/matches" label="Mérkőzések" />
          <QuickLink to="/leaderboard" label="Ranglista" />
          <QuickLink to="/team" label="Csapat" />
          <QuickLink to="/long-tips" label="Végső győztes" />
        </div>
      </section>
    </div>
  )
}

function Step({
  n,
  icon: Icon,
  title,
  description,
  done = false,
  locked = false,
  loading = false,
  actionLabel,
  actionHref,
}: {
  n: number
  icon: ComponentType<LucideProps>
  title: string
  description: string
  done?: boolean
  locked?: boolean
  loading?: boolean
  actionLabel?: string
  actionHref?: string
}) {
  const borderCls = done
    ? 'border-accent'
    : locked
      ? 'border-border-subtle opacity-70'
      : 'border-border-subtle hover:border-accent'

  return (
    <article
      className={`rounded-2xl border bg-elevated p-5 space-y-3 transition ${borderCls}`}
    >
      <div className="flex items-start gap-3">
        <span
          className={`shrink-0 w-9 h-9 rounded-full flex items-center justify-center text-sm font-bold ${
            done
              ? 'bg-accent text-on-accent'
              : locked
                ? 'bg-sunken text-fg-subtle'
                : 'bg-sunken text-fg-default'
          }`}
          aria-hidden="true"
        >
          {done ? <CheckCircle2 size={18} /> : locked ? <LockKeyhole size={16} /> : n}
        </span>
        <div className="flex-1 min-w-0">
          <h3 className="text-sm font-bold text-fg-default inline-flex items-center gap-2">
            <Icon size={16} className="text-accent" />
            {title}
            {done && (
              <span className="text-[10px] font-mono uppercase tracking-[0.15em] text-accent">kész</span>
            )}
            {locked && (
              <span className="text-[10px] font-mono uppercase tracking-[0.15em] text-fg-subtle">lezárva</span>
            )}
          </h3>
          <p className="mt-1 font-mono text-xs text-fg-muted leading-relaxed">
            {loading ? 'betöltés…' : description}
          </p>
        </div>
      </div>
      {actionLabel && actionHref && !locked && (
        <Link
          to={actionHref}
          className={`inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] transition ${
            done
              ? 'border border-border-strong text-fg-muted hover:text-accent hover:border-accent'
              : 'bg-accent text-on-accent hover:bg-accent-strong'
          }`}
        >
          {actionLabel}
          <ArrowRight size={12} />
        </Link>
      )}
    </article>
  )
}

function QuickLink({ to, label }: { to: string; label: string }) {
  return (
    <Link
      to={to}
      className="inline-flex items-center gap-1 rounded-lg border border-border-strong px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-fg-muted transition hover:text-accent hover:border-accent"
    >
      {label}
      <ChevronRight size={12} />
    </Link>
  )
}
