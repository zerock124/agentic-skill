# 個人 AI 開發 Skills

存放我使用的個人通用規範、skill 與 sub agent 定義，讓每個新專案從第一天起就遵循同一套架構慣例。

Skill 採用 Agent Skills 格式（資料夾內含 `SKILL.md`），Claude Code、OpenAI Codex、Gemini CLI、GitHub Copilot 都能讀取。各工具的支援程度不同，見[已知限制](#已知限制)。

**本 repo 是唯一的正本**。安裝位置的檔案都是從這裡複製過去的，修改時請改這裡再重新複製（見[維護方式](#維護方式)）。

## 內容

```
skills/
  README.md
  global/CLAUDE.md            個人通用規範
  agents/                     sub agent 定義（9 個，Claude Code 格式）
  new-fullstack-project/      全端新專案總控流程
  new-backend-project/        .NET 後端骨架
  new-frontend-project/       Vue 前端骨架
```

| Skill | 用途 | 什麼時候用 |
|---|---|---|
| `new-fullstack-project` | 依固定流程派工給 sub agents，從需求確認一路做到測試、審查、文件 | 從零開始一套前後端分離的系統 |
| `new-backend-project` | 用 `dotnet new` 產生骨架，再套用四層架構 | 只要後端，或 MVC + Razor 不分離的專案 |
| `new-frontend-project` | 用 `npm create vite` 產生骨架，再套用 Tailwind v4、vuex、orval | 只要前端，後端已存在 |

## 安裝

安裝方式就是**把檔案複製到所使用工具的資料夾**，不需要執行任何指令。用檔案總管或任何習慣的方式複製即可。

> `~` 代表使用者家目錄。Windows 為 `%USERPROFILE%`，例如 `C:\Users\<user>\`；macOS / Linux 為 `/Users/<user>/` 或 `/home/<user>/`。資料夾不存在時自行建立。

### 1. Skills

把 `new-backend-project/`、`new-frontend-project/`、`new-fullstack-project/` 三個資料夾**整個**複製到下表的位置（包含裡面的 `references/` 與 `templates/`，不要只複製 `SKILL.md`）。

| 工具 | 複製到 |
|---|---|
| Claude Code | `~/.claude/skills/` |
| OpenAI Codex | `~/.agents/skills/` |
| Gemini CLI | `~/.gemini/skills/` 或 `~/.agents/skills/` |
| GitHub Copilot | `~/.copilot/skills/`（也會讀 `~/.claude/skills/`、`~/.agents/skills/`） |

複製完成後的結構應為：

```
~/.claude/skills/              ← 依工具替換成上表的位置
  new-backend-project/
    SKILL.md
    references/
    templates/
  new-frontend-project/
  new-fullstack-project/
```

> 同時使用多個工具時，可以只放一份在 `~/.agents/skills/`（Codex、Gemini CLI、Copilot 共用），Claude Code 另外放 `~/.claude/skills/`。

### 2. 個人通用規範

`global/CLAUDE.md` 是每個 session 都會載入的個人規範（溝通語言、變更原則、編碼、安全、驗證、Git、技術偏好）。依工具複製並改名：

| 工具 | 複製到 |
|---|---|
| Claude Code | `~/.claude/CLAUDE.md` |
| OpenAI Codex | `~/.codex/AGENTS.md` |
| Gemini CLI | `~/.gemini/GEMINI.md` |
| GitHub Copilot | 依使用的介面（VS Code、Copilot CLI）設定個人指示，或放進專案的 `.github/copilot-instructions.md` |

目標檔案**已經存在**時，先備份，再決定要覆蓋還是把內容合併進去。專案自己的規範檔（`CLAUDE.md` / `AGENTS.md`）優先於個人規範。

### 3. Sub Agents

`agents/*.md` 是 Claude Code 的 sub agent 格式（frontmatter 含 `name`、`description`、`tools`、`model`）。

| 工具 | 作法 |
|---|---|
| Claude Code | 直接複製到 `~/.claude/agents/` |
| GitHub Copilot | 複製到 `~/.copilot/agents/`，副檔名改為 `.agent.md`；frontmatter 的 `tools`、`model` 依 Copilot 文件調整 |
| Gemini CLI | 複製到 `~/.gemini/agents/`，並在設定中啟用 subagents；frontmatter 依 Gemini CLI 文件調整 |
| OpenAI Codex | Codex 的 subagent 是 TOML 格式（`~/.codex/agents/*.toml`），無法直接使用，需要改寫 |

個別專案可在專案內的 agent 資料夾（例如 `<專案>/.claude/agents/`）放同名檔案覆蓋。

### 安裝後

- **重新開啟工具的 session**，skill、agent 與個人規範才會生效
- Claude Code 可輸入 `/agents` 確認 agent 清單，輸入 `/` 確認 skill 是否出現

### 更新

1. 先**刪除**安裝位置裡舊的 skill 資料夾，再複製新版（直接覆蓋會殘留已刪除的檔案）
2. agent 與個人規範檔先備份再覆蓋
3. 重新開啟 session

> **不要**把這些 skill 複製到個別專案的 skill 資料夾（例如 `<專案>/.claude/skills/`）。專案層級的同名 skill 會蓋過使用者層級的版本，而且很容易變成沒有同步的舊版。

### 已知限制

這套 skill 與 agent 是以 Claude Code 為主撰寫的，在其他工具使用時要注意：

- **路徑寫死為 `~/.claude/skills/...`**：agent 的「慣例來源」與產生到新專案的 `CLAUDE.md` 都以這個路徑引用 `references/`。安裝在其他位置時，要讓 AI 改讀實際的安裝路徑
- **`new-fullstack-project` 依賴 sub agent 派工**：沒有 sub agent 功能的工具無法照流程派工，只能由主 agent 依序執行各階段
- **提問方式**：skill 中「用 AskUserQuestion 一次問完」是 Claude Code 的工具；其他工具改以一般對話詢問即可

## 使用方式

### 建立全端新專案（建議）

直接描述需求即可觸發（Claude Code 也可輸入 `/new-fullstack-project`）：

```
我要開一個新專案，.NET 10 Web API + Vue 3 Vite TypeScript，用來管理……
```

需求較多時，可以先複製 `new-fullstack-project/templates/project-definition.md` 填好（基本資訊、技術選項、業務模組、非 CRUD 功能），再把路徑交給 AI：「依 D:\specs\xxx.md 建立新專案」。已填寫的項目會直接採用，只詢問空白的部分。

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

直接說「建立新的後端 API 專案」（Claude Code 也可輸入 `/new-backend-project` 或 `/new-frontend-project`）。分離模式先建後端，因為前端的 API 型別要從後端 Swagger 產生。

### 日常開發（專案建好之後）

不需要呼叫 skill。直接描述任務，主 agent 會依任務類型派給對應的 agent，各 agent 會先讀專案的 `CLAUDE.md` 與 `AGENTS.md`。

後端新增功能時，agent 依 `new-backend-project/references/` 的這幾份文件工作：

| 文件 | 內容 |
|---|---|
| `feature-workflow.md` | 新增功能的工作順序、功能類型判斷、驗證要放哪一層、**必做清單**、交付格式 |
| `integrations.md` | Quartz 排程、外部 API Adapter、Webhook、Excel 匯入、SignalR 進度通知的分層與寫法 |
| `database.md` | Code First / Database First、Migration 指令、Seed、常見錯誤 |

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
| `code-reviewer` | 依架構慣例審查變更 | ❌ |
| `security-auditor` | 驗證授權、機密值、相依套件弱點 | ❌ |

每個 agent 開工前依序讀取：

1. 專案根目錄的 `CLAUDE.md` 與 `AGENTS.md`（Read 不會展開 `@AGENTS.md`，所以兩份都讀）
2. `~/.claude/skills/new-backend-project/references/` 與 `~/.claude/skills/new-frontend-project/references/`

專案既有寫法與文件衝突時，以專案為準。

## 新專案的規範檔

骨架 skill 會在每個專案根目錄產生兩份檔案：

| 檔案 | 內容 | 為什麼 |
|---|---|---|
| `AGENTS.md` | 架構規則、編碼規則（跨工具通用，Codex、Copilot 等也讀得到） | 規則只維護這一份 |
| `CLAUDE.md` | 專案資訊、常用指令、驗證指令、**CancellationToken 設定**，並以 `@AGENTS.md` 匯入規則 | Claude Code 與 sub agent 只會自動載入 `CLAUDE.md` |

## 架構慣例摘要

完整內容在各 skill 的 `references/`，這裡只列重點。

### 後端（`new-backend-project/references/`）

- 四層 Domain / Infrastructure / Service / API；**Service 不參考 Infrastructure**，需要外部能力時介面放 Domain
- 型別鏈 `Request → DTO → Entity → DTO → Response`，API 層與 Service 層各一個 `<Module>Mapping : Profile`（AutoMapper）
- 泛型 `IGenericRepository<T>` + `IUnitOfWork`，提交只走 `IUnitOfWork.SaveChangesAsync()`
- 回應格式 `BaseResponse` / `DataResponse<T>` / `PaginationResponse<T>`，JSON 為 **PascalCase**，Controller 一律 `Ok(res)`
- 業務錯誤透過 `ServiceExceptionUtility` 擲 `ArgumentException`(400) / `KeyNotFoundException`(404) / `InvalidOperationException`(409)，需要時加 `ForbiddenException`(403)、`ExternalApiException`(502)，其他 500 遮蔽訊息；不用 DataAnnotations 驗證、不回傳 `bool`
- static 純函式叫 `<Name>Utility` 放 `Utilities/`；可注入的類別叫 `<Name>Helper` 放 `Helpers/`
- 非 CRUD：Quartz Job 在 `API/Jobs`、外部 API 為 Domain 介面加 `Infrastructure/Adapters` 實作、流程協調在 `Service/Implement/Orchestration`
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
| CancellationToken | 不採用（`IUnitOfWork`、Service、Controller 都不帶）／採用（`IUnitOfWork`、Service、Controller 一路傳遞） | 專案 `CLAUDE.md` |
| 模式 | API（前後端分離）／Web（MVC + Razor） | 專案 `CLAUDE.md` |
| 資料庫策略 | Code First（預設）／Database First（既有資料表） | 專案 `CLAUDE.md` |
| 資料庫、選配功能 | 依 Step 0 問答 | 計畫與程式碼 |

## 維護方式

1. 在本 repo 修改 skill、agent 或 `global/CLAUDE.md`
2. 依[更新](#更新)步驟重新複製到安裝位置
3. 重新開啟 session 驗證
4. commit（Conventional Commits，描述用繁體中文，例如 `docs: 調整 backend-dev 的命名規則`）

修改原則：

- **慣例的正本是 skill 的 `references/`**。agent 只寫重點並指向 references，不要在兩邊各寫一套
- `global/CLAUDE.md` 只放跨專案、與技術棧無關的通用規範；架構慣例放 skill，專案專屬資訊放專案的 `CLAUDE.md`
- 優先順序：專案 `CLAUDE.md` > `global/CLAUDE.md` > agent 預設。例如全域規範預設傳遞 `CancellationToken`，但專案 `CLAUDE.md` 寫「不採用」時以專案為準
- skill 與 agent 內容**不可提及特定既有專案或公司名稱**，也不可寫「與既有專案一致」這類需要外部脈絡的說法：新專案的 session 只看得到 skill 與專案自己的檔案，慣例與理由必須在 references 中寫完整
- 所有檔案為 UTF-8（無 BOM），工作目錄為 CRLF（`core.autocrlf=true`）
