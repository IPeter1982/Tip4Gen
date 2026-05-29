import { useAuth0 } from '@auth0/auth0-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useLongTips, useMatches, useMyTeam } from '../api/hooks'
import { isAuthConfigured } from '../auth/authConfig'
import { formatBudapest, formatCountdown } from '../lib/format'

export function Home() {
  const { isAuthenticated, isLoading } = useAuth0()

  return (
    <div className="max-w-2xl mx-auto px-6 py-10 space-y-8">
      <header className="space-y-2">
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">
          Foci VB · Tippjáték
        </p>
        <h1 className="text-5xl sm:text-6xl font-black uppercase tracking-tight">Tip4Gen</h1>
        <p className="font-mono text-sm text-stone-600">
          2026-os labdarúgó-világbajnokság · barátok közötti tippjáték
        </p>
      </header>

      {!isAuthConfigured && (
        <section className="border-2 border-orange-600 bg-orange-50 p-5 font-mono text-sm text-orange-900">
          Auth0 nincs beállítva — másold a <code>web/.env.local.example</code> fájlt
          <code> web/.env.local</code>-ra, töltsd ki a <code>VITE_AUTH0_*</code> értékeket, és
          indítsd újra a dev szervert.
        </section>
      )}

      {isAuthConfigured && isLoading && (
        <p className="font-mono text-stone-500">betöltés…</p>
      )}

      {isAuthConfigured && !isLoading && !isAuthenticated && <GuestPanel />}
      {isAuthConfigured && !isLoading && isAuthenticated && <OnboardingPanel />}
    </div>
  )
}

function GuestPanel() {
  const { loginWithRedirect } = useAuth0()
  return (
    <div className="space-y-6">
      <section className="border-2 border-stone-900 bg-white p-6 space-y-4">
        <h2 className="text-2xl font-black uppercase tracking-tight">Hogyan működik</h2>
        <ol className="space-y-2 font-mono text-sm text-stone-700">
          <li>
            <span className="text-orange-600 font-bold">1.</span> Tippelj minden meccsre — 0–15
            gól, csoportkörben joker (max 3×, dupla pont).
          </li>
          <li>
            <span className="text-orange-600 font-bold">2.</span> Hosszú távú tipp: torna győztese
            + gólkirály — a nyitómérkőzésig zárul.
          </li>
          <li>
            <span className="text-orange-600 font-bold">3.</span> Alakíts 4 fős csapatot
            (opcionális AI tag) — a torna kezdetekor zárul.
          </li>
          <li>
            <span className="text-orange-600 font-bold">4.</span> Egyéni és csapat ranglista a
            torna alatt és után.
          </li>
        </ol>
      </section>
      <button
        type="button"
        onClick={() => loginWithRedirect()}
        className="w-full border-2 border-stone-900 bg-stone-900 text-white py-4 text-sm font-mono uppercase tracking-[0.2em] hover:bg-orange-600 hover:border-orange-600"
      >
        Belépés / Regisztráció
      </button>
    </div>
  )
}

