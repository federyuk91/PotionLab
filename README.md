# The Good Night Potion

## Refactor Character System

Questo documento riassume le regole attualmente presenti nel nuovo sistema di trasformazioni in `Assets/Refactory`.

Le tabelle incrociano:

- righe: status precedente del personaggio
- colonne: pozione bevuta
- celle: effetto risultante

Nota: il codice puo gestire piu status contemporanei e usa una priorita data dall'ordine degli `if`. Le tabelle sotto descrivono il caso in cui lo status indicato sia quello rilevante. Dove l'informazione non e presente o non e ancora implementata, la cella contiene `Da definire`.

Pozioni considerate: `Healing`, `Fire`, `Lava`, `Ice`, `Water`, `Grass`, `Light`, `Dark`, `Poison`, `Ground`.

Status considerati: `Nessuno`, `Burned`, `Wet`, `Freezed`, `Poisoned`, `Grass`, `Grounded`, `Algae`.

## Mage

Fonte: `Assets/Refactory/Transformation/MageCharacter.cs`.

| Status precedente | Healing | Fire | Lava | Ice | Water | Grass | Light | Dark | Poison | Ground |
|---|---|---|---|---|---|---|---|---|---|---|
| Nessuno | Cura `baseValue` HP | Aumenta `Burned` | Danno `baseValue` | Aggiunge `Freezed` | Aggiunge `Wet` | Aggiunge `Grass` | Aggiunge `baseValue` MP; se MP al massimo aumenta `lightLevel` | Se MP = 0 danno 2, altrimenti perde `baseValue` MP | Aumenta `Poisoned` | Aumenta `Grounded` |
| Burned | Cura `baseValue` HP | Aumenta `Burned` e prende 1 danno | Trasforma in `Balrog` | Rimuove `Burned`, aggiunge `Wet`, trigger `vaporing` | Rimuove `Burned`, trigger `smoking` | Aumenta `Burned` | Aggiunge `baseValue` MP; se MP al massimo aumenta `lightLevel` | Se MP = 0 danno 2, altrimenti perde `baseValue` MP | Danno 2, `TriggerExplosion`, trigger `poisonExplosion` | Rimuove `Burned`, aumenta `Grounded` |
| Wet | Cura `baseValue` HP | Rimuove `Wet`, trigger `smoking` | Rimuove `Wet`, danno `baseValue - 1` | Rimuove `Wet`, danno 3, aggiunge `Freezed` | Aggiunge `Wet` | Rimuove `Wet`, aumenta `Algae` | Aggiunge `baseValue` MP; se MP al massimo aumenta `lightLevel` | Se MP = 0 danno 2, altrimenti perde `baseValue` MP | Rimuove `Wet`, trasforma in `PupperFish` | Rimuove `Wet`, aumenta `Grounded` |
| Freezed | Cura `baseValue` HP | Rimuove `Freezed`, aggiunge `Wet`, trigger `vaporing` | Rimuove `Freezed` | Danno 3, aggiunge `Freezed` | Danno 2 | Immunita | Aggiunge `baseValue` MP; se MP al massimo aumenta `lightLevel` | Se MP = 0 danno 2, altrimenti perde `baseValue` MP | Se e gia `Poisoned`, immunita; altrimenti aumenta `Poisoned` | Immunita |
| Poisoned | Cura `baseValue` HP | Danno 2, `TriggerExplosion`, trigger `poisonExplosion` | Danno `baseValue` | Da definire | Rimuove `Poisoned`; se `Grounded` diminuisce `Grounded`, altrimenti aggiunge `Wet` | Immunita | Aggiunge `baseValue` MP; se MP al massimo aumenta `lightLevel` | Se MP = 0 danno 2, altrimenti perde `baseValue` MP | Aumenta `Poisoned` | Aumenta `Grounded` |
| Grass | Cura `baseValue` HP | Rimuove `Grass`, aumenta `Burned` | Danno `baseValue` | Da definire | Rimuove `Grass`, trasforma in `Tree` | Aggiunge `Grass` | Aggiunge `baseValue` MP; se MP al massimo aumenta `lightLevel` | Se MP = 0 danno 2, altrimenti perde `baseValue` MP | Rimuove `Grass` | Rimuove `Grass`, aumenta `Grounded` |
| Grounded | Cura `baseValue` HP | Immunita | Rimuove `Grounded` | Rimuove `Grounded`, trasforma in `Yeti` | Diminuisce `Grounded` | Cura 2 HP | Aggiunge `baseValue` MP; se MP al massimo aumenta `lightLevel` | Se MP = 0 danno 2, altrimenti perde `baseValue` MP | Se e gia `Poisoned`, immunita; altrimenti aumenta `Poisoned` | Aumenta `Grounded` |
| Algae | Cura `baseValue` HP | Aggiunge mana pari a `algaeLevel * 2`, rimuove `Algae` | Danno `baseValue` | Rimuove `Algae`, aggiunge `Freezed` | Aumenta `Algae` | Immunita | Aggiunge `baseValue` MP; se MP al massimo aumenta `lightLevel` | Se MP = 0 danno 2, altrimenti perde `baseValue` MP | Rimuove `Algae` | Rimuove `Algae`, aumenta `Grounded` |

