<script setup lang="ts">
import { computed, ref } from 'vue';
import { VyButton, VyForm, VyFormField, VyIcon, VyInput, VyTrayView } from '@vyui/kit';
import { useAuth } from '../../composables/useAuth';
import { backButtonStyle, errStyle, headerStyle, primaryActionStyle, subStyle, titleStyle } from './styles';

defineEmits<{
  back: [];
}>();

const { login, register, error: authError, isBusy: authBusy } = useAuth();

const mode = ref<'login' | 'register'>('login');
const email = ref('');
const password = ref('');
const showPassword = ref(false);

const emailInput = computed({
  get: () => email.value,
  set: (value: string) => {
    email.value = value.trim().toLowerCase();
  },
});

const hostCta = computed(() => (mode.value === 'login' ? 'Log in' : 'Sign up'));
const hostTitle = computed(() => (mode.value === 'login' ? 'Host sign in' : 'Create host account'));
const canHostSubmit = computed(
  () => !!email.value && password.value.length >= 6 && !authBusy.value,
);

async function submitHost() {
  if (!canHostSubmit.value) return;
  try {
    if (mode.value === 'login') await login(email.value, password.value);
    else await register(email.value, password.value);
  } catch {
    // surfaced via authError
  }
}
</script>

<template>
  <VyTrayView id="host">
    <VyButton
      variant="ghost"
      size="sm"
      leading-icon="lucide:arrow-left"
      :style="backButtonStyle"
      @click="$emit('back')"
    >
      Back
    </VyButton>
    <view :style="headerStyle">
      <text :style="titleStyle">{{ hostTitle }}</text>
      <text :style="subStyle">Hosts create and manage Picknic events.</text>
    </view>

    <VyForm class="flex flex-col items-stretch w-full gap-2">
      <VyFormField label="Email">
        <VyInput
          v-model="emailInput"
          type="email"
          size="lg"
          autocomplete="email"
          leading-icon="lucide:mail"
          placeholder="you@example.com"
        />
      </VyFormField>
      <VyFormField label="Password" :hint="mode === 'register' ? 'At least 6 characters' : undefined">
        <VyInput
          v-model="password"
          :type="showPassword ? 'text' : 'password'"
          size="lg"
          :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
          placeholder="••••••••"
        >
          <template #trailing="{ iconColor }">
            <view @tap="showPassword = !showPassword" :style="{ padding: '4px' }">
              <VyIcon :name="showPassword ? 'lucide:eye-off' : 'lucide:eye'" :style="{ color: iconColor }" />
            </view>
          </template>
        </VyInput>
      </VyFormField>
    </VyForm>

    <text v-if="authError" :style="errStyle">{{ authError }}</text>

    <VyButton
      color="primary"
      size="xl"
      block
      :loading="authBusy"
      :disabled="!canHostSubmit"
      :style="primaryActionStyle"
      @click="submitHost"
    >
      {{ hostCta }}
    </VyButton>
    <VyButton
      variant="ghost"
      size="lg"
      block
      :style="{ marginTop: '2px' }"
      @click="mode = mode === 'login' ? 'register' : 'login'"
    >
      {{ mode === 'login' ? 'Need an account? Sign up' : 'Have an account? Log in' }}
    </VyButton>
  </VyTrayView>
</template>
