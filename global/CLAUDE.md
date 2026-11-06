# 個人通用規範（使用者層級）

> 位置：`~/.claude/CLAUDE.md`，套用於所有專案。
> **優先順序**：專案的 `CLAUDE.md` > 本檔案 > `~/.claude/agents/` 內各 agent 的預設慣例。
> 專案已有的慣例（路由風格、回應格式、狀態管理、測試方式等）與本檔或 agent 預設不同時，一律以專案為準。

## 溝通
- 回覆、文件、程式碼註解使用台灣繁體中文；程式碼識別字使用英文
- 技術名詞保留英文（例：Migration、DTO、composable）
- 回報時說明：改了哪些檔案、做了什麼、如何驗證、還有什麼未完成或需要我決定

## 開始動手前
- 先讀專案的 `CLAUDE.md`、`README.md` 與 `docs/` 中相關文件
- 修改前先閱讀相鄰的既有程式碼，沿用相同的命名、分層與寫法；不要引入專案中沒有的新模式
- 需求不明確、或有多種合理做法且影響範圍大時，先提出問題或選項，不要自行假設後大幅修改
- 不確定的事實（指令、設定、API 行為）先查證程式碼或文件，不要猜測

## 變更原則
- 只改與任務相關的部分；不順手重構、不改無關檔案的格式
- 不隨意新增套件（npm / NuGet）；確實需要時先說明理由與替代方案
- **自動產生的程式碼不可手改**（例：OpenAPI / Orval 產生的 client、EF Migration snapshot），改用產生指令重新產生
- 刪除檔案、刪除資料、改資料庫結構、改 CI/CD 等高風險操作前，先說明影響並取得確認

## 文字編碼
- 既有檔案保留原編碼、BOM 與換行格式（CRLF / LF），不要因為寫入而改變
- 寫入含中文的檔案後，確認沒有變成亂碼或 `?`

## 安全
- 密鑰、密碼、連線字串、Token 不得寫入程式碼或新增進版控
  - .NET 開發環境用 User Secrets；前端用 `.env.local`（並確認在 `.gitignore` 中）
- 前端 `VITE_*` 環境變數會被打包進公開 JS，不可放任何密鑰
- 日誌不得記錄密碼、Token 與個資
- 不對正式或共用環境執行 `dotnet ef database update`、部署或任何破壞性指令

## 驗證
- 宣告完成前，必須執行專案 `CLAUDE.md` 指定的驗證指令並通過（例：build、test、type-check、lint）
- 若專案未指定，預設：後端 `dotnet build` + `dotnet test`；前端 `npm run build`
- 驗證失敗不要隱藏或略過；說明失敗原因，若與本次變更無關也要明確指出

## Git
- Commit 訊息使用 Conventional Commits，描述用繁體中文
  - `feat:` `fix:` `refactor:` `test:` `docs:` `chore:` `perf:` `ci:`
  - 例：`feat: 新增採購單 Excel 匯入`
- 一個 commit 只做一件事
- 未經要求不要 commit、push、建立分支或 PR
- 多 repo 的工作區，commit 要在各自的 repo 目錄內進行

## 技術偏好（專案未規定時的預設值）

### .NET
- 啟用 nullable，正確處理 null；async 一路到底並傳遞 `CancellationToken`，禁止 `.Result` / `.Wait()`
- Controller 不直接使用 DbContext，透過 Service 層
- 不直接回傳 Entity，使用 DTO / ViewModel
- 唯讀查詢加 `AsNoTracking()`，注意 N+1
- 結構化日誌：`logger.LogInformation("Order {OrderId} created", id)`，不用字串內插
- 設定值使用 Options pattern

### Vue / TypeScript
- `<script setup lang="ts">` + Composition API
- 禁止 `any`；props / emits 給明確型別
- 邏輯過長或可重用時抽成 composable
- 不在元件內寫死 API URL，統一透過 API client
- 不使用 `v-html` 渲染未消毒內容

## Sub Agents
- 可用 agent 定義於 `~/.claude/agents/`；各專案可在 `.claude/agents/` 放同名檔案覆蓋
- 專案的 `CLAUDE.md` 若列出團隊與流程，依專案流程派工；未列出時，依任務類型自行判斷
- 派工時必須提供：完整需求、相關檔案路徑、API 契約、專案特有慣例（sub agent 看不到主對話）
- 實作完成後，主動交由 `code-reviewer` 審查；涉及驗證、授權、個資時加上 `security-auditor`