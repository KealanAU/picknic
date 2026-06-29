<script setup lang="ts">
const joining = ref(false);
const code = ref('');

// SSR-rendered tags for SEO / link previews.
useSeoMeta({
  title: 'Picknic — Shared event camera',
  description: 'Snap photos at a wedding or party. One shared roll, revealed at the end.',
  ogTitle: 'Picknic — Shared event camera',
  ogDescription: 'One roll. Revealed at the end.',
  ogType: 'website',
  twitterCard: 'summary_large_image',
});
</script>

<template>
  <main class="min-h-dvh flex flex-col items-center justify-center gap-6 p-6 text-center">
    <div>
      <h1 class="text-5xl font-bold">🧺 Picknic</h1>
      <p class="text-muted mt-2">One roll. Revealed at the end.</p>
    </div>

    <UCard class="w-full max-w-sm">
      <template #header>
        <h2 class="font-semibold">Join an event</h2>
      </template>

      <UFormField label="Event code" name="code">
        <UInput
          v-model="code"
          placeholder="e.g. SARAH-MAX"
          icon="i-lucide-ticket"
          class="w-full"
        />
      </UFormField>

      <template #footer>
        <UButton
          block
          icon="i-lucide-camera"
          label="Join & start snapping"
          :loading="joining"
          :disabled="!code"
          @click="joining = true"
        />
      </template>
    </UCard>
  </main>
</template>
