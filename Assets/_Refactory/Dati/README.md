# Dati del refactory

Questa cartella contiene gli asset di configurazione del sistema refactored. Gli asset vengono collegati da Inspector tramite GUID Unity: spostarli insieme ai rispettivi file `.meta` non rompe le reference, mentre duplicarli crea nuove identita.

## Struttura

| Percorso | Struttura dati | Contenuto e utilizzo |
| --- | --- | --- |
| `Achievements/` | `AchievementDatabase` | Fonte canonica di ID, nome, descrizione, icona e identificativi delle piattaforme per gli achievement. Viene usata dal flusso di sblocco e dal Compendium. |
| `DialogRules/` | `CharacterDialogRule` | Regole di dialogo per personaggio, tipo di pozione e combinazione di status. `DialogManager` seleziona da qui le battute contestuali. |
| `Endless/PhaseSettings/` | `EndlessPhaseSettings` | Fasi attive della modalita Endless: presentazione, evento, durata, variazione di velocita e probabilita di spawn. Sono assegnate alla scena Endless. |
| `Endless/LegacyPhaseSettings/` | vecchi phase settings | Configurazioni del precedente spawner, ancora referenziate da `SpawnerManager.prefab`. Lo script originario non e piu presente: non creare nuovi asset in questa cartella e migrare le reference prima di rimuoverla. |
| `GridList/` | `GridListDatabase`, `GridListCategoryData` | Database e categorie del Compendium ancora basate sulla griglia generica, attualmente familiari e notti. Le pozioni vengono costruite dal catalogo canonico. |
| `Potions/` | `PotionScriptable`, `PotionCatalog` | Ogni `PotionScriptable` descrive una famiglia e le sue varianti opzionali Small/Medium/Large, includendo valore gameplay, presentazione e ordine delle varianti. `PotionCatalog` contiene una sola reference per famiglia e ne stabilisce l'ordine nel Compendium. `EmptyPotion` e descrittiva e non sostituisce il prefab gameplay della bottiglia vuota. |
| `Transformations/` | `TransformationData` | Identita e presentazione delle forme: immagine, animazione idle, descrizione, metodi di trasformazione/cura e lista spell. Alimenta UI e Compendium delle trasformazioni. |
| `UI/` | `UITextColorPalette` | Regole condivise per colorare termini e valori nei testi UI. Non contiene logica gameplay. |

## Vincoli

- Conservare sempre asset e `.meta` insieme.
- Non caricare questi asset tramite path stringa a runtime; assegnarli da Inspector.
- Non aggiungere dati gameplay a `GridList`: il Compendium deve leggere le fonti canoniche del relativo dominio.
- Le descrizioni effetto delle pozioni sono generate da tipo e valore; usare l'override della variante solo quando serve un testo specifico.
- Le nuove fasi Endless devono usare `EndlessPhaseSettings`, non la struttura legacy.
