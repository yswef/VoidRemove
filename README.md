<p align="center">
  <img src="assets/logo.svg" alt="VoidRemove logo" width="128" height="128" />
</p>

<h1 align="center">VoidRemove</h1>

<p align="center">
  <strong>Remove Windows programs completely — down to the last trace.</strong><br />
  <em>أزِل برامج ويندوز بشكل تام — حتى آخر أثر.</em>
</p>

<p align="center">
  <a href="https://github.com/yswef/VoidRemove/actions/workflows/ci.yml"><img src="https://github.com/yswef/VoidRemove/actions/workflows/ci.yml/badge.svg" alt="Build &amp; test" /></a>
  <a href="https://github.com/yswef/VoidRemove/releases/latest"><img src="https://img.shields.io/github/v/release/yswef/VoidRemove?label=release" alt="Latest release" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-2EE6A8" alt="License: MIT" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6" alt="Platform: Windows 10/11" />
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4" alt=".NET 8" />
  <img src="https://img.shields.io/badge/offline--first-yes-2EE6A8" alt="Offline first" />
</p>

<p align="center">
  <a href="#english">English</a> · <a href="#العربية">العربية</a> · <a href="#development--التطوير">Development</a>
</p>

---

## English

**VoidRemove** is a free, open-source, offline-first uninstaller for Windows 10/11.
It runs the vendor uninstaller, then scans for what the uninstaller left behind —
folders, registry keys, services, scheduled tasks, firewall rules, COM/shell
registrations — classifies every leftover by how safe it is to delete, backs it up,
and lets you decide.

The current app has no telemetry, advertising, or account requirement. Note that the
update-check and VirusTotal settings are not wired to online services yet; turning them on
does not currently send update requests or file hashes. External links open only when you
explicitly choose one in the app.

### Sections

| Section | What it does |
| --- | --- |
| **Programs** | Every installed program from `HKLM`/`HKCU` (including `WOW6432Node`), with tabs for Bundleware, Logged, Recently installed, Large and Infrequently used. |
| **Reset app** | Wipes a program's data (AppData, ProgramData, settings, cache, scheduled tasks) so it behaves like a fresh install — without uninstalling it. |
| **Software Health** | Uninstall leftovers, redundant files, app permissions, disturbing notifications, broken uninstall entries, old installers and a fully local malicious-software scan. |
| **Install Monitor** | Records what an installer changed (files, registry, services, tasks) so it can be undone exactly later. |
| **Browser Extensions** | Chrome, Edge, Firefox, Brave, Opera and legacy BHOs, with a locally computed trust tag. |
| **Windows Apps** | Store/UWP/MSIX packages, with their dependencies shown before removal. |
| **Action Center** | Local, ad-free recommendations: what is worth cleaning right now. |
| **Toolbox** | Easy Uninstall, Force Uninstall, Stubborn Program Remover, File Shredder, Windows Updates uninstall. |

### Safety model

1. Nothing is deleted without an explicit confirmation.
2. Every registry key that is about to be removed is exported to a `.reg` backup first.
3. Every file in a residual path is moved into `%ProgramData%\VoidRemove\Quarantine` before it disappears.
4. Leftovers are graded **Safe / Moderate / Advanced** — advanced items are never pre-selected.
5. A permanent, hard-coded protection list refuses to touch critical system folders, critical registry hives and VoidRemove's own data.
6. A **Dry run** mode previews any operation without changing the system.
7. System Restore-point creation is not connected in the current build; do not rely on that checkbox.

### Install

