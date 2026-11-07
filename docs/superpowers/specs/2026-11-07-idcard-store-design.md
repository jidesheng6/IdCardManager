# 加油站卡储值简化版 — 设计规格

**日期:** 2026-11-07  
**状态:** 已确认（范围 B / 复用原厂 DLL / 授权规则 A / 参数策略 C / 架构方案 1）

## 1. 目标

在 `IdCardManager` 交付一套基于 .NET Framework 4.8 + SunnyUI 的 WinForms 简化程序，复用 LRCenter 原厂读卡/写卡 DLL，支持：

1. 自动检测读卡器连接；未连接时提示并禁用相关功能  
2. 检测授权槽卡类型；储值/扣款仅允许授权卡（ValueCard），类型不符提示并禁用确认  
3. 用户卡储值、扣款（取现写卡），本地 SQLite 记流水  
4. 独立页面完整展示用户卡可读数据  
5. 本机参数配置，并可选从原 LRCenter 目录导入  

不做：售卡、挂失、补卡、积分、报表中心等原系统其它模块。

## 2. 约束

| 项 | 值 |
|----|-----|
| 框架 | .NET Framework 4.8 |
| UI | SunnyUI WinForms |
| 数据库 | SQLite + Dapper |
| 卡读写 | 复用 `cpukrnlnet`、`Lr.NativeWrapper.SpCardApp`、`Lr.NativeWrapper.LrReader` 及依赖原生 DLL |
| 界面文案 | 禁止句号；不做无关业务说明 |
| 视觉 | SunnyUI 网格对齐；状态与操作分区清晰 |
| 适老与 DPI | 面向长辈：大按钮/大字号/大导航；`PerMonitorV2` DPI 自适应；启动最大化 |
| 单实例 | 禁止多开；再次启动时恢复并前置已有窗口并最大化 |
| 平台 | Win32 x86（原厂读卡 DLL 为 x86） |
| 源路径 | 原系统 `F:\LrSingle\LRCenter`；受控副本放入 `libs/` |

## 3. 架构

分层单体：

```
UI (SunnyUI)
  → Application（用例服务）
    → Domain（实体/枚举/规则接口）
    → Infrastructure（DLL 适配、SQLite、参数导入）
```

设计模式：

- **门面** `ICardFacade`：对外统一 Open/Detect/Read/Save/Withdraw  
- **策略** `IAuthCardRule`：按操作类型校验授权卡  
- **仓储** `ITransactionRepository` / `ISettingsRepository`  
- **观察/轮询** `IReaderMonitor`：周期性上报读卡器与卡槽状态  

UI 禁止直接引用原厂 native wrapper（仅 Infrastructure 引用 `libs/`）。

## 4. 目录结构

```
IdCardManager/
  src/
    IdCardManager.UI/
    IdCardManager.Application/
    IdCardManager.Domain/
    IdCardManager.Infrastructure/
  libs/                          # 原厂 DLL 受控副本
  analysis/                      # API 对照与分析笔记
  tests/
    IdCardManager.Tests/
  docs/superpowers/specs|
  docs/superpowers/plans/
  resources/
```

## 5. 页面与流程

### 5.1 页面

| 页面 | 职责 |
|------|------|
| 储值 | 金额 + PIN → 写卡储值 → 记流水 |
| 扣款 | 金额 + PIN → 写卡扣款 → 记流水 |
| 卡片数据 | 展示公共/常用/私有/敏感文件及余额等完整可读字段 |
| 流水 | 本地流水查询 |
| 参数 | 终端号、UserKey、站点、原系统路径导入 |

主窗顶部状态条：读卡器、授权卡类型、用户卡在位。

### 5.2 全局状态

1. 读卡器未连接 → 提示 `读卡器未连接`；储值/扣款/读卡禁用  
2. 已连接 → 识别授权槽类型  
3. 储值/扣款确认：授权槽必须为 ValueCard，否则 `授权卡类型不匹配` 并禁用确认  
4. 卡片数据：有用户卡即可读取  

### 5.3 储值/扣款单笔流程

检测读卡器 → 校验授权卡 → 读用户卡摘要 → 输入金额与 PIN → 调用原厂 Save/Withdraw → 成功写 SQLite → 刷新余额。

对应原厂能力：

- 储值：`OilCard.CreditForLoad` / `SpOilCardSaveMoney`  
- 扣款：`OilCard.CreditForUnLoad` / `SpOilCardWithdrawMoney`  
- 读文件：`ReadPubAppFile` / `ReadCommonFile` / `ReadPrivateFile` / `ReadSensitiveFile` / `GetBalance`  
- 卡类型：`CheckCard` + `CardTypeConstants` / `cpukrnlnet.CardTypeEnum`

