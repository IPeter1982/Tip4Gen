import type { ReactNode } from 'react'
import { BookOpen } from 'lucide-react'

const SECTIONS: { n: string; title: string }[] = [
  { n: '01', title: 'Tippelhető események' },
  { n: '02', title: 'Tippelési határidő' },
  { n: '03', title: 'Pontozás meccsenként' },
  { n: '04', title: 'Szakasz-szorzók' },
  { n: '05', title: 'Végső győztes' },
  { n: '06', title: 'Joker' },
  { n: '07', title: 'Csapatok' },
  { n: '08', title: 'Ranglisták' },
  { n: '09', title: 'Holtverseny-eldöntés' },
  { n: '10', title: 'Értesítések' },
  { n: '11', title: 'Különleges esetek' },
]

export function Szabalyzat() {
  return (
    <div className="min-h-[calc(100vh-4rem)] bg-gradient-to-b from-accent/5 via-surface to-surface dark:from-[#0b1438] dark:via-surface dark:to-surface">
      <div className="max-w-3xl mx-auto px-6 py-12 space-y-10">
        <Hero />
        <TableOfContents />

        <RuleSection n="01" title="Tippelhető események">
          <div className="space-y-3">
            <h3 className="text-sm font-mono uppercase tracking-[0.15em] text-fg-subtle">
              Meccsenkénti tippek
            </h3>
            <p>
              A torna <strong className="text-fg-default">minden mérkőzésére</strong>{' '}
              tippelhetsz. A tipp a végeredményre szól — a rendes játékidő (90 perc)
              utáni állásra, hosszabbítás és büntetők <strong className="text-fg-default">nem</strong>{' '}
              számítanak bele.
            </p>
          </div>
          <div className="space-y-3 pt-2">
            <h3 className="text-sm font-mono uppercase tracking-[0.15em] text-fg-subtle">
              Végső tippek
            </h3>
            <p>
              A torna elején leadhatsz két <strong className="text-fg-default">hosszú távú tippet</strong>:
              ki nyeri a tornát, és ki lesz a gólkirály. Ezek a torna első mérkőzésének
              kezdő sípszójakor véglegesednek és többé nem változtathatók.
            </p>
          </div>
        </RuleSection>

        <RuleSection n="02" title="Tippelési határidő" highlight>
          <p>
            Minden meccs leadási határideje a <strong className="text-fg-default">kezdés
            előtti óra</strong> (kezdés −1 óra). Eddig módosíthatod vagy törölheted a tipped.
          </p>
          <p>
            A határidő előtt csak az látszik, <em>ki</em> tippelt már — a tartalom titkos.
            A határidő után minden tipp <strong className="text-fg-default">nyilvánossá</strong>{' '}
            válik a játékosok között.
          </p>
          <Callout label="Megjegyzés">
            A tipp elmulasztásáért nincs büntetés — az adott meccsre 0 pontot kapsz.
            A bukás maga a kihagyott esély.
          </Callout>
        </RuleSection>

        <RuleSection n="03" title="Pontozás meccsenként">
          <p>
            A pontozás kategóriánként történik. Mindig a{' '}
            <strong className="text-fg-default">legmagasabb pontszámú</strong> találat
            számít, a kategóriák nem adódnak össze.
          </p>
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle border-b border-border-subtle">
                <th className="py-2 font-normal">Kategória</th>
                <th className="py-2 font-normal text-right">Pont</th>
              </tr>
            </thead>
            <tbody className="font-mono">
              <ScoreRow label="Pontos eredmény" pts={10} />
              <ScoreRow label="Helyes győztes + gólkülönbség" pts={5} />
              <ScoreRow label="Helyes győztes" pts={3} />
              <ScoreRow label="Helyes döntetlen, rossz gólszámmal" pts={3} />
              <ScoreRow label="Csak egyik csapat gólszáma stimmel" pts={1} />
              <ScoreRow label="Semmi nem stimmel" pts={0} />
            </tbody>
          </table>
          <p className="text-xs text-fg-subtle">
            A „csak egyik csapat gólszáma stimmel" szigorúan{' '}
            <strong className="text-fg-default">hazai–hazai</strong> vagy{' '}
            <strong className="text-fg-default">vendég–vendég</strong> egyezés.
            Felcserélve (pl. tippelt hazai = valós vendég) nem jár pont.
          </p>
        </RuleSection>

        <RuleSection n="04" title="Szakasz-szorzók" highlight>
          <p>
            Az alap pontszámot a torna előrehaladtával szorozzuk fel. A szorzó a meccs
            szakaszától függ.
          </p>
          <ul className="grid gap-2 sm:grid-cols-2 font-mono">
            <StageRow label="Csoportkör" mult="1×" />
            <StageRow label="Tizenhatoddöntő" mult="1.5×" />
            <StageRow label="Nyolcaddöntő" mult="1.5×" />
            <StageRow label="Negyeddöntő" mult="2×" />
            <StageRow label="Elődöntő" mult="2.5×" />
            <StageRow label="Bronzmérkőzés" mult="2×" />
            <StageRow label="Döntő" mult="3×" emphasis />
          </ul>
          <p className="text-xs text-fg-subtle">
            Fél-szorzós eredménynél nullától kerekítünk (pl. 5 × 1.5 = 7.5 → 8 pont).
          </p>
        </RuleSection>

        <RuleSection n="05" title="Végső győztes">
          <p>
            A torna elején két hosszú távú tippet adhatsz le. A torna kezdetekor
            zárolódnak, és az eredmény a döntő után dől el.
          </p>
          <div className="grid gap-3 sm:grid-cols-2 pt-1">
            <LongBetCard title="Torna győztese" pts={50} />
            <LongBetCard title="Gólkirály" pts={30} />
          </div>
        </RuleSection>

        <RuleSection n="06" title="Joker" highlight>
          <ul className="space-y-2 list-disc pl-5 marker:text-accent">
            <li>
              <strong className="text-fg-default">3 joker</strong> áll rendelkezésre az
              egész tornára, meccsenként <strong className="text-fg-default">maximum 1</strong>.
            </li>
            <li>
              A joker megduplázza a meccsre kapott pontszámot — a szakasz-szorzó{' '}
              <strong className="text-fg-default">után</strong>.
            </li>
            <li>
              Csak <strong className="text-fg-default">csoportkörös</strong> meccsekre
              tehető. Egyenes kieséses szakaszra nem.
            </li>
            <li>
              A jokeres meccs téves tippje is bukja a jokert — valódi kockázat.
            </li>
          </ul>
          <Callout label="Tipp">
            Pontos eredmény jokerrel csoportkörben:{' '}
            10 × 1 × 2 = <strong>20 pont</strong>.
          </Callout>
        </RuleSection>

        <RuleSection n="07" title="Csapatok">
          <p>
            A csapat <strong className="text-fg-default">3 fős</strong> — vagy 3 ember,
            vagy 2 ember + 1 AI tag. Egy ember csak egy csapatban lehet. Nincs
            csapatkapitány: bármelyik tag módosíthatja a csapat nevét vagy az AI
            beállításait, amíg a csapat nyitva van.
          </p>
          <p>
            A csapat <strong className="text-fg-default">automatikusan lezárul</strong>{' '}
            amikor a 3. tag csatlakozik és a torna már elkezdődött. Addig új tagot
            lehet felvenni, akár a torna alatt is. Új csapatok is alakíthatók a torna
            közben.
          </p>
          <p>
            A csapatpontszám <strong className="text-fg-default">mind a 3 tag</strong>{' '}
            meccs-pontjainak összege — nincs „leggyengébb dobása". Ha valaki abbahagyja
            a tippelést, a 0 pontja közvetlenül rontja a csapat összpontszámát.
          </p>
          <div className="pt-2 space-y-3">
            <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
              AI tag — kockázati profilok
            </p>
            <p>
              Minden csapatban <strong className="text-fg-default">maximum 1 AI tag</strong>{' '}
              lehet, választható megjelenítendő névvel (de mindenhol AI-ként jelölve).
              Az AI minden meccsre automatikusan tippet ad le a választott profil
              szerint. A profil a torna kezdetéig változtatható, utána zárolt.
            </p>
            <div className="grid gap-3 sm:grid-cols-3">
              <AiModeCard
                name="Konzervatív"
                body="Esélyesek mellett dönt, alacsony gólszám, kevés meglepetés."
              />
              <AiModeCard
                name="Kiegyensúlyozott"
                body="Valószínű eredmény statisztika alapján, néha meglepetés."
              />
              <AiModeCard
                name="Merész"
                body="Sok döntetlen és meglepetés, gyakran magas gólszám."
              />
            </div>
            <p className="text-xs text-fg-subtle">
              Az AI tippek mellett rövid indoklás is megjelenik a határidő után. Ha az
              AI a határidőig (kezdés −1 óra) nem válaszol, automatikus{' '}
              <strong className="text-fg-default">1–1-es fallback</strong> kerül a
              helyére, „AI-fallback" jelöléssel.
            </p>
          </div>
        </RuleSection>

        <RuleSection n="08" title="Ranglisták" highlight>
          <p>
            Két ranglista fut párhuzamosan a torna alatt.
          </p>
          <div className="grid gap-3 sm:grid-cols-2">
            <BoardCard
              title="Egyéni"
              body="Egy játékos összes pontja: meccs-tippek + jokerek + végső tippek."
            />
            <BoardCard
              title="Csapat"
              body="A 3 tag meccs-pontjainak összege + jokerek + végső tippek mindenkitől."
            />
          </div>
        </RuleSection>

        <RuleSection n="09" title="Holtverseny-eldöntés">
          <p>
            Pontegyenlőség esetén a következő sorrendben dől el a helyezés:
          </p>
          <ol className="space-y-2 list-decimal pl-5 marker:text-accent marker:font-mono">
            <li>Több pontos eredmény (10 pontos találat).</li>
            <li>Helyes torna-győztes tipp.</li>
            <li>Helyes gólkirály tipp.</li>
            <li>
              A leghosszabb sorozat, amikor egymás utáni meccseken{' '}
              <strong className="text-fg-default">≥3 pont</strong>ot szereztél.
            </li>
          </ol>
          <p className="text-xs text-fg-subtle">
            Ha minden szempont szerint döntetlen marad, a játékosok{' '}
            <strong className="text-fg-default">megosztott helyezést</strong> kapnak.
          </p>
        </RuleSection>

        <RuleSection n="10" title="Értesítések" highlight>
          <ul className="space-y-2">
            <li className="flex gap-3">
              <span className="shrink-0 inline-flex items-center justify-center w-14 h-6 rounded-md bg-accent-soft text-accent-strong text-xs font-mono font-bold tabular-nums">
                T −24ó
              </span>
              <span>Emlékeztető azoknak, akik még nem tippeltek a meccsre.</span>
            </li>
            <li className="flex gap-3">
              <span className="shrink-0 inline-flex items-center justify-center w-14 h-6 rounded-md bg-accent-soft text-accent-strong text-xs font-mono font-bold tabular-nums">
                T −2ó
              </span>
              <span>Utolsó figyelmeztetés a kezdés előtt.</span>
            </li>
            <li className="flex gap-3">
              <span className="shrink-0 inline-flex items-center justify-center w-14 h-6 rounded-md bg-accent-soft text-accent-strong text-xs font-mono font-bold tabular-nums">
                T +90p
              </span>
              <span>Meccs után: pontösszesítő mindenkinek.</span>
            </li>
          </ul>
        </RuleSection>

        <RuleSection n="11" title="Különleges esetek">
          <div className="space-y-3">
            <h3 className="text-sm font-mono uppercase tracking-[0.15em] text-fg-subtle">
              Késői csatlakozás
            </h3>
            <p>
              A torna közben is csatlakozhatsz. A korábbi meccsekre{' '}
              <strong className="text-fg-default">0 pont</strong> jár. A végső tippek
              csak a torna kezdetéig adhatók le — utána nem. A már lezárult csoportkörös
              meccsekre nem használhatsz jokert visszamenőleg.
            </p>
          </div>
          <div className="space-y-3 pt-2">
            <h3 className="text-sm font-mono uppercase tracking-[0.15em] text-fg-subtle">
              Halasztott meccs
            </h3>
            <p>
              Az új határidő = új kezdési idő − 1 óra. A már leadott tippek automatikusan
              átkerülnek, és az új határidőig szerkeszthetők. Joker visszafizetés nincs.
            </p>
          </div>
          <div className="space-y-3 pt-2">
            <h3 className="text-sm font-mono uppercase tracking-[0.15em] text-fg-subtle">
              FIFA-döntéssel odaítélt meccs
            </h3>
            <p>
              Walkover vagy más FIFA-döntés esetén (pl. 3–0) a hivatalos eredmény
              számít — úgy pontozzuk, mintha ez lett volna a 90 perces végeredmény.
            </p>
          </div>
          <div className="space-y-3 pt-2">
            <h3 className="text-sm font-mono uppercase tracking-[0.15em] text-fg-subtle">
              Törölt meccs
            </h3>
            <p>
              Ha egy meccs el sem indul, mindenki <strong className="text-fg-default">0 pontot</strong>{' '}
              kap rá, és az arra a meccsre tett joker{' '}
              <strong className="text-fg-default">visszajár</strong>.
            </p>
          </div>
        </RuleSection>

        <footer className="pt-4 pb-2 text-center text-xs font-mono text-fg-subtle space-y-1">
          <p>Tip4Gen Szabályzat · v1.0</p>
          <p>WC 2026 · Foci VB Tippjáték</p>
        </footer>
      </div>
    </div>
  )
}