function OnboardingPanel() {
  const longTips = useLongTips()
  const myTeam = useMyTeam()
  const allMatches = useMatches('all')

  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(id)
  }, [])

  const hasTeam = !!myTeam.data
  const hasAnyLongTip =
    !!longTips.data && (!!longTips.data.winnerTeamId || !!longTips.data.topScorerName)
  const hasAnyTip = (allMatches.data ?? []).some((m) => m.myTip !== null)
  const longTipsLocked = longTips.data?.locked ?? false

  return (
    <div className="space-y-6">
      {longTips.data && (
        <section className="border-2 border-stone-900 bg-stone-900 text-white p-5">
          <p className="text-xs font-mono uppercase tracking-[0.15em] text-orange-300">
            {longTipsLocked ? 'A torna elkezdődött' : 'A torna kezdetéig'}
          </p>
          <p className="mt-2 text-3xl font-black tabular-nums">
            {longTipsLocked ? 'él' : formatCountdown(longTips.data.lockUtc, now)}
          </p>
          <p className="mt-2 text-xs font-mono uppercase tracking-[0.15em] text-stone-300">
            nyitány: {formatBudapest(longTips.data.lockUtc)}
          </p>
        </section>
      )}

      <section className="space-y-3">
        <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-stone-500">
          Indítólista
        </h2>

        <Step
          n={1}
          title="Belépés"
          description="Google-fiókkal léptél be — kész."
          done
        />

        <Step
          n={2}
          title="Csapat"
          description={
            hasTeam
              ? `Csapat: ${myTeam.data!.name} (${myTeam.data!.members.length}/4)`
              : 'Hozz létre 4 fős csapatot vagy fogadd el egy meghívót — opcionális AI taggal.'
          }
          done={hasTeam}
          loading={myTeam.isLoading}
          actionLabel={hasTeam ? 'Csapat kezelése' : 'Csapat létrehozása'}
          actionHref="/team"
        />

        <Step
          n={3}
          title="Hosszú tipp"
          description={
            hasAnyLongTip
              ? longTipsLocked
                ? 'A hosszú tippek véglegesek — nézd meg a ranglistán.'
                : 'A hosszú tippeid mentve — a nyitányig módosíthatóak.'
              : longTipsLocked
                ? 'Lezárult — a torna már elkezdődött.'
                : 'Tippeld meg a torna győztesét és a gólkirályt — a nyitányig.'
          }
          done={hasAnyLongTip}
          locked={longTipsLocked && !hasAnyLongTip}
          loading={longTips.isLoading}
          actionLabel={hasAnyLongTip ? 'Megnéz / módosít' : 'Hosszú tipp megadása'}
          actionHref="/long-tips"
        />

        <Step
          n={4}
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
      </section>

      <section className="border-2 border-stone-300 bg-white p-5">
        <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500 mb-3">
          Gyors hivatkozások
        </p>
        <div className="flex flex-wrap gap-2">
          <QuickLink to="/matches" label="Mérkőzések" />
          <QuickLink to="/leaderboard" label="Ranglista" />
          <QuickLink to="/team" label="Csapat" />
          <QuickLink to="/long-tips" label="Hosszú tipp" />
        </div>
      </section>
    </div>
  )
}

function Step({
  n,
  title,
  description,
  done = false,
  locked = false,
  loading = false,
  actionLabel,
  actionHref,
}: {
  n: number
  title: string
  description: string
  done?: boolean
  locked?: boolean
  loading?: boolean
  actionLabel?: string
  actionHref?: string
}) {
  const borderCls = done ? 'border-green-700' : locked ? 'border-stone-400' : 'border-stone-900'
  const bgCls = done ? 'bg-green-50' : locked ? 'bg-stone-50' : 'bg-white'
  return (
    <article className={`border-2 ${borderCls} ${bgCls} p-4 flex items-start gap-4`}>
      <span
        className={`shrink-0 w-9 h-9 border-2 ${
          done
            ? 'border-green-700 bg-green-700 text-white'
            : locked
              ? 'border-stone-400 bg-stone-100 text-stone-400'
              : 'border-stone-900 bg-white text-stone-900'
        } font-black flex items-center justify-center text-sm`}
        aria-hidden="true"
      >
        {done ? '✓' : locked ? '–' : n}
      </span>
      <div className="flex-1 min-w-0 space-y-1">
        <h3 className="text-sm font-mono uppercase tracking-[0.15em] font-bold">
          {title}
          {done && (
            <span className="ml-2 text-[10px] tracking-[0.15em] text-green-700">kész</span>
          )}
          {locked && (
            <span className="ml-2 text-[10px] tracking-[0.15em] text-stone-500">lezárva</span>
          )}
        </h3>
        <p className="font-mono text-xs text-stone-600">
          {loading ? 'betöltés…' : description}
        </p>
      </div>
      {actionLabel && actionHref && !locked && (
        <Link
          to={actionHref}
          className={`shrink-0 border-2 px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] ${
            done
              ? 'border-stone-300 text-stone-600 bg-white hover:border-stone-900 hover:text-stone-900'
              : 'border-stone-900 bg-stone-900 text-white hover:bg-orange-600 hover:border-orange-600'
          }`}
        >
          {actionLabel}
        </Link>
      )}
    </article>
  )
}

function QuickLink({ to, label }: { to: string; label: string }) {
  return (
    <Link
      to={to}
      className="border-2 border-stone-300 px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-stone-700 hover:border-stone-900 hover:text-stone-900"
    >
      {label}
    </Link>
  )
}
