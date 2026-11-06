---
name: new-fullstack-project
description: 依 housewoo 慣例從零建立前後端分離的新專案（.NET 10 Web API + Vue 3 + Vite + TypeScript），並按固定流程派工給 sub agents：需求確認 → architect 規劃 → 後端骨架 → db-specialist → backend-dev → 前端骨架 → frontend-dev → test-engineer → code-reviewer / security-auditor → devops-engineer / docs-writer。當使用者要開新的全端專案、從零開始一套系統時使用。只需要單邊骨架時改用 new-backend-project 或 new-frontend-project。
---

# 建立全端新專案（housewoo 流程）

這個 skill 是**總控流程**，不重複骨架細節：後端骨架照 `new-backend-project`，前端骨架照 `new-frontend-project`，這裡負責「順序、派工內容、階段確認點」。

## 運作原則

1. **每個階段結束都停下來**，簡短回報成果並用 AskUserQuestion 詢問是否進入下一階段（選項至少包含「繼續」「先修改」）。使用者明確說「後面不用停」才可連續執行
2. **骨架由主 agent 親自執行**。sub agent 沒有 Skill 工具，無法呼叫 `new-backend-project` / `new-frontend-project`
3. **派工一律使用 `references/briefs.md` 的範本**。sub agent 看不到主對話，範本裡的需求、絕對路徑、API 契約、驗證指令都要填完整，不要只寫「照計畫做」
4. **API 契約以 architect 的計畫為準**。實作中發現契約要改，先回到主 agent 更新計畫並告知使用者，再通知前後端
5. 所有 git 操作（`git init`、commit）都要先徵得使用者同意；前後端各自是獨立 repo，commit 要在各自目錄內進行

## 目錄配置

```
<workspace>/                      例：D:\xinyu\housewoo-hub
  CLAUDE.md                       ← templates/workspace-CLAUDE.md，說明兩個 repo 的關係
  <kebab-name>-api/               ← 後端（new-backend-project，API 模式）
    docs/initial-plan.md          ← architect 計畫定稿
    docs/adr/0001-*.md            ← docs-writer 產出
  <kebab-name>-web/               ← 前端（new-frontend-project）
```

## Phase 0：需求確認（主 agent）

合併兩個骨架 skill 的 Step 0，之後執行骨架時不再重問。分兩步：

**先用文字請使用者提供**（自由輸入，不適合做成選項）：

- 系統名稱：後端 PascalCase（例：`HousewooHub`）、目錄前綴 kebab-case（例：`housewoo-hub`）、workspace 路徑
- 系統目的、第一批要做的業務模組（建議 1～3 個）、各模組的主要欄位與操作。描述太模糊時再追問一次，不要自行編造業務規則
- 後端 API 的開發與正式位址（不確定可先留空）

**再用 AskUserQuestion 一次問完四題**（工具上限為四題）：

1. 資料庫：SQL Server（預設）／ PostgreSQL ／ 暫不接
2. 後端選配（多選）：JWT 驗證、Serilog、Quartz 排程、Docker、xunit 測試專案（Swagger 為必要，orval 依賴它，不列入選項）
3. 前端選配（多選）：登入頁與 JWT 流程、多頁籤版面、側邊欄選單權限、Excel 匯出、Docker + nginx
4. CancellationToken：不採用（與既有 housewoo 專案一致）／ 採用（IUnitOfWork、Service、Controller 一路傳遞）。兩個選項並列，**不要標示推薦**

## Phase 1：架構規劃（architect）

1. 依 `references/briefs.md` 的「architect」範本派工
2. 收到計畫後，**完整呈現給使用者**，特別列出 architect 的「風險與待確認事項」
3. 使用者確認後，將定稿先暫存在 scratchpad，Phase 2 建好後端目錄後存成 `<api>/docs/initial-plan.md`

> 確認點：計畫未經使用者同意，不得進入 Phase 2。

## Phase 2：後端骨架（主 agent）

照 `new-backend-project` 的 Step 1～4 執行（API 模式，Step 0 的答案沿用 Phase 0）。另外：

