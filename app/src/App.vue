<script setup lang="ts">
import { onMounted } from 'vue';
import { useAuth } from './composables/useAuth';
import { useGuest } from './composables/useGuest';
import HostRoll from './components/host/HostRoll.vue';
import OnboardingTray from './components/OnboardingTray.vue';
import { t } from './theme/tokens';

const { user, status, isAuthenticated, logout } = useAuth();
const { session, isJoined, leave } = useGuest();

onMounted(() => {
  useAuth().initialize();
  useGuest().initialize();
});
</script>

<template>
  <view :style="{ width: '100%', height: '100%', backgroundColor: t.color.cream }">
    <view
      :style="{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: isAuthenticated ? 'flex-start' : 'center',
        padding: '32px',
        gap: isAuthenticated ? '18px' : '43px',
      }"
    >
      <view :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '11px' }">
        <text
          :style="{
            fontFamily: t.font.display,
            fontSize: '62px',
            fontWeight: '400',
            lineHeight: '1.2',
            paddingTop: '8px',
            letterSpacing: t.tracking,
            color: t.color.blue,
          }"
        >
          Picknic
        </text>
        <text
          :style="{
            fontFamily: t.font.body,
            fontSize: '14px',
            fontWeight: '300',
            letterSpacing: t.tracking,
            textTransform: 'uppercase',
            color: t.color.blue,
          }"
        >
          One roll · revealed at the end
        </text>
      </view>

      <!-- Restoring a persisted session -->
      <text
        v-if="status === 'idle' || status === 'loading'"
        :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.muted }"
      >
        Loading…
      </text>

      <!-- Signed in as a host -->
      <HostRoll v-else-if="isAuthenticated" :user="user" :style="{ flex: 1, minHeight: 0 }" @logout="logout" />

      <!-- Joined an event as a guest -->
      <VyCard v-else-if="isJoined" :style="{ width: '100%' }">
        <text
          :style="{ fontFamily: t.font.display, fontSize: '28px', fontWeight: '400', letterSpacing: t.tracking, color: t.color.ink }"
        >
          You're on the roll
        </text>
        <text
          :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.muted, marginTop: '7px' }"
        >
          {{ session?.name }} · room {{ session?.code }}
        </text>
        <VyButton variant="soft" block :style="{ marginTop: '20px' }" @click="leave">
          Leave roll
        </VyButton>
      </VyCard>

      <!-- Signed out: the onboarding tray (below) is up over this canvas. -->
    </view>

    <!-- Gates the app until a host signs in or a guest joins. Self-manages its open state. -->
    <OnboardingTray />
  </view>
</template>
