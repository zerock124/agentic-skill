# __SYSTEM_NAME__ 工作區

本目錄包含兩個獨立的 git repo，各自有自己的 `CLAUDE.md` 與 `AGENTS.md`，在子目錄內工作時以子目錄的規則為準。

| 目錄 | 內容 | 驗證指令 |
|---|---|---|
| `__KEBAB_NAME__-api/` | .NET 10 Web API（housewoo 後端架構） | `dotnet build`、`dotnet test` |
| `__KEBAB_NAME__-web/` | Vue 3 + Vite + TypeScript 前端 | `npm run build`、`npm audit` |

## 跨 repo 規則

- **API 契約由後端決定**：後端改了 Controller / Request / Response 後，前端要重新下載 `swagger.json` 並執行 `npm run generate:api`，不要手改 `src/api/services/` 與 `src/api/models/`
- 模組命名前後端一致：`views/<Module>` ↔ `<Module>Controller` ↔ `api/services/<module>.ts`
- JSON 欄位為 PascalCase，前後端都依賴這個約定
- commit 要在各自的 repo 目錄內進行；跨前後端的功能分成兩個 commit
- 架構計畫與 ADR 放在 `__KEBAB_NAME__-api/docs/`
