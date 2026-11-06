# 後端分層架構

## 依賴形狀：扁平星狀，不是傳統洋蔥

```
                  ┌──────────────────────┐
                  │  <Name>.API / .Web   │  ← DI 接線層，唯一知道所有實作的地方
                  └──────────┬───────────┘
             ┌───────────────┼───────────────┐
             ▼               ▼               ▼
    ┌────────────────┐  ┌─────────┐  ┌──────────────┐
    │ .Infrastructure│  │ .Service│  │              │
    └────────┬───────┘  └────┬────┘  │              │
             └───────┬───────┘       │              │
                     ▼               ▼              │
                  ┌──────────────────────────────────┘
                  │        <Name>.Domain             │  ← 最底層，不參考任何專案
                  └──────────────────────────────────┘
```

| 專案 | 參考 | 職責 |
|---|---|---|
| `.Domain` | 無 | Entities、DTOs、Enums、Configs、**所有介面** |
| `.Infrastructure` | Domain | EF Core `DbContext`、`GenericRepository`、`UnitOfWork`、外部服務實作 |
| `.Service` | Domain | 業務邏輯。**不參考 Infrastructure** |
| `.API` / `.Web` | Domain + Infrastructure + Service | Controller、Filter、Middleware、DI 註冊 |
| `.Tests` | Service + Domain | xunit 單元測試 |

**Service 不參考 Infrastructure 是刻意的**。Service 只認得 Domain 的 `IGenericRepository<T>` / `IUnitOfWork` 介面，實作在最外層才綁定。這讓 Service 可以在測試中直接餵假 repository，不必啟動 EF Core。

若某個 Service 需要 Infrastructure 的能力（寄信、打外部 API、存檔），作法是：**介面定義在 Domain/Interface，實作放 Infrastructure，最外層註冊**——而不是加專案參考。

## 資料夾配置

```
src/<Name>.Domain/
  Entities/            Category.cs                    ← 單數命名
  DTOs/
    Share/             BaseResponse.cs, SearchDTO.cs, PaginationDTO.cs
    <Module>/          CategoryDTO.cs, EditCategoryDTO.cs, SearchCategoryDTO.cs
  Enums/
  Configs/             AppSettingsConfig.cs, JwtSettingConfig.cs
  Interface/           IGenericRepository.cs, IUnitOfWork.cs, IMomoProvider.cs（外部 API 介面）
  Exceptions/          ForbiddenException.cs, ExternalApiException.cs（需要時才建）
  Utilities/

src/<Name>.Infrastructure/
  Data/                EntityDbContext.cs, Migrations/
  Implement/           GenericRepository.cs, UnitOfWork.cs
  Adapters/<Provider>/ MomoAdapter.cs, Contracts/, Mappers/   ← 外部 API，見 integrations.md
  Helpers/

src/<Name>.Service/
  Interface/           ICategoryService.cs, Orchestration/
  Implement/           CategoryService.cs, Orchestration/     ← 串接外部 API 的流程協調
  Mappings/            CategoryMapping.cs               ← 與 API 層同名，以 namespace 區分
  Utilities/           ServiceExceptionUtility.cs, ExcelUtility.cs

src/<Name>.API/
  Controllers/         BaseController.cs, CategoryController.cs
  Jobs/                MomoSyncJob.cs                   ← Quartz 排程（選配）
  SignalR/             ProgressHub.cs                   ← 即時推播（選配）
  Constants/           MenuControllers.cs
  Models/
    Share/             PaginationResponse.cs
    <Module>/          CategoryModels.cs                ← 一模組一檔，內含所有 Request/Response
  Mappings/            CategoryMapping.cs
  Extensions/          ConfigurationExtensions.cs, ServiceCollectionExtensions.cs,
                       MiddlewareExtensions.cs, LoggingExtensions.cs
  Filters/             ApiExceptionFilter.cs, ClaimsFilter.cs
  Middlewares/         ApiExceptionHandlingMiddleware.cs
  Utilities/           ApiExceptionResponseUtility.cs
```

## 命名規則

| 類型 | 慣例 | 範例 |
|---|---|---|
| Entity | 單數 PascalCase | `Category` |
| DTO | `<動作><Module>DTO` | `CategoryDTO`、`EditCategoryDTO`、`SearchCategoryDTO` |
| Service 介面／實作 | `I<Module>Service` / `<Module>Service` | `ICategoryService` / `CategoryService` |
| Service 方法 | `<動作><Module>`，**不加 `Async` 後綴**；取單筆可簡寫為 `Get` | `GetCategoryList`、`CreateCategory`、`UpdateCategory`、`DeleteCategory`、`Get` |
| API Request | `<動作><Module>Request`；清單查詢為 `Get<Module>ListRequest`；Update 繼承 Create | `GetCategoryListRequest`、`CreateCategoryRequest`、`UpdateCategoryRequest : CreateCategoryRequest`、`DeleteCategoryRequest` |
| API Response | `<Module>Response`（單筆與清單項目共用） | `CategoryResponse` |
| Controller | `<Module>Controller` | `CategoryController` |
| Controller action | `<動作><Module>`，與 Service 方法同名；路由為 `/<module>/<action>` 全小寫 | `GetCategory` → `GET /category/getcategory`、`CreateCategory` → `POST /category/createcategory` |
| AutoMapper Profile | `<Module>Mapping : Profile`，API 層與 Service 層各一個同名類別 | `API/Mappings/CategoryMapping.cs`、`Service/Mappings/CategoryMapping.cs` |
| Helper / Utility | 可注入、有狀態或有相依的類別叫 `<Name>Helper`，放 `Helpers/`，**不使用 static**；static 純函式叫 `<Name>Utility`，放 `Utilities/`；常數放 `Constants/` | `ProgressNotifierHelper`、`ServiceExceptionUtility`、`ExcelUtility`、`MenuControllers` |
| 外部 API | 介面 `I<Provider>Provider`（Domain）、實作 `<Provider>Adapter`（Infrastructure）、例外 `<Provider>ApiException` | `IMomoProvider`、`MomoAdapter`、`MomoApiException` |

> `Async` 後綴：Service 方法不加後綴。只有 `IUnitOfWork`（`SaveChangesAsync` 等）與包裝 EF / 框架 API 的基礎設施方法保留 `Async`。

## 新增一個業務模組時要動的檔案

固定這 8 個位置，缺一不可：

1. `Domain/Entities/<Module>.cs`
2. `Domain/DTOs/<Module>/`（DTO、EditDTO、SearchDTO）
3. `Service/Interface/I<Module>Service.cs`
4. `Service/Implement/<Module>Service.cs`
5. `Service/Mappings/<Module>Mapping.cs`（Entity ↔ DTO）
6. `API/Models/<Module>/<Module>Models.cs`
7. `API/Controllers/<Module>Controller.cs` + `API/Mappings/<Module>Mapping.cs`（DTO ↔ Request/Response）
8. `Extensions/ServiceCollectionExtensions.cs` 加一行 `AddTransient`

## 型別對應鏈

`Request` → `DTO` → `Entity` → `DTO` → `Response`，兩次 AutoMapper 轉換：

- API 層的 Mapping 負責 `Request ↔ DTO`、`DTO ↔ Response`
- Service 層的 Mapping 負責 `DTO ↔ Entity`

Controller 不碰 Entity，Service 不碰 Request/Response。這道界線是本架構最容易被打破的地方，請守住。
