import { defineVyuiConfig } from '@vyui/kit/config';

export const vyuiConfig = defineVyuiConfig({
  theme: {
    primary: 'blue',
    gray: 'slate',
    colors: ['primary', 'secondary', 'success', 'info', 'warning', 'error'],
  },
  components: {
    // Solid blue buttons carry cream text instead of the kit's white.
    button: {
      compoundVariants: [
        { color: 'primary', variant: 'solid', class: { label: 'text-cream' } },
        // Ghost text (Back, Retake, Leave the roll) in brand blue, not the kit's darker 600.
        { color: 'primary', variant: 'ghost', class: { label: 'text-primary-500' } },
      ],
    },
  },
});

export default vyuiConfig;
