# Grimoire game options

Home retains serialized category value 6; Options is 7. The existing Home menu, citations and navigation buttons are preserved. The shared `UI Controller - Canvas` prefab contains an Audio page and an Options page, connected to `GameOptionsPanel` and `CompendiumView` through Inspector references.

## Controls

- Master, Music, Sound Effects: sliders, 0–100%.
- Fullscreen, Show Tooltips, Mage Dialogs: toggles.
- Text Speed: 0.5–2x for the progressively typed intro dialog. Instant potion catchphrases remain instant. Disabled when Mage Dialogs is off.
- Reset to Defaults: volumes 100%, speed 1x, toggles on. Does not reset progression or achievements.

Preferences use the `PotionLab.Options.` PlayerPrefs prefix. Changes apply immediately; disk writes are debounced by 0.5 unscaled seconds and flushed when leaving the page, on application pause while the page is active, and on quit. Preferences load before scene startup, including levels opened directly. With no saved fullscreen preference, the game's current display mode is preserved. Check fullscreen in a standalone build, not the embedded Unity Game view.

## Audio routing

Effects use AudioListener gain = Master × Sound Effects. Music sources must have `MusicVolumeBinding` with their AudioSource assigned: they ignore listener volume and use their authored volume × Master × Music. The music source in `Main Menu Refa` is connected. This preserves existing per-source volume balancing and does not require runtime object searches. Add the same binding to future music sources; do not attach it to ambient torch loops or spell effects. Sources that deliberately ignore listener volume need their own explicit routing.

## Dialog visibility

Mage Dialogs controls the character's dialogue box in every transformation and the spoken level introduction. It hides an already visible dialog and stops the intro voice immediately when switched off. It does not modify night titles, grimoire descriptions, gameplay rules or spell/potion sounds. Text-speed scaling changes only progressive reveal, not voice pitch or playback speed.

## Validation checklist

Open Home → Options → Potions → Transformations → Home; close and reopen the book. Check that only the requested pages remain active. Move each slider, toggle each control, then reload a level and restart the player to confirm persistence. Confirm that Music still plays with Sound Effects at zero, Master zero silences both, and tooltips return after re-enabling them. Verify intro text at 0.5x and 2x, and that disabling dialogs hides text and stops its voice. Test Reset without losing saved progression.
