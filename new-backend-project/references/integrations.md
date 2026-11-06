# 非 CRUD 功能：排程、外部 API、Webhook、匯入、即時推播

以下為本架構的標準寫法，範例以電商平台（Momo）訂單同步為例。共通原則：**先定義邊界（觸發方式、資料格式、失敗策略），再決定放哪一層，最後才實作**。

## 分層對照

| 元件 | 位置 | 職責 |
|---|---|---|
| Quartz Job | `API/Jobs/<Name>Job.cs` | 決定何時執行、記錄結果；只呼叫 Service，不寫業務邏輯 |
| 外部 API 介面 | `Domain/Interface/I<Provider>Provider.cs` | 讓 Service 依賴介面而不參考 Infrastructure |
| 外部 API 實作（Adapter） | `Infrastructure/Adapters/<Provider>/` | 只做 HTTP I/O 與格式轉換，**不碰 DB、不解密** |
| 外部 API 例外 | `Domain/Exceptions/<Provider>ApiException.cs` | 繼承 `ExternalApiException`，對應 HTTP 502 |
| 流程協調（Orchestration） | `Service/Implement/Orchestration/<Name>Service.cs` | 取憑證／解密、呼叫 Provider、寫 DB、交易 |
| Excel 解析 | `Service/Utilities/ExcelUtility.cs`（static） | 只取值成 `<Module>ImportRowDTO`，不驗證 |
| SignalR Hub | `API/SignalR/` | 連線管理、群組 |
| 進度推播 | `API/Helpers/ProgressNotifierHelper.cs`（可注入） | 由 Controller 透過回呼呼叫 |

## Quartz 排程

### Job

```csharp
using Quartz;

namespace <Name>.API.Jobs;

/// <summary>
/// 每日同步 Momo 未出貨訂單
/// </summary>
[DisallowConcurrentExecution]   // 上一次還沒跑完時不重疊執行
public class MomoSyncJob(IMomoSyncService momoSyncService, ILogger<MomoSyncJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-7);
        try
        {
            var createdCount = await momoSyncService.SyncUnshippedOrders(startDate, endDate);
            logger.LogInformation("Momo 訂單同步完成，日期 {StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd}，新增 {Count} 筆",
                startDate, endDate, createdCount);
        }
        catch (Exception ex)
        {
            // 排程工作不可讓例外中斷 HostedService，記錄後結束
            logger.LogError(ex, "Momo 訂單同步失敗，日期 {StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd}", startDate, endDate);
        }
    }
}
```

- Job 由 Quartz 以 scoped 方式建立，可以直接注入 Service，不需要自己建 scope
- 一律加 `[DisallowConcurrentExecution]`
- 在 Job 內 catch 並記錄，不要讓例外往外拋
- 記錄執行區間與處理筆數，失敗時才查得到原因

### 註冊

集中在 `Extensions/ServiceCollectionExtensions.cs` 的 `AddQuartzJobs(config)`，時區取自 `AppSettings:TimeZone`：

```csharp
public static IServiceCollection AddQuartzJobs(this IServiceCollection services, AppSettingsConfig config)
{
    services.AddQuartz(q =>
    {
        // 根據 appsettings.json 設定時區，確保 Cron 執行時間正確
        var timeZoneId = config.TimeZone ?? "UTC";
        var appTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        // Momo 未出貨訂單同步排程 - 每日早上 7 點 30 分
        var momoSyncJobKey = new JobKey("MomoSync");
        q.AddJob<MomoSyncJob>(opts => opts.WithIdentity(momoSyncJobKey));
        q.AddTrigger(opts => opts
            .ForJob(momoSyncJobKey)
            .WithIdentity("MomoSync-trigger")
            .WithCronSchedule("0 30 7 * * ?", x => x.InTimeZone(appTimeZone)));
    });

    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    return services;
}
```

