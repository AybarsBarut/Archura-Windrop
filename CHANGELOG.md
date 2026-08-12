# Changelog

## 1.0.1 - 2026-08-12

- Fix installer firewall command quoting so IPP and mDNS rules are created and removed reliably.
- Keep the Apps & Features display name stable across versions for WinGet package correlation.
- Add manually maintained WinGet Community Repository manifests.

## 1.0.0 - 2026-08-11

- First public Windows release of Archura Windrop.
- Receive AirPrint jobs from iPhone, iPad, and macOS over the local network.
- Convert incoming PDFs to maximum-quality PNG or JPEG, or extract text-only documents.
- Prompt for the destination folder and output format when configured.
- Use `Documents\Windrop\Received` as the default receive folder.
- Support English, Turkish, German, Spanish, Russian, and Simplified Chinese.
- Improve tray-menu responsiveness by keeping it on a dedicated UI message loop.
- Add the branded application, taskbar, tray, and installer icon.
- Add a self-contained x64 Windows setup package with firewall configuration.
