import { defineVyuiConfig } from '@vyui/kit/config';

export const vyuiConfig = defineVyuiConfig({
  theme: {
    primary: 'blue',
    gray: 'slate',
    colors: ['primary', 'secondary', 'success', 'info', 'warning', 'error'],
  },
});

export default vyuiConfig;
