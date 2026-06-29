import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import ui from '@nuxt/ui/vite';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    vue(),
    ui({
      ui: {
        colors: { primary: 'orange', neutral: 'stone' },
      },
    }),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Picknic',
        short_name: 'Picknic',
        description: 'Shared event camera — one roll, revealed at the end.',
        theme_color: '#e9573f',
        background_color: '#fdf6ec',
        display: 'standalone',
      },
    }),
  ],
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
    },
  },
});
