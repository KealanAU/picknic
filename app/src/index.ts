import { createApp } from 'vue-lynx';
import { installIntlPolyfill, registerIconSet } from '@vyui/core';
import { VyUI } from '@vyui/kit';
import App from './App.vue';

// Lynx's PrimJS engine ships an incomplete `Intl`; install the shim before any
// component constructs a date/number formatter.
installIntlPolyfill();

// Iconify icon sets don't tree-shake, so register only the icons we use under
// the `lucide` prefix instead of importing the whole vendor JSON.
registerIconSet('lucide', {
  prefix: 'lucide',
  width: 24,
  height: 24,
  icons: {
    camera: {
      body: '<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M14.5 4h-5L7 7H4a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-3z"/><circle cx="12" cy="13" r="3"/></g>',
    },
    ticket: {
      body: '<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M2 9a3 3 0 0 1 0 6v2a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-2a3 3 0 0 1 0-6V7a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2z"/><path d="M13 5v2m0 4v2m0 4v2"/></g>',
    },
  },
});

const app = createApp(App);
app.use(VyUI, { ui: { primary: 'orange', gray: 'stone' } });
app.mount();
