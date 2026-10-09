# Developers

## Architecture

```
src/VoidRemove.Shared   identity, localization, theming, shared view models
src/VoidRemove.Core     ALL system logic (no UI): discovery, engine, scanning, backup
src/VoidRemove.UI.Wpf   WPF front end (binds to Core through interfaces)
src/VoidRemove.UI.WinUI WinUI 3 front end (same Core services and view models)
tests/                  xUnit coverage for the critical, pure logic
tools/                  icon + identity + offline verification scripts
data/                   editable, original data (profiles, stubborn, bundleware)
```

The rule that keeps the two front ends identical: **UIs never touch the OS directly.** They
resolve services from dependency injection and bind to view models; every registry/process/file
operation lives in `VoidRemove.Core` behind an interface (`IProgramReader`, `IResidualScanner`,
`ISnapshotProvider`, …) with a Windows implementation and a safe off-Windows fallback.

Identity is centralized in `AppInfo`; `tools/sync_appinfo.py` mirrors it to
`installer/appinfo.ini` and `docs/appinfo.json` so one edit renames everything.

## Build & test

```
dotnet build VoidRemove.sln -c Release
dotnet test tests/VoidRemove.Core.Tests
python tools/verify_repo.py      # offline consistency checks run in CI too
```

## Adding a program data profile

Create `data/profiles/<id>.json`:

```
{
  "schema": 1,
  "id": "my-app",
  "displayName": "My App",
  "matchNames": ["my app", "myapp"],
  "locations": [
    { "kind": "Folder",   "path": "%AppData%\\MyApp",      "optional": true },
    { "kind": "Registry", "path": "HKEY_CURRENT_USER\\Software\\MyApp", "optional": true }
  ]
}
```

Tokens like `%AppData%`, `%LocalAppData%`, `%ProgramData%` and `%UserProfile%` are expanded at
runtime. Profiles are original definitions; do not import third-party databases.

## Translating strings

1. Add the key to `src/VoidRemove.Shared/Resources/strings.en.json`.
2. Add the same key to `strings.ar.json`.
3. Add the constant to `LocalizationKeys` (the verifier enforces en/ar parity and coverage).

The layout stays left-to-right in every language; only text direction within a string changes.

## Release

Push a tag `v*`; `.github/workflows/release.yml` builds both UIs as single-file executables,
builds `VoidRemove-Setup.exe`, publishes SHA-256 sums and creates the GitHub release.