- **一定要指定時區**。容器內預設為 UTC，不指定會差 8 小時
- 每個 Job 的註冊上方寫一行註解說明用途與執行時間；多個呼叫同一類外部服務的排程要錯開時間（例：平台 A 07:30、平台 B 07:40）
- 停用某個 Job 時把註冊註解掉並寫明原因，同時更新 `docs/architecture.md` 的排程清單

### Cron 參考（Quartz 格式：秒 分 時 日 月 週）

| 表示式 | 說明 |
|---|---|
| `0 30 7 * * ?` | 每天 07:30 |
| `0 0 2 * * ?` | 每天 02:00 |
| `0 */30 * * * ?` | 每 30 分鐘 |
| `0 0 9-17 ? * MON-FRI` | 週一到週五 9:00～17:00 每小時整點 |
| `0 0 3 ? * MON` | 每週一 03:00 |
| `0 0 12 1 * ?` | 每月 1 號 12:00 |

## 外部 API Adapter

### 例外

```csharp
namespace <Name>.Domain.Exceptions;

/// <summary>
/// Momo API 呼叫失敗
/// </summary>
public class MomoApiException(string message, HttpStatusCode? statusCode = null,
    string? errorMessage = null, string? responseContent = null, Exception? innerException = null)
    : ExternalApiException(message, statusCode, errorMessage, responseContent, innerException)
{
    public override string ProviderName => "Momo API";
}
```

`ExternalApiException`（抽象基底，放 `Domain/Exceptions/`）保存 `ProviderName`、`StatusCode`、`ErrorMessage` 與**截斷到 1024 字元**的 `ResponseContent`，避免大型回應（例如 PDF）被整包寫進日誌。專案第一次串接外部 API 時：

1. 建立 `ExternalApiException` 基底類別
2. 在 `ApiExceptionResponseUtility.Resolve` 加上 `ExternalApiException => 502`，對外訊息為 `"<ProviderName>：<ErrorMessage>"`
3. 在 `ApiExceptionFilter` 對 `ExternalApiException` 額外記錄 `Provider`、`StatusCode`、`Content`（`Exception.Message` 裡沒有這些診斷資訊）

### Adapter

- 實作 `Domain/Interface/I<Provider>Provider`，類別名稱為 `<Provider>Adapter`
- 外部 API 的 Request / Response 合約放在 `Adapters/<Provider>/Contracts/`，轉成 Domain DTO 的程式放 `Adapters/<Provider>/Mappers/`；**不要讓外部合約型別流到 Service**
- 只做 HTTP 呼叫、序列化與錯誤轉換；失敗時擲 `<Provider>ApiException`
- 憑證由 Service 解密後透過方法傳入（例：`SetMomoApiCredentialsAsync`），Adapter 不讀資料庫、不解密

### 註冊

`Extensions/HttpClientExtensions.cs` 的 `AddAdapterHttpClients`，每個 Provider 一行，用 typed HttpClient：

```csharp
services.AddAdapterHttpClient<IMomoProvider, MomoAdapter>(configuration);
```

共用的 `AddAdapterHttpClient<TInterface, TImplementation>` 設定 `SocketsHttpHandler` 的 `PooledConnectionLifetime`（定期重連，避免 DNS 變更或連線被 WAF 標記）與 `PooledConnectionIdleTimeout`。

### Orchestration Service

- 放 `Service/Implement/Orchestration/`，介面放 `Service/Interface/Orchestration/`
- 負責：取出並解密憑證 → 呼叫 Provider → 比對既有資料（避免重複建立）→ 透過 `IUnitOfWork` 寫入
- 第三方憑證存在 `SystemParam` 資料表（加密），不要放 `appsettings.json`

## Webhook

需要接收 Webhook 時依下列原則實作。