Tick implementati per Mage:

| Status attivo | Delay | Effetto |
|---|---:|---|
| Burned | 2s | Trigger `isDamaged`, danno pari a `fireLevel` |
| Poisoned | 4s, infinito se `Freezed` | Trigger `isDamaged`; se `Grounded` danno 1, altrimenti danno pari a `poisonLevel`, decrementa `poisonLevel`, rimuove `Poisoned` a 0 |
| Grounded | 5s solo se `groundLevel >= 3` | Trigger `isDamaged`, danno 2 |
| Freezed | Da definire | Nessun effetto implementato oltre a log |

## Balrog

Fonte: `Assets/Refactory/Transformation/BalrogCharacter.cs`.

| Status precedente | Healing | Fire | Lava | Ice | Water | Grass | Light | Dark | Poison | Ground |
|---|---|---|---|---|---|---|---|---|---|---|
| Nessuno | Immunita | Aumenta `Burned` | Cura `baseValue` HP | Danno 4 | Rimuove `Burned` se presente | Immunita | Trigger `ReturnMage` | Aggiunge 2 MP | Danno 1 | Danno 2 |
| Burned | Immunita | Aumenta `Burned` | Cura `baseValue` HP | Rimuove `Burned` | Rimuove `Burned` | Immunita | Trigger `ReturnMage` | Aggiunge 2 MP | `TriggerExplosion`, danno 2 | Rimuove `Burned` |
| Wet | Immunita | Aumenta `Burned` | Cura `baseValue` HP | Danno 4 | Rimuove `Burned` se presente | Immunita | Trigger `ReturnMage` | Aggiunge 2 MP | Danno 1 | Danno 2 |
| Freezed | Immunita | Aumenta `Burned` | Cura `baseValue` HP | Danno 4 | Rimuove `Burned` se presente | Immunita | Trigger `ReturnMage` | Aggiunge 2 MP | Danno 1 | Danno 2 |
| Poisoned | Immunita | Aumenta `Burned` | Cura `baseValue` HP | Danno 4 | Rimuove `Burned` se presente | Immunita | Trigger `ReturnMage` | Aggiunge 2 MP | Danno 1 | Danno 2 |
| Grass | Immunita | Aumenta `Burned` | Cura `baseValue` HP | Danno 4 | Rimuove `Burned` se presente | Immunita | Trigger `ReturnMage` | Aggiunge 2 MP | Danno 1 | Danno 2 |
| Grounded | Immunita | Aumenta `Burned` | Cura `baseValue` HP | Danno 4 | Rimuove `Burned` se presente | Immunita | Trigger `ReturnMage` | Aggiunge 2 MP | Danno 1 | Danno 2 |
| Algae | Immunita | Aumenta `Burned` | Cura `baseValue` HP | Danno 4 | Rimuove `Burned` se presente | Immunita | Trigger `ReturnMage` | Aggiunge 2 MP | Danno 1 | Danno 2 |

