// Design tokens mirrored from tailwind.config.ts for inline `:style` consumers.
// Reference `t` instead of hardcoding hex so the palette is tunable in one place.
// Scrapbook base tokens. Keep these gentle so the app gets texture without
// making dense host controls feel decorative or noisy.
export const t = {
  color: {
    cream: '#fff8f1',
    paper: '#fffdf7',
    card: '#fffaf0',
    ink: '#000000',
    inkSoft: '#3d4148',
    muted: '#8a8f98',
    line: '#eadfce',
    blue: '#006eff',
    // Legacy aliases collapse onto the blue system so old references keep resolving.
    clay: '#006eff',
    clayInk: '#006eff',
    claySoft: 'rgba(0, 110, 255, 0.08)',
    success: '#006eff',
    danger: '#c0392b',
  },
  font: {
    display: "'Comico', Georgia, serif",
    body: "'Founders Grotesk', Inter, ui-sans-serif, system-ui, -apple-system, 'Segoe UI', sans-serif",
    hand: "'Founders Grotesk', Inter, ui-sans-serif, system-ui, -apple-system, 'Segoe UI', sans-serif",
  },
  tracking: '-0.02em',
  radius: {
    sm: '6px',
    md: '10px',
    lg: '14px',
    card: '12px',
    pill: '60px',
  },
  shadow: {
    soft: '0 8px 22px rgba(67, 45, 28, 0.08)',
    lift: '0 16px 36px rgba(67, 45, 28, 0.12)',
    sticker: '0 5px 0 rgba(67, 45, 28, 0.16)',
  },
  tilt: (deg = -1.5) => `rotate(${deg}deg)`,
} as const;

export type Tokens = typeof t;
