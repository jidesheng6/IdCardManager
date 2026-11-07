# LRCenter 卡相关 API 对照

来源目录：`F:\LrSingle\LRCenter`

## 业务模块（.NET）

| 功能 | 程序集 |
|------|--------|
| 用户卡储值 | `Lr.Intero.Center.Modules.StoreValueForCard.dll` |
| 用户卡扣款/取现 | `Lr.Intero.Center.Modules.WithdrawValueFromCard.dll` |
| 用户卡信息 | `Lr.Intero.Center.Modules.ReadCardInfo.dll` |

## 系统密钥（UserKey / AppKey）

原系统储值/扣款调用 `OilCard.CreditForLoad/UnLoad(..., strUserKey, ...)` 时，`strUserKey` 来自：

| 来源 | 说明 |
|------|------|
| SQL 库 `DatCent_Single.SYSCONF.AppKey` | 主来源；`DbService.GetSysConf()` 读入 `SysConfEntity.AppKey` |
| `paraminfo.set` | 仅含 `servername`/`dbname` 等连接信息，不含明文密钥 |
| 本程序「参数 → 从原系统导入」 | 读取 `paraminfo.set` 后连本机 SQL（Windows 认证）导入 `AppKey`、站点号、区域号 |

示例字段：`SYSCONF.AppKey`、`StationInfo.StationNo`、`StationInfo.Area`

## 推荐封装层（本项目直接使用）

| 能力 | 类型 | 方法 |
|------|------|------|
| 打开读卡器 | `Lr.NativeWrapper.LrReader.LrReader` | `Open` / `Close`（ctor: readerType, port） |
| 用户卡 | `SpCardApp.OilCard` | `CheckCard`、`Read*File`、`GetBalance`、`IsGreyCard`、`CreditForLoad`、`CreditForUnLoad` |
| 授权卡 | `SpCardApp.ValueCard` | `CheckCard` |

## 用户卡可读文件字段

### SP_PUBLIC_APP_FILE（公共应用）

IssuerID、AppTypeID、AppVer、SerialNo、EnablementDate、ExpirationDate、InstructionSetVer、FCI

### SP_COMMONLY_INFO_FILE（常用信息）

UseDefaultPassword、EmployeeNo、EmployeePassword、rfu

### SP_PRIVATE_DATA_FILE（私有数据）

CardType、EmployeeID、name、CertificateNo、CertificateType

### SP_SENSITIVITY_INFO_FILE（敏感信息）

UsedOilType、UsedAreaMode、UsedArea、UsedAmount、UsedFrequency、UsedTotalMoney、UsedMotorcar、rfu、PriceIndex

### 需 PIN

- `GetBalance(pin)` → 余额（分）
- `IsGreyCard(pin)` → 是否灰卡

## 读卡器原生依赖

`LrReader.dll` 聚合：`IC_*`、`dc_*`、`mw_*`、`zy_*` 等多厂商接口。
