# WheresMyCraftAt

> **Path of Exile 自動化製作插件**

WheresMyCraftAt 是一個專為 Path of Exile（流亡黯道）遊戲開發的自動化製作插件。這是一個 C# .NET 8.0 程式庫，與 ExileCore API 框架無縫整合，提供強大的遊戲內自動製作序列執行功能。

## ✨ 主要功能

- **🔄 自動化製作序列執行** - 支援複雜條件判斷和分支邏輯
- **🎯 智能製作策略** - 整合 Craft of Exile 模擬器來優化製作策略  
- **🖥️ 視覺化界面** - 基於 ImGui 的直觀界面，輕鬆設計和管理製作序列
- **📦 多種製作模式** - 支援貨幣標籤製作、背包物品製作等多種模式
- **🔍 豐富篩選系統** - 強大的物品篩選條件系統，精確控制製作目標
- **📊 日誌追蹤** - 完整的製作過程記錄和統計分析

## 🚀 快速開始

### 前置需求

- **遊戲環境**: Path of Exile 遊戲客戶端
- **框架依賴**: ExileCore 插件框架
- **開發環境**: .NET 8.0 SDK 或 Visual Studio 2022

### 安裝步驟

1. **設定 ExileCore 環境變數**
   ```bash
   export exapiPackage="/path/to/your/ExileCore"
   ```

2. **編譯專案**
   ```bash
   # 使用 dotnet CLI
   dotnet build --configuration Release
   
   # 或使用 Visual Studio
   # 開啟 WheresMyCraftAt.sln 並建置解決方案
   ```

3. **部署到 ExileCore**
   - 將編譯後的 DLL 複製到 ExileCore 的 Plugins 目錄
   - 重啟 ExileCore 載入插件

## 🏗️ 專案架構

### 核心架構

```
WheresMyCraftAt/
├── 📁 CraftingSequence/          # 製作序列核心邏輯
├── 📁 CraftingMenu/              # UI 界面和選單
├── 📁 Handlers/                  # 功能處理器
│   ├── GameHandler.cs           # 遊戲狀態管理
│   ├── ItemHandler.cs           # 物品操作
│   ├── InventoryHandler.cs      # 背包管理
│   ├── StashHandler.cs          # 倉庫處理
│   └── FilterHandler.cs         # 篩選條件
├── 📁 Extensions/                # 擴展方法
├── 📁 Logging/                   # 日誌系統
└── 📁 Template Crafts/           # 製作範本
```

### 關鍵元件

**製作序列系統**:
- `ModifyThenCheck` - 使用通貨然後檢查結果
- `ConditionalCheckOnly` - 僅檢查物品條件
- `Branch` - 條件分支邏輯

**條件邏輯**:
- 支援 AND/OR/NOT 邏輯組合
- 多層條件嵌套
- 可設定通過條件的最小數量

**動作類型**:
- `Continue` - 繼續下一步
- `End` - 結束序列
- `GoToStep` - 跳轉到指定步驟
- `RepeatStep` - 重複當前步驟
- `Restart` - 重新開始序列

## 🔧 開發指南

### 建置命令

```bash
# 還原 NuGet 套件
dotnet restore

# Debug 建置
dotnet build

# Release 建置
dotnet build --configuration Release

# 清理建置產物
dotnet clean
```

### 程式碼風格

- **Handler 模式**: 主要功能以 Handler 類別組織
- **非同步處理**: 使用 `SyncTask<T>` 和 `CancellationToken`
- **設定管理**: 基於 ExileCore 的設定節點系統
- **日誌系統**: 統一的 `LogMessageType` 分級日誌

### 重要依賴

```xml
<PackageReference Include="ImGui.NET" Version="1.90.0.1" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="SharpDX.Mathematics" Version="4.2.0" />
```

## 📖 使用說明

### 基本工作流程

1. **啟動插件** - 在 ExileCore 中啟用 WheresMyCraftAt
2. **設計序列** - 使用視覺化界面創建製作序列
3. **設定條件** - 配置物品篩選和成功/失敗條件
4. **執行製作** - 啟動自動製作序列
5. **監控結果** - 透過日誌系統追蹤製作進度

### 製作序列範例

```csharp
// 基本製作步驟配置
var step = new CraftingStepInput
{
    CheckType = ConditionalCheckType.ModifyThenCheck,
    CurrencyItem = "Orb of Fusing",
    SuccessAction = SuccessAction.End,
    FailureAction = FailureAction.RepeatStep
};
```

## ⚠️ 注意事項

### 安全使用

- **遊戲規則遵循**: 確保使用方式符合 Path of Exile 使用條款
- **適度使用**: 建議合理設定延遲和暫停機制
- **備份物品**: 重要物品請在製作前備份

### 技術限制

- **平台支援**: 僅支援 Windows 平台 (.NET 8.0-windows)
- **遊戲版本**: 需要與 ExileCore 支援的遊戲版本相容
- **網路延遲**: 製作速度會受到遊戲伺服器延遲影響

## 🤝 貢獻指南

歡迎社群貢獻！請遵循以下步驟：

1. Fork 本專案
2. 創建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交變更 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 開啟 Pull Request

## 📄 授權條款

本專案採用適當的開源授權條款。詳情請參閱 LICENSE 文件。

## 🔗 相關資源

- [Path of Exile](https://www.pathofexile.com/) - 官方遊戲網站
- [ExileCore](https://github.com/ExileCore/ExileCore) - 插件框架
- [Craft of Exile](https://www.craftofexile.com/) - 製作模擬器

---

**⚡ 讓製作變得更智能，讓遊戲體驗更順暢！**