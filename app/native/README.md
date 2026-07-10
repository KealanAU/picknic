# Native camera modules

Lynx ships no camera element or module. Capture is a **custom native module** the
host app registers, mirroring `NativeLocalStorageModule` (see `src/api/storage.ts`).
These files are the host-side implementations of the boundary defined in
`src/native/camera.ts` — they are **not** compiled by rspeedy; they belong in the
iOS/Android host app that embeds the Lynx runtime.

## The JS ⇄ native contract

`src/native/camera.ts` now delegates to the `@kealanau/lynx-camera` package
(developed at `~/Documents/Code/lynx-camera`), which supports two native
module shapes:

1. **Legacy (these files):**

   ```
   NativeModules.CameraModule.capture({ quality: number, facing: "back"|"front" }, callback)
   ```

   `callback` receives a plain object:

   | Field    | Type   | When       |
   | -------- | ------ | ---------- |
   | `base64` | string | success — JPEG bytes, base64 (no data: prefix) |
   | `width`  | int    | success    |
   | `height` | int    | success    |
   | `mime`   | string | success — `"image/jpeg"` |
   | `error`  | string | failure — contains `"cancel"` if the user backed out |

   The package treats this shape as a deprecated fallback (slated for removal
   before its `0.2.0`), so the existing host modules keep working for now.

2. **Current (the package's own module):** compile
   `node_modules/@kealanau/lynx-camera/ios/LynxCameraModule.swift` into the
   host instead — same system-camera capture plus permissions, device
   enumeration, and the version-checked install status
   (`getCameraInstallStatus()`), which the app now shows in the camera screen
   when capture is unavailable. Prefer this for new host builds; see
   `node_modules/@kealanau/lynx-camera/docs/ios-install.md`.

`src/native/camera.ts` decodes `base64` to an `ArrayBuffer` and hands the rest of
the app a plain `CapturedPhoto`. Nothing above the boundary sees `NativeModules`.

## Registration

**iOS** (`CameraModule.swift`) — at LynxView bootstrap:
```swift
let config = LynxConfig(provider: templateProvider)
config.register(CameraModule.self)
```

**Android** (`CameraModule.kt`) — at host startup:
```kotlin
LynxEnv.inst().registerModule("CameraModule", CameraModule::class.java)
```

Then wire the `CameraLauncher` stub to a real CameraX / `ACTION_IMAGE_CAPTURE`
flow (Android) — the iOS side already uses `UIImagePickerController`.

Add permissions: iOS `NSCameraUsageDescription`; Android `<uses-feature camera>`
+ runtime `CAMERA` permission.

## Bundled fonts

The Lynx bundle references the Picknic display face as `local("Comico")` in
`src/style.css`. Do not load the `.ttf` from shared CSS with `url(...)` for the
native bundle: Rspeedy rewrites that to a `webpack:///static/font/...` URL, and
native Lynx cannot fetch that scheme.

The host app must bundle and register the font instead:

- iOS: add `ios/Resources/Fonts/Comico-Regular.ttf` to the app target's copied
  resources and add `Fonts/Comico-Regular.ttf` under `UIAppFonts` in
  `Info.plist`.
- Android: copy `android/src/main/assets/fonts/Comico-Regular.ttf` into the host
  app's `src/main/assets/fonts/` folder.

The font's family/full/postscript names are `Comico`, `Comico Regular`, and
`Comico-Regular`, matching the `local(...)` fallbacks in `src/style.css`.

## Two levels of camera

1. **Capture a photo (implemented here).** System camera → JPEG → the app uploads
   it via `usePhotoUpload`. This is all Picknic needs: the film look is applied
   server-side at reveal (`api/FilmProcessing`), so no on-device filtering.

2. **Live in-app preview + shutter (not here).** The preview is a native surface,
   so it can't be a `<view>`. Register a **custom LynxUI element** wrapping
   `AVCaptureVideoPreviewLayer` (iOS) / CameraX `PreviewView` (Android) to get a
   `<camera-preview>` element in the layout tree. Only needed if you want the
   shutter inside the app rather than the system camera UI.

## Why filters aren't here

Picknic's tagline is *"One roll. Revealed at the end."* — filtering doesn't need
to be real-time, so the film emulation (LUTs, grain, halation, vignette) runs
server-side in `api/FilmProcessing` when the roll develops. Clients stay thin and
new "rolls" ship without an app update.

## Film + instant-print in the app

- `GET /api/film/stocks` — ~23 popular stocks (Portra 160/400/800, Gold, Ektar,
  Superia, Pro 400H, Velvia, CineStill 800T/50D/400D, Tri-X, HP5, Delta 3200, …).
- `GET /api/film/prints` — instant-print frames (Polaroid, Instax Mini/Square/Wide).
- App side:
  - `composables/useFilmStyles.ts` — loads both catalogues + holds the selection.
  - `components/InstaxCard.vue` — renders a photo in a native print frame; use it
    in the reveal grid so developed photos look like instant prints.
  - `screens/FilmStyleScreen.vue` — the picker: choose stock + frame, capture a
    photo, preview the frame live.

The film *look* is baked server-side at reveal (`developPhoto` / auto-develop);
the print *frame* is both baked server-side (`PrintFramer`) and rendered natively
in-app by `InstaxCard`, so it's crisp at any size and the caption stays selectable.
