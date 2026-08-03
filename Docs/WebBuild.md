# Web builds

Install **WebGL Build Support** for Unity `6000.4.10f1` from Unity Hub before using these commands. The setup machine used for this pass did not have that editor module installed, so a full Web player build could not be produced here.

## Build configurations

Run either:

- `Tools > OneStep > Build Web > Development` — development build, embedded debug symbols, full exceptions
- `Tools > OneStep > Build Web > Production` — optimized build, symbols off, explicitly-thrown exceptions only

Configuration assets live in `Assets/_Project/Settings/Build`. Both builds use IL2CPP, Brotli compression with decompression fallback, data caching, a 128 MiB initial/512 MiB maximum growing heap, no Web threads, hashed filenames, and the responsive `OneStepResponsive` template.

## Local testing

Web builds must be served over HTTP; opening `index.html` directly is unsupported. Use Unity Build & Run or any local static server rooted at the output directory. Test with browser developer tools and real iPhone/Android hardware on the same network. A production-like HTTPS host is required for the most representative fullscreen, storage, and WebSocket behavior.

Test at minimum:

- portrait phone sizes, tablets, desktop portrait and landscape windows
- rotation, browser bar collapse/expansion, `visualViewport` resize, fullscreen enter/exit
- safe-area/notch presets and real devices
- touch drag/tap signals, virtual d-pad, mouse, WASD/arrows, Space wait action
- background tab suspension and returning to an active Relay session
- low-memory reloads and storage clearing

The HTML template constrains the canvas to 9:16 and letterboxes outside it. Unity independently fixes the camera viewport, so unusually wide screens do not reveal additional world space. Safe-area UI remains inside Unity's `Screen.safeArea`; overlay and transition layers intentionally cover the full canvas.

## itch.io packaging

1. Build Production to `Builds/Web/Production`.
2. Zip the *contents* of that directory so `index.html`, `Build`, and `TemplateData` are at the archive root.
3. Create an itch.io HTML project, upload the zip, and mark it as playable in browser.
4. Enable mobile-friendly support and use a portrait viewport. Allow fullscreen.
5. The decompression fallback makes the build tolerant of hosts without correct `Content-Encoding` headers, at a download/runtime cost. If hosting headers are fully controlled later, disable fallback and validate Brotli responses.

## Browser risks

- iOS and Android browsers can kill a tab under memory pressure; no Web setting can prevent that.
- Background tabs are throttled or suspended, so a listening duel host can disconnect even with `runInBackground` enabled.
- Browser storage can be evicted or manually cleared; treat local saves and anonymous identity tokens as recoverable caches, not durable account ownership.
- WebGL lacks UDP and Relay DTLS/QoS behavior; this project uses WSS, which adds TCP/WebSocket latency and head-of-line blocking.
- Device pixel ratio is capped at 2 in the template to control render cost and memory on high-density phones.
