/**
 * Picknic design tokens — the single source of truth for the Drive Capital
 * "Summer Drive" look, mirrored from tailwind.config.ts for inline `:style`
 * consumers (Lynx SFCs lean on inline styles; vyui components pull the same
 * palette through the Tailwind preset). Import `t` and reference tokens instead
 * of hardcoding hex, so the whole vibe is tunable in one place.
 *
 * Aesthetic target: retro road-trip poster on warm paper. A warm cream canvas,
 * a single electric Voltage Blue accent, true-black body ink, and hairline Ash
 * dividers doing all the structural work. Surfaces are strictly flat — no
 * shadows, no gradients, no tilt. Type pairs a hairline serif for display
 * headlines against a low-weight grotesk for everything else, tracked tight at
 * -0.02em. Actions are 60px outlined pills, never filled.
 */
export const t = {
  color: {
    cream: '#fff8f1', // page canvas + card surface — warm off-white
    paper: '#fff8f1', // alias (app background)
    card: '#fff8f1', // inset panels — same cream, distinguished by hairline only
    ink: '#000000', // primary text / fine strokes
    inkSoft: '#3d4148', // secondary text (kept near-black for restraint)
    muted: '#8a8f98', // captions / hints — neutral gray
    line: '#e2e8f0', // Ash hairline borders / rule lines
    blue: '#006eff', // Voltage Blue — the only chromatic voice
    // Legacy aliases collapse onto the monochrome-blue system so existing
    // token references keep resolving without reintroducing a second color.
    clay: '#006eff',
    clayInk: '#006eff',
    claySoft: 'rgba(0, 110, 255, 0.08)',
    success: '#006eff',
    danger: '#c0392b', // functional error only — never decorative
  },
  font: {
    // Display face — headlines only. Comico is an informal, all-caps handwriting
    // letterform (ITF Free Font License, bundled) that carries the display voice
    // against the quiet grotesk body.
    display: "'Comico', 'Editorial New', 'Playfair Display', Georgia, serif",
    // All non-display text — a low-weight grotesk carries the quiet confidence.
    body: "'Founders Grotesk', Inter, ui-sans-serif, system-ui, -apple-system, 'Segoe UI', sans-serif",
    // Legacy 'hand' alias maps to the grotesk (labels are grotesk uppercase).
    hand: "'Founders Grotesk', Inter, ui-sans-serif, system-ui, -apple-system, 'Segoe UI', sans-serif",
  },
  /** Uniform tight tracking is a signature — apply to every text run. */
  tracking: '-0.02em',
  radius: {
    // Cards are square; only pills/buttons round, and always to 60px.
    sm: '0px',
    md: '0px',
    lg: '0px',
    card: '0px',
    pill: '60px',
  },
  shadow: {
    // Strictly shadowless — separation comes from hairlines + whitespace.
    soft: 'none',
    lift: 'none',
    sticker: 'none',
  },
  /** No hand-placed tilt in the editorial system. */
  tilt: (_deg?: number) => 'none',
} as const;

export type Tokens = typeof t;
