# Views/Shared — UI Conventions & Reusable Components

> **規則：所有新 Form 必須遵從本文件的 UI 規範。**  
> 所有共用元件均位於 `PremiumLivingOPS/Views/Shared/`。

---

## 檔案清單

| 檔案 | 類型 | 用途 |
|---|---|---|
| `AppShell.cs` | UserControl | TopNavBar + UserBar 導航外殼（所有頁面必用）|
| `TopNavBar.cs` | UserControl | Apple-style 頂部導航列，mega-menu |
| `UserInfoLabel.cs` | UserControl | 用戶姓名 + 部門顯示 chip |
| `FormNavigator.cs` | static class | 路由：(menuLabel, subItem) → 目標 Form |
| `Palette.cs` | static class | 全域顏色常數 |
| `CardPanel.cs` | static class | **卡片包裝器：白色卡片浮在灰色頁面的三層巢狀結構** |

---

## 一、AppShell — 導航外殼（必用）

所有 Form 必須在頂部嵌入 `AppShell`，提供統一的 TopNavBar + UserBar。

```csharp
// Designer.cs
private AppShell _shell;

// InitializeComponent()
_shell = new AppShell();
_shell.SetPopupContainer(pnlMain);   // 必須傳入 root panel，讓 mega-menu 可彈出
pnlMain.Controls.Add(pnlContent);   // 內容 panel 先加（Fill）
pnlMain.Controls.Add(_shell);       // AppShell 最後加（Top）→ 排列在最頂

// Form_Load / BindViewModel
_shell.MenuItemClicked += (menu, sub) => FormNavigator.NavigateTo(this, menu, sub);
_shell.LogoutClicked   += (s, e) => { SessionManager.Clear(); Application.Restart(); };
_shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
_shell.SetVisibleMenus(vm.AllowedMenus);
_shell.SetBreadcrumb("Module  ›  Page Title");
```

---

## 二、CardPanel — 白色卡片浮在灰色頁面（必用）

### 視覺效果

所有內容區塊（搜尋欄、KPI bar、DataGridView 等）均採用**三層巢狀卡片結構**，形成「白色卡片浮在灰色頁面上」的視覺效果。

```
┌─ pnlOuter  BackColor=#F0F4F9  Padding=(20,14,20,8) ──────────────────┐
│                                                                        │
│  ┌─ pnlInner  BackColor=White  PaintCardBorder (1px #DDE3EC) ───────┐ │
│  │                                                                   │ │
│  │   < 你的內容：TableLayoutPanel / DataGridView / etc. >           │ │
│  │                                                                   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

### 使用 CardPanel.cs helper

```csharp
using PremiumLivingOPS.Views.Shared;

// ① 固定高度卡片（搜尋欄、KPI bar 等 DockStyle.Top 區塊）
var (outerSearch, innerSearch) = CardPanel.Create(outerHeight: 300);
innerSearch.Controls.Add(mySearchTlp);   // 放入你的內容
pnlMain.Controls.Add(outerSearch);

// ② 填滿剩餘空間卡片（DataGridView 等 DockStyle.Fill 區塊）
var (outerGrid, innerGrid) = CardPanel.CreateFill();
innerGrid.Controls.Add(dgvOrders);
pnlMain.Controls.Add(outerGrid);
```

### 三層結構的顏色規範

| 層次 | Panel | BackColor | 說明 |
|------|-------|-----------|------|
| 頁面根 | `pnlMain` | `#F0F4F9` | 整頁灰藍色背景 |
| 外層 | `pnlOuter` | `#F0F4F9` + Padding | Padding 形成卡片四周間距 |
| 卡片 | `pnlInner` | `White` + `PaintCardBorder` | 白底 + 1px `#DDE3EC` 邊框 |
| 內容 | TLP / Control | `Transparent` | 實際內容排版層 |

### Padding 規範

| 用途 | `outerPadding` |
|------|----------------|
| 搜尋欄 / 固定高度區塊 | `new Padding(20, 14, 20, 8)` |
| Grid / 填滿區塊 | `new Padding(20, 12, 20, 0)` |

### 手動建立（不使用 helper）

如需更多自訂控制，可手動建立三層結構：

```csharp
// 1. 外層灰底
var pnlOuter = new Panel
{
    Dock      = DockStyle.Top,
    Height    = 300,
    BackColor = Color.FromArgb(240, 244, 249),  // Palette.BgPage
    Padding   = new Padding(20, 14, 20, 8)
};

// 2. 白色卡片（含 1px 邊框）
var pnlInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
pnlInner.Paint += (s, e) =>
{
    var p = (Panel)s;
    using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
};

// 3. 內容層
var myContent = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
// ... 加入欄位、控件等 ...

// 組裝
pnlInner.Controls.Add(myContent);
pnlOuter.Controls.Add(pnlInner);
parentPanel.Controls.Add(pnlOuter);
```

---

## 三、Palette — 顏色常數

所有顏色必須引用 `Palette.cs` 常數，禁止在各 Form 中重複硬編碼 `Color.FromArgb(...)`。

```csharp
// 常用顏色
Palette.BgPage        // #F0F4F9 — 頁面背景 / 卡片外層
Palette.BgCard        // White   — 卡片白底
Palette.BorderColor   // #DDE3EC — 卡片邊框 / 分隔線
Palette.TextMain      // #0F1F35 — 主要文字
Palette.TextMuted     // #627087 — 次要標籤文字
Palette.Primary       // #2F6FED — 主要藍色按鈕
Palette.Success       // Green   — 成功狀態
Palette.Danger        // Red     — 危險 / 錯誤狀態
```

---

## 四、FormNavigator — 頁面路由

```csharp
// 從任何 Form 導航至目標頁面
FormNavigator.NavigateTo(this, "Order Processing", "View Order");
FormNavigator.NavigateTo(this, "Order Processing", "Modify Order");
```

---

## 五、新 Form 建立檢查清單

建立任何新 Form 前，確認以下項目：

- [ ] `AppShell` 已嵌入頂部，`SetPopupContainer` 已呼叫
- [ ] `MenuItemClicked` 及 `LogoutClicked` 事件已訂閱
- [ ] `SetUser` / `SetVisibleMenus` / `SetBreadcrumb` 已在 ViewModel 綁定後呼叫
- [ ] 所有內容區塊均使用 `CardPanel.Create()` 或 `CardPanel.CreateFill()` 包裝
- [ ] 頁面根 Panel `BackColor = Color.FromArgb(240, 244, 249)`（即 `Palette.BgPage`）
- [ ] 所有顏色引用 `Palette.cs`，不硬編碼
- [ ] 按鈕使用 `MakePrimaryBtn` / `MakeOutlineBtn` / `MakeWarningBtn` 工廠方法

---

*Last updated: 2026-05-31*
