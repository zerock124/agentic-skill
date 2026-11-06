# 個人 Claude Code Skills

存放我使用的 Claude Code 個人通用規範、skill 與 sub agent 定義，讓每個新專案從第一天起就遵循同一套 housewoo 架構慣例。

**本 repo 是唯一的正本**。`~/.claude/` 底下的檔案是從這裡複製過去的安裝版本，修改時請改這裡再重新安裝（見[維護方式](#維護方式)）。

## 內容

```
skills/
  README.md
  global/CLAUDE.md            個人通用規範           → 安裝到 ~/.claude/CLAUDE.md
  agents/                     sub agent 定義（9 個）→ 安裝到 ~/.claude/agents/
  new-fullstack-project/      全端新專案總控流程      → 安裝到 ~/.claude/skills/
  new-backend-project/        .NET 後端骨架           → 安裝到 ~/.claude/skills/
  new-frontend-project/       Vue 前端骨架            → 安裝到 ~/.claude/skills/
  dotnet-ddd-architecture/    舊版 DDD 架構 skill（未安裝，見文末說明）
```

| Skill | 用途 | 什麼時候用 |
|---|---|---|
| `new-fullstack-project` | 依固定流程派工給 sub agents，從需求確認一路做到測試、審查、文件 | 從零開始一套前後端分離的系統 |
| `new-backend-project` | 用 `dotnet new` 產生骨架，再套用 housewoo 四層架構 | 只要後端，或 MVC + Razor 不分離的專案 |
| `new-frontend-project` | 用 `npm create vite` 產生骨架，再套用 Tailwind v4、vuex、orval | 只要前端，後端已存在 |

## 安裝位置

| 來源 | 安裝到 | 效果 |
|---|---|---|
| `global/CLAUDE.md` | `~/.claude/CLAUDE.md` | 每個 session 都會載入的個人通用規範（溝通語言、變更原則、編碼、安全、驗證、Git、技術偏好）。專案的 `CLAUDE.md` 優先於它 |
| `new-*-project/` | `~/.claude/skills/<skill 名稱>/` | 在**任何目錄**開 Claude Code 都能使用 |
| `agents/*.md` | `~/.claude/agents/` | 所有專案共用；個別專案可在 `<專案>/.claude/agents/` 放同名檔案覆蓋 |

> Windows 上 `~` 為 `%USERPROFILE%`，例如 `C:\Users\<user>\.claude\`。
>
> **不要**把這些 skill 放到個別專案的 `.claude/skills/`。專案層級的同名 skill 會蓋過使用者層級的版本，而且很容易變成沒有同步的舊版。

### 安裝／更新指令

PowerShell（在本 repo 根目錄執行）：

```powershell
$claude = Join-Path $env:USERPROFILE '.claude'

# Skills（整個資料夾覆蓋，避免殘留已刪除的檔案）
foreach ($s in 'new-backend-project', 'new-frontend-project', 'new-fullstack-project') {
    $dest = Join-Path $claude "skills\$s"
    if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
    Copy-Item ".\$s" $dest -Recurse
}

# Agents 與全域 CLAUDE.md（先備份現有版本）
$backup = Join-Path $claude ("backups\" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force (Join-Path $backup 'agents'), (Join-Path $claude 'agents') | Out-Null
Copy-Item (Join-Path $claude 'agents\*.md') (Join-Path $backup 'agents') -ErrorAction SilentlyContinue
if (Test-Path (Join-Path $claude 'CLAUDE.md')) { Copy-Item (Join-Path $claude 'CLAUDE.md') $backup }
Copy-Item '.\agents\*.md' (Join-Path $claude 'agents') -Force
Copy-Item '.\global\CLAUDE.md' (Join-Path $claude 'CLAUDE.md') -Force
```

Git Bash：

```bash
for s in new-backend-project new-frontend-project new-fullstack-project; do
  rm -rf ~/.claude/skills/$s && cp -r "$s" ~/.claude/skills/
done
backup=~/.claude/backups/$(date +%Y%m%d-%H%M%S)
mkdir -p "$backup/agents" ~/.claude/agents
cp ~/.claude/agents/*.md "$backup/agents/" 2>/dev/null
[ -f ~/.claude/CLAUDE.md ] && cp ~/.claude/CLAUDE.md "$backup/"
cp agents/*.md ~/.claude/agents/
cp global/CLAUDE.md ~/.claude/CLAUDE.md
```

安裝後**重新開啟 Claude Code session**，agent 定義與全域 CLAUDE.md 才會生效。可在 Claude Code 輸入 `/agents` 確認清單。

## 使用方式

### 建立全端新專案（建議）

在 Claude Code 中直接描述需求即可觸發，或輸入 `/new-fullstack-project`：

```
我要開一個新專案，.NET 10 Web API + Vue 3 Vite TypeScript，用來管理……
```

流程（每個階段結束都會停下來等你確認）：

| Phase | 執行者 | 內容 |
|---|---|---|
| 0 | 主 agent | 先用文字問名稱、路徑、業務模組，再一次問完資料庫、後端選配、前端選配、**CancellationToken** |
| 1 | `architect` | 產出計畫：API 契約、資料模型、前端頁面、任務拆解、風險與待確認事項 |
| 2 | 主 agent | 執行 `new-backend-project` 建立後端骨架 |
| 3 | `db-specialist` | Entity、`EntityDbContext` 設定、初始 Migration |
| 4 | `backend-dev` | 依 API 契約實作 Controller / Service / DTO |
| 5 | 主 agent | 執行 `new-frontend-project`，從後端 Swagger 產生 API client |
| 6 | `frontend-dev` | 清單頁、編輯頁、路由、串接 |
| 7 | `test-engineer` | 後端 Service 單元測試（Fake）與 Repository 測試（SQLite in-memory） |
| 8 | `code-reviewer`（+ `security-auditor`） | 審查；有 JWT、個資、檔案上傳時加上資安審查 |
| 9 | `devops-engineer` / `docs-writer` | Docker（有勾選才做）、README、ADR |
| 10 | 主 agent | 全部重新驗證，詢問是否 `git init` 與首次 commit |

骨架由主 agent 執行，因為 sub agent 沒有 Skill 工具。派工內容使用 `new-fullstack-project/references/briefs.md` 的範本，確保 sub agent 拿到完整需求、絕對路徑與 API 契約。

產生的目錄結構：

```
<workspace>/
  CLAUDE.md                ← 說明兩個 repo 的關係
  <name>-api/              ← 後端（獨立 git repo），含 CLAUDE.md + AGENTS.md
    docs/initial-plan.md   ← architect 計畫定稿
    docs/adr/0001-*.md
  <name>-web/              ← 前端（獨立 git repo），含 CLAUDE.md + AGENTS.md
```

### 只建後端或只建前端

輸入 `/new-backend-project` 或 `/new-frontend-project`，或直接說「建立新的後端 API 專案」。分離模式先建後端，因為前端的 API 型別要從後端 Swagger 產生。

### 日常開發（專案建好之後）

不需要呼叫 skill。直接描述任務，主 agent 會依任務類型派給對應的 agent，各 agent 會先讀專案的 `CLAUDE.md` 與 `AGENTS.md`。

## Sub Agents

| Agent | 角色 | 能否改檔 |
|---|---|---|
| `architect` | 拆解需求、定義 API 契約、技術決策、ADR 草稿 | ❌ |
| `backend-dev` | Controller、Service、DTO、Mapping、DI | ✅ |
| `db-specialist` | Entity、Fluent API、Migration、索引、查詢效能 | ✅ |
| `frontend-dev` | Vue 頁面、元件、composable、vuex、路由、Tailwind | ✅ |
| `test-engineer` | xUnit（Fake + SQLite in-memory）；前端已有測試框架時才寫前端測試 | ✅ 只改測試 |
| `devops-engineer` | Dockerfile、docker-compose、GitHub Actions | ✅ |
| `docs-writer` | README、ADR、Swagger XML 註解、CHANGELOG | ✅ 只改文件 |
| `code-reviewer` | 依 housewoo 慣例審查變更 | ❌ |
| `security-auditor` | 驗證授權、機密值、相依套件弱點 | ❌ |

每個 agent 開工前依序讀取：

1. 專案根目錄的 `CLAUDE.md` 與 `AGENTS.md`（Read 不會展開 `@AGENTS.md`，所以兩份都讀）
2. `~/.claude/skills/new-backend-project/references/` 與 `~/.claude/skills/new-frontend-project/references/`

專案既有寫法與文件衝突時，以專案為準。

## 新專案的規範檔

骨架 skill 會在每個專案根目錄產生兩份檔案：

| 檔案 | 內容 | 為什麼 |
|---|---|---|
| `AGENTS.md` | 架構規則、編碼規則（跨工具通用，Codex 等也讀得到） | 規則只維護這一份 |
| `CLAUDE.md` | 專案資訊、常用指令、驗證指令、**CancellationToken 設定**，並以 `@AGENTS.md` 匯入規則 | Claude Code 與 sub agent 只會自動載入 `CLAUDE.md` |

## housewoo 架構慣例摘要

完整內容在各 skill 的 `references/`，這裡只列重點。

### 後端（`new-backend-project/references/`）

- 四層 Domain / Infrastructure / Service / API；**Service 不參考 Infrastructure**，需要外部能力時介面放 Domain
- 型別鏈 `Request → DTO → Entity → DTO → Response`，API 層與 Service 層各一個 `<Module>Mapping : Profile`（AutoMapper）
- 泛型 `IGenericRepository<T>` + `IUnitOfWork`，提交只走 `IUnitOfWork.SaveChangesAsync()`
- 回應格式 `BaseResponse` / `DataResponse<T>` / `PaginationResponse<T>`，JSON 為 **PascalCase**，Controller 一律 `Ok(res)`
- 業務錯誤擲 `ArgumentException`(400) / `KeyNotFoundException`(404) / `InvalidOperationException`(409)，其他 500 遮蔽訊息
- 路由 `[controller]/[action]` 小寫；查詢 GET + `[FromQuery]`，寫入 POST + `[FromBody]`
- 命名：Service 方法與 action 為 `<動作><Module>`，**不加 `Async` 後綴**；Request 為 `Get<Module>ListRequest` / `Create<Module>Request` / `Update<Module>Request` / `Delete<Module>Request`，Response 為 `<Module>Response`
- Service 手動註冊為 Transient，不用組件掃描；機密值走 user-secrets / 環境變數
- Swagger 用 Swashbuckle（移除 `Microsoft.AspNetCore.OpenApi`，原因見 SKILL.md）

### 前端（`new-frontend-project/references/`）

- Vue 3 `<script setup lang="ts">`、vuex 4 單一 store、vue-router 單檔、**不使用 UI 元件庫**
- Tailwind v4，主題寫在 `@theme`，不使用 `tailwind.config.js`
- axios + **orval** 從後端 Swagger 產生 `src/api/services/` 與 `src/api/models/`（產生物不可手改），唯一手寫的是 `orval-fetch.ts`
- 模組對應 `views/<Module>` ↔ `<Module>Controller` ↔ `api/services/<module>.ts`
- 環境變數 `VITE_API_URL`；`.env.local` 不進版控

### 建立專案時才決定的項目

| 項目 | 選項 | 記錄在 |
|---|---|---|
| CancellationToken | 不採用（與既有 housewoo 專案一致）／採用（`IUnitOfWork`、Service、Controller 一路傳遞） | 專案 `CLAUDE.md` |
| 模式 | API（前後端分離）／Web（MVC + Razor） | 專案 `CLAUDE.md` |
| 資料庫、選配功能 | 依 Step 0 問答 | 計畫與程式碼 |

## 維護方式

1. 在本 repo 修改 skill、agent 或 `global/CLAUDE.md`
2. 依[安裝／更新指令](#安裝更新指令)重新安裝到 `~/.claude/`
3. 重開 Claude Code session 驗證
4. commit（Conventional Commits，描述用繁體中文，例如 `docs: 調整 backend-dev 的命名規則`）

修改原則：

- **慣例的正本是 skill 的 `references/`**。agent 只寫重點並指向 references，不要在兩邊各寫一套
- `global/CLAUDE.md` 只放跨專案、與技術棧無關的通用規範；housewoo 專屬慣例放 skill，專案專屬資訊放專案的 `CLAUDE.md`
- 優先順序：專案 `CLAUDE.md` > `global/CLAUDE.md` > agent 預設。例如全域規範預設傳遞 `CancellationToken`，但專案 `CLAUDE.md` 寫「不採用」時以專案為準
- 慣例以既有 housewoo 專案（`housewoo-erp-api` / `housewoo-erp-web`）的現行程式碼為準；文件與程式不一致時，先確認再修正文件
- 所有檔案為 UTF-8（無 BOM），工作目錄為 CRLF（`core.autocrlf=true`）

## dotnet-ddd-architecture

較早期的 DDD / Clean Architecture skill，與 `new-backend-project` 的用途重疊，但分層與寫法不同。**目前沒有安裝到 `~/.claude/skills/`，也沒有納入這次的統一規範**，新專案請使用 `new-*-project`。是否保留或移除尚未決定。
