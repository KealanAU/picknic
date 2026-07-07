// Capture concern only: wraps the native camera boundary in reactive state.
// Compose with usePhotoUpload.
import { readonly, ref } from 'vue';
import {
  capturePhoto,
  CameraCancelled,
  isCameraAvailable,
  type CaptureOptions,
  type CapturedPhoto,
} from '../native/camera';

export function useCamera() {
  const available = isCameraAvailable();
  const busy = ref(false);
  const error = ref<string | null>(null);
  const lastPhoto = ref<CapturedPhoto | null>(null);

  async function capture(options?: CaptureOptions): Promise<CapturedPhoto | null> {
    if (!available) {
      error.value = "Camera isn't available on this device.";
      return null;
    }
    busy.value = true;
    error.value = null;
    try {
      const photo = await capturePhoto(options);
      lastPhoto.value = photo;
      return photo;
    } catch (e) {
      if (e instanceof CameraCancelled) return null;
      error.value = e instanceof Error ? e.message : String(e);
      return null;
    } finally {
      busy.value = false;
    }
  }

  return {
    available,
    busy: readonly(busy),
    error: readonly(error),
    lastPhoto: readonly(lastPhoto),
    capture,
  };
}