Tick implementati per Balrog:

| Status attivo | Delay | Effetto |
|---|---:|---|
| Burned | `7f - fireLevel` | Cura 1 HP |
| Poisoned | Da definire | Nessun effetto implementato |
| Grounded | Da definire | Nessun effetto implementato |
| Freezed | Da definire | Nessun effetto implementato |

Ingresso/uscita trasformazione:

| Evento | Effetto |
|---|---|
| Entrata in Balrog | Rimuove `Burned` |
| Uscita da Balrog | Porta HP a 1 tramite danno, imposta MP a 10 |

## Yeti

Fonte: `Assets/Refactory/Transformation/YetiCharacter.cs`.

| Status precedente | Healing | Fire | Lava | Ice | Water | Grass | Light | Dark | Poison | Ground |
|---|---|---|---|---|---|---|---|---|---|---|
| Nessuno | Cura `baseValue` HP | Immunita | Danno `baseValue` | Cura `baseValue` HP | Immunita | Immunita | Danno `baseValue` e aggiunge `baseValue` MP | Se MP > 0 perde 1 MP, altrimenti danno 2 | Aggiunge `Poisoned` | Aumenta `Grounded` |
| Burned | Cura `baseValue` HP | Immunita | Danno `baseValue` | Cura `baseValue` HP | Immunita | Immunita | Danno `baseValue` e aggiunge `baseValue` MP | Se MP > 0 perde 1 MP, altrimenti danno 2 | Aggiunge `Poisoned` | Aumenta `Grounded` |
| Wet | Cura `baseValue` HP | Immunita | Danno `baseValue` | Cura `baseValue` HP | Immunita | Immunita | Danno `baseValue` e aggiunge `baseValue` MP | Se MP > 0 perde 1 MP, altrimenti danno 2 | Aggiunge `Poisoned` | Aumenta `Grounded` |
| Freezed | Cura `baseValue` HP | Immunita | Danno `baseValue` | Cura `baseValue` HP | Immunita | Immunita | Danno `baseValue` e aggiunge `baseValue` MP | Se MP > 0 perde 1 MP, altrimenti danno 2 | Aggiunge `Poisoned` | Aumenta `Grounded` |
| Poisoned | Cura `baseValue` HP | Immunita | Danno `baseValue` | Cura `baseValue` HP | Rimuove `Poisoned` | Immunita | Danno `baseValue` e aggiunge `baseValue` MP | Se MP > 0 perde 1 MP, altrimenti danno 2 | Aggiunge `Poisoned` | Aumenta `Grounded` |
| Grass | Cura `baseValue` HP | Immunita | Danno `baseValue` | Cura `baseValue` HP | Immunita | Immunita | Danno `baseValue` e aggiunge `baseValue` MP | Se MP > 0 perde 1 MP, altrimenti danno 2 | Aggiunge `Poisoned` | Aumenta `Grounded` |
| Grounded | Cura `baseValue` HP | Immunita | Rimuove `Grounded` | Cura `baseValue` HP | Immunita | Immunita | Danno `baseValue` e aggiunge `baseValue` MP | Se MP > 0 perde 1 MP, altrimenti danno 2 | Aggiunge `Poisoned` | Aumenta `Grounded` |
| Algae | Cura `baseValue` HP | Immunita | Danno `baseValue` | Cura `baseValue` HP | Immunita | Immunita | Danno `baseValue` e aggiunge `baseValue` MP | Se MP > 0 perde 1 MP, altrimenti danno 2 | Aggiunge `Poisoned` | Aumenta `Grounded` |

Tick implementati per Yeti:

| Status attivo | Delay | Effetto |
|---|---:|---|
| Poisoned | 4s, oppure 5s se `Grounded` | Trigger `isDamaged`; se `Grounded` danno 1 senza ridurre `poisonLevel`, altrimenti danno 1, decrementa `poisonLevel`, rimuove `Poisoned` a 0 |
| Grounded | 5s | Se `groundLevel == 3`, danno 2 |
| Burned | Da definire | Nessun effetto implementato |
| Freezed | Da definire | Nessun effetto implementato |

