<script setup lang="ts">
const email = ref('');
const password = ref('');
const loading = ref(false);

useSeoMeta({ title: 'Host sign in — Picknic', robots: 'noindex' });

// TODO: wire to /api/auth/login + /api/auth/register (ASP.NET Identity).
function submit() {
  loading.value = true;
}
</script>

<template>
  <main class="flex min-h-dvh items-center justify-center p-6">
    <div class="w-full max-w-sm">
      <div class="mb-8 text-center">
        <NuxtLink to="/" class="text-subheading font-bold">🧺 Picknic</NuxtLink>
        <p class="text-body-sm mt-1 text-muted">Host your event</p>
      </div>

      <UCard class="ring-hairline bg-default">
        <div class="flex flex-col gap-4">
          <UFormField label="Email" name="email">
            <UInput v-model="email" type="email" placeholder="you@example.com" size="lg" class="w-full" />
          </UFormField>
          <UFormField label="Password" name="password">
            <UInput v-model="password" type="password" placeholder="••••••••" size="lg" class="w-full" />
          </UFormField>
          <UButton
            block
            size="lg"
            color="neutral"
            label="Continue"
            :loading="loading"
            :disabled="!email || !password"
            @click="submit"
          />
        </div>
      </UCard>
    </div>
  </main>
</template>
