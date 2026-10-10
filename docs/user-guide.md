# User Guide

VoidRemove keeps every action behind a confirmation, and every risky action behind a backup.

## Programs

Lists everything registered in Add/Remove Programs (HKLM and HKCU, including 32-bit entries).
Use the search box (Ctrl+F), refresh with F5, and switch sub-tabs: **All**, **Bundleware**,
**Recently installed**, **Large**. Select a row and press **Uninstall** (or Delete) to start
removal after confirmation. This quick action uses the engine defaults; it does not create a
System Restore point.

## Total Uninstall

Select a program (use **Refresh** if needed), then press **Preview** to see the count of
leftovers matched at the current Moderate safety ceiling. **Uninstall** runs the program's own
uninstaller, backs up removed registry keys to `.reg`, and quarantines selected leftovers.
**Dry run** computes without touching anything. The visible restore-point checkbox is not
connected in this build; do not rely on it. A user-facing safety-level selector is also still
missing.

## Reset App

Resets an app's *data* without uninstalling it. Type the app name and press **Discover** to see
data found through profiles, automatic matching, and the Install Monitor. Choose **Dry run** to
preview, **Reset** to back up to a ZIP and clear, and **Restore last backup** to undo.

## Software Health

**Scan now** groups findings: uninstall leftovers, redundant files, permissions, disturbing
notifications, uninstall issues, installation files, and local malware hints. Each row shows its
size so you can clean the biggest wins first. The VirusTotal setting is not connected to the
scanner yet; the current build does not send hashes to VirusTotal.

## Install Monitor

**Auto monitor** watches for installer processes (WMI) and records what they add. Use
**Begin/End manual** around a drag-and-drop install. Recorded installs feed Reset App and the
Programs *Logged* view.

## Browser Extensions / Windows Apps

Review extensions across IE/Chrome/Edge/Brave/Firefox/Opera with a local trust score and policy
detection; remove individually or all suspicious ones. Windows Apps lists packaged apps with
Safe/Dependent/System tags — system packages are protected.

## Toolbox

- **File Shredder** — multi-pass overwrite; irreversible.
- **Stubborn Program Remover** — recipes from `data/stubborn.json`.
- **Windows Updates Uninstall** — remove updates via `wusa`.
- **Force Uninstall** — stops processes first, then uninstalls.

## Tips

- Global hotkey **Ctrl+Alt+U** brings up Programs from anywhere.
- Right-click an `.exe`/`.lnk`/`.msi` and choose *Uninstall with VoidRemove* (enable in Settings).
