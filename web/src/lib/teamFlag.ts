// Team code → ISO 3166-1 alpha-2 (lowercase) for flagcdn.com.
//
// worldcup26.ir (current provider, since 2026-06-03) returns real FIFA codes
// — IRN/NED/ESP/JPN/SUI/KSA/MAR/etc. The older api-football abbreviations
// (IRA/NET/SPA/JAP/SWI/SAU/MOR) are still listed below as harmless aliases
// in case we ever swap upstream again. Unknown codes return null and the
// caller renders no flag.
//
// UK subdivisions (ENG/WAL/SCO/NIR) map to flagcdn's gb-eng/gb-wls/gb-sct/
// gb-nir subdivision SVGs so each nation gets its own flag.

const FIFA_TO_ISO2: Record<string, string> = {
  // --- Codes seen in the api-football WC 2022 seed (kept as aliases) ---
  QAT: 'qa', ECU: 'ec', SEN: 'sn', NET: 'nl',
  ENG: 'gb-eng', IRA: 'ir', USA: 'us', WAL: 'gb-wls',
  ARG: 'ar', SAU: 'sa', MEX: 'mx', POL: 'pl',
  FRA: 'fr', AUS: 'au', DEN: 'dk', TUN: 'tn',
  SPA: 'es', COS: 'cr', GER: 'de', JAP: 'jp',
  BEL: 'be', CAN: 'ca', MOR: 'ma', CRO: 'hr',
  BRA: 'br', SER: 'rs', SWI: 'ch', CAM: 'cm',
  POR: 'pt', GHA: 'gh', URU: 'uy', KOR: 'kr',
  // --- Real FIFA codes (current worldcup26.ir provider) ---
  NED: 'nl', IRN: 'ir', KSA: 'sa', ESP: 'es',
  CRC: 'cr', JPN: 'jp', MAR: 'ma', SRB: 'rs',
  SUI: 'ch', CMR: 'cm',
  // --- Extra WC 2026 likely participants (host trio + qualifiers) ---
  // (USA / MEX / CAN already above as hosts)
  ITA: 'it', AUT: 'at', HUN: 'hu', NOR: 'no',
  SWE: 'se', TUR: 'tr', CZE: 'cz', SVK: 'sk',
  ROU: 'ro', UKR: 'ua', SCO: 'gb-sct', NIR: 'gb-nir',
  IRL: 'ie', GRE: 'gr', ISR: 'il', ALB: 'al',
  COL: 'co', VEN: 've', PAR: 'py', PER: 'pe',
  CHI: 'cl', BOL: 'bo', JAM: 'jm', PAN: 'pa',
  HON: 'hn', SLV: 'sv', GUA: 'gt', HAI: 'ht',
  CUW: 'cw', SUR: 'sr', TRI: 'tt',
  EGY: 'eg', NGA: 'ng', ALG: 'dz', CIV: 'ci',
  RSA: 'za', MLI: 'ml', BFA: 'bf', GNB: 'gw',
  ZAM: 'zm', UGA: 'ug', GAB: 'ga', GUI: 'gn',
  COD: 'cd', CGO: 'cg', ANG: 'ao', CPV: 'cv',
  COM: 'km', LBR: 'lr', SLE: 'sl', NIG: 'ne',
  TOG: 'tg', BEN: 'bj', MTN: 'mr', ZIM: 'zw',
  MOZ: 'mz', RWA: 'rw', ETH: 'et', TAN: 'tz',
  KEN: 'ke', SUD: 'sd', LBY: 'ly',
  IRQ: 'iq', UAE: 'ae', OMA: 'om', SYR: 'sy',
  JOR: 'jo', LBN: 'lb', BHR: 'bh', KUW: 'kw',
  YEM: 'ye', PLE: 'ps',
  THA: 'th', VIE: 'vn', PHI: 'ph', IDN: 'id',
  MAS: 'my', SIN: 'sg', PRK: 'kp', CHN: 'cn',
  HKG: 'hk', TPE: 'tw', UZB: 'uz', KAZ: 'kz',
  TJK: 'tj', TKM: 'tm', KGZ: 'kg', IND: 'in',
  PAK: 'pk', BAN: 'bd', NEP: 'np', SRI: 'lk',
  NZL: 'nz', FIJ: 'fj', PNG: 'pg', SOL: 'sb',
  VAN: 'vu', NCL: 'nc', TAH: 'pf',
  // --- A few common Europe extras (qualifying pool) ---
  BIH: 'ba', SVN: 'si', MKD: 'mk', MNE: 'me',
  BUL: 'bg', BLR: 'by', RUS: 'ru', MDA: 'md',
  LTU: 'lt', LVA: 'lv', EST: 'ee', FIN: 'fi',
  ISL: 'is', LUX: 'lu', AND: 'ad', MLT: 'mt',
  CYP: 'cy', KOS: 'xk', GIB: 'gi', LIE: 'li',
  ARM: 'am', AZE: 'az', GEO: 'ge',
}

export function fifaToIso2(code: string | null | undefined): string | null {
  if (!code) return null
  const iso = FIFA_TO_ISO2[code.toUpperCase()]
  return iso ?? null
}
