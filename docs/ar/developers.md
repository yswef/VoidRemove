# للمطورين

## البنية

```
src/VoidRemove.Shared   الهوية، الترجمة، الثيمات، نماذج العرض المشتركة
src/VoidRemove.Core     كل منطق النظام (بدون واجهة): الاكتشاف، المحرك، المسح، النسخ
src/VoidRemove.UI.Wpf   واجهة WPF (تربط Core عبر الواجهات)
src/VoidRemove.UI.WinUI واجهة WinUI 3 (نفس خدمات Core ونماذج العرض)
tests/                  تغطية xUnit للمنطق الحرج النقي
tools/                  سكربتات الأيقونات والهوية والتحقق دون اتصال
data/                   بيانات أصلية قابلة للتحرير (profiles، stubborn، bundleware)
```

القاعدة التي تُبقي الواجهتين متطابقتين: **الواجهات لا تلمس نظام التشغيل مباشرة**؛ بل تحلّ
الخدمات من حقن الاعتمادات وتربط بنماذج العرض، وكل عملية ريجستري/عملية/ملف في `VoidRemove.Core`
خلف واجهة (`IProgramReader`، `IResidualScanner`، `ISnapshotProvider`…) بتنفيذ لويندوز وبديل آمن
خارجها.

الهوية مركزية في `AppInfo`؛ ويعكسها `tools/sync_appinfo.py` إلى `installer/appinfo.ini`
و`docs/appinfo.json` فتعديل واحد يغيّر الاسم في كل مكان.

## البناء والاختبار

```
dotnet build VoidRemove.sln -c Release
dotnet test tests/VoidRemove.Core.Tests
python tools/verify_repo.py
```

## إضافة ملف تعريف لبيانات برنامج

أنشئ `data/profiles/<id>.json`:

```
{
  "schema": 1,
  "id": "my-app",
  "displayName": "My App",
  "matchNames": ["my app", "myapp"],
  "locations": [
    { "kind": "Folder",   "path": "%AppData%\\MyApp", "optional": true },
    { "kind": "Registry", "path": "HKEY_CURRENT_USER\\Software\\MyApp", "optional": true }
  ]
}
```

الرموز مثل `%AppData%` و`%LocalAppData%` و`%ProgramData%` و`%UserProfile%` تُوسَّع وقت
التشغيل. الملفات تعريفات أصلية؛ لا تستورد قواعد بيانات طرف ثالث.

## ترجمة النصوص

1. أضف المفتاح إلى `strings.en.json`.
2. أضف المفتاح نفسه إلى `strings.ar.json`.
3. أضف الثابت إلى `LocalizationKeys` (المدقّق يفرض التكافؤ والتغطية).

يبقى الاتجاه يسارًا-لليمين في كل اللغات.

## الإصدار

ادفع وسم `v*`؛ يبني `release.yml` الواجهتين كملفات مفردة، وينشئ `VoidRemove-Setup.exe` ومجاميع
SHA-256 وإصدار GitHub.
