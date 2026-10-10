// "The Gates Below" — Infernal March / The Good Night Potion
// 88 BPM, 4/4. Export cycles 0–32, stereo (~1:27).
// Opening string strike; drums enter in bar 2 and march until the loop restarts.
setcpm(88 / 4)

// Individual drum levels: keep the march heavy without an overpowering kick.
const drumLevel = .17
const snareLevel = .11
const kickLevel = .055

const violinStrike = note("<[d5,f5,a5,eb6] ~ ~ ~>")
  .s('gm_tremolo_strings:0')
  .attack(.005).decay(.22).sustain(.15).release(.25).clip(.22)
  .hpf(350).lpf(5400).gain(.23).room(.3).orbit(3)

const marchingDrums = stack(
  s("lt mt lt mt").bank('RolandTR707')
    .gain(drumLevel).velocity("1 .6 .85 .6").lpf(1900),
  s("~ sd ~ sd").bank('RolandTR707')
    .gain(snareLevel).lpf(2600),
  s("bd ~ bd ~").bank('RolandTR707').gain(kickLevel)
).room(.12).orbit(1)

// D minor / Eb major / D minor / A7b9.
const darkHarmony = note("<[f3,a3,d4] [g3,bb3,eb4] [f3,a3,d4] [g3,bb3,c#4]>")

const lowProcession = note(`<
  [d2 ~ a1 ~ d2 ~ c2 c#2]
  [eb2 ~ bb1 ~ eb2 ~ d2 ~]
  [d2 ~ a1 ~ d2 ~ f2 e2]
  [a1 ~ e2 ~ a1 ~ bb1 c#2]
>`).s('sawtooth')
  .attack(.012).decay(.24).sustain(.08).release(.07)
  .lpf(580).lpq(2).gain(.4).orbit(2)

const bowedPulse = darkHarmony
  .struct("x ~ x ~").s('gm_tremolo_strings:2')
  .attack(.015).decay(.18).sustain(.12).release(.12).clip(.4)
  .hpf(240).lpf(2600).gain(.13).room(.2).orbit(4)

const violinTheme = note(`<
  [d5@2 eb5 d5]
  [bb4@2 g4 bb4]
  [a4 d5 f5 e5]
  [eb5@2 c#5@2]
>`).s('gm_tremolo_strings:0')
  .attack(.035).decay(.3).sustain(.4).release(.2).clip(.8)
  .hpf(380).lpf(3600).gain(.17).room(.28).orbit(5)

const distantAnswer = note(`<
  [~ ~ [d3,a3] ~]
  [~ ~ [eb3,bb3] ~]
  [~ ~ [d3,a3] ~]
  [~ ~ [a2,e3,g3] ~]
>`).s('sawtooth')
  .attack(.08).decay(.2).sustain(.2).release(.18)
  .hpf(200).lpf(1000).gain(.1).room(.24).pan(.4).orbit(6)

const openingGates = stack(
  violinStrike,
  marchingDrums.mask("<0 1 1 1>"),
  lowProcession.mask("<0 0 1 1>")
)

const approaching = stack(
  marchingDrums,
  lowProcession,
  bowedPulse
)

const infernalMarch = stack(
  marchingDrums,
  lowProcession,
  bowedPulse,
  violinTheme,
  distantAnswer
)

// The drums continue unchanged while the orchestra recedes.
const behindTheDoor = stack(
  marchingDrums,
  lowProcession.lpf(420).gain(.34),
  distantAnswer,
  violinTheme.mask("<0 0 1 0>").gain(.12)
)

const gatesWideOpen = stack(
  marchingDrums,
  lowProcession,
  bowedPulse,
  violinTheme,
  distantAnswer,
  note("<~ ~ [~ ~ d6 a5] [~ ~ eb6 c#6]>")
    .s('gm_tremolo_strings:1').attack(.02).clip(.4).release(.15)
    .hpf(700).lpf(3200).gain(.08).room(.25).orbit(7)
)

arrange(
  [4, openingGates],
  [8, approaching],
  [8, infernalMarch],
  [4, behindTheDoor],
  [8, gatesWideOpen]
)
