# Contributing to VoidRemove

Thanks for helping make removal safer and cleaner. This guide gets you productive quickly.

## Getting started

1. Fork and clone the repository.
2. Install the .NET 8 SDK and, for the UIs, develop on Windows 10/11.
3. `dotnet build VoidRemove.sln -c Release` and `dotnet test tests/VoidRemove.Core.Tests`.
4. Run `python tools/verify_repo.py` before pushing; it is also enforced in CI.

## Architecture rules (please keep them)

- **All system logic lives in `VoidRemove.Core`.** The WPF and WinUI front ends only bind to
  it through interfaces so they behave identically. Do not put registry/process/file logic in
  a UI project.
- **Shared presentation** (identity, localization, theming, shared view models) lives in
  `VoidRemove.Shared`.
- **Never delete without confirmation**, and always offer a backup/restore point for risky
  operations. Respect the permanent exclusion list.
- **Offline-first**: network is only for the optional update check and optional VirusTotal
  (SHA-256 only), both off by default.

## Adding or changing user-facing text

Every string goes in `src/VoidRemove.Shared/Resources/strings.en.json` **and**
`strings.ar.json`, then regenerate keys with `tools` (the verifier enforces en/ar parity and
`LocalizationKeys` coverage). The layout stays left-to-right even in Arabic.

## Adding a program data profile

Drop a JSON file under `data/profiles/` following the schema in
`docs/developers.md`. Profiles are original definitions; do not import third-party databases.

## Commit & PR style

- Small, focused PRs with a clear description (see the PR template).
- Keep the safety checklist in the template honest.
- Tests: add xUnit coverage for any critical/pure logic you touch.

## Licensing

By contributing you agree your work is licensed under the project's MIT license.