```csharp
/// <summary>
/// 接收 XXX 平台的事件通知
/// </summary>
[HttpPost]
[AllowAnonymous]                                  // 第三方沒有 JWT，改以簽章驗證來源
[ServiceFilter(typeof(XxxWebhookSignatureFilter))]
[ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
public async Task<IActionResult> ReceiveXxxEvent()
{
    // 原始內容由 Filter 讀取並驗證；這裡只轉交 Service，盡快回 200
    await webhookService.EnqueueXxxEvent(HttpContext.Items["WebhookBody"] as string ?? string.Empty);
    return Ok(new BaseResponse { Success = true });
}
```

簽章驗證 Filter 的要點：

- 用 `Request.EnableBuffering()` 讀**原始 body** 計算簽章，不要對反序列化後的物件重新序列化（格式只要差一個空白，簽章就對不上）
- 比對簽章用 `CryptographicOperations.FixedTimeEquals`，不要用字串 `Equals`（會有時序攻擊風險）
- 密鑰放 user-secrets／環境變數或 `SystemParam`，不寫進 `appsettings.json`
- 驗證失敗回 401，並記錄來源 IP，**不要**把 payload 寫進日誌（可能含個資）

處理原則：

- **快速回應**：驗證後立即回 200，耗時處理交給背景（Quartz Job 定期處理、或先寫入待處理資料表）
- **冪等**：發送方會重試，以事件 ID 建唯一索引，重複事件直接略過
- 端點加入 `security-auditor` 審查範圍

## Excel 匯入

### 流程

1. Controller 以 `[FromForm]` 接收檔案（`ImportXxxRequest.ExcelFile`），檔案為空時回 400
2. `ExcelUtility.ParseXxxImportExcel(stream)` 解析成 `List<XxxImportRowDTO>`：**只取值、不驗證**，每列保留 `RowNumber`
3. Service 的 `ImportXxx(rows, accountId)` 逐列驗證與寫入，回傳 `ImportResultDTO`
4. Controller 回 `DataResponse<ImportResultDTO>`，前端依 `Errors` 顯示逐列錯誤

```csharp
public class ImportResultDTO
{
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<ImportRowErrorDTO> Errors { get; set; } = [];
}

public class ImportRowErrorDTO
{
    public int RowNumber { get; set; }        // Excel 實際列號，使用者才能對照
    public string ErrorMessage { get; set; } = string.Empty;
}
```

### 原則

- 解析器只負責「讀得到什麼」，驗證規則集中在 Service，才能統一回報逐列錯誤
- 單列錯誤不中斷整批，累積到 `Errors`；整批需要全有或全無時，在 Service 用交易並在有錯誤時回滾
- 前端匯入入口要搭配欄位格式說明（例：`ExcelFormatHint.vue` 元件），欄位順序與解析器一致
- Excel 套件沿用專案設定

## SignalR 進度通知

長時間的批次作業（例：批次轉單）用 SignalR 推送進度。作法是讓 **Service 不依賴 SignalR**：

```csharp
// Service：只接收回呼，不知道 SignalR 的存在
Task<BatchResultDTO> BatchCreateFromOrder(BatchDTO data, Guid accountId, Action<int, int>? progressCallback = null);

// Controller：把回呼接到 ProgressNotifierHelper
var result = await salesOrderService.BatchCreateFromOrder(data, accountId,
    progressCallback: (current, total) =>
    {
        // 推播失敗不應影響作業本身，故不等待結果
        _ = progressNotifierHelper.SendProgressUpdateAsync(progressKey, current, total);
    });
```

- Hub 放 `API/SignalR/`，加 `[Authorize]`；群組依 JWT 中的帳號 Id 建立（`account:{accountId}`），**不信任前端傳入的帳號**
- `ProgressStateStore` 記錄每個帳號執行中的作業：重複啟動時回 409，使用者重新整理頁面時能補送目前進度
- 作業結束（成功或失敗）一律在 `finally` 解除執行中狀態，否則該帳號無法再啟動
- 端點：`app.MapHub<ProgressHub>("/progresshub").RequireCors("AllowSpecificOrigins")`
