import { defineConfig } from '@lynx-js/rspeedy';
import { pluginVueLynx } from 'vue-lynx/plugin';

export default defineConfig({
  source: {
    entry: {
      main: './src/index.ts',
    },
  },
  plugins: [
    pluginVueLynx({
      optionsApi: false,
    }),
  ],
});
