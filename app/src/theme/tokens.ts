// Design tokens mirrored from tailwind.config.ts for inline `:style` consumers.
// Reference `t` instead of hardcoding hex so the palette is tunable in one place.
// The look is intentionally flat: shadowless, untilted, square cards, one accent.
export const t = {
  color: {
    cream: '#fff8f1',
    paper: '#fff8f1',
    card: '#fff8f1',
    ink: '#000000',
    inkSoft: '#3d4148',
    muted: '#8a8f98',
    line: '#e2e8f0',
    blue: '#006eff',
    // Legacy aliases collapse onto the blue system so old references keep resolving.
    clay: '#006eff',
    clayInk: '#006eff',
    claySoft: 'rgba(0, 110, 255, 0.08)',
    success: '#006eff',
    danger: '#c0392b',
  },
  font: {
    display: "'Comico', 'Editorial New', 'Playfair Display', Georgia, serif",
    body: "'Founders Grotesk', Inter, ui-sans-serif, system-ui, -apple-system, 'Segoe UI', sans-serif",
    hand: "'Founders Grotesk', Inter, ui-sans-serif, system-ui, -apple-system, 'Segoe UI', sans-serif",
  },
  tracking: '-0.02em',
  radius: {
    sm: '0px',
    md: '0px',
    lg: '0px',
    card: '0px',
    pill: '60px',
  },
  shadow: {
    soft: 'none',
    lift: 'none',
    sticker: 'none',
  },
  tilt: (_deg?: number) => 'none',
} as const;

export type Tokens = typeof t;
