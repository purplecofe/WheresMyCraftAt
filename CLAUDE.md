# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

WheresMyCraftAt 是一個專為 Path of Exile（流亡黯道）遊戲開發的自動化製作插件。這是一個 C# .NET 8.0 程式庫，設計用於與 ExileCore API 框架整合，提供遊戲內自動製作序列執行功能。

主要功能包括：
- 自動化製作序列執行，支援複雜條件判斷和分支邏輯
- 整合 Craft of Exile 模擬器來優化製作策略
- 視覺化 ImGui 界面來設計和管理製作序列
- 支援多種製作模式（貨幣標籤製作、背包物品製作）
- 豐富的物品篩選條件系統

## 建置指令

**前置需求**：
- 需要設定 `exapiPackage` 環境變數，指向 ExileCore 安裝目錄
- .NET 8.0 SDK 或 Visual Studio 2022

**常用指令**：
```bash
# 建置專案
dotnet build

# 建置 Release 版本
dotnet build --configuration Release

# 還原 NuGet 套件
dotnet restore

# 清理建置產物
dotnet clean
```

**使用 Visual Studio**：
- 開啟 `WheresMyCraftAt.sln`
- 設定 Debug 或 Release 配置
- 建置解決方案 (Ctrl+Shift+B)

## 專案架構

### 核心架構模式

**主要架構基於 Handler 模式**，每個 Handler 負責特定功能領域：

```
WheresMyCraftAt (主插件)
├── CraftingSequenceExecutor (執行引擎)
├── Handlers/ (功能處理器)
│   ├── GameHandler (遊戲狀態)
│   ├── ItemHandler (物品操作)
│   ├── InventoryHandler (背包管理)
│   ├── StashHandler (倉庫處理)
│   ├── FilterHandler (篩選條件)
│   └── Input Handlers (輸入處理)
├── CraftingSequence/ (序列定義)
└── CraftingMenu/ (UI 界面)
```

### 製作序列系統

**製作步驟類型**：
- `ModifyThenCheck` - 使用通貨然後檢查結果
- `ConditionalCheckOnly` - 僅檢查物品條件
- `Branch` - 分支邏輯處理

**條件系統**：
- 支援 AND/OR/NOT 邏輯組合
- 多層條件嵌套
- 可設定通過條件的最小數量

**動作類型**：
- `Continue`, `End`, `GoToStep`, `RepeatStep`, `Restart`

### 關鍵技術元件

**非同步處理**：
- 使用 `SyncTask<T>` 模式進行非阻塞操作
- `CancellationToken` 支援操作取消
- `AsyncResult` 處理非同步結果

**物品篩選系統**：
- 整合 `ItemFilterLibrary` 提供強大的篩選功能
- 支援 Craft of Exile 格式匯入和轉換
- 即時篩選條件編譯檢查

**UI 架構**：
- 基於 ImGui.NET 的即時渲染 UI
- 拖拽重新排序功能
- 視覺化錯誤提示和狀態顯示

## 技術依賴

**核心框架**：
- .NET 8.0 (net8.0-windows)
- ExileCore API
- ItemFilterLibrary

**重要套件**：
- ImGui.NET (1.90.0.1) - 用戶界面
- Newtonsoft.Json (13.0.3) - JSON 序列化
- SharpDX.Mathematics (4.2.0) - 數學運算

## 開發慣例

**命名規範**：
- Handler 類別使用 `Handler` 後綴 (例：`ItemHandler`, `GameHandler`)
- 設定類別使用 `Settings` 後綴
- 非同步方法使用 `Async` 前綴

**程式碼組織**：
- UI 相關樣式類別放在對應功能的 `Styling/` 子目錄
- 製作範本存放在 `Template Crafts/` 目錄
- Craft of Exile 相關結構在 `CraftofExileStructs/` 子目錄

**日誌系統**：
- 使用 `LogMessageType` 枚舉進行日誌分級
- 透過 `Logging.Logging` 類別統一管理日誌
- 支援自動日誌保存和統計追蹤

**設定管理**：
- 主設定類別：`WheresMyCraftAtSettings`
- 分類設定：`RunOptions`, `DelayOptions`, `DebugOptions`, `StylingDooDads`
- 使用 ExileCore 的設定節點系統

## 特殊考量

**遊戲整合**：
- 需要與 ExileCore 插件框架配合
- 依賴遊戲視窗狀態和座標系統
- 處理遊戲延遲和網路狀況

**安全性**：
- 包含輸入模擬功能（滑鼠、鍵盤）
- 需要謹慎處理遊戲狀態檢查
- 實作適當的停止和取消機制

**效能考量**：
- 使用背景任務避免阻塞 UI
- 實作適當的延遲機制
- 記憶體管理注意釋放資源