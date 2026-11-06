# 既有專案新增功能

專案骨架建好之後，日常新增功能照這份流程做。分層、命名、回應格式等規則見 `architecture.md` 與 `conventions.md`，這裡只放流程與檢查清單。

## 工作順序

1. **確認功能目的與範圍**：輸入、輸出、資料來源、錯誤情境。範圍太大時先切出第一階段
2. **確認是否動到資料庫**：要動就先交給 db-specialist（Entity、設定、Migration）
3. **判斷類型**：標準 CRUD，或非標準流程（見下表）
4. **找最接近的既有實作當參照**：例如新增「廠商」就先讀 `CategoryController` / `CategoryService` 的完整鏈路，照抄結構
5. **決定檔案**：列出要新增與要修改的檔案（見「必做清單」）
6. **實作**：依 `Request → DTO → Entity → DTO → Response` 順序
7. **同步更新**：DI 註冊、兩層 Mapping、權限設定
8. **補測試**：Service 單元測試至少涵蓋成功路徑與錯誤情境
9. **驗證**：`dotnet build`（零錯誤零警告）、`dotnet test`
10. **交付**：依「交付格式」回報

## 功能類型

| 類型 | 典型需求 | 參考 |
|---|---|---|
| 標準 CRUD | 基本資料維護（分類、商品、客戶） | `architecture.md` 的「新增業務模組要動的 8 個位置」 |
| 背景排程 | 每日同步訂單、定期清理、通知 | `integrations.md`「Quartz 排程」 |
| 外部 API 串接 | 電商平台、物流、社群平台 API | `integrations.md`「外部 API Adapter」 |
| Webhook 接收 | 第三方主動推送事件 | `integrations.md`「Webhook」 |
| 匯入 | 上傳 Excel 批次建立資料 | `integrations.md`「Excel 匯入」 |
| 匯出 | Excel / PDF 報表 | 參照既有的匯出功能；檔案由 Service 產生 `byte[]`，Controller 用 `File(...)` 回傳 |
| 即時推播 | 長時間作業的進度通知 | `integrations.md`「SignalR 進度通知」 |

非標準流程先定義**邊界**（觸發方式、資料格式、失敗策略），再決定放哪一層，最後才實作。

## 驗證要放哪裡

本架構不使用 DataAnnotations 驗證與 FluentValidation，驗證集中在 Service，以例外表達：

| 情境 | 位置 | 作法 |
|---|---|---|
| 必填、長度、格式、數值範圍 | Service | 檢查後擲 `ArgumentException`（400），訊息寫給使用者看 |
| 資料不存在（含已軟刪除） | Service | 擲 `KeyNotFoundException`（404） |
| 資料重複、狀態不允許此操作 | Service | 擲 `InvalidOperationException`（409） |
| 唯一性 | Service + 資料庫 | Service 先查並擲 409；同時以唯一索引保證，避免並發時重複 |
| 權限 | Controller attribute | `[RequireAuthority(MenuControllers.X, "Edit")]`；Service 內的資料層級權限擲 `ForbiddenException`（403） |
| 外部 API 失敗 | Adapter | 擲 `ExternalApiException` 的子類別（502） |

錯誤訊息要具體到使用者能自行修正（例：「分類代碼 A01 已存在」），但不要包含連線字串、SQL、內部路徑。

## 必做清單

### 程式碼
- [ ] 每個新檔案放在正確的層（Domain / Infrastructure / Service / API）
- [ ] Controller 不碰 Entity、Service 不碰 Request / Response
- [ ] 命名符合 `architecture.md`（Service 方法與 action 為 `<動作><Module>`，不加 `Async`）
- [ ] Controller action 有 XML 註解與 `[ProducesResponseType]`（前端 orval 依賴它）
- [ ] 需要權限的 action 有 `[RequireAuthority]`，匿名端點明確標 `[AllowAnonymous]`

### 整合
- [ ] AutoMapper 兩層都已補齊（`API/Mappings`、`Service/Mappings`）
- [ ] 新 Service 已在 `ServiceCollectionExtensions.AddBusinessServices` 手動註冊
- [ ] 新選單已加入 `MenuControllers` 常數與選單種子資料（需要權限控管時）
- [ ] 資料庫變更已產生 Migration 並審查過
- [ ] 新設定值已加入 `appsettings.json`（機密值只留空字串佔位）與對應的 Config 類別

### 測試與驗證
- [ ] Service 單元測試涵蓋成功路徑、400 / 404 / 409 錯誤情境
- [ ] `dotnet build` 零錯誤零警告、`dotnet test` 通過
- [ ] 前端需要時：更新 `swagger.json` 並執行 `npm run generate:api`

### 文件
- [ ] 對應模組的 `docs/` 文件已更新（對外行為有變更時）

## 交付格式

```
### 功能摘要
功能名稱、目的、影響範圍（新增 / 修改 / 刪除）

### 檔案清單
新增：
- Domain/Entities/<Module>.cs
- Domain/DTOs/<Module>/<Module>DTO.cs、Edit<Module>DTO.cs、Search<Module>DTO.cs
- Service/Interface/I<Module>Service.cs、Service/Implement/<Module>Service.cs
- Service/Mappings/<Module>Mapping.cs
- API/Models/<Module>/<Module>Models.cs
- API/Mappings/<Module>Mapping.cs
- API/Controllers/<Module>Controller.cs
- tests/<Name>.Tests/Service/<Module>ServiceTests.cs
修改：
- Infrastructure/Data/EntityDbContext.cs（DbSet 與設定）
- API/Extensions/ServiceCollectionExtensions.cs（DI 註冊）

### 資料庫變更
Migration 名稱、SQL 重點、風險（資料遺失、鎖表）

### 新增端點
| Method | Route | Request | Response |

### 驗證方式
執行過的指令與結果；測試案例名稱

### 已知限制與風險
尚未實作的部分、效能考量、需要前端或 DB 配合的事項
```
