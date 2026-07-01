export default defineAppConfig({
  ui: {
    // Ink-black structure + primary action; Deep Violet as the power color;
    // Ember reserved for atmosphere (mapped to warning, never a default CTA).
    colors: {
      primary: 'ink',
      neutral: 'ink',
      secondary: 'violet',
      info: 'violet',
      warning: 'ember',
      success: 'green',
      error: 'red',
    },

    // 12px radius, medium weight, tight tracking on the filled Ink Black button.
    button: {
      slots: {
        base: 'rounded-xl font-medium tracking-[-0.16px]',
      },
      compoundVariants: [
        // Secondary button: white fill, 1px Ink Black border (border is the hierarchy).
        {
          color: 'neutral',
          variant: 'outline',
          class: 'ring-1 ring-[var(--color-ink-900)] text-highlighted bg-default',
        },
      ],
    },

    input: { slots: { base: 'rounded-xl' } },
    textarea: { slots: { base: 'rounded-xl' } },
    select: { slots: { base: 'rounded-xl' } },
    selectMenu: { slots: { base: 'rounded-xl' } },

    // Flat, border-driven cards — hairline inset ring, no drop shadow.
    card: {
      slots: {
        root: 'rounded-xl ring-1 ring-default shadow-none',
      },
    },

    // Announcement / eyebrow pills.
    badge: { slots: { base: 'rounded-full font-medium' } },

    modal: { slots: { content: 'rounded-xl' } },
  },
});
