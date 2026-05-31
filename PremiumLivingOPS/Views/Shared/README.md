# Views/Shared — UI Conventions & Reusable Components

> **Rule: Every new Form must follow the UI conventions defined in this document.**
> All shared components are located in `PremiumLivingOPS/Views/Shared/`.

---

## File Index

| File | Type | Purpose |
|---|---|---|
| `AppShell.cs` | UserControl | TopNavBar + UserBar navigation chrome — **required on every Form** |
| `TopNavBar.cs` | UserControl | Apple-style top navigation bar with mega-menu |
| `UserInfoLabel.cs` | UserControl | User display name + department chip |
| `FormNavigator.cs` | static class | Router: (menuLabel, subItem) → target Form |
| `Palette.cs` | static class | Global colour constants |
| `CardPanel.cs` | static class | **Card wrapper: white card floating on grey page (3-layer nested structure)** |

---

## 1. AppShell — Navigation Chrome (Required)

Every Form must embed `AppShell` at the top to provide a consistent TopNavBar + UserBar.

```csharp
// Designer.cs
private AppShell _shell;

// InitializeComponent()
_shell = new AppShell();
_shell.SetPopupContainer(pnlMain);   // MUST pass the root panel so mega-menu can escape clipping
pnlMain.Controls.Add(pnlContent);   // Add content panel first (Fill)
pnlMain.Controls.Add(_shell);       // Add AppShell last (Top) → renders at the very top

// Form_Load / BindViewModel
_shell.MenuItemClicked += (menu, sub) => FormNavigator.NavigateTo(this, menu, sub);
_shell.LogoutClicked   += (s, e) => { SessionManager.Clear(); Application.Restart(); };
_shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
_shell.SetVisibleMenus(vm.AllowedMenus);
_shell.SetBreadcrumb("Module  ›  Page Title");
```

---

## 2. CardPanel — White Card Floating on Grey Page (Required)

### Visual Effect

All content sections (search bar, KPI bar, DataGridView, etc.) must use the **3-layer nested card structure** to achieve the "white card floating on a grey page" appearance.

```
┌─────── pnlOuter  BackColor=#F0F4F9  Padding=(20,14,20,8) ──────────────┐
│                                                                        │
│  ┌──── pnlInner  BackColor=White  PaintCardBorder (1px #DDE3EC) ─────┐ │
│  │                                                                   │ │
│  │   < Your content: TableLayoutPanel / DataGridView / etc. >        │ │
│  │                                                                   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

### Using the CardPanel.cs Helper

```csharp
using PremiumLivingOPS.Views.Shared;

// ① Fixed-height card  (Search bar, KPI bar — DockStyle.Top sections)
var (outerSearch, innerSearch) = CardPanel.Create(outerHeight: 300);
innerSearch.Controls.Add(mySearchTlp);   // add your content here
pnlMain.Controls.Add(outerSearch);

// ② Fill-remaining card  (DataGridView — DockStyle.Fill sections)
var (outerGrid, innerGrid) = CardPanel.CreateFill();
innerGrid.Controls.Add(dgvOrders);
pnlMain.Controls.Add(outerGrid);
```

### Colour Conventions for the 3-Layer Structure

| Layer | Panel | BackColor | Notes |
|-------|-------|-----------|-------|
| Page root | `pnlMain` | `#F0F4F9` | Full-page grey-blue background |
| Outer | `pnlOuter` | `#F0F4F9` + Padding | Padding creates the gap around the card |
| Card | `pnlInner` | `White` + `PaintCardBorder` | White background + 1px `#DDE3EC` border |
| Content | TLP / Control | `Transparent` | Actual layout layer |

### Padding Reference

| Use case | `outerPadding` |
|----------|----------------|
| Search bar / fixed-height sections | `new Padding(20, 14, 20, 8)` |
| Grid / fill sections | `new Padding(20, 12, 20, 0)` |

### Manual Construction (without helper)

If you need more control, build the 3-layer structure manually:

```csharp
// 1. Outer grey background
var pnlOuter = new Panel
{
    Dock      = DockStyle.Top,
    Height    = 300,
    BackColor = Color.FromArgb(240, 244, 249),  // Palette.BgPage
    Padding   = new Padding(20, 14, 20, 8)
};

// 2. White card with 1px border
var pnlInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
pnlInner.Paint += (s, e) =>
{
    var p = (Panel)s;
    using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
};

// 3. Content layer
var myContent = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
// ... add columns, controls, etc. ...

// Assemble
pnlInner.Controls.Add(myContent);
pnlOuter.Controls.Add(pnlInner);
parentPanel.Controls.Add(pnlOuter);
```

---

## 3. Palette — Colour Constants

All colours must reference `Palette.cs` constants. **Never hardcode `Color.FromArgb(...)` directly in Form files.**

```csharp
Palette.BgPage        // #F0F4F9 — page background / card outer layer
Palette.BgCard        // White   — card white background
Palette.BorderColor   // #DDE3EC — card border / divider lines
Palette.TextMain      // #0F1F35 — primary text
Palette.TextMuted     // #627087 — secondary label text
Palette.Primary       // #2F6FED — primary blue button
Palette.Success       // Green   — success state
Palette.Danger        // Red     — danger / error state
```

---

## 4. FormNavigator — Page Routing

```csharp
// Navigate to a target Form from any Form
FormNavigator.NavigateTo(this, "Order Processing", "View Order");
FormNavigator.NavigateTo(this, "Order Processing", "Modify Order");
```

---

## 5. Button Factories

All buttons must be created via the factory methods defined in each Form's Designer.cs. Do not instantiate raw `Button` objects with manually set colours.

| Method | Appearance | Use case |
|--------|-----------|----------|
| `MakePrimaryBtn(text, loc, w, h)` | Blue filled | Primary actions (Search, Save, Confirm) |
| `MakeOutlineBtn(text, loc, w, h)` | White + grey border | Secondary actions (Reset, Cancel) |
| `MakeWarningBtn(text, loc, w, h)` | Amber filled | Destructive / warning actions (Modify, Delete) |

---

## 6. New Form Checklist

Before creating any new Form, verify all of the following:

- [ ] `AppShell` embedded at top; `SetPopupContainer` called with root panel
- [ ] `MenuItemClicked` and `LogoutClicked` events subscribed
- [ ] `SetUser` / `SetVisibleMenus` / `SetBreadcrumb` called after ViewModel binding
- [ ] All content sections wrapped with `CardPanel.Create()` or `CardPanel.CreateFill()`
- [ ] Root panel `BackColor = Color.FromArgb(240, 244, 249)` (`Palette.BgPage`)
- [ ] All colours reference `Palette.cs` — no hardcoded `Color.FromArgb(...)` in Form files
- [ ] All buttons created via `MakePrimaryBtn` / `MakeOutlineBtn` / `MakeWarningBtn`

---

*Last updated: 2026-05-31*
