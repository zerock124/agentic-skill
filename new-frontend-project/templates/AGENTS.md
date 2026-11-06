# 專案規範

## 文字編碼規則

本專案含有正體中文文字，避免亂碼與意外重新編碼是最高優先。

### 必要規則

- 讀取或編輯可能含正體中文的既有檔案前，先確認：編碼、是否有 BOM、換行風格
- 若懷疑出現亂碼，在確認編碼判讀可信之前不要存檔
- 既有檔案一律保留原本的編碼、BOM 與換行風格
- 「轉換為 UTF-8」視為獨立且需明確指示的任務，不要順手做
- 新檔案以 UTF-8 建立，並說明是否含 BOM
- 不要使用無法明確控制編碼的寫入方式（例如 shell 重導向）
- 寫入後重新開啟檔案，確認代表性的正體中文行內容正確
- 出現以下情況立即停止並回報：替換字元、非預期的 `?`、BOM 變動、換行風格變動、無業務理由的整檔差異

### Windows PowerShell 注意事項

PowerShell 5.1 讀取無 BOM 的 UTF-8 檔案時會以系統 ANSI 編碼解讀，導致正體中文變成亂碼。

- 讀檔一律加上 `-Encoding UTF8`
- 寫檔使用 `Out-File -Encoding utf8` 或 `Set-Content -Encoding utf8`，不要依賴預設值

### 變更回報格式

每個異動的文字檔請回報：路徑、編碼、是否有 BOM、換行風格、如何驗證、代表性中文內容是否完整。

## API 層規則

- **`src/api/services/` 與 `src/api/models/` 是 orval 產生物，不要手改** —— 下次執行 `npm run generate:api` 就會被覆蓋
- 要調整 API 行為（攔截器、錯誤處理、token）請改 `src/api/orval-fetch.ts`，那是唯一手寫的 API 基礎層
- 更新 API 契約的流程：從後端下載最新 `swagger.json` 放到 `src/api/` → `npm run generate:api`
- 錯誤訊息取自後端回應的 `Message` 欄位（PascalCase），這是與後端 `BaseResponse` 的約定，不要改成 camelCase

## 開發規則

- 樣式主題定義在 `src/assets/css/tailwind.css` 的 `@theme` 區塊（Tailwind v4），不使用 `tailwind.config.js`
- 選色時 primary 與 danger 的色相至少相距 30°，狀態色之間也要拉開 —— 管理後台表格的操作按鈕密集並排，太接近會誤點
- `tsconfig` 不要加 `baseUrl`（TypeScript 6 已棄用，會讓 `vue-tsc` 報 `TS5101`），路徑別名只用 `paths`
- 環境變數必須以 `VITE_` 開頭才會被注入；`.env.local` 不進版控
- 新增頁面時：`views/<Module>/` 建立清單頁與編輯頁 → `router/index.ts` 加路由（`meta: { title, requiresAuth }`）→ 需要權限控管時補上選單資料。模組命名要與後端 Controller 一致
- `npm run build` 應通過（含 `vue-tsc` 型別檢查），`npm audit` 應為 0 弱點
