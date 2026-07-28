# AGENTS.md

Linee guida per lavorare su TheGoodNightPotion.

## Codice Unity/C#

- Usa tipi espliciti; evita `var` salvo casi banali e molto leggibili.
- Preferisci reference assegnate da Inspector con `[SerializeField]`.
- Usa `GetComponent` solo per fallback locali, non ripetuti, e mai come scorciatoia per collegare sistemi distanti.
- Mantieni commenti brevi e utili: logica non ovvia, vincoli Unity, assunzioni di gameplay.
- Non introdurre dipendenze dal sistema legacy nel refactor.

## Struttura progetto

- Il codice legacy resta in `Assets/_Project`.
- Il refactor resta in `Assets/_Refactory`.
- `TestingNew.unity` e gli asset sotto `Assets/_Refactory` sono il riferimento per il nuovo sistema.
- Non modificare scene/prefab legacy per adattarli al refactor, salvo richiesta esplicita.
- Quando sposti asset Unity, preserva sempre i `.meta` per non rompere i GUID.

## Responsabilita'

- `GameManager` gestisce flusso di livello, morte, completamento e registri di gioco essenziali.
- `GameManager` non deve gestire UI, dialoghi, VFX, audio o dettagli delle trasformazioni.
- `DialogManager` gestisce tutti i dialoghi.
- `CharacterUIController` gestisce HP, MP, spell UI, status UI e schermata morte.
- `LightUIController` gestisce solo UI della luce.
- `LightController` gestisce luce, timer luce e field di luce.
- `TransformationManager` gestisce solo il cambio forma e gli eventi di trasformazione.
- `CharacterStatusController` conserva status e livelli, emette eventi, ma non decide regole di gameplay.
- Ogni `BaseCharacter` concreto decide le regole della propria forma: pozioni, spell, tick e ritorno al mago.
- VFX/audio/animazioni complesse devono stare in componenti dedicati quando superano una chiamata semplice sull'animator della forma.

## Refactor

- Prima di refactor ampi, proponi un piano breve e segnala rischi.
- Mantieni compatibilita' legacy solo nei componenti legacy o con fallback espliciti gia' concordati.
- Non trasformare componenti helper in nuovi manager generici.
- Se una modifica aggiunge una responsabilita' a una classe, verifica prima che non appartenga a un controller dedicato.
- Dopo modifiche C#, esegui `dotnet build TheGoodNightPotion.sln` quando possibile.
