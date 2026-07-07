<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useAuth } from './composables/useAuth';
import { useGuest } from './composables/useGuest';
import AppBrand from './components/AppBrand.vue';
import GuestSessionCard from './components/GuestSessionCard.vue';
import HostRoll from './components/host/HostRoll.vue';
import OnboardingTray from './components/OnboardingTray.vue';
import { t } from './theme/tokens';

const { user, status, isAuthenticated, logout } = useAuth();
const { session, isJoined, isReady: guestReady, leave } = useGuest();

const appState = computed<'loading' | 'host' | 'guest' | 'login'>(() => {
  const authReady = status.value === 'authenticated' || status.value === 'unauthenticated';
  if (!authReady || !guestReady.value) return 'loading';
  if (isAuthenticated.value) return 'host';
  if (isJoined.value) return 'guest';
  return 'login';
});

onMounted(() => {
  useAuth().initialize();
  useGuest().initialize();
});
</script>

<template>
  <view :style="{ width: '100%', minHeight: '100vh', backgroundColor: t.color.cream }">
    <view
      v-if="appState === 'loading'"
      :style="{
        minHeight: '100vh',
        backgroundColor: t.color.cream,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '32px',
        gap: '24px',
      }"
    >
      <AppBrand />

      <text
        :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.muted }"
      >
        Loading…
      </text>
    </view>

    <view
      v-else-if="appState === 'host'"
      :style="{
        height: '100vh',
        backgroundColor: t.color.cream,
        display: 'flex',
        flexDirection: 'column',
      }"
    >
      <HostRoll :user="user" :style="{ width: '100%', height: '100%' }" @logout="logout" />
    </view>

    <view
      v-else-if="appState === 'guest'"
      :style="{
        minHeight: '100vh',
        backgroundColor: t.color.cream,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '32px',
      }"
    >
      <GuestSessionCard :session="session" @leave="leave" />
    </view>

    <view
      v-else-if="appState === 'login'"
      :style="{
        minHeight: '100vh',
        backgroundColor: t.color.cream,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '32px',
        gap: '43px',
      }"
    >
      <AppBrand />

      <OnboardingTray />
    </view>
  </view>
</template>
