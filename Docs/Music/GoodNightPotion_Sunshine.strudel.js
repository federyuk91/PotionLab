// Moonlit Cauldron — Sunshine Funk / The Good Night Potion
// 110 BPM, 36 bars. Export cycles 0–36, stereo.
setcpm(110 / 4)

const potionChords = chord("<D7 G^7 C^7 A7>").dict('ireal')
const warmChords = note("<[f#3,a3,c4,e4] [f#3,a3,b3,d4] [e3,g3,b3,d4] [e3,g3,b3,c#4]>")
const kickLevel = .08

const pocketDrums = stack(
  s("bd ~ bd ~").bank('RolandTR707').gain(kickLevel),
  s("[~ ~ ~ ~] [sd ~ ~ ~] [~ ~ ~ ~] [sd ~ [~ sd] ~]")
    .bank('RolandTR707').gain(.18),
  s("hh*8")
    .bank('RolandTR707').gain("[.025 .035]*4"),
  s("~ ~ ~ [cb ~] ~ cb ~ [~ cb]")
    .bank('RolandTR808').gain(.065).pan(.68)
).orbit(1)

const bubblingBass = note(`<
  [d2 ~ d2 [f#2 g2] ~ a1 c2 [c#2 d2]]
  [g1 ~ g2 a1 ~ b1 d2 [f#2 g2]]
  [c2 ~ c2 [e2 g2] ~ a1 g1 b1]
  [a1 ~ a1 c#2 e2 ~ g2 [c#2 d2]]
>`).s('sawtooth')
  .lpf(sine.slow(8).range(620, 1350))
  .lpq(7).decay(.16).sustain(.08).release(.06)
  .shape(.14).gain(.64).orbit(2)

const spellStabs = warmChords
  .struct("[~ x] ~ [~ x] [~ x]").s('gm_epiano1')
  .clip(.65).attack(.008).decay(.22).sustain(.15).release(.16)
  .hpf(260).lpf(2200).gain(.26).pan(.42).orbit(3)

const moonPad = warmChords.s('triangle')
  .attack(.25).release(.6).hpf(260).lpf(1500)
  .room(.3).gain(.13).orbit(4)

const brassAnswer = n("<~ [4 3] ~ [2 1]>")
  .set(potionChords).voicing().s('sawtooth').clip(.18)
  .attack(.015).decay(.1).sustain(.22).release(.08)
  .hpf(320).lpf(3600).shape(.12).gain(.23).pan(.62).orbit(5)

// A repeated rising hook, answered above the bass.
const mainMelody = note(`<
  [a4 ~ b4 [d5 e5] f#5 ~ e5 d5]
  [b4 ~ d5 [e5 d5] b4 ~ a4 b4]
  [g4 ~ a4 [c5 d5] e5 ~ d5 c5]
  [a4 ~ b4 [c#5 e5] ~ e5 [g5 e5] c#5]
>`).s('triangle')
  .attack(.015).decay(.13).sustain(.16).release(.18)
  .lpf(2800).delay(.16).delaytime(.125).delayfeedback(.22)
  .room(.2).gain(.27).pan(.38).orbit(7)

const potionSparkles = n("<~ ~ [7 9] [11 9 7 4]>")
  .set(potionChords).voicing().s('gm_xylophone').clip(.3)
  .delay(.28).delaytime(.125).delayfeedback(.34)
  .room(.35).gain(.22).pan(sine.slow(4).range(.2, .8)).orbit(6)

const intro = stack(
  moonPad.lpf(sine.range(550, 2200).slow(4)),
  bubblingBass.mask("<0 1 1 1>")
    .lpf(sine.range(360, 1250).slow(4)).gain(.7),
  mainMelody.mask("<0 1 1 1>")
    .lpf(sine.range(1100, 3000).slow(4)).gain(.24),
  spellStabs.mask("<0 0 1 1>"),
  s("<~ ~ ~ [hh*4]>").bank('RolandTR707').gain(.025)
)

const mainGroove = stack(
  pocketDrums,
  bubblingBass.lastOf(4, pattern => pattern.off(
    1 / 16, echo => echo.add(12).gain(.15)
  )),
  spellStabs.mask("<1 0 1 1>").gain(.27),
  mainMelody.mask("<1 0 1 0>").gain(.25),
  brassAnswer.mask("<0 1 0 1>").gain(.2),
  s("[~ rim] ~ [~ rim] ~").bank('RolandTR707').gain(.05).pan(.58)
)

const nightBreak = stack(
  moonPad,
  bubblingBass.lpf(430).gain(.42),
  s("bd ~ ~ ~").bank('RolandTR707').lpf(1700).gain(kickLevel * .65),
  s("~ ~ sd ~").bank('RolandTR707').lpf(1700).gain(.12),
  s("hh*4").bank('RolandTR707').lpf(1700).gain(.02),
  potionSparkles
)

const transformedGroove = stack(
  pocketDrums,
  bubblingBass.lastOf(4, pattern => pattern.add(12).gain(.36)),
  spellStabs,
  brassAnswer,
  potionSparkles.mask("<0 1 1 1>")
)

const finale = stack(
  pocketDrums, bubblingBass, spellStabs,
  moonPad.gain(.16),
  mainMelody.mask("<1 0 1 0>").gain(.21),
  brassAnswer.off(1 / 16, pattern => pattern.add(12).gain(.16)),
  potionSparkles
)

const outro = stack(
  warmChords.s('gm_epiano1').attack(.02).release(2.5).room(.75).gain(.24),
  note("<d2 ~ ~ ~>").s('sawtooth')
    .lpf(sine.range(900, 180).slow(2)).release(1).gain(.4),
  s("<bd ~ ~ ~>").bank('RolandTR707').gain(kickLevel * .65)
)

arrange(
  [4, intro], [8, mainGroove], [4, nightBreak],
  [8, transformedGroove], [8, finale], [4, outro]
)
