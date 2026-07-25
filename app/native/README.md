# Native camera modules

Lynx ships no camera element or module. Capture is a **custom native module**
the host app registers. The boundary is defined in `src/native/camera.ts`,
which delegates to the `@vyui/camera` package (developed at
`~/Documents/Code/chimera-camera`).

## The JS ⇄ native contract

The host compiles the package's own natives —
`node_modules/@vyui/camera/ios/` — which provide system-camera capture,
the `<camera-view>` live preview element, plus permissions, device enumeration,
and the version-checked install status (`getCameraInstallStatus()`), shown in
the camera screen when capture is unavailable. See
`node_modules/@vyui/camera/INSTALLATION.md`.

`src/native/camera.ts` decodes `base64` to an `ArrayBuffer` and hands the rest of
the app a plain `CapturedPhoto`. Nothing above the boundary sees `NativeModules`.

## Registration

**iOS** — at LynxView bootstrap (see `ios-host/HostSources/ViewController.swift`):
```swift
let config = LynxConfig(provider: templateProvider)
config.register(ChimeraCameraModule.self)
```
(Hosts that use Lynx's global config can call `ChimeraCamera.register()`
instead; this one builds its own per-view config, which that helper misses.)

**Android** — no host exists yet; when one does, compile the package's Android
module and register it at startup, and add `<uses-feature camera>` + runtime
`CAMERA` permission. iOS needs `NSCameraUsageDescription`.

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

1. **Live in-app preview + shutter (the default).** `<camera-view>` — the
   package's LynxUI element wrapping `AVCaptureVideoPreviewLayer` (iOS) /
   CameraX `PreviewView` (Android). It self-registers when the natives are
   compiled in; `captureFromView()` fires its shutter.

2. **System camera sheet (fallback).** `capturePhoto()` → JPEG → the app uploads
   it via `usePhotoUpload`. Kept for the Live toggle and hosts without the
   element. Either way the film look is applied server-side at reveal
   (`api/FilmProcessing`), so no on-device filtering.

## Why filters aren't here

Picknic's tagline is *"One roll. Revealed at the end."* — filtering doesn't need
to be real-time, so the film emulation (LUTs, grain, halation, vignette) runs
server-side in `api/FilmProcessing` when the roll develops. Clients stay thin and
new "rolls" ship without an app update.

## Film + instant-print in the app

- `GET /api/film/stocks` — ~23 popular stocks (Portra 160/400/800, Gold, Ektar,
  Superia, Pro 400H, Velvia, CineStill 800T/50D/400D, Tri-X, HP5, Delta 3200, …).
- `GET /api/film/prints` — instant-print frames (Polaroid, Instax Mini/Square/Wide).
- App side: `components/InstaxCard.vue` — renders a photo in a native print
  frame; use it in the reveal grid so developed photos look like instant prints.

The film *look* is baked server-side at reveal (`developPhoto` / auto-develop);
the print *frame* is both baked server-side (`PrintFramer`) and rendered natively
in-app by `InstaxCard`, so it's crisp at any size and the caption stays selectable.
