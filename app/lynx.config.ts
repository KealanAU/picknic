import { defineConfig, type RsbuildPlugin } from '@lynx-js/rspeedy';
import { pluginQRCode } from '@lynx-js/qrcode-rsbuild-plugin';
import { pluginTailwindCSS } from 'rsbuild-plugin-tailwindcss';
import { pluginVueLynx } from 'vue-lynx/plugin';
import { createWebVirtualFilesMiddleware } from '@lynx-js/web-rsbuild-server-middleware';

// Path the mocked Lynx Web Platform shell is served under during `rspeedy dev`.
const WEB_PREVIEW_PATH = '/web';

// Serves the Lynx Web Platform shell and prints a clickable browser URL that
// renders the same bundle the QR code points native Lynx Explorer at.
const pluginWebPreview = (): RsbuildPlugin => ({
  name: 'picknic:web-preview',
  setup(api) {
    api.modifyRsbuildConfig((config) => {
      config.dev ??= {};
      const existing = config.dev.setupMiddlewares ?? [];
      config.dev.setupMiddlewares = [
        (middlewares) => {
          middlewares.unshift(createWebVirtualFilesMiddleware(WEB_PREVIEW_PATH));
        },
        ...(Array.isArray(existing) ? existing : [existing]),
      ];
    });

    let printed = false;
    api.onDevCompileDone(() => {
      if (printed || !api.context.devServer) return;
      printed = true;
      const { port } = api.context.devServer;
      const { dev } = api.getNormalizedConfig();
      const prefix = (typeof dev.assetPrefix === 'string' ? dev.assetPrefix : `http://localhost:<port>/`)
        .replaceAll('<port>', String(port))
        .replace(/\/$/, '');
      const bundleUrl = `${prefix}/main.lynx.bundle`;
      const webUrl = `${prefix}${WEB_PREVIEW_PATH}/?casename=${encodeURIComponent(bundleUrl)}`;
      console.log(`\n  ➜  Web       ${webUrl}\n`);
    });
  },
});

export default defineConfig({
  source: {
    entry: {
      main: './src/index.ts',
    },
  },
  plugins: [
    // Prints a scannable QR code + dev URLs for the Lynx bundle on `rspeedy dev`.
    // Press `a` in the terminal to switch between the offered schemas.
    pluginQRCode({
      schema(url) {
        return `${url}?fullscreen=true`;
      },
    }),
    // Prints a browser-openable web preview URL for the same bundle.
    pluginWebPreview(),
    pluginVueLynx({
      optionsApi: false,
      // Needed so vyui's `--ui-*` token vars (imported via @vyui/kit/style.css)
      // resolve on Lynx native, not just web preview.
      enableCSSInheritance: true,
      enableCSSInlineVariables: true,
    }),
    pluginTailwindCSS({
      config: 'tailwind.config.ts',
      exclude: [/[\\/]node_modules[\\/]/],
    }),
  ],
});
