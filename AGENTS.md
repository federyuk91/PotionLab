# AGENTS.md

Guidelines for working on TheGoodNightPotion.

## Unity/C# Code

- Use explicit types; avoid `var` except for trivial and highly readable cases.
- Prefer Inspector-assigned references via `[SerializeField]`.
- Use `GetComponent` only for local, non-repeated fallbacks; never use it to connect distant systems.
- Do not use `GameManager.Instance`, `TransformationManager.Instance`, `Find`, or runtime searches in refactored gameplay components.
- If a required reference is missing, log an explicit warning or error telling which Inspector reference must be assigned.
- Keep comments short and useful: non-obvious logic, Unity constraints, gameplay assumptions, or workarounds.
- Do not introduce new dependencies from refactored code to legacy code.

## Project Structure

- Legacy code stays in `Assets/_Project`.
- Refactored code stays in `Assets/_Refactory`.
- `TestingNew.unity` and assets under `Assets/_Refactory` are the reference for the new system.
- Do not modify legacy scenes or prefabs to support the refactor unless explicitly requested.
- When moving Unity assets, preserve `.meta` files to avoid breaking GUID references.

## Asset Naming And Paths

- Character animation clips: `Assets/_Refactory/Animation/T#_<Character>/Anim_<Character>_<StateOrAction>[_Variant].anim`.
- Character Animator Controllers: `Assets/_Refactory/Animation/_Controller/<Character>.controller`.
- Level object animation clips: `Assets/_Refactory/Animation/_LevelObject/<Category>/Anim_<Object>_<StateOrAction>[_Variant].anim`.
- Level object Animator Controllers: `Assets/_Refactory/Animation/_LevelObject/_Controller/AC_<Object>[_Variant].controller`.
- FX, potion, familiar, and UI animation assets: `Assets/_Refactory/Animation/<Category>/Anim_<Category>_<Action>.anim` and `AC_<Category>_<Action>.controller`.
- Refactored audio: `Assets/_Refactory/Audio/<Category>/SFX_<Category>_<ActionOrMeaning>.<ext>`.
- Refactored sprites/art: `Assets/_Refactory/Arts/<Category>/`, keeping UI art under `Arts/Ui`.
- Refactored prefabs: `Assets/_Refactory/Prefabs/<Category>/`, keeping potion prefabs under `Prefabs/Potions`.
- Refactored fonts/materials/data: `Assets/_Refactory/Fonts`, `Assets/_Refactory/Materials`, and `Assets/_Refactory/Dati/<Category>`.

## Responsibilities

### Core Gameplay

- `GameManager` handles level flow, start, death, completion, retry/next level, and essential potion/droppable registries.
- `GameManager` must not manage UI, dialogs, VFX, audio, light behavior, or transformation details.
- `TransformationManager` handles only the active form, form switching, and `OnTransformation`.
- `TransformationManager` must not decide potion, spell, status, UI, dialog, audio, or VFX rules.
- Each concrete `BaseCharacter` owns the rules for its form: potions, spells, ticks, vulnerabilities, immunities, transformations, and return-to-mage behavior.
- `BaseCharacter` exposes shared form data, including `spellList` and `TransformationLightColor`.
- `CharacterSpells` coordinates spell input, costs, powered state, and spell UI events; it must not know concrete UI objects.

### Stats, Status, And Ticks

- `CharacterStats` owns HP/MP, death, stat popup events, and stat change events; it must not directly manage UI.
- `CharacterStats.lightColor` and `hpColor` are stat popup colors, not transformation light colors.
- `CharacterStatusController` stores statuses and levels, emits status events, and does not decide gameplay meaning for status combinations.
- `StatusTickRunner` runs active status ticks by calling the current character; it must not contain form-specific rules.

### Light

- `LightController` owns light intensity, light timer, light color, and active light field.
- `LightController.IsPoweredFor(CharacterType)` decides powered state from light intensity or the active field.
- Transformation light color comes from `BaseCharacter.TransformationLightColor`, not from a character-color table inside `LightController`.
- `LightController` must not manage UI; light text and timer UI belong to `LightUIController`.

### UI, Dialogs, VFX, And Audio

- `CharacterUIController` owns HP, MP, stat popups, spell buttons, costs, sprites, status UI, spell bar visibility, retry panel, and death text.
- `LightUIController` owns only light level text and light timer UI.
- `DialogManager` owns all dialogs and dialog rules.
- `VFXStatusController` reacts to status/reaction events and visualizes them; it must not decide gameplay.
- `CharacterAudioController` reacts to potion/spell/status events and plays audio; it must not decide gameplay.
- Simple animator triggers for the active form may stay in the character; complex or reusable animation behavior should move to a dedicated component.

### Specific Gameplay Objects

- `PotionDestroyTrigger` consumes/destroys potions in a 2D trigger and reports effects through Inspector references.
- `Flower` owns the refactored flower behavior and uses `DialogManager`/`GameManager` through Inspector references.
- Spell effect components such as `DarkRaySpellEffect` must receive dependencies from Inspector and must not recover them through singletons.

## Refactor

- Before broad refactors, propose a short plan and call out risks.
- Keep legacy compatibility only inside legacy components or explicitly agreed fallback paths.
- Do not turn helper components into new generic managers.
- If a change adds a responsibility to a class, first check whether that responsibility belongs to an existing dedicated controller.
- After C# changes, run `dotnet build TheGoodNightPotion.sln` when possible.