Download `VoidRemove-Setup.exe` (or the portable `VoidRemove-Wpf-x64.exe` /
`VoidRemove-WinUI-x64.exe`) from the
[releases page](https://github.com/yswef/VoidRemove/releases/latest). The app needs
administrator rights — it removes machine-wide registrations.

### License

MIT — see [LICENSE](LICENSE). Contributions welcome: [CONTRIBUTING](CONTRIBUTING.md) ·
[Code of Conduct](CODE_OF_CONDUCT.md) · [Security policy](SECURITY.md).

---

## العربية

**VoidRemove** برنامج مجاني ومفتوح المصدر لإزالة البرامج من ويندوز 10/11 بشكل تام،
ويعمل بالكامل دون اتصال بالإنترنت. يشغّل مثبّت البرنامج الرسمي أولاً، ثم يفحص ما
تبقّى بعده — المجلدات ومفاتيح الريجستري والخدمات والمهام المجدولة وقواعد جدار
الحماية وتسجيلات COM — ويصنّف كل بقية حسب درجة أمان حذفها، ويأخذ نسخة احتياطية،
ثم يترك القرار لك.

لا يحتوي التطبيق الحالي على تتبّع أو إعلانات ولا يتطلب حسابًا. تنبيه: خياري فحص التحديثات
وVirusTotal غير موصولين بالخدمات الشبكية بعد؛ تفعيلهما لا يرسل حاليًا طلبات تحديث أو تجزئات
ملفات. لا تُفتح الروابط الخارجية إلا إذا اخترتها بنفسك داخل التطبيق.

### الأقسام

| القسم | الوظيفة |
| --- | --- |
| **البرامج** | كل البرامج المثبّتة من `HKLM` و`HKCU` (بما فيها `WOW6432Node`) مع تبويبات: البرامج المرفقة، المسجَّلة، المثبَّتة حديثاً، الكبيرة، وقليلة الاستخدام. |
| **تصفير البرنامج** | يحذف بيانات البرنامج فقط (AppData وProgramData والإعدادات والكاش والمهام المجدولة) ليعود كأنه ثُبّت للتو، دون إزالة البرنامج نفسه. |
| **صحة البرامج** | بقايا الحذف، الملفات الزائدة، صلاحيات التطبيقات، الإشعارات المزعجة، مدخلات الإزالة التالفة، ملفات التثبيت القديمة، وفحص محلي بالكامل للبرامج الضارة. |
| **مراقبة التثبيت** | يسجّل ما غيّره المثبّت (ملفات، ريجستري، خدمات، مهام) ليمكن التراجع عنه بدقة لاحقاً. |
| **إضافات المتصفحات** | كروم وإيدج وفايرفوكس وبريف وأوبرا وإضافات IE القديمة، مع وسم أمان محسوب محلياً. |
| **تطبيقات ويندوز** | حزم المتجر وUWP/MSIX مع عرض الاعتماديات قبل الحذف. |
| **مركز الإجراءات** | توصيات محلية بلا إعلانات: ما يستحق التنظيف الآن. |
| **صندوق الأدوات** | إزالة سهلة، إزالة قسرية، مُزيل البرامج العنيدة، إتلاف الملفات، وإزالة تحديثات ويندوز. |

### نموذج السلامة

1. لا يُحذف أي شيء دون تأكيد صريح.
2. كل مفتاح ريجستري سيُحذف يُصدَّر إلى نسخة `.reg` أولاً.
3. كل ملف داخل مسار البقايا يُنقل إلى `%ProgramData%\VoidRemove\Quarantine` قبل حذفه.
4. تُصنَّف البقايا إلى **آمن / متوسط / متقدم**، والعناصر المتقدمة لا تُحدَّد افتراضياً أبداً.
5. قائمة حماية ثابتة ترفض لمس مجلدات النظام الحرجة وأقسام الريجستري الحساسة وبيانات VoidRemove نفسه.
6. وضع **معاينة (Dry run)** يعرض أي عملية دون تعديل النظام.
7. خدمة نقاط استعادة النظام غير موصولة في النسخة الحالية؛ فلا تعتمد على خيارها.

### التثبيت

نزّل `VoidRemove-Setup.exe` (أو النسخة المحمولة `VoidRemove-Wpf-x64.exe` أو
`VoidRemove-WinUI-x64.exe`) من
[صفحة الإصدارات](https://github.com/yswef/VoidRemove/releases/latest). التطبيق يطلب
صلاحيات مدير لأنه يزيل تسجيلات على مستوى الجهاز.

### الترخيص

MIT — راجع [LICENSE](LICENSE).

> ملاحظة عن الاتجاه: واجهة التطبيق تبقى LTR دائماً حتى عند اختيار العربية؛ النصوص
> وحدها تُترجم، ولا يُقلب التخطيط ولا ترتيب الأزرار ولا الشريط الجانبي.

---

## Development · التطوير

### Repository layout · بنية المستودع

```
VoidRemove/
├── src/
│   ├── VoidRemove.Shared/     # AppInfo, localization (en/ar), themes, shared view models
│   ├── VoidRemove.Core/       # ALL system logic: no UI code in here
│   ├── VoidRemove.UI.Wpf/     # WPF (.NET 8) front end
│   └── VoidRemove.UI.WinUI/   # WinUI 3 (Windows App SDK) front end
├── tests/VoidRemove.Core.Tests/
├── data/                      # editable JSON: profiles, detection lists, recipes
├── assets/                    # logo.svg + generated PNG/ICO
├── tools/                     # build_icons.py, sync_appinfo.py, verify_repo.py
├── installer/                 # Inno Setup script
├── docs/                      # MkDocs Material site (GitHub Pages)
└── .github/                   # CI, release and docs workflows, templates
```

`VoidRemove.Shared` sits at the bottom: `Core` references it for identity, and both
front ends reference `Core` through interfaces, so WPF and WinUI behave identically.

### Requirements · المتطلبات

- Windows 10 1809+ / Windows 11 (for building and running)
- .NET SDK 8.0.x — see [global.json](global.json)
- Inno Setup 6 (only to build the installer)
- Python 3.9+ with Pillow (only to regenerate the icons)

### Build & test · البناء والاختبار

```powershell
dotnet restore VoidRemove.sln
dotnet build   VoidRemove.sln -c Release
dotnet test    tests/VoidRemove.Core.Tests -c Release
```

Run the WPF front end:

```powershell
dotnet run --project src/VoidRemove.UI.Wpf -c Release
```

Publish the single-file self-contained executables:

```powershell
dotnet publish src/VoidRemove.UI.Wpf -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:AssemblyName=VoidRemove-Wpf-x64
```

### Single source of identity · مصدر الهوية الواحد

The product name is declared **once**, in
[`src/VoidRemove.Shared/AppInfo.cs`](src/VoidRemove.Shared/AppInfo.cs). Window
titles, the `HKLM\Software\VoidRemove` key, `%ProgramData%\VoidRemove`, the mutex,
the named pipe and the AUMID all derive from it. Run

```bash
python tools/sync_appinfo.py            # regenerate installer/appinfo.ini + docs/appinfo.json
python tools/sync_appinfo.py --check    # CI mode: fails if they are out of date
```

### Icon pipeline · خط إنتاج الأيقونة

`assets/logo.svg` and every raster size come from one script:

```bash
python -m venv .venv && .venv/bin/pip install pillow
python tools/build_icons.py             # rewrites logo.svg, assets/png/*.png, assets/icon.ico
```

### Status · الحالة

The repository contains the nine implementation milestones recorded in
[CHANGELOG.md](CHANGELOG.md). A successful source build is not the same as a published
release: downloadable executables appear only after the tagged Release workflow finishes
successfully. See [Releases](https://github.com/yswef/VoidRemove/releases) for what is
actually published; the feature list describes the intended/current code, not a guarantee
that every Windows-only operation has been manually validated on every Windows version.
