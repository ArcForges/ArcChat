# NC00 NextChat Public Files Disposition

Source: `C:\MyFile\DevAll\QmlSharp\NextChat\public` at commit `89b8f26ff8f03a0c5b98fc3026d980721495227e`.

Completeness check: all 15 top-level files in `public/` are listed below. There is no nested Next.js build-output directory in this checkout.

| Source file | Bytes | Decision | Target / reason | Owner |
| --- | ---: | --- | --- | --- |
| `public/prompts.json` | 205046 | Vendor data | `apps/desktop/ArcChat.Desktop/Resources/Seed/Prompts.json`; 164 `en`, 121 `cn`, 123 `tw` prompt tuples | NC07.03 |
| `public/plugins.json` | 631 | Vendor data | `Resources/Seed/Plugins.json`; 3 plugin catalog entries | NC07.04 |
| `public/masks.json` | 65732 | Vendor data | `Resources/Seed/Masks/Catalog.json`; 3 mask catalog groups | NC07.01 |
| `public/android-chrome-192x192.png` | 16447 | Drop/replace | browser PWA icon; Velopack/Avalonia icons are generated from approved desktop icon sources | NC16 |
| `public/android-chrome-512x512.png` | 66831 | Drop/replace | browser PWA icon; not desktop runtime asset | NC16 |
| `public/apple-touch-icon.png` | 12762 | Drop/replace | browser touch icon; not desktop runtime asset | NC16 |
| `public/favicon-16x16.png` | 719 | Drop/replace | browser favicon; Velopack icon plan replaces it | NC16 |
| `public/favicon-32x32.png` | 1596 | Drop/replace | browser favicon; Velopack icon plan replaces it | NC16 |
| `public/favicon.ico` | 15406 | Drop/replace | browser favicon; desktop app icon handled by Avalonia/Velopack resources | NC16 |
| `public/macos.png` | 59228 | Drop/replace | web marketing/platform image; not used by desktop MVP | NC16 |
| `public/audio-processor.js` | 1218 | Drop | browser audio worklet; realtime audio is implemented with .NET platform audio services | NC11 |
| `public/serviceWorker.js` | 2110 | Drop | browser service worker; no web frontend | NC01 forbidden-module tests |
| `public/serviceWorkerRegister.js` | 1109 | Drop | browser service worker registration; no web frontend | NC01 forbidden-module tests |
| `public/robots.txt` | 76 | Drop | browser/search metadata; no web frontend | NC01 forbidden-module tests |
| `public/site.webmanifest` | 414 | Drop | PWA manifest; no web frontend | NC16 |
