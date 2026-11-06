# 派工範本

sub agent 看不到主對話。派工前把 `{{...}}` 全部替換成實際內容，**路徑一律用絕對路徑**。

`{{SKILLS}}` 是使用者層級 skill 目錄的絕對路徑，例如 `C:\Users\<user>\.claude\skills`。

每份派工都要附上這段共用區塊：

```
## 慣例來源（開工前必讀）
- 專案規則：{{PROJECT_DIR}}\CLAUDE.md 與 {{PROJECT_DIR}}\AGENTS.md
- 後端架構：{{SKILLS}}\new-backend-project\references\architecture.md
- 後端慣例：{{SKILLS}}\new-backend-project\references\conventions.md
- 前端慣例：{{SKILLS}}\new-frontend-project\references\conventions.md
- 架構計畫：{{API_DIR}}\docs\initial-plan.md（Phase 2 之前尚未存檔，改由下方「計畫內容」提供）
專案既有寫法與上述文件衝突時，以專案為準並在回報中指出。
```

---

## architect

```
## 任務
為新系統「{{SYSTEM_NAME}}」做初始架構規劃。目前尚無程式碼，請依 housewoo 慣例規劃，不要引入慣例外的架構或套件。

## 需求
- 系統目的：{{PURPOSE}}
- 第一批業務模組：{{MODULES_WITH_FIELDS_AND_OPERATIONS}}
- 資料庫：{{DB}}
- CancellationToken：{{CANCELLATION_TOKEN}}   ← 採用／不採用，API 契約與任務拆解要依此規劃
- 後端選配：{{BACKEND_OPTIONS}}
- 前端選配：{{FRONTEND_OPTIONS}}

## 預定目錄
- 後端：{{API_DIR}}（方案 {{Name}}，專案 {{Name}}.Domain / .Infrastructure / .Service / .API）
- 前端：{{WEB_DIR}}

{{共用區塊}}

## 產出要求
依你的標準輸出格式，另外：
- API 契約要列出每個 Controller action 的 HTTP 方法、路由、Request / Response 型別與 Data 的欄位
- 資料模型要列出每個 Entity 的欄位、型別、長度、是否必填、索引、關聯與刪除行為
- 前端要列出 views 清單與路由
- 「風險與待確認事項」不可省略；需求不足之處列成問題，不要自行假設業務規則
```

## db-specialist

```
## 任務
在 {{API_DIR}} 建立以下資料模型，並產生初始 Migration。

## 計畫內容（資料模型）
{{DATA_MODEL_SECTION}}

{{共用區塊}}

## 注意
- DbContext：{{API_DIR}}\src\{{Name}}.Infrastructure\Data\EntityDbContext.cs
- Migration 指令：
  dotnet ef migrations add InitialCreate --project src/{{Name}}.Infrastructure --startup-project src/{{Name}}.API --output-dir Data/Migrations
- {{DB_APPLY_INSTRUCTION}}   ← 例：「不要套用到資料庫」或「可套用到本機 LocalDB：(連線字串由 user-secrets 提供)」
- 完成後執行 dotnet build（零錯誤零警告）
```

## backend-dev

```
## 任務
在 {{API_DIR}} 實作以下業務模組的 API：{{MODULE_LIST}}

## API 契約（以此為準，不可自行更動；需要變更時回報，不要直接改）
{{API_CONTRACT_TABLE}}

## 已完成的前置工作
- Entity 與 EntityDbContext 已由 db-specialist 建立：{{ENTITY_FILES}}

{{共用區塊}}

## 注意
- 每個模組照 architecture.md 的「新增業務模組要動的 8 個位置」，缺一不可
- 新 Service 要在 Extensions/ServiceCollectionExtensions.cs 手動註冊
- CancellationToken：{{CANCELLATION_TOKEN}}（以專案 CLAUDE.md 為準）
- 命名：Service 方法與 Controller action 為 <動作><Module>，不加 Async 後綴（見 architecture.md 命名規則）
- Entity 有缺漏或需要調整時，回報給主 agent，不要自己改 Entity 或 Migration
- 驗證：dotnet build（零錯誤零警告）、dotnet test
```

## frontend-dev

