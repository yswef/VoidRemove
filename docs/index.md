# VoidRemove

**Remove programs completely — down to the last trace.**

VoidRemove is a free, open-source (MIT), offline-first uninstaller for Windows 10/11. It runs
the program's own uninstaller, then performs a deep, safety-graded sweep for leftovers:
folders, registry keys, shortcuts, services, tasks and more — with confirmation, registry
exports and quarantine where supported. System Restore-point creation is not wired in yet.

## Highlights

- **Programs** — full list from HKLM/HKCU (incl. WOW6432Node) with x86/x64/User tags,
  bundleware detection, and one-click removal.
- **Total Uninstall** — per-key `.reg` backup, quarantine, three safety levels, dry run and
  a permanent never-delete list. The restore-point option is not yet connected.
- **Reset App** — data-only reset with three-layer discovery (profiles, auto-discovery,
  Install Monitor), ZIP backup and restore.
- **Software Health** — leftovers, redundant files, permissions, notifications, uninstall
  issues, installation files and local malware hints. VirusTotal is not wired to the scanner yet.
- **Install Monitor** — automatic (WMI) and manual before/after snapshots of installs.
- **Browser Extensions**, **Windows Apps**, **Action Center** and a full **Toolbox**.

## Offline first

The current app does not send telemetry and works without network access. The update-check and
VirusTotal settings are not wired to online services yet, so enabling them currently sends no
update request or file hash. External links open only after an explicit user action.

## Get it

Download `VoidRemove-Setup.exe` from the
[releases page](https://github.com/yswef/VoidRemove/releases), or build it yourself — see
[Developers](developers.md).

## License

MIT. See [LICENSE](https://github.com/yswef/VoidRemove/blob/main/LICENSE).