双槽约定（与原系统一致）：用户卡 NAD 与授权卡 NAD 由适配层按读卡器型号配置（默认同原厂双槽约定，可在参数中调整）。

## 6. 领域模型

### 6.1 枚举

- `ReaderConnectionState`: Connected / Disconnected  
- `AuthCardType`: None / ValueCard / MotherCard / UserMotherCard / LrKeyCard / PsamCard / Unknown  
- `TradeType`: Store / Withdraw  
- `TradeResult`: Success / Failed  

### 6.2 实体

- `CardSnapshot`：卡号、余额、有效期、各文件结构化字段、原始十六进制（便于完整展示）  
- `TradeRecord`：流水号、类型、卡号、金额分、操作员、时间、结果、错误信息  
- `AppSettings`：TerminalNo、UserKey、StationNo、AreaNo、LrCenterPath、Reader 相关（devmodel/端口/NAD）

### 6.3 授权规则（规则 A）

| 操作 | 要求授权卡 | PIN |
|------|------------|-----|
| 储值 | ValueCard | 必填 |
| 扣款 | ValueCard | 必填 |
| 卡片数据只读 | 不强制 | 读余额等需 PIN 的字段按原厂要求；能无 PIN 读的字段直接展示 |

## 7. 数据存储（SQLite）

路径：`%LocalAppData%\IdCardManager\idcard.db`（也可随程序 `data/idcard.db`，默认 LocalAppData）。

表 `settings`（KV 或单行配置）  
表 `trades`：

| 列 | 类型 |
|----|------|
| Id | INTEGER PK AUTOINCREMENT |
| TradeNo | TEXT UNIQUE |
| TradeType | INTEGER |
| CardSerialNo | TEXT |
| AmountFen | INTEGER |
| BalanceAfterFen | INTEGER NULL |
| OperatorName | TEXT |
| CreatedAt | TEXT (ISO) |
| Success | INTEGER |
| ErrorMessage | TEXT NULL |

ORM：Dapper；启动时 `EnsureCreated` 执行 DDL。

## 8. 原厂 DLL 清单（libs）

至少包含并随输出复制：

- `cpukrnl.dll` / `cpukrnlnet.dll`  
- `Lr.NativeWrapper.LrReader.dll` / `Lr.NativeWrapper.SpCardApp.dll`  
- `LrReader.dll` 及读卡器依赖（`dcic32.dll`、`dcrf32.dll`、`mwrf32.dll`、`icdll.dll`、`Mwic_32*.dll`、`ZY_CardReader.dll`、`mwCardReader.dll`、`m1reader.dll` 等按原目录实际存在复制）  
- `SpCardApp.dll` / `CardAppDll.dll` / `LrCardApp.dll`（若 SpCardApp 包装依赖）  
- 其它运行期缺省再按依赖补齐  

`analysis/api-map.md` 记录托管 API 与业务操作对照。

## 9. 参数导入（策略 C）

- 本程序参数页可手工维护 TerminalNo / 系统密钥(AppKey) / Station 等  
- 「从原系统导入」：读取 `paraminfo.set` 的库连接信息，再连本机 SQL `DatCent_Single`，导入 `SYSCONF.AppKey`、`StationInfo` 站点/区域；失败则提示确认 SQL 已启动  
- 储值/扣款页：先输入金额再点操作按钮，弹窗输入用户卡密码  
- 业务流水仅本程序 SQLite；导入只读原库配置，不写回原业务库

## 10. 错误处理

- 读卡器异常 → 状态条断开 + 功能禁用  
- 授权卡类型错误 → 明确提示，不发起写卡  
- PIN 错误 / 卡锁定 → 展示原厂错误语义的简短中文（无句号）  
- 写卡失败 → 不写成功流水；可写失败流水（Success=0）  
- UI 线程不直接调耗时卡操作；`async`/后台线程 + 进度禁用防重复提交  

## 11. 测试

- 领域：授权卡规则、金额校验（>0、分转换）  
- 仓储：SQLite 增查（可用临时库文件）  
- 卡适配：`FakeCardFacade` 供 UI/用例测试  
- 真实硬件：手工测试清单（连接/拔出、错卡类型、储值、扣款、读全量数据）

## 12. 非目标

- Ghidra 重写协议（分析仅作对照文档，运行路径仍走原厂 API）  
- 多机同步、权限账号体系、打印小票（首期不做）

## 13. 成功标准

- 无读卡器时功能正确禁用并提示  
- 非授权卡无法储值/扣款  
- 授权卡 + 正确 PIN 可完成储值与扣款，余额变化与流水一致  
- 卡片数据页能展示原厂可读的完整结构化字段  
- 解决方案结构清晰，可在 Visual Studio 打开编译  
