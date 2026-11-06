---
name: new-backend-project
description: 建立符合 housewoo 架構慣例的 .NET 後端專案骨架（Domain / Infrastructure / Service 四層）。當使用者要開新的後端專案、建立新 API、scaffold .NET 服務時使用。支援 API 模式（前後端分離）與 Web 模式（MVC + Razor，不分離）。分離模式的前端請另外使用 new-frontend-project skill。
---

# 建立 .NET 後端專案架構

依照 housewoo 既有的架構慣例，快速建立新的後端專案骨架。

作法是**用 `dotnet new` 產生原生骨架，再依慣例改寫**，而非整包複製舊專案。`templates/` 只放「CLI 產不出來、且屬於架構核心」的檔案。

## Step 0：先問清需求

用 AskUserQuestion 一次問完，不要逐題來回：

1. **專案名稱**（PascalCase，例如 `HousewooHub`）與**輸出目錄**
2. **模式**：
   - `API` — `<Name>.API`，回 JSON，供獨立前端呼叫
   - `Web` — `<Name>.Web`，MVC + Razor Views，不分離前後端
3. **資料庫**：SQL Server（預設）／ PostgreSQL ／ 暫不接
4. **選配**（多選）：JWT 驗證、Serilog、Swagger、Quartz 排程、Docker、xunit 測試專案
5. **CancellationToken**：
   - `不採用` — 與既有 housewoo 專案一致，`IUnitOfWork`、Service、Controller 都不帶 `CancellationToken`
   - `採用` — `IUnitOfWork` 加參數並傳給 EF，Service 與 Controller 一路傳遞（客戶端中斷時可提早停止查詢）

   這題**不要預設推薦**，兩個選項並列，由使用者決定。

後續步驟一律用 `<Name>` 代表專案名稱。

## Step 1：建立專案與參考關係

兩種模式共用 Domain / Infrastructure / Service 三層，只有最外層專案不同。

```bash
dotnet new sln -n <Name> --format slnx
dotnet new classlib -o src/<Name>.Domain
dotnet new classlib -o src/<Name>.Infrastructure
dotnet new classlib -o src/<Name>.Service

# 依模式擇一
dotnet new webapi -o src/<Name>.API --use-controllers   # API 模式
dotnet new mvc    -o src/<Name>.Web                     # Web 模式

dotnet new xunit  -o tests/<Name>.Tests                 # 選配
```

`--use-controllers` 是必要的 —— .NET 8 起 `webapi` 預設產出 Minimal API，與本架構的 Controller 慣例不符。

設定專案參考（**這是本架構最關鍵的一點，不要接錯**）：

```bash
dotnet add src/<Name>.Infrastructure reference src/<Name>.Domain
dotnet add src/<Name>.Service        reference src/<Name>.Domain
dotnet add src/<Name>.API            reference src/<Name>.Domain src/<Name>.Infrastructure src/<Name>.Service
dotnet add tests/<Name>.Tests        reference src/<Name>.Service src/<Name>.Domain

dotnet sln add src/<Name>.Domain src/<Name>.Infrastructure src/<Name>.Service src/<Name>.API tests/<Name>.Tests
```

`dotnet sln add` 請逐一列出專案，不要用 `src/**/*.csproj` —— `**` 在 PowerShell 不會展開，bash 也需要先開 globstar。

**Service 刻意不參考 Infrastructure**：Service 只依賴 Domain 裡的 `IGenericRepository<>` / `IUnitOfWork` 介面，實作由最外層在 DI 接線。若你發現需要在 Service 加 Infrastructure 參考，代表該介面該往 Domain 搬，而不是加參考。

## Step 2：移除內建的 OpenApi 套件（API 模式必做）

`dotnet new webapi` 會帶入 `Microsoft.AspNetCore.OpenApi`，它相依的 `Microsoft.OpenApi 2.0.0` 有已知高嚴重性弱點（NU1903）。

**不要用升級的方式解決** —— 實測升到 3.x 會讓 `Microsoft.AspNetCore.OpenApi` 的 source generator 編譯失敗（`CS0200: IOpenApiMediaType.Example 為唯讀`）。

本架構的 Swagger 走 Swashbuckle，直接移除這兩個套件即可：

```bash
dotnet package remove Microsoft.OpenApi --project src/<Name>.API/<Name>.API.csproj
dotnet package remove Microsoft.AspNetCore.OpenApi --project src/<Name>.API/<Name>.API.csproj
dotnet add src/<Name>.API package Swashbuckle.AspNetCore
```

注意 .NET 10 移除套件的指令是 `dotnet package remove <套件> --project <csproj>`。舊寫法 `dotnet remove <專案> package <套件>` 在此版會回報「找不到任何專案」。

同時把 `Program.cs` 內建的 `AddOpenApi()` / `MapOpenApi()` 換成 `AddSwaggerGen()` / `UseSwagger()` + `UseSwaggerUI()`，並刪掉範本產生的 `WeatherForecast.cs` 與 `WeatherForecastController.cs`。

## Step 3：套用架構慣例

