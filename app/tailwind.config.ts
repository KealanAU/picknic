import type { Config } from 'tailwindcss';
import lynxPreset from '@lynx-js/tailwind-preset';
import { createVyuiPreset } from '@vyui/kit/tailwind';

// ── Picknic brand palette (Drive Capital "Summer Drive") ───────────────────
// Monochrome Voltage Blue on a warm cream canvas, with Ash hairline grays. These
// override vyui's semantic `primary` / `neutral` scales with STATIC hex, so the
// generated `primary-500` / `neutral-200` utilities the kit themes reference
// resolve to brand colors without relying on runtime CSS vars (which Lynx
// native treats inconsistently). See src/theme/tokens.ts for the inline-style
// mirror of these values. #006eff is primary-500; #e2e8f0 (Ash) is neutral-200.
const voltage = {
  50: '#e6f0ff',
  100: '#cce0ff',
  200: '#99c2ff',
  300: '#66a3ff',
  400: '#3385ff',
  500: '#006eff',
  600: '#0058cc',
  700: '#004299',
  800: '#002c66',
  900: '#001633',
  950: '#000b1a',
};

// Neutral ash grays — 200 is the canonical hairline (#e2e8f0).
const ash = {
  50: '#fbfcfd',
  100: '#f4f6f9',
  200: '#e2e8f0',
  300: '#cbd3de',
  400: '#9aa4b2',
  500: '#6b7480',
  600: '#4b535d',
  700: '#343a42',
  800: '#1f242a',
  900: '#0d1013',
  950: '#060809',
};

const config: Config = {
  content: [
    './src/**/*.{vue,js,ts}',
    // Kit component themes carry their class strings in dist — scan them so the
    // utilities they use actually get generated.
    './node_modules/@vyui/kit/dist/**/*.js',
  ],
  // createVyuiPreset() ships a safelist for the kit's runtime-built color
  // classes (`bg-${c}-500`, …) which Tailwind's static scan can't see. Its
  // default covers all 6 semantic scales × 11 shades × ~27 state variants,
  // which balloons the Lynx bundle to ~6.7 MB. A top-level `safelist` REPLACES
  // the preset's (Tailwind resolves safelist first-defined-wins, not merge), so
  // we scope it to what this app renders: primary + neutral, the shades the kit
  // themes emit, and the handful of state variants we actually use — press is
  // `data-[state=active]` (vyui core drives it via a data attr, not CSS :active).
  safelist: [
    {
      pattern: /(bg|text|ring|border)-(primary|neutral)-(50|100|200|300|400|500|600|700|900)/,
      variants: ['active', 'focus', 'disabled', 'data-[state=active]', 'ui-highlighted'],
    },
    'text-white',
  ],
  presets: [lynxPreset, createVyuiPreset()],
  theme: {
    extend: {
      colors: {
        primary: voltage,
        neutral: ash,
        // Named tokens for hand-crafted components (bg-cream, text-ink, …).
        cream: '#fff8f1',
        paper: '#fff8f1',
        ink: '#000000',
        voltage,
        ash,
      },
      fontFamily: {
        display: ['Comico', 'Editorial New', 'Playfair Display', 'Georgia', 'serif'],
        body: ['Founders Grotesk', 'Inter', 'system-ui', '-apple-system', 'Segoe UI', 'sans-serif'],
        sans: ['Founders Grotesk', 'Inter', 'system-ui', '-apple-system', 'Segoe UI', 'sans-serif'],
      },
      letterSpacing: {
        tight: '-0.02em',
      },
      borderRadius: {
        card: '0px',
        pill: '60px',
      },
    },
  },
};

export default config;
