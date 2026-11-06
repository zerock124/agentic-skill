# 後端程式碼慣例

## Program.cs 保持極薄

`Program.cs` 只留編號步驟與註解，實作全部推到 `Extensions/`。目標是掃一眼就知道啟動流程做了哪些事。

```csharp
using <Name>.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. 載入設定與註冊資料庫 (Configuration)
var config = builder.Services.AddAppSettings(builder.Configuration);

// 2. 註冊依賴注入服務 (Dependency Injection)
builder.Services.AddApplicationDefaults(builder.Configuration); // Controllers, Session, AutoMapper
builder.Services.AddSecurity(config);                           // JWT, CORS
builder.Services.AddSwagger();                                  // API 文件
builder.Services.AddBusinessServices();                         // 業務邏輯服務

// 3. 設定全域日誌 (Logging)
builder.Host.UseCustomSerilog(config);

var app = builder.Build();

// 4. 配置 HTTP 請求處理管線 (Middleware Pipeline)
app.UseCustomMiddleware();

// 5. 註冊端點 (Endpoints)
app.MapControllers();

app.Run();
```

要建立的 Extension 檔案（`src/<Name>.API/Extensions/`）：

| 檔案 | 內容 |
|---|---|
| `ConfigurationExtensions.cs` | `AddAppSettings` — 綁定設定物件、註冊 `DbContext` |
| `ServiceCollectionExtensions.cs` | `AddApplicationDefaults` / `AddSecurity` / `AddSwagger` / `AddBusinessServices` |
| `MiddlewareExtensions.cs` | `UseCustomMiddleware` — 管線順序 |
| `LoggingExtensions.cs` | `UseCustomSerilog` — 選配 |

## JSON 與路由

```csharp
services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
})
.AddJsonOptions(options =>
{
    // 保留 C# 屬性名稱原樣 (PascalCase)，不自動轉為 camelCase
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
```

**PascalCase 是硬性約定**。前端 orval 產生的型別直接吃後端 Swagger，兩邊都是 PascalCase 才對得上；改成 camelCase 會讓前端錯誤處理（讀 `Message` 欄位）整片失效。

## DI 生命週期

```csharp
public static IServiceCollection AddBusinessServices(this IServiceCollection services)
{
    // 資料存取：Scoped，與 DbContext 同生命週期
    services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    services.AddScoped<IUnitOfWork, UnitOfWork>();

    // 業務服務：一律 Transient，手動逐條列出
    services.AddTransient<ICategoryService, CategoryService>();

    return services;
}
```

刻意**不使用** Scrutor 之類的組件掃描自動註冊。手動列舉雖然囉唆，但註冊清單本身就是一份可讀的服務目錄，也避免誤註冊到不該公開的類別。

## 回應格式三件套

| 類別 | 位置 | 用途 |
|---|---|---|
| `BaseResponse` | Domain/DTOs/Share | `{ Success, Message }`，錯誤回應與無資料的成功回應 |
| `DataResponse<T>` | Domain/DTOs/Share | 繼承 `BaseResponse`，加 `T? Data` |
| `PaginationResponse<T>` | API/Models/Share | 繼承 `BaseResponse`，`Data` 內含分頁資訊與 `DataList` |

Controller 回傳一律 `Ok(res)`，不要用 `ActionResult<T>`：

```csharp
[Tags("Category")]
public class CategoryController(ICategoryService service, IMapper mapper) : BaseController
{
    /// <summary>
    /// 依分類代碼取得單一分類資料
    /// </summary>
    /// <param name="categoryCode">分類代碼。</param>
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategory([FromQuery] string categoryCode)
    {
        var res = new DataResponse<CategoryResponse>();
        var category = await service.Get(categoryCode);
        res.Data = mapper.Map<CategoryResponse>(category);
        res.Success = category != null;
        return Ok(res);
    }

    /// <summary>
    /// 編輯分類
    /// </summary>
    /// <param name="data">請求資料。</param>
    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryRequest data)
    {
        var res = new BaseResponse();
        await service.UpdateCategory(mapper.Map<EditCategoryDTO>(data));
        res.Success = true;
        return Ok(res);
    }
}
```

- action 名稱與 Service 方法同名（`<動作><Module>`），路由由 `[Route("[controller]/[action]")]` + 小寫 URL 產生：`GET /category/getcategory`、`POST /category/updatecategory`
- 查詢用 `[HttpGet]` + `[FromQuery]`，新增／修改／刪除用 `[HttpPost]` + `[FromBody]`，上傳用 `[FromForm]`；不使用 PUT / DELETE
- 新增、修改、刪除通常回 `BaseResponse`；查詢單筆回 `DataResponse<T>`；分頁清單回 `PaginationResponse<T>`

## 例外處理：雙保險 + 集中映射

