# IdCardManager

加油站卡储值简化版（.NET Framework 4.8 + SunnyUI）

## 功能

- 读卡器连接检测
- 授权卡类型校验（储值/扣款需授权卡）
- 用户卡储值与扣款
- 卡片完整数据展示
- 本地 SQLite 流水
- 参数配置与原系统目录导入

## 构建

```bat
dotnet restore IdCardManager.sln
dotnet build IdCardManager.sln -c Release
dotnet test IdCardManager.sln -c Release
```

输出：`src\IdCardManager.UI\bin\Release\net48\IdCardManager.exe`

## 调试

参数页勾选「使用模拟读卡器」并保存后重启，可无硬件联调界面与流水。

## 文档

- 设计：`docs/superpowers/specs/2026-11-07-idcard-store-design.md`
- 计划：`docs/superpowers/plans/2026-11-07-idcard-store.md`
- API：`analysis/api-map.md`
