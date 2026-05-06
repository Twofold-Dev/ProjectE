# Interactive ASCII Hover Effect for Main Menu

## Overview

Add a decorative ASCII character grid behind the main menu card. When hovering a character, it scales up and glows brighter, and nearby characters get a subtle secondary glow via `:has()`. This preserves the Terminal CLI aesthetic while adding an engaging interactive backdrop.

**Confirmed S&Box CSS support:**
- `:hover` ✅
- `:has()` ✅ (implemented in `StyleSelector.TestHas()`)
- `transform: scale()` ✅ (`PanelTransform` with `Parse()`)
- `transition` ✅
- `text-shadow` ✅

---

## Implementation Steps

### Step 1: Define the ASCII art character grid in Razor

Add a decorative grid of individual `<span>` characters positioned behind the menu card. Each character is a separate element so it can receive `:hover`.

**File:** [`Code/UI/MainMenuPanel.razor`](Code/UI/MainMenuPanel.razor)

**Changes:**
1. Add a `_asciiArt` string array in the `@code` block containing lines of ASCII characters
2. Render the grid as individual `<span class="ascii-char">` elements inside a `<div class="ascii-grid">` container
3. Place the grid BEFORE the `menu-overlay` div but still inside the `if (isMainMenu && gm != null)` block, so it sits behind the menu card
4. The grid uses `pointer-events: all` while the characters themselves also allow pointer events

The character set should be a mix of terminal-looking symbols: `#`, `$`, `%`, `&`, `@`, `*`, `+`, `-`, `=`, `.`, `:`, `;`, `~`, `^`, `/`, `\`, `|`

**Structure:**
```razor
<div class="ascii-grid">
    @foreach ( var line in _asciiArt )
    {
        @foreach ( var ch in line )
        {
            <span class="ascii-char">@ch</span>
        }
        <br/>
    }
</div>
```

### Step 2: Style the grid container

**File:** [`Code/UI/MainMenuPanel.razor.scss`](Code/UI/MainMenuPanel.razor.scss)

**Add styles:**
- `.ascii-grid` positioned absolute, fills the parent, centers content
- Uses `flex-wrap: wrap` with `justify-content: center`
- Each `.ascii-char` is:
  - `display: inline-block` (for transform to work)
  - `color: #0f3f0f` (dim green, barely visible by default)
  - `font-size: 10px`
  - `line-height: 1`
  - `transition: all 0.15s ease`
  - `cursor: default`
  - `user-select: none`

### Step 3: Apply hover + has() effects

**File:** [`Code/UI/MainMenuPanel.razor.scss`](Code/UI/MainMenuPanel.razor.scss)

**Hover effect on individual character:**
```scss
.ascii-char:hover {
    color: #33ff00;
    text-shadow: 0 0 6px #33ff00;
    transform: scale(1.8);
    z-index: 2;
}
```

**Cascading glow on siblings using :has():**
```scss
.ascii-char:has(+ .ascii-char:hover),
.ascii-char:has(+ .ascii-char + .ascii-char:hover),
.ascii-char:has(+ .ascii-char + .ascii-char + .ascii-char:hover) {
    color: #1f7f1f;
    text-shadow: 0 0 3px rgba(51, 255, 0, 0.3);
}
```

This creates a cascading effect where characters next to the hovered one also glow, but dimmer.

**Alternative: Parent container with :has():**
```scss
.ascii-grid:has(.ascii-char:hover) .ascii-char:not(:hover) {
    /* subtle dim on non-hovered when any is hovered */
}
```

### Step 4: Ensure proper z-ordering

The `.ascii-grid` must render behind the `.menu-overlay`:
- `.ascii-grid` - no special z-index needed, rendered first in DOM
- `.menu-overlay` - rendered after in DOM, naturally on top
- The menu card itself has `pointer-events: all` and background

### Step 5: Add animated wave effect (optional)

A subtle CSS animation on the ASCII grid to add ambient movement:
```scss
@keyframes asciiPulse {
    0%, 100% { opacity: 0.5; }
    50% { opacity: 0.8; }
}
.ascii-grid {
    animation: asciiPulse 8s ease-in-out infinite;
}
```

---

## Files to Modify

| # | File | Action |
|---|------|--------|
| 1 | [`Code/UI/MainMenuPanel.razor`](Code/UI/MainMenuPanel.razor) | Add `_asciiArt` string array + render grid HTML before menu-overlay |
| 2 | [`Code/UI/MainMenuPanel.razor.scss`](Code/UI/MainMenuPanel.razor.scss) | Add `.ascii-grid`, `.ascii-char`, hover effects, `:has()` cascading, animations |

---

## Design Details

- **Font size**: 10px for ASCII chars so they're small background decoration
- **Default color**: `#0f3f0f` (very dim green) — barely visible as background texture
- **Hover color**: `#33ff00` (full bright green) with `text-shadow` glow
- **Neighbor glow**: `#1f7f1f` (medium dim green) with subtle shadow
- **Scale**: 1.8x on hover, neighbor chars don't scale
- **Transition**: 0.15s ease for smooth animation

---

## Visual Layout

```
╔═══════════════════════════════════╗
║  +--- ASCII GRID (background) ---+║
║  # $ % & @ * + - = . : ; ~ ^     ║
║  @ * + - = . : ; ~ ^ / \ | # $   ║
║  + - = . : ; ~ ^ / \ | # $ % &   ║
║  ══════════════════════════════   ║
║       ┌─────────────────┐         ║
║       │  PROJECT E      │         ║
║       │  [ READY ]      │         ║
║       │  [ START ]      │         ║
║       └─────────────────┘         ║
║  # $ % & @ * + - = . : ; ~ ^     ║
╚═══════════════════════════════════╝
```

The ASCII grid fills the entire screen behind the menu card, providing ambient texture. Characters near the hovered one glow in cascade.