function Hero() {
  return (
    <section className="text-center space-y-5">
      <p className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.25em] text-accent">
        <BookOpen size={14} />
        Hivatalos szabályzat
      </p>
      <h1 className="text-4xl sm:text-5xl font-bold tracking-tight text-fg-default">
        Foci VB Tippjáték
      </h1>
      <p className="font-mono text-sm text-fg-muted max-w-xl mx-auto">
        Pontozás, jokerek, csapatok, ranglisták és különleges esetek — minden, amit a
        játékhoz tudnod kell.
      </p>
      <div className="flex flex-wrap justify-center gap-2 pt-2">
        <Pill label="Csapat 3 fős" />
        <Pill label="AI tag max 1" />
        <Pill label="Joker 3 db" />
        <Pill label="Max/meccs 30 pont" />
      </div>
    </section>
  )
}

function TableOfContents() {
  return (
    <nav
      aria-label="Tartalomjegyzék"
      className="rounded-2xl border border-border-subtle bg-elevated p-5"
    >
      <p className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle mb-3">
        Tartalomjegyzék
      </p>
      <ol className="grid gap-x-6 gap-y-2 sm:grid-cols-2 text-sm font-mono">
        {SECTIONS.map((s) => (
          <li key={s.n}>
            <a
              href={`#szabaly-${s.n}`}
              className="text-fg-muted hover:text-accent transition"
            >
              <span className="text-fg-subtle">§{s.n}</span> {s.title}
            </a>
          </li>
        ))}
      </ol>
    </nav>
  )
}