```
## 任務
在 {{WEB_DIR}} 實作以下模組的頁面：{{MODULE_LIST}}

## 頁面與路由（以計畫為準）
{{VIEWS_AND_ROUTES}}

## API
- orval 已產生：{{WEB_DIR}}\src\api\services\ 與 {{WEB_DIR}}\src\api\models\（產生物，不可手改）
- 對應的後端契約：
{{API_CONTRACT_TABLE}}

{{共用區塊}}

## 注意
- 模組命名對應：views/<Module> ↔ <Module>Controller ↔ api/services/<module>.ts
- 不引入 UI 元件庫或其他新套件；確實需要時回報理由
- 驗證：npm run build（含 vue-tsc 型別檢查）
```

## test-engineer

```
## 任務
為 {{TARGET}} 撰寫並執行測試。   ← 例：「{{API_DIR}} 的 CategoryService、ProductService」

## 要涵蓋的行為
{{BEHAVIORS}}   ← 從計畫的 API 契約與業務規則整理：正常路徑、查無資料(404)、重複資料(409)、輸入錯誤(400)

{{共用區塊}}

## 注意
- 測試專案：{{API_DIR}}\tests\{{Name}}.Tests（參考 Service + Domain）
- Service 單元測試用手寫 Fake 實作 IGenericRepository<T> / IUnitOfWork，不啟動 EF Core
- 需要驗證 EF 行為（Repository、UnitOfWork、交易）時用 SQLite in-memory，不用 EF InMemory provider
- 前端目前沒有測試框架：不要自行安裝，只在回報中提出建議
- 發現產品 bug 時不要修改產品程式碼，依格式回報
```

## code-reviewer

```
## 任務
審查新專案的初版程式碼：{{API_DIR}} 與 {{WEB_DIR}}

## 範圍
專案尚無 git 歷史（或只有骨架 commit），git diff 不代表全部變更。請審查：
- 後端：{{API_DIR}}\src\ 全部（略過 Migrations 與 obj/bin）
- 前端：{{WEB_DIR}}\src\（略過 src\api\services\ 與 src\api\models\ 這兩個 orval 產生物）

{{共用區塊}}

## 重點
- 是否遵守 housewoo 架構：分層參考方向、型別轉換鏈、回應格式、例外映射、DI 手動註冊
- 實作是否與 API 契約一致：
{{API_CONTRACT_TABLE}}
```

## security-auditor

```
## 任務
上線前的初版資安審查：{{API_DIR}} 與 {{WEB_DIR}}

## 背景
- 驗證方式：{{AUTH}}   ← 例：JWT Bearer，token 存在 localStorage（housewoo 既有設計）
- 涉及的敏感資料：{{SENSITIVE_DATA}}

{{共用區塊}}

## 重點
- appsettings*.json 與版控中沒有明碼連線字串或 JWT SecretKey
- CORS 來源、JWT 驗證參數、未預期例外是否遮蔽訊息
- 執行 dotnet list package --vulnerable --include-transitive 與 npm audit
```

## devops-engineer

```
## 任務
為 {{API_DIR}} 與 {{WEB_DIR}} 建立 Docker 部署設定：{{SCOPE}}   ← 例：兩邊的 Dockerfile、workspace 層 docker-compose.yml（api / web / db）、.env.example

{{共用區塊}}

## 注意
- 後端方案檔：{{API_DIR}}\{{Name}}.slnx，啟動專案 src\{{Name}}.API
- 前端環境變數為 VITE_API_URL（建置時注入，不可放密鑰）
- 機密值用環境變數注入：AppSettings__ConnectionString、JwtSetting__SecretKey
```

## docs-writer

```
## 任務
1. 更新 {{API_DIR}}\README.md 與 {{WEB_DIR}}\README.md
2. 依 {{API_DIR}}\docs\initial-plan.md 撰寫 {{API_DIR}}\docs\adr\0001-initial-architecture.md

{{共用區塊}}

## 注意
- 指令、埠號、環境變數要以實際檔案為準（launchSettings.json、.env、package.json）
- API 文件以 Swagger（Swashbuckle + XML 註解）為準，不另外手寫端點清單
```