- 從 `templates/workspace-CLAUDE.md` 建立 `<workspace>/CLAUDE.md`
- 後端 `CLAUDE.md` 的 `__FRONTEND_DIR__` 填 `../<kebab-name>-web`
- 此階段**不建業務模組**，只要 `dotnet build` 零錯誤零警告

## Phase 3：資料模型（db-specialist）

依「db-specialist」範本派工，內容為計畫中的「資料模型變更」：Entity、`EntityDbContext` 設定、初始 Migration。

- Migration 只用指令產生；若要套用到資料庫，**只能套用到本機或 Docker 內的開發資料庫**，而且要先問使用者
- 收到回報後，把 Migration 風險評估轉述給使用者

## Phase 4：後端 API（backend-dev）

依「backend-dev」範本派工，每個模組照 `architecture.md` 的「新增業務模組要動的 8 個位置」實作，並以計畫中的 API 契約為準。

完成後，主 agent 自己再驗證一次：

```bash
dotnet build      # 零錯誤零警告
dotnet test
```

## Phase 5：前端骨架（主 agent）

照 `new-frontend-project` 的 Step 1～5 執行，Step 0 的答案沿用 Phase 0。前端 `CLAUDE.md` 的 `__BACKEND_DIR__` 填 `../<kebab-name>-api`。

取得 `swagger.json`：

1. 背景啟動後端：`dotnet run --project src/<Name>.API`（若 `MigrateDatabase()` 會在啟動時連 DB，而本機 DB 還沒好，先暫時停用或設定好連線）
2. 下載 `/swagger/v1/swagger.json` 存到 `src/api/swagger.json`（實際網址以 `launchSettings.json` 為準）
3. `npm run generate:api`，接著 `npm run build` 必須通過
4. 停止後端程序

## Phase 6：前端頁面（frontend-dev）

依「frontend-dev」範本派工。每個模組建清單頁與編輯頁、加路由、串接 orval 產生的 service。

> 可並行：Phase 6 進行時，可以同時派 Phase 7 的**後端測試**部分（兩者不會改到同一批檔案）。

## Phase 7：測試（test-engineer）

依「test-engineer」範本派工。

- 後端：Service 單元測試（手寫 Fake repository），以及 Repository / UnitOfWork 測試（SQLite in-memory）
- 前端：housewoo 前端骨架**預設沒有測試框架**。test-engineer 只回報建議，不自行安裝 Vitest；要不要加由使用者決定

test-engineer 回報的產品 bug，交回 backend-dev / frontend-dev 修正後再跑一次測試。

## Phase 8：審查（code-reviewer + security-auditor）

- 一律派 `code-reviewer`
- 有 JWT 驗證、登入、帳號權限、個資或檔案上傳時，**同時**派 `security-auditor`（兩者可並行）
- 新專案通常還沒有 git 歷史，`git diff` 會是空的。範本已註明改為審查整個 `src/`

處理方式：🔴 嚴重與 Critical / High 必須修（交給對應的 dev agent），修完再請 code-reviewer 複查**一次**；🟡 以下列給使用者決定。

## Phase 9：部署設定與文件（devops-engineer / docs-writer）

- Phase 0 有勾選 Docker 才派 `devops-engineer`：前後端 Dockerfile、`docker-compose.yml`（含 MSSQL）、`.env.example`
- 派 `docs-writer`：更新兩邊的 `README.md`，並把 `initial-plan.md` 的設計決策整理成 `docs/adr/0001-initial-architecture.md`

## Phase 10：收尾（主 agent）

1. 兩邊各自再跑一次驗證：後端 `dotnet build` + `dotnet test`；前端 `npm run build` + `npm audit`
2. 確認三份 `CLAUDE.md`（workspace、api、web）都沒有殘留 `__` 佔位符
3. 回報：建立了哪些專案與模組、各階段的驗證結果、未完成項目、需要使用者決定的事（例如前端是否加測試框架）
4. 詢問是否要 `git init` 並建立首次 commit（各 repo 分開，訊息用 Conventional Commits，例如 `chore: 建立專案骨架`）
