// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  modules: ['@nuxt/ui', '@vite-pwa/nuxt'],
  css: ['~/assets/css/main.css'],
  devtools: { enabled: true },

  // The Amplemarket brand is a light-only system — the atmosphere needs a white canvas.
  colorMode: { preference: 'light', fallback: 'light' },

  // SSR is on by default — kept explicit for the SEO use case.
  ssr: true,

  app: {
    head: {
      htmlAttrs: { lang: 'en' },
      meta: [
        { name: 'viewport', content: 'width=device-width, initial-scale=1, viewport-fit=cover' },
        { name: 'theme-color', content: '#ffffff' },
      ],
    },
  },

  runtimeConfig: {
    // Overridable at runtime via NUXT_API_BASE. Consumed by the /api proxy
    // handler (server/routes/api/[...].ts) so browser + SSR share one origin.
    apiBase: process.env.NUXT_API_BASE || 'http://localhost:5000',
    public: {
      siteUrl: process.env.NUXT_PUBLIC_SITE_URL || 'http://localhost:3000',
    },
  },

  pwa: {
    registerType: 'autoUpdate',
    manifest: {
      name: 'Picknic',
      short_name: 'Picknic',
      description: 'Shared event camera — one roll, revealed at the end.',
      theme_color: '#ffffff',
      background_color: '#ffffff',
      display: 'standalone',
    },
  },
});
