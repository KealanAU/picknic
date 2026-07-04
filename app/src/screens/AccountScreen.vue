<script setup lang="ts">
import { computed, ref } from 'vue';
import { useAuth } from '../composables/useAuth';
import { t } from '../theme/tokens';

const { login, register, error, isBusy } = useAuth();

const mode = ref<'login' | 'register'>('login');
const email = ref('');
const password = ref('');
const showPassword = ref(false);

const title = computed(() => (mode.value === 'login' ? 'Welcome back' : 'Create your account'));
const cta = computed(() => (mode.value === 'login' ? 'Log in' : 'Sign up'));
const canSubmit = computed(() => !!email.value && password.value.length >= 6 && !isBusy.value);

function toggle() {
  mode.value = mode.value === 'login' ? 'register' : 'login';
}

async function submit() {
  if (!canSubmit.value) return;
  try {
    if (mode.value === 'login') await login(email.value, password.value);
    else await register(email.value, password.value);
  } catch {
    // error is surfaced reactively via useAuth().error
  }
}
</script>

<template>
  <view :style="{ width: '100%' }">
    <VyCard :style="{ width: '100%' }">
      <view :style="{ display: 'flex', flexDirection: 'column', gap: '7px', marginBottom: '20px' }">
        <text :style="{ fontFamily: t.font.display, fontSize: '38px', fontWeight: '300', lineHeight: '1', letterSpacing: t.tracking, color: t.color.ink }">{{ title }}</text>
        <text :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.muted }">Hosts create and manage Picknic events.</text>
      </view>

      <VyForm class="flex flex-col items-stretch w-full gap-4">
        <VyFormField label="Email">
          <VyInput
            v-model="email"
            type="email"
            size="xl"
            autocomplete="email"
            leading-icon="lucide:mail"
            placeholder="you@example.com"
          />
        </VyFormField>

        <VyFormField label="Password" :hint="mode === 'register' ? 'At least 6 characters' : undefined">
          <VyInput
            v-model="password"
            :type="showPassword ? 'text' : 'password'"
            size="xl"
            :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
            placeholder="••••••••"
          >
            <template #trailing="{ iconColor }">
              <view @tap="showPassword = !showPassword" :style="{ padding: '4px' }">
                <VyIcon
                  :name="showPassword ? 'lucide:eye-off' : 'lucide:eye'"
                  :style="{ color: iconColor }"
                />
              </view>
            </template>
          </VyInput>
        </VyFormField>
      </VyForm>

      <text v-if="error" :style="{ fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.danger, marginTop: '7px' }">
        {{ error }}
      </text>

      <VyButton
        color="primary"
        size="xl"
        block
        :loading="isBusy"
        :disabled="!canSubmit"
        :style="{ marginTop: '16px' }"
        @click="submit"
      >
        {{ cta }}
      </VyButton>

      <VyButton variant="ghost" size="lg" block :style="{ marginTop: '6px' }" @click="toggle">
        {{ mode === 'login' ? 'Need an account? Sign up' : 'Have an account? Log in' }}
      </VyButton>
    </VyCard>
  </view>
</template>
