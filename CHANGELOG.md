# Changelog

All notable changes to **VoidRemove** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Repository skeleton: `VoidRemove.sln` with `VoidRemove.Shared`, `VoidRemove.Core`,
  `VoidRemove.UI.Wpf`, `VoidRemove.UI.WinUI` and `VoidRemove.Core.Tests`.
- `AppInfo`: one central file that carries the product identity (name, registry key,
  data folder, mutex, pipe, AUMID, repository and docs URLs). `tools/sync_appinfo.py`
  mirrors it into `installer/appinfo.ini` and `docs/appinfo.json` so the installer and
  the docs never drift from the code.
- Original VoidRemove identity: `assets/logo.svg` plus generated PNG sizes and
  `assets/icon.ico`, produced by `tools/build_icons.py`.
- Localization layer (`en` / `ar`) backed by embedded JSON catalogs, with instant
  switching and a permanent LTR layout.
- Theme layer: Dark, Light, AMOLED, Neon and Glass palettes with a customizable
  accent color, exposed as UI-framework-neutral colors.
- Core infrastructure: application data paths under `%ProgramData%\VoidRemove`,
  JSON settings with an optional `HKLM\Software\VoidRemove` mirror, DPAPI-protected
  secrets, Serilog file logging, single-instance guard, and a `Result` type.
- A working WPF shell (custom title bar, sidebar navigation, Home dashboard) and the
  matching WinUI 3 shell.
- Tests for identity, paths, settings, localization and theming.
- **Phase 2** — removal engine: registry program reader (HKLM/HKCU × 64/32), UninstallString
  parser (MSI/MSIX/custom), safety-graded residual matcher, restorable quarantine, per-key
  `.reg` backup, JSON history, permanent exclusion list, dry run.
- **Phase 3** — Reset App (profiles + three-layer discovery + ZIP backup/restore) and the
  Install Monitor (WMI automatic + manual snapshots).
- **Phase 4** — Software Health categories, optional SHA-256-only VirusTotal, browser
  extension scanner with local trust scoring and policy detection, PowerShell-backed Windows
  Apps, and the Toolbox (shredder, stubborn remover, updates, force uninstall).
- **Phase 5** — full WPF section UI bound to Core view models.
- **Phase 6** — section view models moved to Core and shared by a matching WinUI 3 UI.
- **Phase 7** — Explorer context menu (own shell verbs, clean removal), global hotkey
  Ctrl+Alt+U, F5/Delete/Ctrl+F shortcuts and a tray icon.
- **Phase 8** — Inno Setup installer (admin, EN+AR, WPF/WinUI choice, clean uninstall),
  GitHub workflows (CI, release, docs) and community files.
- **Phase 9** — MkDocs Material site (English + Arabic) with per-section guides, FAQ and a
  developer page.

[Unreleased]: https://github.com/yswef/VoidRemove/commits/main
