// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  modules: ['@nuxt/ui', '@vite-pwa/nuxt'],
  css: ['~/assets/css/main.css'],
  devtools: { enabled: true },

  runtimeConfig: {
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5000',
    },
  },

  pwa: {
    registerType: 'autoUpdate',
    manifest: {
      name: 'Picknic',
      short_name: 'Picknic',
      description: 'Shared event camera — one roll, revealed at the end.',
      theme_color: '#e9573f',
      background_color: '#fdf6ec',
      display: 'standalone',
    },
  },
});
