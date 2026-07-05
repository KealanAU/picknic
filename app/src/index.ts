import { createApp } from 'vue-lynx';
import { installIntlPolyfill, registerIconSet } from '@vyui/core';
import { VyUI } from '@vyui/kit';
import App from './App.vue';
import { activeStorageName } from './api/storage';
// Brand layer: Tailwind utilities, vyui tokens/animations, fonts, palette.
import './style.css';

// In Explorer this logs "memory" (no persistence); web preview logs
// "localStorage"; a real host with the module logs "native-kv"/"sqlite:kv".
console.log(`[picknic] storage backend: ${activeStorageName()}`);

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
    mail: {
      body: '<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="m22 7-8.991 5.727a2 2 0 0 1-2.009 0L2 7"/><rect x="2" y="4" width="20" height="16" rx="2"/></g>',
    },
    eye: {
      body: '<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M2.062 12.348a1 1 0 0 1 0-.696 10.75 10.75 0 0 1 19.876 0 1 1 0 0 1 0 .696 10.75 10.75 0 0 1-19.876 0"/><circle cx="12" cy="12" r="3"/></g>',
    },
    'eye-off': {
      body: '<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M10.733 5.076a10.744 10.744 0 0 1 11.205 6.575 1 1 0 0 1 0 .696 10.747 10.747 0 0 1-1.444 2.49"/><path d="M14.084 14.158a3 3 0 0 1-4.242-4.242"/><path d="M17.479 17.499a10.75 10.75 0 0 1-15.417-5.151 1 1 0 0 1 0-.696 10.75 10.75 0 0 1 4.446-5.143"/><path d="m2 2 20 20"/></g>',
    },
    'arrow-left': {
      body: '<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="m12 19-7-7 7-7"/><path d="M19 12H5"/></g>',
    },
    'calendar-plus': {
      body: '<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M8 2v4"/><path d="M16 2v4"/><path d="M21 13V6a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h8"/><path d="M3 10h18"/><path d="M16 19h6"/><path d="M19 16v6"/></g>',
    },
  },
});

const app = createApp(App);
app.use(VyUI, { ui: { primary: 'orange', gray: 'stone' } });
app.mount();
