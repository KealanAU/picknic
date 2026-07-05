// Upload orchestration: drives the 3-step SAS handshake and exposes progress.
// Takes raw bytes, so it's decoupled from where they came from (camera, library,
// a test fixture). Compose with useCamera in a screen.
import { readonly, ref } from 'vue';
import { completeUpload, createUpload, putBlob } from '../api/photos';

export type UploadStage =
  | 'idle'
  | 'requesting'  // minting the SAS target
  | 'uploading'   // PUT to blob storage
  | 'finalizing'  // registering the blob + caption
  | 'done'
  | 'error';

export interface UploadResult {
  blobPath: string;
  photoId?: string;
}

export function usePhotoUpload(eventId: string, guestToken: string) {
  const stage = ref<UploadStage>('idle');
  const error = ref<string | null>(null);

  async function upload(
    bytes: ArrayBuffer,
    caption: string | null = null,
    mime = 'image/jpeg',
  ): Promise<UploadResult> {
    error.value = null;
    try {
      stage.value = 'requesting';
      const target = await createUpload(eventId, guestToken);

      stage.value = 'uploading';
      await putBlob(target.uploadUrl, bytes, mime);

      stage.value = 'finalizing';
      const res = await completeUpload(eventId, target.blobPath, caption, guestToken);

      stage.value = 'done';
      return { blobPath: target.blobPath, photoId: res.id };
    } catch (e) {
      stage.value = 'error';
      error.value = e instanceof Error ? e.message : String(e);
      throw e;
    }
  }

  function reset(): void {
    stage.value = 'idle';
    error.value = null;
  }

  return { stage: readonly(stage), error: readonly(error), upload, reset };
}