function RuleSection({
  n,
  title,
  highlight = false,
  children,
}: {
  n: string
  title: string
  highlight?: boolean
  children: ReactNode
}) {
  const borderCls = highlight ? 'border-accent/40' : 'border-border-subtle'
  return (
    <section
      id={`szabaly-${n}`}
      className={`rounded-2xl border ${borderCls} bg-elevated p-5 sm:p-6 space-y-4 scroll-mt-20`}
    >
      <header className="flex items-baseline gap-3">
        <span className="shrink-0 inline-flex items-center justify-center min-w-[2.5rem] h-8 px-2 rounded-md bg-accent-soft text-accent-strong text-xs font-mono font-bold tracking-[0.15em]">
          §{n}
        </span>
        <h2 className="text-xl sm:text-2xl font-bold tracking-tight text-fg-default">
          {title}
        </h2>
      </header>
      <div className="space-y-3 text-sm text-fg-muted leading-relaxed">{children}</div>
    </section>
  )
}

function Pill({ label }: { label: string }) {
  return (
    <span className="inline-flex items-center rounded-full border border-accent/30 bg-elevated px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] text-fg-muted">
      {label}
    </span>
  )
}

function ScoreRow({ label, pts }: { label: string; pts: number }) {
  return (
    <tr className="border-b border-border-subtle/50 last:border-b-0">
      <td className="py-2 text-fg-default">{label}</td>
      <td className="py-2 text-right tabular-nums font-bold text-accent-strong">
        {pts}
      </td>
    </tr>
  )
}

