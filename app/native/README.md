# Native camera modules

Lynx ships no camera element or module. Capture is a **custom native module**
the host app registers. The boundary is defined in `src/native/camera.ts`,
which delegates to the `@kealanau/lynx-camera` package (developed at
`~/Documents/Code/lynx-camera`).

## The JS ⇄ native contract

The host compiles the package's own module —
`node_modules/@kealanau/lynx-camera/ios/LynxCameraModule.swift` — which
provides system-camera capture plus permissions, device enumeration, and the
version-checked install status (`getCameraInstallStatus()`), shown in the
camera screen when capture is unavailable. See
`node_modules/@kealanau/lynx-camera/docs/ios-install.md`. (The package also
accepts a legacy `CameraModule.capture()` shape, deprecated and slated for
removal before its `0.2.0`; the legacy host files that implemented it were
deleted from this repo.)

`src/native/camera.ts` decodes `base64` to an `ArrayBuffer` and hands the rest of
the app a plain `CapturedPhoto`. Nothing above the boundary sees `NativeModules`.

## Registration

**iOS** — at LynxView bootstrap (see `ios-host/HostSources/ViewController.swift`):
```swift
let config = LynxConfig(provider: templateProvider)
config.register(LynxCameraModule.self)
```

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
- App side: `components/InstaxCard.vue` — renders a photo in a native print
  frame; use it in the reveal grid so developed photos look like instant prints.

The film *look* is baked server-side at reveal (`developPhoto` / auto-develop);
the print *frame* is both baked server-side (`PrintFramer`) and rendered natively
in-app by `InstaxCard`, so it's crisp at any size and the caption stays selectable.