- `Middlewares/ApiExceptionHandlingMiddleware.cs` 掛在管線最外層，接住 filter 管不到的例外
- `Filters/ApiExceptionFilter.cs` 全域 filter，處理 action 內的例外
- 兩者共用 `Helpers/ApiExceptionResponseHelper.cs` 做狀態碼映射

映射規則：

| 例外 | 狀態碼 | 訊息 |
|---|---|---|
| `ArgumentException` | 400 | 原始訊息 |
| `KeyNotFoundException` | 404 | 原始訊息 |
| `InvalidOperationException` | 409 | 原始訊息 |
| 其他 | 500 | 遮蔽為「系統發生錯誤，請稍後再試」 |

Service 層擲出前三種例外來表達業務錯誤，不要自己回傳錯誤碼。未預期的例外訊息一律遮蔽，避免洩漏內部細節。

## Middleware 順序

```csharp
app.UseMiddleware<ApiExceptionHandlingMiddleware>();  // 最外層
app.UseRequestBuffering();                            // 非 multipart 才 EnableBuffering
app.MigrateDatabase();                                // 選配
app.UseSwagger(); app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowSpecificOrigins");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
```

## 設定檔

```jsonc
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*",
  "AppSettings": {
    "IsDev": true,
    "ConnectionString": "",        // 空字串佔位，實際值走 user-secrets / 環境變數
    "TimeZone": "Asia/Taipei"
  },
  "JwtSetting": {
    "Issuer": "<Name>",
    "Audience": "<Name>",
    "SecretKey": "",               // 同上，絕不寫進版控
    "ExpireHours": 8
  }
}
```

綁定到 `Domain/Configs/AppSettingsConfig`，以 **Singleton** 註冊。

**機密值處理**：開發用 `dotnet user-secrets set "AppSettings:ConnectionString" "..."`，部署用環境變數（`AppSettings__ConnectionString`）。不要把明碼密碼寫進 `appsettings.json` —— 這是既有專案的技術債，新專案別複製。

**不要用 `services.BuildServiceProvider()` 取設定**。`AddAppSettings` 回傳已綁定的設定物件，後續 extension 直接以參數接收即可。

## 資料存取

EF Core（無 Dapper）。Service 直接注入泛型 repository，不做 per-entity repository：

```csharp
public class CategoryService(IGenericRepository<Category> repo, IUnitOfWork uow, IMapper mapper) : ICategoryService
{
    public async Task UpdateCategory(EditCategoryDTO data)
    {
        var entity = repo.Get(x => x.CategoryCode == data.CategoryCode && !x.IsDeleted)
            ?? throw new KeyNotFoundException("查無此分類");
        mapper.Map(data, entity);
        repo.Update(entity);
        await uow.SaveChangesAsync();   // 提交只走 UnitOfWork
    }
}
```

Service 方法命名為 `<動作><Module>`，**不加 `Async` 後綴**（與既有專案一致）；`IUnitOfWork` 與包裝 EF / 框架 API 的方法才保留 `Async`。

`IGenericRepository` **沒有** `SaveChangesAsync`，提交一律經 `IUnitOfWork`。這樣同一次請求內跨多個 repository 的變更才會落在同一個交易裡。

Migration 只用 `dotnet ef migrations add <Name>` 產生，**不要手改** migration 檔案。

## CancellationToken（建立專案時由使用者決定）

既有專案的 `IUnitOfWork` 與 Service 方法**不帶** `CancellationToken`。新專案在 `new-backend-project` Step 0 詢問使用者，並把結果寫進專案 `CLAUDE.md` 的「CancellationToken」欄位，之後所有 agent 依該欄位行事。

| 選擇 | 作法 |
|---|---|
| **不採用**（與既有專案一致） | 範本維持原樣。Service、Controller 都不帶 `CancellationToken`；審查時不要把「缺少 CancellationToken」當成問題 |
| **採用** | 依下列規則全面套用，新增的程式碼也要遵守 |

採用時的規則：

1. `IUnitOfWork` / `UnitOfWork` 的四個方法都加上 `CancellationToken cancellationToken = default`，並往下傳給 EF：
   ```csharp
   Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
   // 實作
   public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
       => context.SaveChangesAsync(cancellationToken);
   ```
   `BeginTransactionAsync` / `CommitTransactionAsync` / `RollbackTransactionAsync` 同理（`context.Database.BeginTransactionAsync(cancellationToken)`、`_transaction.CommitAsync(cancellationToken)`）
2. `IGenericRepository<T>` 的方法是同步的，不需要改；`GetAll()` 回傳的 `IQueryable` 在 Service 用 `ToListAsync(cancellationToken)` / `FirstOrDefaultAsync(cancellationToken)` 時傳入
3. Service 方法最後一個參數加 `CancellationToken cancellationToken = default`
4. Controller action 加 `CancellationToken cancellationToken` 參數（ASP.NET Core 會自動綁定 `HttpContext.RequestAborted`），一路傳給 Service
5. 方法命名規則不變（Service 仍不加 `Async` 後綴）
