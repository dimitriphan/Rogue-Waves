# Rogue Tide

The Rogue Waves mode's own UI on a page: the TDS HUD's bracket frames and bevelled square tiles, recoloured with the
pack palette already used in game for the vote and the shop (docs/STYLE.md "Palette").

## Color Palette

- **Deep Swell**: `#2E4A62` - Primary background, panels and shadows
- **Rogue Teal**: `#3F8F96` - Main colour: tiles, buttons, frames, links (bevel `#4A9AA1`, shade `#377A80`)
- **Foam**: `#E9F1EE` - Text, rims, frame lines, highlights
- **Storm Coral**: `#E2725B` - The one hot accent: the active button, danger and alarm banners
- **Wet Sand**: `#D8C49A` - Quiet second accent: progress bars, washed-up things, the shop stall
- **Pearl**: `#F4EEF8` - Pure Aura only (sheen `#C9E4F2` / `#F2D9E6`)

Supporting tones: Mist `#B4C8CF` for secondary text on Deep Swell; Button Slate `#5B5D69` for back and cancel tiles
(the stock game's own).

## Typography

- **Headers**: Exo 2 Light, all caps, letter-spacing 0.12em (fallback: DejaVu Sans, Segoe UI Light)
- **Body Text**: Barlow Regular (fallback: DejaVu Sans, Segoe UI)
- **Labels and numbers**: Pixelify Sans (fallback: DejaVu Sans Mono)

All three are free Google Fonts.

## Shapes

- **Bracket frame**: a thin Foam line along the top and right of a group, a small notch at its top-left, a filled
  corner tick at its top-right. No full boxes.
- **Tiles**: square, flat fill, a 3px lighter bevel inside, a 1.5px Foam outline set 3px outside. No rounded corners,
  no drop shadows.
- **Banners**: a flat fill with a 1px Foam outline set 2px outside, header type inside.

## Rules

- One Storm Coral element per view; everything else stays teal, foam and swell.
- Never pure black, never a black outline (the house style).
- Pearl means Pure Aura and nothing else.
- Contrast: Foam on Deep Swell is 8.0:1 and fine for any text. Foam on Rogue Teal (3.3:1) and on Storm Coral (2.9:1)
  is for icons and large header text only, never body text. Storm Coral on Deep Swell (3.0:1) is for fills and large
  numbers, not small text.

## CSS tokens

```css
:root {
  --rt-swell: #2E4A62; --rt-swell-2: #35546D; --rt-teal: #3F8F96; --rt-teal-hi: #4A9AA1;
  --rt-foam: #E9F1EE; --rt-mist: #B4C8CF; --rt-coral: #E2725B; --rt-sand: #D8C49A;
  --rt-pearl: #F4EEF8; --rt-slate: #5B5D69;
  --rt-head: "Exo 2", "Segoe UI", sans-serif; --rt-body: "Barlow", "Segoe UI", sans-serif;
  --rt-label: "Pixelify Sans", ui-monospace, monospace;
}
```

## Best Used For

The mode's pages and editors (Scope Editor, Vessel Editor, Art Bench), the feature spotlight, pitch and progress decks,
and anything that presents Rogue Waves itself. Long dated reports may read better on a light page.
