# Differenze Regole Dopo Refactor

Questo documento riassume le differenze di comportamento da verificare rispetto alla versione precedente.

## Aggiornato

- Gli spell di Balrog, Yeti, PufferFish e Albero sono stati riportati nel nuovo sistema.
- I campi speciali delle trasformazioni non vengono piu' creati e distrutti: ora vengono attivati e disattivati.
- Lo scudo/corteccia dell'Albero e' stato ripristinato: protegge da fuoco, lava e ghiaccio consumandosi.

## Da Verificare

### Veleno, ghiaccio e terreno

Il comportamento del veleno quando il personaggio e' congelato o interrato e' cambiato. Prima alcune combinazioni bloccavano il veleno o creavano terreno velenoso; ora questi casi sono meno espliciti.

### Trasformazioni e mana

Prima alcune trasformazioni davano anche mana bonus:

- Mago bruciato + lava -> Balrog + mana
- Mago interrato + ghiaccio -> Yeti + mana

Ora la trasformazione avviene, ma il bonus mana non e' sempre presente.

### Lava su ghiaccio

Prima la lava su un Mago congelato scioglieva il ghiaccio ma causava anche danno ridotto. Ora potrebbe limitarsi a rimuovere il congelamento.

### Erba e fuoco

Il comportamento tra erba e fuoco sembra diverso:

- prima l'erba sul fuoco poteva essere negata
- ora in alcuni casi puo' alimentare il fuoco

Serve confermare quale versione e' desiderata.

### Danni fissi di alcune forme

Alcuni danni erano fissi nella vecchia versione, per esempio:

- ghiaccio su PufferFish
- ghiaccio su Yeti

Ora alcuni valori dipendono dal valore della pozione. Va deciso se devono restare fissi o scalare con la pozione.

### Balrog e ritorno al Mago

La luce dovrebbe far tornare il Balrog alla forma Mago. Nella nuova versione questo passaggio dipende dalla corretta animazione/evento di ritorno.

## Priorita Consigliata

1. Confermare le regole di veleno + ghiaccio/terreno.
2. Confermare se le trasformazioni devono dare mana bonus.
3. Confermare i danni fissi delle forme speciali.
4. Confermare il ritorno Balrog -> Mago tramite luce.
