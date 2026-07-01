<script setup lang="ts">
const code = ref('');
const joining = ref(false);

function join() {
  if (!code.value) return;
  joining.value = true;
  navigateTo(`/join/${code.value.trim().toUpperCase()}`);
}

// SSR-rendered tags for SEO / link previews.
useSeoMeta({
  title: 'Picknic — Shared event camera',
  description: 'Snap photos at a wedding or party. One shared roll, revealed at the end.',
  ogTitle: 'Picknic — Shared event camera',
  ogDescription: 'One roll. Revealed at the end.',
  ogType: 'website',
  twitterCard: 'summary_large_image',
});

const steps = [
  { icon: 'i-lucide-qr-code', tile: 'bg-blush', title: 'Scan to join', body: 'Guests scan the QR at the table — no app store, no account.' },
  { icon: 'i-lucide-camera', tile: 'bg-butter', title: 'Snap the night', body: 'Everyone shoots into one shared roll during the party.' },
  { icon: 'i-lucide-sparkles', tile: 'bg-lilac', title: 'Reveal at the end', body: 'The roll “develops” and unlocks for everyone at once.' },
];
</script>

<template>
  <div class="min-h-dvh">
    <!-- Sticky pill navigation -->
    <header class="sticky top-0 z-50 px-4 pt-4">
      <UContainer>
        <nav class="ring-hairline flex h-14 items-center justify-between rounded-lg bg-default/80 px-3 backdrop-blur-md sm:px-4">
          <span class="text-subheading font-bold">🧺 Picknic</span>
          <div class="hidden items-center gap-6 text-body-sm text-muted sm:flex">
            <a href="#how" class="hover:text-highlighted">How it works</a>
            <a href="#pricing" class="hover:text-highlighted">Pricing</a>
          </div>
          <div class="flex items-center gap-2">
            <UButton to="/host" color="neutral" variant="outline" label="Host sign in" class="hidden sm:inline-flex" />
            <UButton to="/host" color="neutral" label="Start an event" />
          </div>
        </nav>
      </UContainer>
    </header>

    <!-- Hero -->
    <section class="atmosphere-warm relative overflow-hidden">
      <UContainer class="relative py-24 sm:py-32">
        <div class="mx-auto flex max-w-2xl flex-col items-center text-center">
          <UBadge color="neutral" variant="outline" class="ring-hairline mb-8 gap-1.5 bg-default py-1.5 pl-1.5 pr-3">
            <span class="rounded-full bg-inverted px-2 py-0.5 text-eyebrow text-inverted">NEW</span>
            <span class="text-eyebrow text-highlighted">Live QR join is here</span>
            <UIcon name="i-lucide-arrow-right" class="size-3" />
          </UBadge>

          <h1 class="text-display text-highlighted sm:text-hero">
            One roll.<br >Revealed at the end.
          </h1>
          <p class="text-subheading mt-6 text-muted">
            Picknic is the shared event camera. Guests snap through the night; the
            photos pool into a single roll that develops when the party ends.
          </p>

          <!-- Join capture -->
          <div class="ring-hairline mt-10 flex w-full max-w-md items-center rounded-xl bg-default p-1.5">
            <UInput
              v-model="code"
              placeholder="Enter your event code"
              variant="none"
              size="xl"
              class="flex-1"
              :ui="{ base: 'bg-transparent' }"
              @keydown.enter="join"
            />
            <UButton
              color="neutral"
              size="xl"
              label="Join"
              trailing-icon="i-lucide-camera"
              :loading="joining"
              @click="join"
            />
          </div>

          <div class="mt-6 flex items-center gap-2 text-body-sm text-muted">
            <UIcon name="i-lucide-heart" class="size-4 text-ember-500" />
            Loved at weddings, birthdays &amp; reunions
          </div>
        </div>
      </UContainer>
    </section>

    <!-- How it works — pastel tiles -->
    <section id="how" class="py-20 sm:py-28">
      <UContainer>
        <h2 class="text-heading-lg max-w-xl text-highlighted">How a Picknic works</h2>
        <div class="mt-10 grid gap-5 sm:grid-cols-3">
          <div v-for="s in steps" :key="s.title" :class="s.tile" class="rounded-xl p-5">
            <UIcon :name="s.icon" class="size-6 text-highlighted" />
            <h3 class="text-subheading mt-4 font-medium text-highlighted">{{ s.title }}</h3>
            <p class="mt-1.5 text-body-sm text-ink-700">{{ s.body }}</p>
          </div>
        </div>
      </UContainer>
    </section>

    <!-- Dark CTA band -->
    <section class="bg-obsidian">
      <UContainer class="py-20 text-center sm:py-24">
        <div class="mx-auto flex max-w-xl flex-col items-center">
          <UBadge class="mb-6 rounded-full bg-violet-900 px-3 py-1 text-eyebrow text-paper">
            For hosts
          </UBadge>
          <h2 class="text-heading-lg text-paper sm:text-display">
            Set up your event in a minute.
          </h2>
          <p class="mt-4 text-body text-ink-300">
            Create a roll, print the QR, and let your guests do the rest. Free to
            start — upgrade only if you need a bigger night.
          </p>
          <UButton
            to="/host"
            size="xl"
            class="mt-8 bg-paper text-highlighted hover:bg-ink-100"
            label="Start an event"
          />
        </div>
      </UContainer>
    </section>

    <!-- Footer -->
    <footer class="border-t border-default">
      <UContainer class="flex flex-col items-center justify-between gap-3 py-8 text-body-sm text-muted sm:flex-row">
        <span class="font-bold text-highlighted">🧺 Picknic</span>
        <span>© {{ new Date().getFullYear() }} Picknic. One roll, revealed at the end.</span>
      </UContainer>
    </footer>
  </div>
</template>
