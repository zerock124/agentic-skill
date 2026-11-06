# 前端慣例（Vue 3 + Vite + Tailwind v4）

## 技術選型

| 項目 | 選擇 | 說明 |
|---|---|---|
| 框架 | Vue 3 + TypeScript | `<script setup lang="ts">` |
| 建置 | Vite | `vue-tsc -b && vite build` |
| 樣式 | Tailwind CSS **v4** | 主題寫在 CSS 的 `@theme`，不是 `tailwind.config.js` |
| 狀態 | vuex 4 | 單一 store，非 Pinia |
| 路由 | vue-router 4 | 單檔集中管理 |
| API | axios + **orval** | 由後端 Swagger 自動產生 |
| UI 元件庫 | **無** | 全部自刻 + Tailwind |

刻意不引入 UI 元件庫。管理後台表單的樣式需求瑣碎，套件的客製成本往往高於自刻。

## vite.config.ts

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

`tsconfig.app.json` 的 `compilerOptions` 要對應加上：

```jsonc
"paths": { "@/*": ["./src/*"] }
```

**不要加 `baseUrl`**。TypeScript 6 已將它標為棄用，`vue-tsc` 會直接報 `TS5101` 讓建置失敗。省略 `baseUrl` 時 `paths` 會以 tsconfig 所在目錄為基準解析，正是我們要的行為。

`noUnusedLocals` / `noUnusedParameters` 在 Vite 的 `vue-ts` 範本已預設開啟，不需另外設定。

## Tailwind v4 設定

v4 不再用 `tailwind.config.js` 定義主題。`src/assets/css/tailwind.css`：

```css
@import 'tailwindcss';

@theme {
  --color-primary: #4f46e5;
  --color-primary-hover: #4338ca;
  /* ... */
}
```

`postcss.config.js`：

```js
export default { plugins: { '@tailwindcss/postcss': {} } }
```

在 `main.ts` 匯入 `@/assets/css/tailwind.css`。

v4 會 tree-shake 沒被用到的主題變數 —— 建置產物裡看不到 `--color-primary` 是正常的，只要有元件用了 `bg-primary` 就會出現。想確認設定生效，用一個實際 class 建置後檢查產出的 CSS，不要只看 `@theme` 有沒有寫對。

`--spacing-*` 這類非顏色 token 同樣會產生 utility（`--spacing-sidenav: 260px` → `w-sidenav`）。

**選色時注意色相角度**：primary 與 danger 至少相距 30°，否則在密集的表格操作按鈕裡會分不出來。狀態色 success / warning / danger / info 彼此也要拉開。

## API 層：Swagger 單向驅動

契約由後端決定，前端不手寫 API 呼叫程式碼。

```
後端 Swagger → src/api/swagger.json → orval → src/api/services/  (依 tag 分檔)
                                             → src/api/models/    (型別定義)
```

`orval.config.ts`：

```ts
import { defineConfig } from 'orval';

export default defineConfig({
  <name>: {
    input: { target: './src/api/swagger.json' },
    output: {
      target: './src/api/services/',
      schemas: './src/api/models/',
      mode: 'tags',
      indexFiles: true,
      client: 'axios-functions',
      override: {
        mutator: { path: './src/api/orval-fetch.ts', name: 'orvalFetch' },
      },
    },
  },
});
```

`package.json` 加 `"generate:api": "orval"`。

安裝時務必用 `npm i -D orval@latest` —— 不帶 `@latest` 會解析到 7.x 舊版，`npm audit` 會報出 15 個弱點（`@orval/core` / `js-yaml` / `lodash`）。

**`src/api/services/` 與 `src/api/models/` 是產生物，永遠不要手改** —— 下次重新產生就會被覆蓋。要調整行為請改 `orval-fetch.ts`。

### orval-fetch.ts 是唯一手寫的 API 基礎層

它負責四件事：

1. 建立 axios instance，`baseURL` 取自 `import.meta.env.VITE_API_URL`
2. request interceptor：加 `Bearer` token、啟動 loading
3. response interceptor：關閉 loading；401 時清除認證資料並轉交全域「登入過期」流程，**回傳永不 settle 的 Promise**，避免各呼叫端的 catch 再彈一次錯誤
4. 其餘錯誤包成 `ApiError`，訊息從 response body 的 `Message` 欄位取（對應後端 `BaseResponse`）

### orval-fetch.ts 的兩個相依模組

`orval-fetch.ts` 匯入了下面兩個模組，scaffold 時要一併建立，否則型別檢查不會過。

`src/composables/useLoading.ts` —— 用計數器管理並行請求，避免多個請求同時進行時提早關閉 loading：

```ts
import { ref } from 'vue';

const pending = ref(0);
export const isLoading = ref(false);

export function startLoading(): void {
  pending.value += 1;
  isLoading.value = true;
}

export function stopLoading(): void {
  pending.value = Math.max(0, pending.value - 1);
  isLoading.value = pending.value > 0;
}
```

`src/util/localstorage.ts`：

```ts
const TOKEN_KEY = 'token';

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearAuthStorage(): void {
  localStorage.removeItem(TOKEN_KEY);
}
```

## 目錄結構

```
src/
  main.ts              createApp(App).use(router).use(store)，接上 401 → 登入過期流程
  App.vue
  api/
    orval-fetch.ts     ← 唯一手寫
    swagger.json       ← 從後端下載
    services/ models/  ← orval 產生物
  assets/css/          tailwind.css (@theme), global.css
  components/
    Common/            跨模組共用元件
  composables/         useLoading, useAlert, useSessionExpired
  router/index.ts      單檔，meta: { title, requiresAuth }
  store/index.ts       單一 vuex store：帳號、選單權限、多頁籤、側邊欄狀態
  types/
  util/                auth, localstorage, downloadFile, formatValue
  views/<Module>/      <Module>.vue（清單）+ <Module>Edit.vue（編輯）
```

## 模組對應關係

前後端模組名稱一一對應，命名一致才好維護：

```
views/Category/  ↔  CategoryController  ↔  api/services/category.ts
```

新增頁面的固定動作：
1. `views/<Module>/<Module>.vue` + `<Module>Edit.vue`
2. `router/index.ts` 加一筆路由，`meta: { title, requiresAuth: true }`
3. 若需權限控管，於 store 的選單權限資料補上對應項目

## 環境變數

| 檔案 | 用途 |
|---|---|
| `.env` | 預設值（正式站 API 位址）、`VITE_APP_NAME` |
| `.env.local` | 本機開發覆寫（通常指向 `https://localhost:8090`），**加入 .gitignore** |

所有前端環境變數必須以 `VITE_` 開頭才會被 Vite 注入。
