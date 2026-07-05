import { defineConfig } from '@lynx-js/rspeedy';
import { pluginQRCode } from '@lynx-js/qrcode-rsbuild-plugin';
import { pluginTailwindCSS } from 'rsbuild-plugin-tailwindcss';
import { pluginVueLynx } from 'vue-lynx/plugin';

export default defineConfig({
  // Declaring the `web` environment makes vue-lynx emit `main.web.bundle` and
  // wires up rspeedy's built-in `__web_preview` shell, which prints a
  // browser-openable URL that renders the web bundle. The `lynx` environment
  // keeps emitting `main.lynx.bundle` for native Lynx Explorer (the QR code).
  environments: {
    web: {},
    lynx: {},
  },
  source: {
    entry: {
      main: './src/index.ts',
    },
  },
  output: {
    // Native Lynx cannot fetch CSS-emitted font assets when they are rewritten
    // to `webpack:///static/font/...`. Inline fonts so Lynx Explorer can use
    // the same @font-face fallback as a custom host app.
    dataUriLimit: {
      font: Number.MAX_SAFE_INTEGER,
    },
  },
  plugins: [
    // Prints a scannable QR code + dev URLs for the native Lynx bundle on
    // `rspeedy dev`. Press `a` in the terminal to switch between schemas.
    pluginQRCode({
      schema(url) {
        return `${url}?fullscreen=true`;
      },
    }),
    pluginVueLynx({
      optionsApi: false,
      // Needed so vyui's `--ui-*` token vars (imported via @vyui/kit/style.css)
      // resolve on Lynx native, not just web preview.
      enableCSSInheritance: true,
      enableCSSInlineVariables: true,
    }),
    pluginTailwindCSS({
      config: 'tailwind.config.ts',
      // This plugin rebuilds Tailwind's `content` from the bundled module graph
      // and drops anything matching `exclude`. Excluding all of node_modules
      // meant @vyui/kit's component class strings (button padding/rounding/flex,
      // input borders, …) were never scanned, so kit components rendered as
      // unstyled boxes. Keep node_modules excluded for speed, but let @vyui
      // through so its dist classes get generated. (The kit's dynamically-built
      // color classes — `bg-${c}-500` — still can't be statically extracted;
      // those are covered by the safelist in tailwind.config.ts.)
      exclude: [/[\\/]node_modules[\\/](?!.*@vyui)/],
    }),
  ],
});