function StageRow({
  label,
  mult,
  emphasis = false,
}: {
  label: string
  mult: string
  emphasis?: boolean
}) {
  const borderCls = emphasis ? 'border-accent/40' : 'border-border-subtle'
  return (
    <li
      className={`flex items-center justify-between rounded-lg border ${borderCls} bg-sunken px-3 py-2 text-sm`}
    >
      <span className="text-fg-default">{label}</span>
      <span className="font-bold text-accent-strong tabular-nums">{mult}</span>
    </li>
  )
}

function LongBetCard({ title, pts }: { title: string; pts: number }) {
  return (
    <article className="rounded-xl border border-border-subtle bg-sunken p-4 flex items-center justify-between gap-3">
      <span className="text-sm text-fg-default">{title}</span>
      <span className="text-2xl font-mono font-bold tabular-nums text-accent-strong">
        {pts}
      </span>
    </article>
  )
}

function AiModeCard({ name, body }: { name: string; body: string }) {
  return (
    <article className="rounded-xl border border-border-subtle bg-sunken p-4 space-y-1.5">
      <h4 className="text-sm font-bold text-fg-default">{name}</h4>
      <p className="font-mono text-xs text-fg-muted leading-relaxed">{body}</p>
    </article>
  )
}

function BoardCard({ title, body }: { title: string; body: string }) {
  return (
    <article className="rounded-xl border border-border-subtle bg-sunken p-4 space-y-1.5">
      <h3 className="text-sm font-bold text-fg-default">{title}</h3>
      <p className="font-mono text-xs text-fg-muted leading-relaxed">{body}</p>
    </article>
  )
}

function Callout({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="rounded-lg border border-accent/30 bg-accent-soft/50 p-3 text-xs font-mono text-accent-strong">
      <span className="font-bold uppercase tracking-[0.15em]">{label} · </span>
      {children}
    </div>
  )
}
