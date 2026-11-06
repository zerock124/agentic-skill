# 不分離模式：ASP.NET Core MVC + Razor Views

當專案不需要獨立前端（內部工具、後台管理、頁面以表單為主）時採用這個模式。省掉一整套前端建置與 API 契約同步成本。

**Domain / Infrastructure / Service 三層與分離模式完全相同**，差別只在最外層用 `<Name>.Web` 取代 `<Name>.API`。

## 建立專案

```bash
dotnet new mvc -o src/<Name>.Web
dotnet add src/<Name>.Web reference src/<Name>.Domain src/<Name>.Infrastructure src/<Name>.Service
```

## 與 API 模式的差異

| 項目 | API 模式 | Web 模式 |
|---|---|---|
| Controller 基底 | `ControllerBase` | `Controller`（要回傳 View） |
| 路由 | `[Route("[controller]/[action]")]` | 慣例路由 `{controller=Home}/{action=Index}/{id?}` |
| 回應 | `DataResponse<T>` JSON | `View(viewModel)` |
| 例外處理 | 回 JSON `BaseResponse` | `UseExceptionHandler("/Home/Error")` + 錯誤頁 |
| 驗證 | JWT Bearer | Cookie 驗證 |
| Swagger | 需要 | 不需要 |
| 資料傳遞 | Request/Response Models | ViewModels |

**沒有 `BaseController` / `ApiExceptionResponseUtility` / `PaginationResponse`**，這三個範本在此模式不需要複製。

`Models/` 底下放 ViewModel，命名 `<Module>ViewModel` / `<Module>EditViewModel`，職責等同 API 模式的 Request/Response —— 一樣不要讓 Entity 直接進 View。

## Program.cs

```csharp
using <Name>.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. 載入設定與註冊資料庫
var config = builder.Services.AddAppSettings(builder.Configuration);

// 2. 註冊依賴注入服務
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
    });
builder.Services.AddBusinessServices();

// 3. 設定全域日誌
builder.Host.UseCustomSerilog(config);

var app = builder.Build();

// 4. HTTP 請求處理管線
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 5. 註冊端點
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

`AddAppSettings` / `AddBusinessServices` 的內容與 API 模式一致，見 `conventions.md`。

## Tailwind 設定

Razor Views 一樣用 Tailwind，但改用 Tailwind CLI 直接建置到 `wwwroot/css/`，不引入 Vite 或 PostCSS。

在 `src/<Name>.Web/` 下：

```bash
npm init -y
npm i -D tailwindcss @tailwindcss/cli
```

建立 `Styles/app.css`：

```css
@import 'tailwindcss';

/* 掃描 Razor 檔案取用到的 class */
@source '../Views/**/*.cshtml';
@source '../Areas/**/*.cshtml';

@theme {
  --color-primary: #4f46e5;
  --color-primary-hover: #4338ca;
  /* 其餘色票見 templates/tailwind.css */
}
```

`package.json` 加入指令：

```json
{
  "scripts": {
    "css:dev": "tailwindcss -i ./Styles/app.css -o ./wwwroot/css/app.css --watch",
    "css:build": "tailwindcss -i ./Styles/app.css -o ./wwwroot/css/app.css --minify"
  }
}
```

`.csproj` 掛上建置前置動作，讓 `dotnet build` 自動產生 CSS：

```xml
<Target Name="BuildTailwind" BeforeTargets="Build">
  <Exec Command="npm run css:build" WorkingDirectory="$(ProjectDir)" />
</Target>
```

`.gitignore` 要排除 `node_modules/` 與 `wwwroot/css/app.css`（產生物）。

## _Layout.cshtml

```html
<!DOCTYPE html>
<html lang="zh-Hant-TW">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - <Name></title>
    <link rel="stylesheet" href="~/css/app.css" asp-append-version="true" />
</head>
<body class="bg-gray-50 text-gray-900">
    <div class="flex min-h-screen">
        <aside class="w-64 bg-slate-800 text-slate-200">
            <partial name="_Sidenav" />
        </aside>
        <div class="flex flex-1 flex-col">
            <header class="h-11 border-b bg-white px-4">
                <partial name="_Topbar" />
            </header>
            <main class="flex-1 p-6">
                @RenderBody()
            </main>
        </div>
    </div>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

`asp-append-version="true"` 會加上內容雜湊查詢字串，避免改樣式後使用者拿到快取的舊 CSS。

## 新增模組時要動的檔案

1~5 與 `architecture.md` 相同（Entity / DTO / Service 介面實作 / Mapping），第 6 步之後改為：

6. `Models/<Module>ViewModel.cs`
7. `Controllers/<Module>Controller.cs`
8. `Views/<Module>/Index.cshtml` + `Edit.cshtml`
9. `Extensions/ServiceCollectionExtensions.cs` 加一行 `AddTransient`
