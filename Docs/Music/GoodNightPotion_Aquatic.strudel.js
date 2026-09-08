// "Bubble Lagoon" — Aquatic Funk / The Good Night Potion
// 110 BPM, 4/4, 36 bars (~1:19). Export cycles 0–36, stereo.
// For a continuous gameplay loop, replace arrange(...) with lagoonGroove.
setcpm(110 / 4)

// Fmaj9 / G6/9 / Em7 / Am9: open, gently suspended colours.
const waterChords = note("<[a3,c4,e4,g4] [a3,b3,d4,e4] [g3,b3,d4,e4] [g3,b3,c4,e4]>")

const waterDrums = stack(
  s("bd ~ bd ~").bank('RolandTR707').gain(.065),
  s("~ rim ~ rim").bank('RolandTR707').gain(.065),
  s("hh*8").bank('RolandTR707').gain("[.018 .026]*4")
).orbit(1)

const swimmingBass = note(`<
  [f2 ~ f2 [a2 g2] ~ c2 e2 [g2 a2]]
  [g2 ~ d2 [e2 g2] ~ b1 d2 [f#2 g2]]
  [e2 ~ e2 [g2 a2] ~ b1 d2 [e2 g2]]
  [a1 ~ a2 [g2 e2] ~ c2 e2 [g2 e2]]
>`).s('sawtooth')
  .attack(.012).decay(.21).sustain(.12).release(.08)
  .lpf(sine.slow(8).range(450, 950)).lpq(3)
  .shape(.07).gain(.6).orbit(2)

const rippleKeys = waterChords
  .struct("[~ x] ~ [~ x] [~ x]").s('gm_epiano1')
  .attack(.015).decay(.25).sustain(.12).release(.23).clip(.7)
  .hpf(280).lpf(2200).gain(.23).pan(.4).orbit(3)

const tidePad = waterChords.s('triangle')
  .attack(.35).release(.6).hpf(280).lpf(1600)
  .room(.4).gain(.09).orbit(4)

// A recurring upward bubble figure followed by a floating answer.
const lagoonMelody = note(`<
  [c5 ~ [e5 g5] a5 ~ g5 e5 ~]
  [d5 ~ [e5 g5] a5 ~ b5 a5 ~]
  [b4 ~ [d5 e5] g5 ~ e5 d5 ~]
  [c5 ~ [e5 g5] a5 ~ g5 [e5 c5] ~]
>`).s('triangle')
  .attack(.025).decay(.2).sustain(.18).release(.22)
  .lpf(2600).gain(.25).pan(.56)
  .delay(.16).delaytime(60 / 110 / 2).delayfeedback(.24)
  .room(.25).orbit(5)

const bubbles = note(`<
  [~ ~ ~ [a5 c6]]
  [~ [b5 a5] ~ ~]
  [~ ~ ~ [g5 b5]]
  [~ [e6 c6] ~ ~]
>`).s('sine')
  .attack(.005).decay(.09).sustain(0).release(.06)
  .gain(.11).pan("<.25 .75 .35 .65>")
  .delay(.2).delaytime(60 / 110 / 4).delayfeedback(.2)
  .room(.2).orbit(6)

const coralChimes = note("<[~ e6 ~ g5] [~ d6 ~ b5] [~ b5 ~ g5] [~ c6 ~ e6]>")
  .s('gm_xylophone').clip(.4).lpf(2500).gain(.085)
  .room(.35).pan(.7).orbit(7)

const diveIn = stack(
  tidePad,
  bubbles,
  swimmingBass.mask("<0 1 1 1>").lpf(600),
  rippleKeys.mask("<0 0 1 1>"),
  lagoonMelody.mask("<0 0 1 1>").gain(.21)
)

const lagoonGroove = stack(
  waterDrums,
  swimmingBass,
  rippleKeys,
  lagoonMelody,
  bubbles.mask("<0 1 0 1>")
)

const deepWater = stack(
  tidePad,
  swimmingBass.lpf(400).gain(.5),
  rippleKeys.mask("<1 0 1 0>").gain(.18),
  lagoonMelody.mask("<0 1 0 1>").gain(.18),
  bubbles,
  s("hh*4").bank('RolandTR707').gain(.015).orbit(1)
)

const coralDance = stack(
  waterDrums,
  swimmingBass,
  rippleKeys,
  lagoonMelody.mask("<1 1 0 1>"),
  bubbles,
  coralChimes.mask("<0 0 1 0>")
)

const surfaceLight = stack(
  waterDrums,
  swimmingBass,
  rippleKeys,
  lagoonMelody,
  tidePad.gain(.065),
  coralChimes.mask("<0 1 0 1>")
)

const driftAway = stack(
  note("<[a3,c4,e4,g4] ~ ~ ~>").s('gm_epiano1')
    .attack(.03).release(2).room(.4).gain(.18).orbit(3),
  note("<[f2 ~ c2 f2] ~ ~ ~>").s('sawtooth')
    .decay(.25).sustain(.08).release(.2).lpf(500).gain(.4).orbit(2),
  note("<~ [c5 e5 g5 a5] g5 ~>").s('triangle')
    .attack(.025).decay(.2).sustain(.1).release(.6)
    .gain(.17).room(.3).orbit(5)
)

arrange(
  [4, diveIn],
  [8, lagoonGroove],
  [4, deepWater],
  [8, coralDance],
  [8, surfaceLight],
  [4, driftAway]
)
