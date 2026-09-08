// "Devil in the Cauldron" — Demon Funk / The Good Night Potion
// 110 BPM, 4/4, 36 bars (~1:19). Export cycles 0–36, stereo.
// For a continuous encounter loop, replace arrange(...) with infernoGroove.
setcpm(110 / 4)

// Dm9 / Ebmaj7 / Dm9 / A7b9: the flat second gives the demon its grin.
const demonChords = note("<[f3,a3,c4,e4] [g3,bb3,d4,eb4] [f3,a3,c4,e4] [g3,bb3,c#4,e4]>")

const demonDrums = stack(
  s("bd ~ bd ~").bank('RolandTR707').gain(.08),
  s("~ sd ~ sd").bank('RolandTR707').gain(.14),
  s("hh*8").bank('RolandTR707').gain("[.02 .03]*4"),
  s("~ ~ [~ rim] ~").bank('RolandTR707').gain(.04).pan(.62),
  s("~ cb ~ ~").bank('RolandTR808').gain(.035).pan(.35)
).orbit(1)

const demonBass = note(`<
  [d2 ~ d2 [f2 d2] ~ a1 c2 [c#2 d2]]
  [eb2 ~ eb2 [g2 eb2] ~ bb1 d2 [eb2 d2]]
  [d2 ~ d3 c2 ~ a1 [ab1 a1] c2]
  [a1 ~ a2 [g2 e2] ~ bb1 c#2 [e2 c#2]]
>`).s('sawtooth')
  .attack(.006).decay(.18).sustain(.08).release(.06)
  .lpf(sine.slow(4).range(520, 1150)).lpq(5)
  .shape(.16).gain(.62).orbit(2)

const crookedKeys = demonChords
  .struct("[~ x] ~ [~ x] [~ x]").s('gm_epiano1')
  .attack(.008).decay(.2).sustain(.12).release(.14).clip(.6)
  .hpf(260).lpf(1800).gain(.22).pan(.4).orbit(3)

const shadowPad = demonChords.s('triangle')
  .attack(.3).release(.55).hpf(280).lpf(1100)
  .room(.35).gain(.09).orbit(4)

// Short, repeated question; the last bar turns back towards D.
const devilHook = note(`<
  [a4 ~ d5 [eb5 d5] ~ c5 a4 ~]
  [bb4 ~ eb5 [f5 eb5] ~ d5 bb4 ~]
  [a4 ~ d5 [eb5 d5] ~ c5 [ab4 a4] ~]
  [g4 ~ bb4 [a4 g4] ~ e4 c#5 ~]
>`).s('triangle')
  .attack(.008).decay(.14).sustain(.12).release(.12)
  .hpf(300).lpf(2300).gain(.25)
  .delay(.12).delaytime(60 / 110 / 2).delayfeedback(.2)
  .room(.16).pan(.55).orbit(5)

const demonReply = note(`<
  [~ ~ ~ ~ ~ [f4 a4] ~ c5]
  [~ ~ ~ ~ ~ [g4 bb4] ~ d5]
  [~ ~ ~ ~ ~ [f4 ab4] a4 ~]
  [~ ~ ~ ~ ~ [g4 bb4] c#5 ~]
>`).s('sawtooth')
  .attack(.025).decay(.12).sustain(.08).release(.09)
  .hpf(420).lpf(1450).gain(.13).pan(.3).orbit(6)

const cursedBells = note("<~ [~ bb5 ~ g5] ~ [~ bb5 c#6 ~]>")
  .s('gm_xylophone').clip(.4).lpf(2400).gain(.1)
  .delay(.18).delaytime(60 / 110 / 2).delayfeedback(.25)
  .room(.3).pan(.7).orbit(7)

// The low hit and descending phrase mark the transformation.
const apparition = stack(
  note("<d2 ~ ~ ~>").s('sine')
    .attack(.01).decay(.5).sustain(0).release(.15).gain(.22).orbit(8),
  note("<[d5 ab4 f4 eb4] ~ ~ ~>").s('triangle')
    .decay(.18).sustain(.05).release(.25).gain(.18).room(.3).orbit(5),
  shadowPad,
  demonBass.mask("<0 1 1 1>").lpf(650),
  crookedKeys.mask("<0 0 1 1>"),
  devilHook.mask("<0 0 1 1>")
)

const stalkingGroove = stack(
  demonDrums, demonBass, crookedKeys,
  devilHook.mask("<1 0 1 0>"),
  demonReply.mask("<0 1 0 1>")
)

const smokeBreak = stack(
  demonBass.lpf(480).gain(.52),
  shadowPad,
  crookedKeys.mask("<1 0 1 0>").gain(.17),
  s("bd ~ ~ ~").bank('RolandTR707').gain(.045).orbit(1),
  s("hh*4").bank('RolandTR707').gain(.018).orbit(1),
  cursedBells
)

const infernoGroove = stack(
  demonDrums,
  demonBass,
  crookedKeys,
  devilHook,
  demonReply.mask("<0 1 0 1>"),
  cursedBells.mask("<0 0 0 1>")
)

const lastDance = stack(
  demonDrums,
  demonBass,
  crookedKeys,
  devilHook,
  shadowPad.gain(.07),
  cursedBells
)

const disappearance = stack(
  note("<[f3,a3,c4,e4] ~ ~ ~>").s('gm_epiano1')
    .release(2).room(.4).gain(.18).orbit(3),
  note("<[d2 ~ a1 d2] ~ ~ ~>").s('sawtooth')
    .decay(.2).sustain(.05).release(.2).lpf(600).gain(.45).orbit(2),
  note("<~ [a4 ab4 f4 eb4] d4 ~>").s('triangle')
    .decay(.15).sustain(.05).release(.4).gain(.16).room(.25).orbit(5)
)

arrange(
  [4, apparition],
  [8, stalkingGroove],
  [4, smokeBreak],
  [8, infernoGroove],
  [8, lastDance],
  [4, disappearance]
)
