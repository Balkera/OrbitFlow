# Square Flow GUI Pro Visual Skin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the GUI Pro-CasualGame visual style and sweeter text treatment to Square Flow while preserving every existing button, action, gameplay mechanic, and gameplay-screen layout position.

**Architecture:** Keep `SquareFlowGameController` as the runtime uGUI builder. Add a small owned runtime asset subset under `Assets/Resources/SquareFlow/GUIPro/`, load those assets in `EnsureRuntimeSprites`, and apply them through existing helper methods so object hierarchy, `RectTransform` positions, sizes, and button listeners stay intact.

**Tech Stack:** Unity 6000.3.15f1, C#, uGUI, TextMesh Pro, NUnit edit-mode tests, GUI Pro-CasualGame imported assets.

---

### File Structure

- Modify: `Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs`
  - Adds tests that prove the GUI Pro skin is active while menu/gameplay controls remain unchanged.
  - Adds helper assertions for GUI Pro font, panel sprites, and button sprites.
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`
  - Loads GUI Pro font and selected texture sprites from `Resources`.
  - Applies text/font/outline/shadow styling in `AddText`.
  - Applies GUI Pro panel/button sprites through existing panel and button helper paths.
  - Keeps all existing layout constants, `RectTransform` positions, object names, and click handlers.
- Create: `Assets/Resources/SquareFlow/GUIPro/`
- Create: `Assets/Resources/SquareFlow/UI/`
  - Contains a small runtime-loadable copy of selected GUI Pro assets.
  - The original third-party files under `Assets/Layer Lab/GUI Pro-CasualGame/` remain unedited.

---

### Task 1: Write Failing Skin And Layout-Lock Tests

**Files:**
- Modify: `Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs`

- [ ] **Step 1: Add helper assertions near existing UI helper methods**

Add these helpers near `AssertGlassPanel`, `AssertSpriteButton`, and `FindText`:

```csharp
private static void AssertGuiProFont(TMP_Text text)
{
    Assert.That(text, Is.Not.Null);
    Assert.That(text.font, Is.Not.Null);
    Assert.That(text.font.name, Does.Contain("LilitaOne"));
    Assert.That(text.outlineWidth, Is.GreaterThan(0f));
}

private static void AssertGuiProPanel(Transform transform, string expectedTextureName = "BasicFrame_Round20")
{
    Assert.That(transform, Is.Not.Null);
    Image image = transform.GetComponent<Image>();
    Assert.That(image, Is.Not.Null);
    Assert.That(image.sprite, Is.Not.Null);
    Assert.That(image.sprite.texture.name, Is.EqualTo(expectedTextureName));
    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
    AssertSoftPanelShadow(transform);
}

