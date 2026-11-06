---
name: new-frontend-project
description: 建立符合 housewoo 慣例的 Vue 3 前端專案骨架（Vue + Vite + TypeScript + Tailwind v4 + orval）。當使用者要開新的前端專案、建立管理後台介面、scaffold Vue 應用時使用。API 型別由後端 Swagger 經 orval 產生，後端請使用 new-backend-project skill。
---

# 建立 Vue 前端專案架構

依照 housewoo 既有的前端慣例，快速建立新的 Vue 專案骨架。

作法是**用 `npm create vite` 產生原生骨架，再依慣例改寫**。`templates/` 只放「CLI 產不出來、且屬於架構核心」的檔案。

## 前置確認

這個 skill 只負責**獨立的前端專案**（前後端分離）。若專案不分離前後端、要用 MVC + Razor Views，樣式設定含在 `new-backend-project` skill 的 `references/web-mvc.md`，不需要走這裡。

前端的 API 呼叫程式碼由後端 Swagger 產生，**建議後端先完成**再建前端。若後端尚未就緒也可以先建骨架，`npm run generate:api` 之後再跑。

## Step 0：先問清需求

用 AskUserQuestion 一次問完：

1. **專案名稱**（小寫連字號，例如 `housewoo-erp-web`）與**輸出目錄**
2. **後端 API 位址**（開發用與正式用，寫進 `.env.local` / `.env`）
3. **選配**（多選）：登入頁與 JWT 流程、多頁籤版面、側邊欄選單權限、Excel 匯出、Docker + nginx

## Step 1：建立專案與安裝套件

```bash
npm create vite@latest <name> -- --template vue-ts
cd <name>
npm i vue-router vuex axios
npm i -D tailwindcss @tailwindcss/postcss postcss prettier
npm i -D orval@latest
```

**orval 一定要帶 `@latest`**。實測 `npm i -D orval` 會裝到 7.13.2，帶進 15 個弱點（含 11 個 critical）；`orval@latest` 是 8.x，0 弱點。

刻意**不引入 UI 元件庫**。ERP 表單的樣式需求瑣碎，套件的客製成本往往高於自刻。

## Step 2：套用設定

### vite.config.ts

```ts
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: { host: true, port: 8848 },
})
```

### tsconfig.app.json

在 `compilerOptions` 加入 `"paths": { "@/*": ["./src/*"] }`。

**不要加 `baseUrl`** —— TypeScript 6 已將它標為棄用，`vue-tsc` 會直接報 `TS5101` 讓建置失敗。省略時 `paths` 會以 tsconfig 所在目錄為基準解析，正是我們要的行為。

### postcss.config.js

```js
export default { plugins: { '@tailwindcss/postcss': {} } }
```

## Step 3：複製範本檔

| 範本 | 目的地 |
|---|---|
| `orval.config.ts` | 專案根目錄 |
| `orval-fetch.ts` | `src/api/` |
| `tailwind.css` | `src/assets/css/` |
| `AGENTS.md` | 專案根目錄 |
| `CLAUDE.md` | 專案根目錄 |

`CLAUDE.md` 的佔位符：`__PROJECT_NAME__` → 專案名稱；`__BACKEND_DIR__` → 對應後端專案的相對路徑（尚未決定時刪除該行）。

> **為什麼需要 CLAUDE.md**：Claude Code 與 sub agent 只會自動載入 `CLAUDE.md`，不會讀 `AGENTS.md`。範本的 `CLAUDE.md` 用 `@AGENTS.md` 匯入規則，規則仍只維護 `AGENTS.md` 一份。

`orval.config.ts` 內有 `__PROJECT_KEY__` 佔位符，替換成專案的小寫短名（例如 `housewoo`）。

在 `main.ts` 匯入 `@/assets/css/tailwind.css`。

`package.json` 加入 `"generate:api": "orval"`。

`orval-fetch.ts` 相依兩個模組，**必須一併建立否則無法編譯**（完整程式碼見 `references/conventions.md`）：

- `src/composables/useLoading.ts` — 匯出 `startLoading` / `stopLoading`，用計數器管理並行請求
- `src/util/localstorage.ts` — 匯出 `getToken` / `setToken` / `clearAuthStorage`

## Step 4：建立目錄骨架

```
src/
  api/          orval-fetch.ts（唯一手寫）、swagger.json、services/、models/
  assets/css/   tailwind.css、global.css
  components/Common/
  composables/  useLoading.ts、useAlert.ts、useSessionExpired.ts
  router/index.ts
  store/index.ts
  types/
  util/         auth.ts、localstorage.ts、downloadFile.ts、formatValue.ts
  views/
```

環境變數：`.env`（正式站 API 位址、`VITE_APP_NAME`）、`.env.local`（本機覆寫，**加入 .gitignore**）。所有變數必須以 `VITE_` 開頭。

各層職責、API 產碼流程、模組對應關係、配色原則見 `references/conventions.md`。

## Step 5：收尾與驗證

1. `npm run build` 必須通過（`vue-tsc -b && vite build`）
2. `npm audit` 應為 0 弱點
3. 確認 Tailwind 主題生效：在元件用一個自訂 class（例如 `bg-primary`）建置後，檢查產出的 CSS 是否含 `--color-primary`。v4 會 tree-shake 沒被用到的主題變數，只看 `@theme` 寫對不代表有效
4. 產生 `README.md`（含啟動方式、環境變數、`generate:api` 用法）
5. 確認根目錄有 `CLAUDE.md`（含 `@AGENTS.md` 匯入）且佔位符都已替換：`grep -n "__" CLAUDE.md` 應無結果
6. `git init` 並建立首次 commit — **這一步要先徵得使用者同意再執行**

## 相關 skill

要一次建立前後端並走完規劃、實作、測試、審查流程，請改用 **`new-fullstack-project`**。

後端請使用 **`new-backend-project`**。前端的 `src/api/services/` 與 `src/api/models/` 都由後端 Swagger 產生，兩者的模組命名要一一對應（`views/Category` ↔ `CategoryController` ↔ `api/services/category.ts`）。
