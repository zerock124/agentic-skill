# __PROJECT_NAME__（前端）

@AGENTS.md

## 專案資訊

- 對應後端專案：__BACKEND_DIR__（API 型別由其 Swagger 經 orval 產生）
- 前端慣例：遵循 housewoo 前端慣例（Vue 3 + Vite + TypeScript + Tailwind v4 + vuex + orval，不使用 UI 元件庫）
  - 技術選型、API 層、目錄結構、模組對應：`~/.claude/skills/new-frontend-project/references/conventions.md`

## 常用指令

```bash
npm run dev            # 開發伺服器（port 8848）
npm run build          # vue-tsc -b && vite build，含型別檢查
npm run generate:api   # 依 src/api/swagger.json 重新產生 src/api/services 與 src/api/models
npm audit              # 應為 0 弱點
```

## 驗證（宣告完成前必須通過）

1. `npm run build` 通過
2. `npm audit` 為 0 弱點
