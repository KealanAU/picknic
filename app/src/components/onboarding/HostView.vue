<script setup lang="ts">
import { computed, ref } from 'vue';
import { VyButton, VyForm, VyFormField, VyIcon, VyInput, VyTrayView } from '@vyui/kit';
import { useAuth } from '../../composables/useAuth';
import { sanitizeEmail } from '../../api/sanitize';
import RememberMeToggle from '../RememberMeToggle.vue';
import { backButtonStyle, errStyle, headerStyle, primaryActionStyle, subStyle, titleStyle } from './styles';

defineEmits<{
  back: [];
}>();

const { login, register, error: authError, isBusy: authBusy } = useAuth();

const mode = ref<'login' | 'register'>('login');
const email = ref('');
const password = ref('');
const rememberMe = ref(true);
const showPassword = ref(false);

const emailInput = computed({
  get: () => email.value,
  set: (value: string) => {
    email.value = sanitizeEmail(value);
  },
});

const hostCta = computed(() => (mode.value === 'login' ? 'Log in' : 'Sign up'));
const hostTitle = computed(() => (mode.value === 'login' ? 'Welcome back' : 'Create your host account'));
const canHostSubmit = computed(
  () => !!email.value && password.value.length >= 6 && !authBusy.value,
);

async function submitHost() {
  if (!canHostSubmit.value) return;
  try {
    if (mode.value === 'login') await login(email.value, password.value, rememberMe.value);
    else await register(email.value, password.value, rememberMe.value);
  } catch {
    return;
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
      @tap="$emit('back')"
    >
      Back
    </VyButton>
    <view :style="headerStyle">
      <text :style="titleStyle">{{ hostTitle }}</text>
      <text :style="subStyle">Make the party, share the code, reveal the roll.</text>
    </view>

    <!-- No gap here: VyForm/VyFormField have fragment roots whose anchor nodes
         become real flex items on native and collect phantom gaps; space with
         margins on the field roots instead. -->
    <VyForm class="flex flex-col items-stretch w-full">
      <VyFormField label="Email">
        <VyInput
          v-model="emailInput"
          type="email"
          size="lg"
          autocomplete="email"
          leading-icon="streamline:envelope-letter-front"
          placeholder="you@example.com"
        />
      </VyFormField>
      <VyFormField class="mt-2" label="Password" :hint="mode === 'register' ? 'At least 6 characters' : undefined">
        <VyInput
          v-model="password"
          :type="showPassword ? 'text' : 'password'"
          size="lg"
          :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
          placeholder="••••••••"
        >
          <template #trailing="{ iconColor }">
            <view @tap="showPassword = !showPassword" :style="{ padding: '4px' }">
              <VyIcon :name="showPassword ? 'streamline:view-eye-off' : 'streamline:view-eye-1'" :style="{ color: iconColor }" />
            </view>
          </template>
        </VyInput>
      </VyFormField>
      <RememberMeToggle v-model="rememberMe" size="sm" class="mt-2" />
    </VyForm>

    <text v-if="authError" :style="errStyle">{{ authError }}</text>

    <VyButton
      color="primary"
      size="xl"
      block
      :loading="authBusy"
      :disabled="!canHostSubmit"
      :style="primaryActionStyle"
      @tap="submitHost"
    >
      {{ hostCta }}
    </VyButton>
    <VyButton
      variant="ghost"
      size="lg"
      block
      :style="{ marginTop: '2px' }"
      @tap="mode = mode === 'login' ? 'register' : 'login'"
    >
      {{ mode === 'login' ? 'Need an account? Sign up' : 'Have an account? Log in' }}
    </VyButton>
  </VyTrayView>
</template>
