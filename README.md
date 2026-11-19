<div dir="rtl">

# 🔢 شمارش با عکس (PhotoCounter)

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-Android-512BD4)
![Android](https://img.shields.io/badge/Android-10%2B-3DDC84?logo=android&logoColor=white)

یک اپ اندروید ساده و دقیق برای **شمردن هر چیزی از روی عکس**. عکس می‌گیرید، روی هر مورد می‌زنید، و اپ برایش یک علامت شماره‌دار می‌گذارد. شمارهٔ آخر همان تعداد کل است.

<!-- اسکرین‌شات: عکس را در docs/screenshot.png بگذارید و این خط را فعال کنید
<img src="docs/screenshot.png" width="300" alt="اسکرین‌شات اپ">
-->

## 🤔 چرا ساخته شد؟

شمردنِ دستی و دانه‌به‌دانهٔ اقلام (مثلاً نان‌های داخل یک کیسهٔ پلاستیکی، قطعه‌ها یا جعبه‌ها) هم خسته‌کننده است و هم احتمال اشتباه و جاافتادن دارد. با این اپ هر مورد دقیقاً یک بار شمرده می‌شود و علامتش روی عکس می‌ماند؛ پس همیشه می‌توانید دوباره بررسی کنید.

## ✨ امکانات

- 📷 **گرفتن عکس** با دوربین یا 🖼 **انتخاب از گالری**
- 👆 **شمارش با لمس**: با هر لمس یک علامت شماره‌دار (۱، ۲، ۳، …) روی عکس می‌افتد و تعداد کل بالای صفحه نمایش داده می‌شود
- 🔍 **زوم و جابه‌جایی**: زوم با دو انگشت، جابه‌جایی با یک انگشت، به‌اضافهٔ دکمه‌های زوم و «تمام‌عکس»
- ✏️ **ویرایش علامت‌ها**: لمس یک علامتِ موجود، منوی *حذف* و *تغییر شماره* را باز می‌کند (با تغییر شماره، بقیه خودکار جابه‌جا می‌شوند و شماره‌ها همیشه ۱ تا N می‌مانند)
- ↩️ **بازگشت (Undo)** تا ۱۰۰ مرحله و **پاک‌کردن همهٔ علامت‌ها**
- ⭕ **اسلایدر اندازهٔ علامت** برای وقتی که موارد خیلی به هم نزدیک‌اند
- 💾 **خروجی**: ذخیرهٔ عکسِ شماره‌دار در گالری (`Pictures/PhotoCounter`) یا اشتراک‌گذاری آن
- 🌐 رابط کاربری فارسی و راست‌به‌چپ

## 📖 روش استفاده

1. «عکس‌گرفتن» یا «گالری» را بزنید.
2. روی هر مورد یک بار بزنید تا شمرده شود.
3. برای دقت بیشتر زوم کنید و علامت‌ها را ردیف‌به‌ردیف بزنید.
4. اگر اشتباه زدید: «بازگشت»، یا روی خود علامت بزنید و حذف/تغییر شماره را انتخاب کنید.
5. در پایان با «تمام‌عکس» یک نگاه کلی بیندازید و در صورت نیاز از «خروجی» برای ذخیره یا ارسال عکس استفاده کنید.

> **نکتهٔ دقت:** اقلام را یک لایه و بدون روی‌هم‌افتادن بچینید و از بالا عکس بگیرید. موردی که زیر مورد دیگری باشد در عکس دیده نمی‌شود. بازتابِ نور روی پلاستیک را هم با کمی تغییر زاویه و بدون فلاش کم کنید.

## 🧰 پیش‌نیازها

- Visual Studio 2026 با ورک‌لود **.NET Multi-platform App UI** (شامل Android SDK و JDK)
- .NET 10 SDK
- گوشی یا امولاتور اندروید ۱۰ (API 29) به بالا

## ▶️ اجرا از روی سورس

۱. پروژه را کلون کنید:

</div>

```bash
git clone <آدرس-مخزن>
cd PhotoCounter
```

<div dir="rtl">

۲. فایل `PhotoCounter.csproj` را در Visual Studio باز کنید، دستگاه یا امولاتور اندروید را انتخاب کنید و **Run** بزنید. یا از ترمینال:

</div>

```bash
dotnet build -f net10.0-android -t:Run
```

<div dir="rtl">

## 📦 ساخت APK امضاشده

### روش ۱: اسکریپت آماده (پیشنهادی)

در پوشهٔ پروژه، در PowerShell:

</div>

```powershell
.\build-apk.ps1
```

<div dir="rtl">

- بار اول، یک فایل کلید به نام `signing\photocounter.keystore` می‌سازد (رمز را از شما می‌پرسد). دفعه‌های بعد از همان کلید استفاده می‌کند.
- خروجی: `dist\PhotoCounter.apk`
- اگر ویندوز اجرای اسکریپت را بلاک کرد: `powershell -ExecutionPolicy Bypass -File .\build-apk.ps1`

### روش ۲: دستی

ساخت کلید (فقط یک بار):

</div>

```powershell
keytool -genkeypair -v -keystore photocounter.keystore -alias photocounter -keyalg RSA -keysize 2048 -validity 10000 -storetype PKCS12
```

<div dir="rtl">

ساخت APK:

</div>

```powershell
$env:KEYPASS = Read-Host "Keystore password"

dotnet publish -f net10.0-android -c Release `
  -p:AndroidPackageFormat=apk `
  -p:AndroidKeyStore=true `
  "-p:AndroidSigningKeyStore=$PWD\photocounter.keystore" `
  -p:AndroidSigningKeyAlias=photocounter `
  -p:AndroidSigningKeyPass=env:KEYPASS `
  -p:AndroidSigningStorePass=env:KEYPASS

Copy-Item bin\Release\net10.0-android\publish\*-Signed.apk .\PhotoCounter.apk
```

<div dir="rtl">

نصب روی گوشی:

</div>

```bash
adb install PhotoCounter.apk
```

<div dir="rtl">

> ⚠️ **از فایل keystore و رمزش بکاپ بگیرید و هرگز آن را در گیت قرار ندهید** (پوشهٔ `signing/` و فایل‌های `*.keystore` در `.gitignore` هستند). بدون همان کلید نمی‌توانید آپدیتی منتشر کنید که روی نسخهٔ نصب‌شده نصب شود.
>
> برای انتشار در Google Play به‌جای APK باید **AAB** بسازید: `-p:AndroidPackageFormat=aab`

## 🗂 ساختار پروژه

</div>

```text
PhotoCounter/
├── PhotoCounter.csproj            # تنظیمات پروژه (.NET 10، فقط اندروید) و امضای Release
├── build-apk.ps1                  # ساخت APK امضاشده
├── MauiProgram.cs                 # ثبت handler
├── App.xaml(.cs)
├── MainPage.xaml(.cs)             # رابط کاربری و منطق صفحه (دوربین، منو، خروجی)
├── CounterView.cs                 # کنترل مشترک MAUI (پراپرتی‌ها، رویدادها، دستورها)
└── Platforms/Android/
    ├── CounterNativeView.cs       # رسم عکس، زوم/پن، علامت‌ها (View بومی اندروید)
    ├── CounterViewHandler.cs      # اتصال CounterView به View بومی
    ├── GallerySaver.cs            # ذخیره در گالری (MediaStore)
    ├── MainActivity.cs
    ├── MainApplication.cs
    └── AndroidManifest.xml
```

<div dir="rtl">

## ⚙️ نکات فنی

- **View بومی با handler سفارشی**: رسم عکس و ژست‌ها (`ScaleGestureDetector` و `GestureDetector`) مستقیم روی اندروید انجام می‌شود تا زوم و جابه‌جایی روان باشد.
- **مختصات علامت‌ها نسبت به پیکسل‌های خود عکس** ذخیره می‌شود؛ پس با زوم، جابه‌جایی و چرخش صفحه سر جایشان می‌مانند.
- **عکس‌های بزرگ** برای نمایش حداکثر تا ۴۰۹۶ پیکسل کوچک می‌شوند (محدودیت GPU خیلی از گوشی‌ها) و چرخش EXIF دوربین اعمال می‌شود. بارگذاری روی نخ پس‌زمینه انجام می‌شود.
- **Undo** با نگه‌داشتن snapshot از فهرست علامت‌ها پیاده شده؛ شماره‌ها همیشه از جایگاه علامت در فهرست محاسبه می‌شوند.
- **خروجی** عکس را با کیفیت (اندازهٔ) نمایش‌داده‌شده + علامت‌ها در یک JPEG می‌سازد.
- **مجوزها**: فقط `CAMERA` (برای گرفتن عکس). انتخاب از گالری و ذخیره در گالری به مجوز اضافه نیاز ندارند.

## 🛣 ایده‌ها برای آینده

- [ ] تاریخچهٔ شمارش‌ها (تاریخ، عنوان، تعداد)
- [ ] خروجی CSV/Excel از تاریخچه
- [ ] جابه‌جایی علامت‌ها با کشیدن
- [ ] تشخیص خودکار اقلام (پیشنهاد اولیه برای علامت‌گذاری)
- [ ] ذخیرهٔ وضعیت برای ادامهٔ کار بعد از بسته‌شدن اپ

## 🤝 مشارکت

Issue و Pull Request خوش‌آمد است. برای تغییرات بزرگ، اول یک Issue باز کنید تا دربارهٔ ایده صحبت کنیم.

## 📄 مجوز

این پروژه تحت مجوز [MIT](LICENSE) منتشر شده است.

</div>