1. 所有 `.csproj` 補上 `<Nullable>enable</Nullable>`、`<ImplicitUsings>enable</ImplicitUsings>`；最外層專案另加 `<GenerateDocumentationFile>true</GenerateDocumentationFile>` 與 `<NoWarn>$(NoWarn);CS1591</NoWarn>`
2. 從 `templates/` 複製核心檔案，把 `__PROJECT_NAME__` 全部替換成 `<Name>`
3. 依 `references/conventions.md` 改寫 `Program.cs` 並建立 `Extensions/`
4. Step 0 選擇**採用 CancellationToken** 時，依 `references/conventions.md` 的「CancellationToken」一節修改複製後的 `IUnitOfWork` / `UnitOfWork`；不採用則維持範本原樣

範本檔落點：

| 範本 | 目的地 |
|---|---|
| `BaseResponse.cs` | `src/<Name>.Domain/DTOs/Share/` |
| `IGenericRepository.cs` | `src/<Name>.Domain/Interface/` |
| `GenericRepository.cs` | `src/<Name>.Infrastructure/Implement/` |
| `PaginationResponse.cs` | `src/<Name>.API/Models/Share/`（API 模式才需要） |
| `BaseController.cs` | `src/<Name>.API/Controllers/`（API 模式才需要） |
| `ApiExceptionResponseHelper.cs` | `src/<Name>.API/Helpers/`（API 模式才需要） |
| `AGENTS.md` | 專案根目錄 |
| `CLAUDE.md` | 專案根目錄（見下方說明） |

`CLAUDE.md` 的佔位符替換規則：`__PROJECT_NAME__` → `<Name>`；`__MODE__` → `API` 或 `Web`；`__OUTER_LAYER__` → `API` 或 `Web`；`__FRONTEND_DIR__` → 對應前端專案的相對路徑（Web 模式或尚未決定時刪除該行）；`__CANCELLATION_TOKEN__` → 依 Step 0 的選擇填 `採用` 或 `不採用`，並刪除下方未被選擇的那一行說明。

> **為什麼需要 CLAUDE.md**：Claude Code 與 sub agent 只會自動載入 `CLAUDE.md`，不會讀 `AGENTS.md`。範本的 `CLAUDE.md` 用 `@AGENTS.md` 匯入架構規則，規則仍只維護 `AGENTS.md` 一份（Codex 等其他工具也讀得到）。專案特有的指令、模組清單寫在 `CLAUDE.md`，架構規則寫在 `AGENTS.md`。

> `IGenericRepository.cs` 與 `GenericRepository.cs` 各自同時含有 UnitOfWork 的介面與實作，複製後請拆成兩個檔案（`IUnitOfWork.cs`、`UnitOfWork.cs`）。`BaseResponse.cs` 同理，內含 `DataResponse<T>`，可留在同檔或拆開。

`GenericRepository.cs` 需要 `Infrastructure/Data/EntityDbContext.cs` 才能編譯，記得一併建立並安裝 EF Core 套件。

參考文件：

- `references/architecture.md` — 分層職責、依賴形狀、資料夾配置、命名規則
- `references/conventions.md` — `Program.cs` 結構、DI 生命週期、回應格式、例外處理、設定檔
- `references/web-mvc.md` — Web 模式專屬（Cookie 驗證、ViewModel、Razor、Tailwind CLI）

## Step 4：收尾與驗證

1. `dotnet build` 必須零錯誤零警告通過
2. 產生 `README.md`（含專案結構、啟動方式、環境變數說明）
3. 確認根目錄有 `CLAUDE.md`（含 `@AGENTS.md` 匯入）且佔位符都已替換：`grep -n "__" CLAUDE.md` 應無結果
4. `git init` 並建立首次 commit — **這一步要先徵得使用者同意再執行**

## 接下來

若是**前後端分離**的專案，後端完成後接著用 **`new-frontend-project`** skill 建立 Vue 前端。要一次走完前後端與規劃、實作、測試、審查的完整流程，改用 **`new-fullstack-project`**。前端的 API 型別由本專案的 Swagger 產生，所以順序上先做後端。

Web 模式（MVC）不需要另外建前端，樣式設定已含在 `references/web-mvc.md`。

## 不要沿用的既有技術債

既有的 `housewoo-erp-api` 有兩處問題，新專案**不要複製**：

1. **`appsettings.json` 內含明碼資料庫密碼與 JWT SecretKey**。新專案改用 user-secrets（開發）＋ 環境變數（部署），`appsettings.json` 只留空字串佔位。
2. **`AddSecurity()` 內呼叫 `services.BuildServiceProvider()` 取設定**，會額外建出一個容器、造成 singleton 重複實例化。新專案改用 `IOptions<T>` 或直接傳入已綁定的設定物件。

此外兩處較小的整理，範本已修正，不要改回舊寫法：

- 既有的 `GenericRepository` 有一個未宣告在介面上的 `SaveChangesAsync()`。提交應該只走 `IUnitOfWork`，範本已移除。
- 既有 `BaseController` 的 protected 屬性用 `_currentAccountId` 這種底線前綴命名（C# 慣例上底線只用於私有欄位）。範本改為 `CurrentAccountId`。
