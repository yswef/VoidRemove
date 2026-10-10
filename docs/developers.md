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

Release by pushing a stable version tag such as `v0.1.1`, or dispatch **Release** manually
from the default branch with a new tag. The workflow checks that the tag matches
`VersionPrefix`, publishes the portable executables, builds `VoidRemove-Setup.exe`, computes
SHA-256 sums, then creates the GitHub release. A manual run refuses existing tags and creates
the new tag at the default-branch commit it builds; rerun a failed Actions run to retry an
existing tag. If code changes after a tag is created, publish a new version instead of
silently building newer source under the old tag. The workflow does not create a bot branch.

Dependabot branches are separate: they contain proposed dependency updates for review and
CI. Building locally, in CI, or from a release tag does not require Dependabot or create a
branch. Non-breaking NuGet updates and all GitHub Actions updates are grouped to reduce
bot PR noise; major NuGet updates remain separate for safer review.
