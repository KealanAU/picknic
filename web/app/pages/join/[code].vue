<script setup lang="ts">
const route = useRoute();
const code = computed(() => String(route.params.code || '').toUpperCase());
const name = ref('');
const joining = ref(false);

useSeoMeta({ title: () => `Join ${code.value} — Picknic`, robots: 'noindex' });

// TODO: POST /api/events/{code}/join with the scan secret (from the URL fragment)
// and this name, then route into the capture screen.
function join() {
  joining.value = true;
}
</script>

<template>
  <main class="atmosphere-warm flex min-h-dvh items-center justify-center p-6">
    <UCard class="ring-hairline w-full max-w-sm bg-default">
      <template #header>
        <p class="text-eyebrow text-muted">You're invited to</p>
        <h1 class="text-heading text-highlighted">Event {{ code }}</h1>
      </template>

      <UFormField label="Your name" name="name">
        <UInput v-model="name" placeholder="e.g. Alex" size="lg" class="w-full" />
      </UFormField>

      <template #footer>
        <UButton
          block
          size="lg"
          color="neutral"
          label="Join & start snapping"
          trailing-icon="i-lucide-camera"
          :loading="joining"
          :disabled="!name"
          @click="join"
        />
      </template>
    </UCard>
  </main>
</template>
