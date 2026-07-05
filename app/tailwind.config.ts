import type { Config } from 'tailwindcss';
import lynxPreset from '@lynx-js/tailwind-preset';
import { createVyuiPreset } from '@vyui/kit/tailwind';

// Brand palette as STATIC hex overriding vyui's primary/neutral scales, so
// primary-500/neutral-200 utilities resolve without runtime CSS vars (which Lynx
// native treats inconsistently). Mirrored for inline styles in src/theme/tokens.ts.
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
    // Kit themes carry their class strings in dist; scan them so those utilities generate.
    './node_modules/@vyui/kit/dist/**/*.js',
  ],
  // This top-level safelist REPLACES the vyui preset's (Tailwind is first-wins, not
  // merge). The preset's default covers all scales × shades × variants and balloons
  // the Lynx bundle to ~6.7 MB, so we scope it to just what this app renders.
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
