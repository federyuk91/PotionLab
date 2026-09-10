# UI text color scheme

Source of truth: `Dati/UI_TextColorPalette.asset` (`UITextColorPalette`). Edit colors and aliases in the Inspector. The palette currently applies only to grimoire text; gameplay tooltips remain neutral.

| Meaning | Color | Recognized words |
| --- | --- | --- |
| HP | #A12642 | HP, health, vita |
| MP | #2855A1 | MP, mana |
| Damage | #A84020 | damage, damages, danno, danni |
| Healing | #296C3C | heal, heals, healing, restores, cura |
| Fire | #A64B16 | Fire, Burn, Burning, Lava |
| Ice | #246D87 | Ice, Frozen, Freeze |
| Poison | #70439A | Poison, Poisoned, toxins |
| Nature | #427039 | Grass, Algae, Seed, Seeds |
| Ground | #775030 | Grounded, Ground |
| Water | #267B86 | Water, Wet |
| Magic | #693B7D | Powered, empowered, Cost, Curse, Dark |
| Light | #85601F | Light, Bless |

Matching is case-insensitive and uses whole words. A preceding number is colored with its term (e.g. `3 HP`). Existing TMP tags are skipped. Grimoire cursor illumination blends the semantic base color to white, then back. Texts in TransformationData stay plain; do not duplicate color tags in descriptions.

Reuse: assign this asset to UI components; `BuildSourceColors(text, baseColor)` returns a source-indexed color array compatible with `TMP_CharacterInfo.index`. Rebuild only when text or palette revision changes. These dark colors target light/parchment backgrounds: check contrast before reusing on dark panels.

Spell data: `descrizioneBreve` is the concise gameplay tooltip. `descrizioneNormale` and `descrizionePotenziata` remain the detailed grimoire descriptions. Tooltip delay is set on `SpellHoverTooltip` (default 1 second); it hides on pointer exit, click, disabled/unavailable slots, pause and an open grimoire.
