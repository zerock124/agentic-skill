# 資料庫操作指引

預設採用 **Code First**。只有在資料表已經存在、而且由 DBA 或其他系統維護時才用 Database First。兩種模式不要混用；選定後寫進專案 `CLAUDE.md`。

| 情境 | 策略 | 原因 |
|---|---|---|
| 全新專案 | Code First（預設） | 結構由程式碼控制，Migration 留下完整變更歷史 |
| 串接既有資料庫、結構由他人維護 | Database First | 不重複定義，資料表變更後重新 scaffold |
| 只讀取舊系統資料（例：資料轉移） | 另建一個唯讀 DbContext 或用 ADO.NET | 不要讓舊資料表進入主 DbContext 的 Migration |

## Code First

### Entity 與設定

- Entity 放 `Domain/Entities/`，單數命名，**不加 Data Annotations**
- 設定集中在 `Infrastructure/Data/EntityDbContext.cs` 的 `OnModelCreating`，每張表一個 `modelBuilder.Entity<T>(entity => { ... })` 區塊
- 每張表 `ToTable(t => t.HasComment("..."))`、每個欄位 `.HasComment("...")`
- GUID 主鍵 `.HasDefaultValueSql("newid()")`；字串 `HasMaxLength`；金額 `decimal(18,2)`；軟刪除 `IsDeleted` 搭配篩選唯一索引
- 詳細規則見 db-specialist agent 的「資料庫規範」

### Migration 指令

在後端專案根目錄執行（`<Name>` 為方案名稱）：

```bash
# 共用參數
EF="--project src/<Name>.Infrastructure --startup-project src/<Name>.API"

dotnet ef migrations add <描述性名稱> $EF --output-dir Data/Migrations   # 新增
dotnet ef migrations list $EF                                             # 列出全部與套用狀態
dotnet ef migrations script --idempotent $EF -o migration.sql             # 產生 SQL 供審查（不執行）
dotnet ef migrations script <起點Migration> $EF -o migration.sql          # 從某個 Migration 之後到最新
dotnet ef migrations remove $EF                                           # 移除最後一個（僅限尚未套用）
dotnet ef database update $EF                                             # 套用到資料庫（只限本機）
dotnet ef database update <目標Migration> $EF                             # 回滾到指定版本（只限本機）
```

PowerShell 沒有 `$EF` 這種字串展開，請直接把參數寫完整。

規則：

- Migration 只用指令產生，**不要手改** migration 與 snapshot
- `migrations remove` 只能用在尚未套用的 Migration；已套用到共用環境的，改用新的 Migration 修正
- `database update` 只能對本機或 Docker 中的開發資料庫執行，**絕不**對正式或共用環境執行
- 正式環境用 `migrations script --idempotent` 產生的 SQL，或啟動時的 `MigrateDatabase()`（骨架的選配作法）。採用自動 migrate 時，部署前要先審查這次會套用的 SQL

### 初始資料（Seed）

Seed 寫在 `API/Extensions/DatabaseSeedExtensions.cs`，以 `WebApplication` 擴充方法植入，啟動時於 `MigrateDatabase()` 之後呼叫：

```csharp
public static WebApplication SeedMenuData(this WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<EntityDbContext>();
        if (!context.Database.CanConnect())
        {
            logger.LogWarning("Database is not available; menu seed skipped");
            return app;
        }

        // 以穩定代碼判斷是否已存在，不要用可被使用者修改的名稱
        // ...不存在才新增，已存在則補齊缺漏欄位...
        context.SaveChanges();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Menu seed failed");
    }

    return app;
}
```

- 必須**可重複執行**（冪等）：先查再新增，不要每次啟動都插入
- 用穩定的代碼（例：`RoleCode`）判斷，不用顯示名稱
- 預設帳號的密碼不可寫死在程式碼；由設定或首次啟動流程提供
- 不使用 `HasData()`：它會把資料寫進 Migration，之後改資料就要產生新的 Migration

## Database First

### 初次 scaffold

```bash
# 在後端專案根目錄執行；連線字串從 user-secrets 讀取，不要直接寫在指令裡
dotnet ef dbcontext scaffold "Name=AppSettings:ConnectionString" Microsoft.EntityFrameworkCore.SqlServer \
  --project src/<Name>.Infrastructure --startup-project src/<Name>.API \
  --output-dir ../<Name>.Domain/Entities --namespace <Name>.Domain.Entities \
  --context-dir Data --context EntityDbContext --context-namespace <Name>.Infrastructure.Data \
  --no-onconfiguring --force
```

- `Name=AppSettings:ConnectionString` 會從設定（含 user-secrets）讀取，避免連線字串留在終端機歷史
- `--no-onconfiguring`：不產生內含連線字串的 `OnConfiguring`，改由 DI 註冊
- 只產生特定資料表：加 `--table Product --table Category`

### scaffold 之後

1. scaffold 產生的檔案**每次重新 scaffold 都會被覆蓋**，不要直接修改
2. 需要擴充 Entity（計算屬性、方法）時，用 `partial class` 寫在另一個檔案
3. 需要額外的 Fluent API 設定時，寫在 `EntityDbContext` 的 `partial void OnModelCreatingPartial(ModelBuilder modelBuilder)`（放在另一個 partial 檔案）
4. 資料表變更後重新執行 scaffold（加 `--force`），再跑 `dotnet build` 檢查受影響的 Service 與 Mapping
5. Database First 不產生 Migration；資料庫結構變更由 DBA 的 SQL 腳本負責

## 常見問題

| 錯誤 | 原因與處理 |
|---|---|
| `Unable to find provider assembly` | Infrastructure 缺 `Microsoft.EntityFrameworkCore.SqlServer`，或啟動專案缺 `Microsoft.EntityFrameworkCore.Design` |
| `Unable to create an object of type 'EntityDbContext'` | 設計階段建不出 DbContext：確認啟動專案有在 DI 註冊 `EntityDbContext`，且 user-secrets 有連線字串。仍不行再加 `IDesignTimeDbContextFactory<EntityDbContext>`，連線字串同樣從設定讀取，不要寫死 |
| scaffold 後中文註解變亂碼 | 確認資料庫欄位描述（`MS_Description`）本身正確，並以 UTF-8 儲存產生的檔案 |
| 測試（SQLite in-memory）建表失敗 | SQL Server 專屬語法（`newid()`、`N''` 篩選條件、定序）需要在測試用的 DbContext 工廠中處理 |
