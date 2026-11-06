# __PROJECT_NAME__（後端）

@AGENTS.md

## 專案資訊

- 模式：__MODE__（API：前後端分離／Web：MVC + Razor）
- 對應前端專案：__FRONTEND_DIR__（Web 模式或無前端時刪除此行）
- 架構慣例：四層後端架構（Domain / Infrastructure / Service / __OUTER_LAYER__），規則摘要在 `AGENTS.md`，完整說明見：
  - 分層、資料夾、命名：`~/.claude/skills/new-backend-project/references/architecture.md`
  - Program.cs、DI、回應格式、例外處理、設定檔：`~/.claude/skills/new-backend-project/references/conventions.md`
  - Web 模式專屬：`~/.claude/skills/new-backend-project/references/web-mvc.md`
- CancellationToken：__CANCELLATION_TOKEN__
  - 不採用：本專案不使用 `CancellationToken`，新程式碼不要加入，審查時不視為問題
  - 採用：本專案使用 `CancellationToken`。`IUnitOfWork`、Service 方法、Controller action 都要帶 `CancellationToken cancellationToken` 並一路傳遞到 EF 非同步方法，新程式碼必須遵守

## 常用指令

```bash
dotnet build                                   # 必須零錯誤零警告
dotnet test
dotnet run --project src/__PROJECT_NAME__.__OUTER_LAYER__

# 新增 Migration（只用指令產生，不要手改 migration 與 snapshot）
dotnet ef migrations add <MigrationName> --project src/__PROJECT_NAME__.Infrastructure --startup-project src/__PROJECT_NAME__.__OUTER_LAYER__ --output-dir Data/Migrations

# 設定開發用機密（不寫進 appsettings.json）
dotnet user-secrets set "AppSettings:ConnectionString" "<連線字串>" --project src/__PROJECT_NAME__.__OUTER_LAYER__
```

## 驗證（宣告完成前必須通過）

1. `dotnet build` 零錯誤零警告
2. `dotnet test` 全數通過
