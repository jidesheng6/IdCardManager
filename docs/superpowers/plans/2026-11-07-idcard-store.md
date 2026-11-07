# 卡储值简化版 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 .NET Framework 4.8 + SunnyUI 的加油站卡储值/扣款简化 WinForms 程序，复用 LRCenter 原厂 DLL，SQLite 记流水，并展示完整卡数据。

**Architecture:** UI → Application → Domain / Infrastructure；卡读写经门面适配 `cpukrnlnet` 与 `Lr.NativeWrapper.SpCardApp`；读卡器状态轮询驱动控件启停；参数本机存储并可从 LRCenter 导入。

**Tech Stack:** .NET Framework 4.8、SunnyUI、SQLite + Dapper、原厂卡 DLL（libs）

**Spec:** `docs/superpowers/specs/2026-11-07-idcard-store-design.md`

## Global Constraints

- 目标框架：.NET Framework 4.8  
- UI：SunnyUI；界面文案禁止句号  
- 储值/扣款必须授权卡 ValueCard + 用户 PIN  
- 不直连原 SQL Server；流水仅 SQLite  
- 原厂 DLL 副本置于 `libs/` 并复制到输出目录  
- 未经用户明确要求不执行 git commit（仓库可能尚未初始化）

---

## File Structure

```
src/IdCardManager.Domain/
  Enums/ReaderConnectionState.cs, AuthCardType.cs, TradeType.cs
  Entities/CardSnapshot.cs, TradeRecord.cs, AppSettings.cs
  Rules/AuthCardRules.cs
  Abstractions/ICardFacade.cs, IReaderMonitor.cs, ITransactionRepository.cs, ISettingsRepository.cs

src/IdCardManager.Application/
  Services/StoreValueService.cs, WithdrawValueService.cs, CardDataService.cs
  Services/ReaderStatusHub.cs, SettingsService.cs
  Models/StoreValueRequest.cs, WithdrawValueRequest.cs

src/IdCardManager.Infrastructure/
  Card/SpCardFacade.cs, FakeCardFacade.cs, ReaderMonitor.cs
  Persistence/SqliteConnectionFactory.cs, SchemaInitializer.cs
  Persistence/TransactionRepository.cs, SettingsRepository.cs
  Import/LrCenterParamImporter.cs
  libs.props / 复制 DLL 的构建目标

src/IdCardManager.UI/
  Program.cs, MainForm.cs
  Pages/StoreValuePage.cs, WithdrawValuePage.cs, CardDataPage.cs, TradeListPage.cs, SettingsPage.cs
  Controls/StatusHeader.cs

tests/IdCardManager.Tests/
  AuthCardRulesTests.cs, TransactionRepositoryTests.cs, AmountHelperTests.cs

libs/   # 从 F:\LrSingle\LRCenter 复制
analysis/api-map.md
```

---

### Task 1: 解决方案骨架与 Domain

**Files:**
- Create: `src/IdCardManager.Domain/IdCardManager.Domain.csproj`
- Create: Domain 枚举、实体、`AuthCardRules`、抽象接口
- Create: `IdCardManager.sln`
- Test: `tests/IdCardManager.Tests/AuthCardRulesTests.cs`

**Interfaces:**
- Produces: `AuthCardRules.CanProceed(TradeType, AuthCardType) -> bool`  
- Produces: `ICardFacade`（Open/Close/GetStatus/ReadSnapshot/Store/Withdraw）

- [x] **Step 1: 创建 Domain 项目与规则测试（先写测试）**

```csharp
[Test]
public void Store_Requires_ValueCard()
{
    Assert.IsFalse(AuthCardRules.CanProceed(TradeType.Store, AuthCardType.MotherCard));
    Assert.IsTrue(AuthCardRules.CanProceed(TradeType.Store, AuthCardType.ValueCard));
}
```

- [x] **Step 2: 实现 `AuthCardRules` 与实体使测试通过**
- [x] **Step 3: 创建空白 Application / Infrastructure / UI / Tests 项目并加入解决方案**
- [x] **Step 4: 编译 Domain + Tests 通过**

---

### Task 2: SQLite 仓储

**Files:**
- Create: Infrastructure 持久化类
- Test: `TransactionRepositoryTests.cs`

**Interfaces:**
- Consumes: `TradeRecord`  
- Produces: `ITransactionRepository.InsertAsync` / `QueryAsync`；`ISettingsRepository.Get/Save`

- [x] **Step 1: 写仓储失败测试（插入后能按卡号查出）**
- [x] **Step 2: 实现 Schema + Dapper 仓储使测试通过**
- [x] **Step 3: NuGet：`System.Data.SQLite.Core`、`Dapper`（net48 兼容版本）**

---

### Task 3: 复制 libs 与 SpCardFacade

**Files:**
- Create: `libs/*`（从 `F:\LrSingle\LRCenter` 与 `csmodules` 复制）
- Create: `SpCardFacade.cs`、`ReaderMonitor.cs`、`FakeCardFacade.cs`
- Modify: Infrastructure csproj `Content Include=libs\** CopyToOutputDirectory`

**Interfaces:**
- Produces: `CardSlotStatus { ReaderConnected, AuthCardType, UserCardPresent, UserSummary }`  
- Produces: `ICardFacade.StoreAsync(amountFen, pin, userKey, terminalNo)`  

- [x] **Step 1: 复制规格所列 DLL 到 `libs/`**
- [x] **Step 2: 实现 `FakeCardFacade` 供无硬件调试**
- [x] **Step 3: 实现 `SpCardFacade` 包装 Open/Check/Read/Save/Withdraw**
- [x] **Step 4: `ReaderMonitor` 定时（约 800ms）刷新 `CardSlotStatus` 并引发事件**

---

### Task 4: Application 用例服务

**Files:**
- Create: `StoreValueService`、`WithdrawValueService`、`CardDataService`、`SettingsService`、`ReaderStatusHub`

- [x] **Step 1: Store/Withdraw 先校验读卡器与 `AuthCardRules`，再调门面，再写流水**
- [x] **Step 2: SettingsService 读写设置 + 调用 `LrCenterParamImporter`**
- [x] **Step 3: 金额辅助：元 ↔ 分，金额必须 > 0**

---

### Task 5: SunnyUI 主界面

**Files:**
- Create: UI 项目、MainForm、各 Page、StatusHeader
- NuGet: SunnyUI（net48）

- [x] **Step 1: MainForm 导航五页 + 顶部状态**
- [x] **Step 2: 订阅 ReaderStatusHub；未连接/授权不符时禁用确认按钮并设提示文案（无句号）**
- [x] **Step 3: 储值/扣款页提交走 Application 服务；成功刷新余额**
- [x] **Step 4: 卡片数据页网格展示 `CardSnapshot` 全字段**
- [x] **Step 5: 流水页、参数页（含导入按钮）**
- [x] **Step 6: 适老大控件 + DPI（app.manifest PerMonitorV2 + UiScale）**
- [x] **Step 7: 单实例（Mutex）；二次启动前置并最大化已有窗体**

---

### Task 6: 联调与手工验证清单

- [x] **Step 1: Release 编译，确认 libs DLL 出现在输出目录**
- [ ] **Step 2: 无硬件启动：Fake 模式或断开提示正确**
- [ ] **Step 3: 有硬件时核对：连接检测、错卡提示、储值、扣款、读全量、流水**
- [ ] **Step 4: 更新 `analysis/api-map.md` 若发现额外依赖 DLL**
- [ ] **Step 5: 验证单实例与高 DPI 缩放观感**

---

## Execution note

默认采用 **Inline Execution**（本会话连续实现）。不自动 git commit。
