import { networkInterfaces } from 'node:os';
import { defineConfig } from '@lynx-js/rspeedy';
import { pluginQRCode } from '@lynx-js/qrcode-rsbuild-plugin';
import { pluginTailwindCSS } from 'rsbuild-plugin-tailwindcss';
import { pluginVueLynx } from 'vue-lynx/plugin';

// Native bundles have no `location` global, so api/config.ts can't infer the
// dev machine's address the way the web preview does. Inline the LAN IP the
// QR code already points devices at, so they reach the local API too.
function lanHost(): string {
  for (const nets of Object.values(networkInterfaces())) {
    for (const net of nets ?? []) {
      if (net.family === 'IPv4' && !net.internal) return net.address;
    }
  }
  return 'localhost';
}

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
    define: {
      'import.meta.env.PUBLIC_DEV_LAN_HOST': JSON.stringify(lanHost()),
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
      // Route @vyui packages' worklets through the main-thread loader. vue-lynx
      // otherwise excludes all of node_modules, so vyui's MT worklet components
      // (SwipeAction, ScrollView, Slider, gestures) never reach the MT graph.
      // Replaces the old patches/vue-lynx@0.4.0.patch, which hard-coded this
      // before vue-lynx exposed the allowlist option.
      includeWorkletPackages: [/@vyui\//],
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
