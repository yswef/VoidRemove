# FAQ

## Is VoidRemove free and open source?

Yes. MIT licensed, with no ads and no bundled software. The Action Center only shows internal
recommendations derived from your own system.

## Does it work offline?

Fully. The only optional network features are an update check and a VirusTotal hash lookup,
both disabled by default.

## Is it safe? Will it delete something important?

Nothing is deleted without your confirmation. Risky operations create a backup or restore
point first, leftovers go to a restorable quarantine, and a permanent exclusion list plus a
built-in floor of system locations are never touched. Use **Dry run** to preview any operation.

## What do the safety levels mean?

- **Safe** — clearly belongs to the removed program (product code, install folder, exact name).
- **Moderate** — very likely related, removed only with your agreement.
- **Advanced** — shared or system-adjacent; never pre-selected.

## WPF or WinUI — which should I choose?

Both behave identically. WPF is the broadly compatible classic; WinUI 3 gives the latest
Fluent look on Windows 10/11. The installer lets you pick one or both.

## Where is my data stored?

Settings and logs live under `%ProgramData%\VoidRemove`; a mirror of settings is kept at
`HKLM\Software\VoidRemove`. Quarantine, backups and history are under the same root.

## Can I undo a reset or an uninstall cleanup?

Yes. Resets keep a ZIP backup you can restore; uninstalls quarantine leftovers and keep a
`.reg` backup of removed registry keys.
