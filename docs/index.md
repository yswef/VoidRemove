# VoidRemove

**Remove programs completely — down to the last trace.**

VoidRemove is a free, open-source (MIT), offline-first uninstaller for Windows 10/11. It runs
the program's own uninstaller, then performs a deep, safety-graded sweep for leftovers:
folders, registry keys, shortcuts, services, tasks and more — always with confirmation and
always with a backup or restore point for risky operations.

## Highlights

- **Programs** — full list from HKLM/HKCU (incl. WOW6432Node) with x86/x64/User tags,
  bundleware detection, and one-click removal.
- **Total Uninstall** — restore point, per-key `.reg` backup, quarantine, three safety levels,
  dry run and a permanent never-delete list.
- **Reset App** — data-only reset with three-layer discovery (profiles, auto-discovery,
  Install Monitor), ZIP backup and restore.
- **Software Health** — leftovers, redundant files, permissions, notifications, uninstall
  issues, installation files and local malware hints (optional VirusTotal, SHA-256 only).
- **Install Monitor** — automatic (WMI) and manual before/after snapshots of installs.
- **Browser Extensions**, **Windows Apps**, **Action Center** and a full **Toolbox**.

## Offline first

VoidRemove works fully offline. The only network features are an **optional** update check and
an **optional** VirusTotal lookup that sends a SHA-256 hash only — both disabled by default.

## Get it

Download `VoidRemove-Setup.exe` from the
[releases page](https://github.com/yswef/VoidRemove/releases), or build it yourself — see
[Developers](developers.md).

## License

MIT. See [LICENSE](https://github.com/yswef/VoidRemove/blob/main/LICENSE).