Ritorno a Mage:

| Condizione | Effetto |
|---|---|
| Dopo modifiche HP/MP, se `HP == MP` | Trasforma in `Mage` |

## Tree

Fonte: `Assets/Refactory/Transformation/TreeCharacter.cs`.

| Status precedente | Healing | Fire | Lava | Ice | Water | Grass | Light | Dark | Poison | Ground |
|---|---|---|---|---|---|---|---|---|---|---|
| Nessuno | Immunita | Aumenta `Burned` | Danno `baseValue` | Danno 2 | Cura 2 HP | Aggiunge 2 MP | Aggiunge `baseValue` MP | Immunita | Danno 1 | Aumenta `Grounded` |
| Burned | Immunita | Aumenta `Burned` | Danno `baseValue` | Rimuove `Burned`, cura 2 HP, trigger `ReturnMage` | Rimuove `Burned`, trigger `ReturnMage` | Aggiunge 2 MP | Aggiunge `baseValue` MP | Immunita | Danno 1 | Rimuove `Burned`, aumenta `Grounded` |
| Wet | Immunita | Aumenta `Burned` | Danno `baseValue` | Danno 2 | Cura 2 HP | Aggiunge 2 MP | Aggiunge `baseValue` MP | Immunita | Danno 1 | Aumenta `Grounded` |
| Freezed | Immunita | Aumenta `Burned` | Danno `baseValue` | Danno 2 | Cura 2 HP | Aggiunge 2 MP | Aggiunge `baseValue` MP | Immunita | Danno 1 | Aumenta `Grounded` |
| Poisoned | Immunita | Aumenta `Burned` | Danno `baseValue` | Danno 2 | Cura 2 HP | Aggiunge 2 MP | Aggiunge `baseValue` MP | Immunita | Danno 1 | Aumenta `Grounded` |
| Grass | Immunita | Aumenta `Burned` | Danno `baseValue` | Danno 2 | Cura 2 HP | Aggiunge 2 MP | Aggiunge `baseValue` MP | Immunita | Danno 1 | Aumenta `Grounded` |
| Grounded | Immunita | Immunita | Rimuove `Grounded` | Danno 2 | Cura 3 HP | Aggiunge 2 MP | Aggiunge `baseValue` MP | Immunita | Immunita | Aumenta `Grounded` |
| Algae | Immunita | Aumenta `Burned` | Danno `baseValue` | Danno 2 | Cura 2 HP | Aggiunge 2 MP | Aggiunge `baseValue` MP | Immunita | Danno 1 | Aumenta `Grounded` |

Tick implementati per Tree:

| Status attivo | Delay | Effetto |
|---|---:|---|
| Burned | 3s | Danno pari a `fireLevel`, poi aumenta `Burned` |
| Grounded | 5s | Se `groundLevel == 3`, cura 1 HP |
| Poisoned | Da definire | Nessun effetto implementato |
| Freezed | Da definire | Nessun effetto implementato |

## PupperFish

Fonte: `Assets/Refactory/Transformation/PupperfishCharacter.cs`.