private static void AssertGuiProButton(Transform button, string expectedTextureName)
{
    Assert.That(button, Is.Not.Null);
    Image image = button.GetComponent<Image>();
    Assert.That(image, Is.Not.Null);
    Assert.That(image.sprite, Is.Not.Null);
    Assert.That(image.sprite.texture.name, Is.EqualTo(expectedTextureName));
    Assert.That(button.GetComponent<Button>(), Is.Not.Null);
}
```

- [ ] **Step 2: Update the existing glass-panel helper to expect the GUI Pro panel**

Replace the body of `AssertGlassPanel` with:

```csharp
private static void AssertGlassPanel(Transform transform)
{
    AssertGuiProPanel(transform);
}
```

This makes the existing gameplay panel assertions part of the visual-skin lock.

- [ ] **Step 3: Add a menu test that preserves controls**

Add this test after `MainMenuPanelStretchesToFillCanvas`:

```csharp
[Test]
public void MainMenuUsesGuiProSkinWithoutAddingControls()
{
    GameObject host = new GameObject("SquareFlowControllerHost");

    try
    {
        SquareFlowGameController controller = host.AddComponent<SquareFlowGameController>();
        InvokePrivate(controller, "Awake");
        InvokePrivate(controller, "ShowMenu");

        Transform canvas = host.transform.Find("SquareFlowCanvas");
        Transform content = canvas.Find("MenuPanel/MenuContent");
        Assert.That(content, Is.Not.Null);

        AssertGuiProFont(FindText(content, "Square Flow"));
        AssertGuiProPanel(content.Find("MenuStatsCard"));
        AssertGuiProPanel(content.Find("InstructionsCard"), "BasicFrame_Round12");

        Transform playButton = content.Find("PlayButton");
        AssertGuiProButton(playButton, "Button01_225_Yellow");
        AssertGuiProFont(FindText(playButton, "Play"));

        Transform resetButton = content.Find("MenuStatsCard/ResetAllButton");
        AssertGuiProButton(resetButton, "Button01_175_Red");
        AssertGuiProFont(FindText(resetButton, "Reset All"));

        Button[] buttons = content.GetComponentsInChildren<Button>();
        Assert.That(buttons.Length, Is.EqualTo(BoardShapeCatalog.Count + 3));
        Assert.That(content.Find("ThemeToggle/ThemeButton"), Is.Not.Null);
        Assert.That(FindText(content, "Shop"), Is.Null);
        Assert.That(FindText(content, "Inventory"), Is.Null);
    }
    finally
    {
        Object.DestroyImmediate(host);
    }
}
```

- [ ] **Step 4: Add a gameplay test that locks the current layout and controls**

Add this test after `GameplayViewBuildsFigmaReferencePanelsAndHorizontalQueues`:

```csharp
[Test]
public void GameplayUsesGuiProSkinWithoutMovingControls()
{
    GameObject host = new GameObject("SquareFlowControllerHost");

    try
    {
        SquareFlowGameController controller = host.AddComponent<SquareFlowGameController>();
        InvokePrivate(controller, "Awake");
        InvokePrivate(controller, "SelectLevel", 5);
        InvokePrivate(controller, "StartLevel");

        Transform canvas = host.transform.Find("SquareFlowCanvas");
        Assert.That(canvas, Is.Not.Null);

        Transform header = canvas.Find("GameHeader");
        Transform status = canvas.Find("GameStatusBar");
        Transform orbiterStrip = canvas.Find("OrbiterStrip");
        Transform waiting = canvas.Find("WaitingQueue");
        Transform columns = canvas.Find("ShooterColumns");

        Assert.That(header, Is.Not.Null);
        AssertGuiProPanel(header.Find("ScoreCard"));
        AssertGuiProPanel(header.Find("BestCard"));
        AssertGuiProPanel(header.Find("LevelBadge"));
        AssertGuiProPanel(status);
        AssertGuiProPanel(orbiterStrip);
        AssertGuiProPanel(waiting);

        RectTransform statusRect = status.GetComponent<RectTransform>();
        Assert.That(statusRect.sizeDelta.y, Is.EqualTo(86f));
        Assert.That(statusRect.anchoredPosition.y, Is.EqualTo(-136f));

        Transform hudActions = status.Find("HudActions");
        Assert.That(hudActions, Is.Not.Null);
        Assert.That(hudActions.GetComponentsInChildren<Button>().Length, Is.EqualTo(4));
        AssertSpriteButton(hudActions.Find("HomeButton"), "FlowHomeButton");
        AssertSpriteButton(hudActions.Find("RestartButton"), "FlowRestartButton");
        AssertSpriteButton(hudActions.Find("PaletteButton"), "FlowPaletteButton");
        AssertSpriteButton(hudActions.Find("MuteButton"), "FlowMuteButton");

        AssertGuiProFont(FindText(header.Find("ScoreCard"), "SCORE"));
        AssertGuiProFont(FindText(header.Find("BestCard"), "BEST"));
        AssertGuiProFont(FindText(status, "0 moves"));
        AssertGuiProFont(FindText(waiting, "WAITING 0/5"));

        RectTransform waitingRect = waiting.GetComponent<RectTransform>();
        Assert.That(waitingRect.anchorMin, Is.EqualTo(new Vector2(0f, 0f)));
        Assert.That(waitingRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
        Assert.That(waitingRect.sizeDelta, Is.EqualTo(new Vector2(-16f, 164f)));
        Assert.That(waitingRect.offsetMin.x, Is.EqualTo(8f));
        Assert.That(waitingRect.offsetMax.x, Is.EqualTo(-8f));

        RectTransform[] cards = NamedChildren(columns, "ShooterColumnCard");
        Assert.That(cards.Length, Is.EqualTo(3));
        for (int i = 0; i < cards.Length; i++)
            AssertGuiProPanel(cards[i]);

        Assert.That(columns.GetComponentsInChildren<Button>().Length, Is.EqualTo(3));
    }
    finally
    {
        Object.DestroyImmediate(host);
    }
}
```

- [ ] **Step 5: Run tests and verify they fail for the intended reason**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\aleny\Documents\GitHub\OrbitFlow" -runTests -testPlatform EditMode -testResults "Temp\gui-pro-skin-red.xml" -quit -nographics
```

Expected: edit-mode tests run and the two new tests fail because text still uses the default font or GUI Pro runtime resources are not loaded. Existing gameplay rule tests should not be the source of the new failure.

- [ ] **Step 6: Commit the failing tests**

```powershell
git add -- "Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs"
git commit -m "test: lock GUI Pro visual skin behavior"
```

---

### Task 2: Add Runtime-Loadable GUI Pro Asset Subset

**Files:**
- Create: `Assets/Resources/SquareFlow/GUIPro/LilitaOne-Regular SDF.asset`
- Create: `Assets/Resources/SquareFlow/GUIPro/BasicFrame_Round20.png`
- Create: `Assets/Resources/SquareFlow/GUIPro/BasicFrame_Round12.png`
- Create: `Assets/Resources/SquareFlow/GUIPro/Button01_225_Yellow.png`
- Create: `Assets/Resources/SquareFlow/GUIPro/Button01_225_Blue.png`
- Create: `Assets/Resources/SquareFlow/GUIPro/Button01_175_Blue.png`
- Create: `Assets/Resources/SquareFlow/GUIPro/Button01_175_Red.png`
- Create: `Assets/Resources/SquareFlow/GUIPro/Button01_175_Green.png`
- Create: `Assets/Resources/SquareFlow/GUIPro/Label_Ribbon_Single_Orange.png`
- Create: `Assets/Resources/SquareFlow/UI/FlowCrown.png`
- Create: `Assets/Resources/SquareFlow/UI/FlowGem.png`
- Create: `Assets/Resources/SquareFlow/UI/FlowHomeButton.png`
- Create: `Assets/Resources/SquareFlow/UI/FlowRestartButton.png`
- Create: `Assets/Resources/SquareFlow/UI/FlowPaletteButton.png`
- Create: `Assets/Resources/SquareFlow/UI/FlowMuteButton.png`

- [ ] **Step 1: Copy only the selected original assets into `Resources`**

Run:

```powershell
New-Item -ItemType Directory -Force -Path "Assets/Resources/SquareFlow/GUIPro" | Out-Null
New-Item -ItemType Directory -Force -Path "Assets/Resources/SquareFlow/UI" | Out-Null
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular SDF.asset" -Destination "Assets/Resources/SquareFlow/GUIPro/LilitaOne-Regular SDF.asset"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Frame/BasicFrame_Round20.png" -Destination "Assets/Resources/SquareFlow/GUIPro/BasicFrame_Round20.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Frame/BasicFrame_Round12.png" -Destination "Assets/Resources/SquareFlow/GUIPro/BasicFrame_Round12.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Button/Button01_225_Yellow.png" -Destination "Assets/Resources/SquareFlow/GUIPro/Button01_225_Yellow.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Button/Button01_225_Blue.png" -Destination "Assets/Resources/SquareFlow/GUIPro/Button01_225_Blue.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Button/Button01_175_Blue.png" -Destination "Assets/Resources/SquareFlow/GUIPro/Button01_175_Blue.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Button/Button01_175_Red.png" -Destination "Assets/Resources/SquareFlow/GUIPro/Button01_175_Red.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Button/Button01_175_Green.png" -Destination "Assets/Resources/SquareFlow/GUIPro/Button01_175_Green.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Label/Label_Ribbon_Single_Orange.png" -Destination "Assets/Resources/SquareFlow/GUIPro/Label_Ribbon_Single_Orange.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Crown.png" -Destination "Assets/Resources/SquareFlow/UI/FlowCrown.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Gem03_Diamond_Blue.png" -Destination "Assets/Resources/SquareFlow/UI/FlowGem.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_PictoIcons/128/Pictoicon_Home_0.Png" -Destination "Assets/Resources/SquareFlow/UI/FlowHomeButton.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_PictoIcons/128/Pictoicon_Refresh.Png" -Destination "Assets/Resources/SquareFlow/UI/FlowRestartButton.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_PictoIcons/128/Pictoicon_Magic.Png" -Destination "Assets/Resources/SquareFlow/UI/FlowPaletteButton.png"
Copy-Item -LiteralPath "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_PictoIcons/128/Pictoicon_Sound_Off.Png" -Destination "Assets/Resources/SquareFlow/UI/FlowMuteButton.png"
```

Do not copy `.meta` files from the third-party package. Unity must generate new `.meta` files for the owned runtime copies to avoid duplicate GUIDs.

- [ ] **Step 2: Force Unity to import the new assets**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\aleny\Documents\GitHub\OrbitFlow" -quit -nographics
```

Expected: Unity exits with code 0 and creates `.meta` files next to the copied resources.

- [ ] **Step 3: Commit the runtime asset subset**

```powershell
git add -- "Assets/Resources/SquareFlow/GUIPro" "Assets/Resources/SquareFlow/UI"
git commit -m "chore: add GUI Pro runtime skin assets"
```

---

### Task 3: Load GUI Pro Font And Sprites In Runtime UI

**Files:**
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`

- [ ] **Step 1: Add private fields near the existing sprite fields**

Add these fields after `private Sprite muteButtonSprite;`:

```csharp
private TMP_FontAsset guiProFont;
private Sprite guiProPanelSprite;
private Sprite guiProInsetPanelSprite;
private Sprite guiProPlayButtonSprite;
private Sprite guiProPrimaryButtonSprite;
private Sprite guiProSmallButtonSprite;
private Sprite guiProDangerButtonSprite;
private Sprite guiProConfirmButtonSprite;
private Sprite guiProTitleRibbonSprite;
```

- [ ] **Step 2: Extend `EnsureRuntimeSprites` to load the GUI Pro assets**

Add this block at the end of `EnsureRuntimeSprites`, after the existing `muteButtonSprite` load:

```csharp
guiProFont = Resources.Load<TMP_FontAsset>("SquareFlow/GUIPro/LilitaOne-Regular SDF");
guiProPanelSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/BasicFrame_Round20", "BasicFrame_Round20", new Vector4(88f, 88f, 88f, 88f), 180f);
guiProInsetPanelSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/BasicFrame_Round12", "BasicFrame_Round12", new Vector4(62f, 62f, 62f, 62f), 180f);
guiProPlayButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_225_Yellow", "Button01_225_Yellow", new Vector4(88f, 78f, 88f, 78f), 220f);
guiProPrimaryButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_225_Blue", "Button01_225_Blue", new Vector4(88f, 78f, 88f, 78f), 220f);
guiProSmallButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_175_Blue", "Button01_175_Blue", new Vector4(70f, 62f, 70f, 62f), 190f);
guiProDangerButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_175_Red", "Button01_175_Red", new Vector4(70f, 62f, 70f, 62f), 190f);
guiProConfirmButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_175_Green", "Button01_175_Green", new Vector4(70f, 62f, 70f, 62f), 190f);
guiProTitleRibbonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Label_Ribbon_Single_Orange", "Label_Ribbon_Single_Orange", new Vector4(96f, 58f, 96f, 58f), 210f);
if (guiProPanelSprite != null)
    glassPanelSprite = guiProPanelSprite;
```

- [ ] **Step 3: Run tests and verify they still fail on styling application**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\aleny\Documents\GitHub\OrbitFlow" -runTests -testPlatform EditMode -testResults "Temp\gui-pro-load-red.xml" -quit -nographics
```

Expected: the resources load, but tests still fail because `AddText`, menu panels, and buttons have not yet applied the font/sprites everywhere.

- [ ] **Step 4: Commit the runtime loading fields**

```powershell
git add -- "Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs"
git commit -m "feat: load GUI Pro skin resources"
```

---

### Task 4: Apply Sweet GUI Pro Text Styling Without Moving Text

**Files:**
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`

- [ ] **Step 1: Add text styling helpers near `ToTmpFontStyle`**

Add these methods before `ToTmpFontStyle`:

```csharp
private void ApplyGuiProTextSkin(TMP_Text text, int size, FontStyle style, Color color)
{
    if (guiProFont != null)
        text.font = guiProFont;

    text.fontSize = size;
    text.fontStyle = ToTmpFontStyle(style);
    text.color = color;
    text.outlineWidth = TextOutlineWidth(size, style);
    text.outlineColor = TextOutlineColor(color);
    text.characterSpacing = size >= 40 ? 1.5f : 0.5f;
    text.wordSpacing = 0f;
}

private static float TextOutlineWidth(int size, FontStyle style)
{
    if (size >= 52) return 0.18f;
    if (size >= 32) return 0.13f;
    return style == FontStyle.Bold || style == FontStyle.BoldAndItalic ? 0.09f : 0.055f;
}

private static Color32 TextOutlineColor(Color color)
{
    Color dark = Color.Lerp(new Color32(42, 36, 104, 255), Color.black, 0.12f);
    if (color.r + color.g + color.b < 1.5f)
        return new Color32(255, 255, 255, 190);

    return ColorWithAlpha(dark, 0.86f);
}
```

- [ ] **Step 2: Update `AddText` to use the styling helper**

Replace this block:

```csharp
text.text = value;
text.fontSize = size;
text.fontStyle = ToTmpFontStyle(style);
text.color = color;
text.alignment = ToTmpAlignment(alignment);
```

with:

```csharp
text.text = value;
ApplyGuiProTextSkin(text, size, style, color);
text.alignment = ToTmpAlignment(alignment);
```

- [ ] **Step 3: Keep text dimensions and overflow behavior unchanged**

Confirm this block remains unchanged in `AddText`:

```csharp
rect.sizeDelta = dimensions;
SetAnchored(rect, position);
text.textWrappingMode = TextWrappingModes.Normal;
text.overflowMode = TextOverflowModes.Truncate;
text.raycastTarget = false;
```

- [ ] **Step 4: Run the skin tests**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\aleny\Documents\GitHub\OrbitFlow" -runTests -testPlatform EditMode -testResults "Temp\gui-pro-text.xml" -quit -nographics
```

Expected: GUI Pro font assertions pass. Panel and button sprite assertions may still fail until Task 5.

- [ ] **Step 5: Commit text styling**

```powershell
git add -- "Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs"
git commit -m "feat: apply GUI Pro text styling"
```

---

### Task 5: Apply GUI Pro Panels And Buttons Through Existing Helpers

**Files:**
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`

- [ ] **Step 1: Add sprite selection helpers near `AddButton`**

Add these methods before `AddButton`:

```csharp
private Sprite ButtonSpriteForLabel(string label, Vector2 size)
{
    if (string.Equals(label, "Play", System.StringComparison.Ordinal)
        || string.Equals(label, "Next Level", System.StringComparison.Ordinal)
        || string.Equals(label, "Try Again", System.StringComparison.Ordinal))
        return guiProPlayButtonSprite != null ? guiProPlayButtonSprite : guiProConfirmButtonSprite;

    if (string.Equals(label, "Reset All", System.StringComparison.Ordinal))
        return guiProDangerButtonSprite;

    if (size.x <= 180f || size.y <= 70f)
        return guiProSmallButtonSprite;

    return guiProPrimaryButtonSprite;
}

private void ApplyGuiProPanelSkin(RectTransform rect, Sprite sprite, Color fallbackColor)
{
    Image image = rect.GetComponent<Image>();
    if (image == null) return;

    if (sprite != null)
    {
        image.sprite = sprite;
        image.type = HasSpriteBorder(sprite) ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;
    }
    else
    {
        image.color = fallbackColor;
    }

    ApplySoftPanelDepth(rect, 0.24f, 7f);
}
```

- [ ] **Step 2: Update `AddGlassPanel` to use the GUI Pro panel sprite**

Replace `AddGlassPanel` with:

```csharp
private RectTransform AddGlassPanel(RectTransform parent, string objectName, Vector2 size)
{
    Sprite panelSprite = guiProPanelSprite != null ? guiProPanelSprite : glassPanelSprite != null ? glassPanelSprite : roundedRectSprite;
    RectTransform panel = AddPanel(parent, objectName, size, Color.white, panelSprite);
    ApplyOutline(panel, ColorWithAlpha(Color.white, 0.50f), 1f);
    ApplySoftPanelDepth(panel, 0.28f, 8f);
    SetRaycastTarget(panel, false);
    return panel;
}
```

- [ ] **Step 3: Update `AddButton` to skin buttons without changing actions**

Replace the first line inside the `AddButton(..., int fontSize)` method:

```csharp
RectTransform rect = AddPanel(parent, "Button", size, color);
```

with:

```csharp
Sprite buttonSprite = ButtonSpriteForLabel(label, size);
RectTransform rect = AddPanel(parent, "Button", size, buttonSprite != null ? Color.white : color, buttonSprite != null ? buttonSprite : roundedRectSprite);
```

Then replace:

```csharp
button.colors = ButtonColors(color);
```

with:

```csharp
button.colors = ButtonColors(buttonSprite != null ? Color.white : color);
```

Do not change this existing listener line:

```csharp
button.onClick.AddListener(action);
```

- [ ] **Step 4: Skin existing menu/result panels after creation**

In `ShowMenu`, after the `stats` panel is anchored, add:

```csharp
ApplyGuiProPanelSkin(stats, guiProPanelSprite, ColorWithAlpha(theme.Panel, 0.94f));
```

In `ShowMenu`, after the `instructions` panel is anchored, add:

```csharp
ApplyGuiProPanelSkin(instructions, guiProInsetPanelSprite != null ? guiProInsetPanelSprite : guiProPanelSprite, ColorWithAlpha(theme.Panel, 0.74f));
```

In `ShowResultPanel`, after the `ResultPanel` is anchored, add:

```csharp
ApplyGuiProPanelSkin(panel, guiProPanelSprite, theme.Panel);
```

Do not alter any `SetAnchored`, `SetTopStretch`, `SetBottomStretch`, or layout constant values.

- [ ] **Step 5: Give the existing menu title glow object the approved ribbon visual**

Replace the current `titleGlow` construction block in `ShowMenu`:

```csharp
RectTransform titleGlow = AddPanel(content, "MenuTitleGlow", new Vector2(360f, 66f), ColorWithAlpha(theme.TitleGlow, 0f));
SetAnchored(titleGlow, startLayout.TitlePosition + new Vector2(0f, -2f));
SetRaycastTarget(titleGlow, false);
```

with:

```csharp
RectTransform titleGlow = AddPanel(content, "MenuTitleGlow", new Vector2(760f, 126f), guiProTitleRibbonSprite != null ? Color.white : ColorWithAlpha(theme.TitleGlow, 0.22f), guiProTitleRibbonSprite != null ? guiProTitleRibbonSprite : roundedRectSprite);
SetAnchored(titleGlow, startLayout.TitlePosition + new Vector2(0f, -2f));
SetRaycastTarget(titleGlow, false);
titleGlow.SetAsFirstSibling();
```

This changes only the existing menu decorative surface. It does not add a button or affect gameplay UI positions.

- [ ] **Step 6: Run the skin tests and full edit-mode tests**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\aleny\Documents\GitHub\OrbitFlow" -runTests -testPlatform EditMode -testResults "Temp\gui-pro-skin-green.xml" -quit -nographics
```

Expected: all edit-mode tests pass. The new tests prove GUI Pro font/panels/buttons are applied and gameplay controls remain in their current positions.

- [ ] **Step 7: Commit panel and button skinning**

```powershell
git add -- "Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs" "Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs"
git commit -m "feat: apply GUI Pro panel and button skin"
```

---

### Task 6: Final Unity Smoke Check And Cleanup

**Files:**
- No source changes expected unless smoke testing reveals a real regression.

- [ ] **Step 1: Check git state before smoke**

Run:

```powershell
git status --short --branch
```

Expected: only the user's pre-existing imported `Assets/Layer Lab/` files and TextMesh Pro fallback asset may remain outside committed work.

- [ ] **Step 2: Open the scene in Unity and enter Play Mode manually**

Open:

```text
Assets/Scenes/SampleScene.unity
```

Check:

- Main menu uses the sweet GUI Pro-style font.
- Menu still has Play, Reset All, theme toggle, and the level buttons.
- Menu does not include Shop, Inventory, reward, or new control buttons.
- Play starts the level.
- Gameplay score, best, level, moves/combo, orbiter strip, waiting queue, and shooter columns remain in their existing positions.
- Home, restart, palette/theme, and mute still work.
- Shooter column buttons and waiting queue buttons still fire shooters.
- Result panel buttons still work.

- [ ] **Step 3: Run one final edit-mode verification**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\aleny\Documents\GitHub\OrbitFlow" -runTests -testPlatform EditMode -testResults "Temp\gui-pro-final.xml" -quit -nographics
```

Expected: Unity exits with code 0 and `Temp/gui-pro-final.xml` reports no failing tests.

- [ ] **Step 4: Final commit if smoke produced any source fix**

If Step 2 required a source fix, commit only the files touched for that fix:

```powershell
git add -- "Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs" "Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs"
git commit -m "fix: polish GUI Pro skin smoke issues"
```

If Step 2 produced no source changes, do not create an empty commit.
