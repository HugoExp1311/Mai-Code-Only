# Shop System Setup Guide

## Quick Setup (5 steps)

### 1. Create Item Visual Assets

Right-click in Project window:
- `Create > Shop > Item Visual`
- Select item from dropdown (pulls from DefaultSettings.ShopItems)
- Asset name auto-generates as "Item_{itemId}"
- Assign icon sprite
- Set availability if needed

The editor shows:
- Item info from DefaultSettings (category, type, price, effect)
- Localization keys that will be used

Create one asset per item you want in the shop.

### 2. Create Item UI Prefab

Create a prefab with this structure:
```
ShopItem (GameObject)
├── Icon (Image)
├── NameText (TextMeshProUGUI)
├── PriceText (TextMeshProUGUI)
└── PurchaseButton (Button)
```

Add `ShopItemUI` component to root, assign references.

### 3. Setup Shop Panel Hierarchy

```
ShopPanel (UIPanel)
├── TabButtons
│   ├── GoodsButton (with ScrollViewTabButton)
│   └── GiftsButton (with ScrollViewTabButton)
├── TabManager (GameObject with ScrollViewTabManager)
└── Content
    ├── GoodsScrollView (ScrollRect)
    │   └── Content (with GridLayoutGroup)
    └── GiftsScrollView (ScrollRect)
        └── Content (with GridLayoutGroup)
```

### 4. Configure Components

**ScrollViewTabManager**:
- Click "Auto-Discover ScrollViews"
- Set initial section (e.g., "GoodsScrollView")

**ScrollViewTabButton** (on each tab button):
- Assign Tab Manager reference
- Set Target Section ID

**ShopManager** (add to ShopPanel):
- Assign Item Prefab
- Assign Goods Container (GoodsScrollView/Content)
- Assign Gifts Container (GiftsScrollView/Content)
- Add all ShopItemData assets to "All Items" list

### 5. Test

Play mode - items should populate automatically!

## Item Prefab Example Layout

Recommended layout for item prefab:
```
[Icon]  Item Name
        Money: 100
        [Buy Button]
```

**Required Components:**
- Icon (Image)
- Name (LocalizedText) - displays item name
- Price (LocalizedText) - displays "Money: {amount}" with variable substitution
- Purchase Button (Button)

**Optional:**
- Description (LocalizedText)

**Localization Keys Needed:**
- `UI_ItemPrice` - Format: "Money: {amount}" (or localized equivalent)
  - English: "Money: {amount}"
  - Vietnamese: "Tiền: {amount}"
  - Japanese: "お金: {amount}"

Use GridLayoutGroup on Content containers for automatic grid layout.

## Adding More Categories

1. Add new category to `ShopItemCategory` enum
2. Add new ScrollView to shop panel
3. Add new container field to `ShopManager`
4. Add populate call in `PopulateShop()`

## Tips

- Use sprites for icons (import as Sprite type)
- Set GridLayoutGroup cell size to match item prefab size
- Enable "Control Child Size" on GridLayoutGroup
- Use Content Size Fitter on Content for auto-sizing
- Test with different item counts to ensure scrolling works