| Status precedente | Healing | Fire | Lava | Ice | Water | Grass | Light | Dark | Poison | Ground |
|---|---|---|---|---|---|---|---|---|---|---|
| Nessuno | Cura `baseValue` HP | Immunita | Danno `baseValue` | Danno `baseValue` | Aggiunge `Wet` | Aggiunge `Grass` | Aggiunge `baseValue` MP | Trasforma in `Mage` | Aggiunge 1 MP | Immunita |
| Burned | Cura `baseValue` HP | Immunita | Danno `baseValue` | Danno `baseValue` | Aggiunge `Wet` | Aggiunge `Grass` | Aggiunge `baseValue` MP | Trasforma in `Mage` | Aggiunge 1 MP | Immunita |
| Wet | Cura `baseValue` HP | Immunita | Danno `baseValue` | Danno `baseValue` | Aggiunge `Wet` | Rimuove `Wet`, aumenta `Algae` | Aggiunge `baseValue` MP | Trasforma in `Mage` | Aggiunge 1 MP | Immunita |
| Freezed | Cura `baseValue` HP | Immunita | Danno `baseValue` | Danno `baseValue` | Aggiunge `Wet` | Aggiunge `Grass` | Aggiunge `baseValue` MP | Trasforma in `Mage` | Aggiunge 1 MP | Immunita |
| Poisoned | Cura `baseValue` HP | Immunita | Danno `baseValue` | Danno `baseValue` | Aggiunge `Wet` | Aggiunge `Grass` | Aggiunge `baseValue` MP | Trasforma in `Mage` | Aggiunge 1 MP | Immunita |
| Grass | Cura `baseValue` HP | Immunita | Danno `baseValue` | Danno `baseValue` | Rimuove `Grass`, aumenta `Algae` | Aggiunge `Grass` | Aggiunge `baseValue` MP | Trasforma in `Mage` | Aggiunge 1 MP | Immunita |
| Grounded | Cura `baseValue` HP | Immunita | Danno `baseValue` | Danno `baseValue` | Aggiunge `Wet` | Aggiunge `Grass` | Aggiunge `baseValue` MP | Trasforma in `Mage` | Aggiunge 1 MP | Immunita |
| Algae | Cura `baseValue` HP | Immunita | Danno `baseValue` | Danno `baseValue` | Aumenta `Algae` | Immunita | Aggiunge `baseValue` MP | Trasforma in `Mage` | Aggiunge 1 MP | Immunita |

Tick implementati per PupperFish:

| Status attivo | Delay | Effetto |
|---|---:|---|
| Burned | Da definire | Nessun effetto implementato |
| Poisoned | Da definire | Nessun effetto implementato |
| Grounded | Da definire | Nessun effetto implementato |
| Freezed | Da definire | Nessun effetto implementato |

## Litch

Fonte: `Assets/Refactory/Transformation/LitchCharacter.cs`.

`LitchCharacter` contiene ancora `NotImplementedException` per le pozioni, i tick e gli hook di trasformazione. Tutte le interazioni sono quindi da definire.

| Status precedente | Healing | Fire | Lava | Ice | Water | Grass | Light | Dark | Poison | Ground |
|---|---|---|---|---|---|---|---|---|---|---|
| Nessuno | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Burned | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Wet | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Freezed | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Poisoned | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Grass | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Grounded | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Algae | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |

## WhiteMage

Fonte: `Assets/Refactory/Transformation/WhiteMageCharacter.cs`.

`WhiteMageCharacter` implementa solo uno scheletro di `Cast`; le pozioni, i tick e gli hook di trasformazione contengono ancora `NotImplementedException`. Tutte le interazioni sono quindi da definire.

| Status precedente | Healing | Fire | Lava | Ice | Water | Grass | Light | Dark | Poison | Ground |
|---|---|---|---|---|---|---|---|---|---|---|
| Nessuno | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Burned | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Wet | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Freezed | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Poisoned | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Grass | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Grounded | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |
| Algae | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire | Da definire |

## Trasformazioni note

| Da | Condizione | A |
|---|---|---|
| Mage | `Lava` con `Burned` | Balrog |
| Mage | `Ice` con `Grounded` | Yeti |
| Mage | `Poison` con `Wet` | PupperFish |
| Mage | `Water` con `Grass` | Tree |
| PupperFish | `Dark` | Mage |
| Yeti | Dopo cambio HP/MP, se `HP == MP` | Mage |

## Note di implementazione

- `CharacterStatusController` gestisce status e livelli: `fireLevel`, `algaeLevel`, `groundLevel`, `poisonLevel`.
- `StatusTickRunner` delega i tick al `BaseCharacter` attivo.
- Le regole qui documentate rappresentano lo stato corrente del refactor, non necessariamente il design finale.
- Le celle `Da definire` vanno chiarite prima di considerare completa una trasformazione.
