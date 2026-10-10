// "Voices Below" — Infernal background ambience
// 64 BPM. Export cycles 0–32 (~2:00), stereo.
// Synthetic cries, not recordings of human voices. No melody or chord loop.
setcpm(64 / 4)

const drumsLevel = .085
const highCryLevel = .07
const deepCryLevel = .1

const distantDrums = stack(
  s("lt ~ lt ~").bank('RolandTR707')
    .speed(.55).gain(drumsLevel).velocity(".85 1").lpf(380),
  s("<~ [~ ~ ~ mt] ~ [~ mt ~ ~]>").bank('RolandTR707')
    .speed(.6).gain(drumsLevel * .55).lpf(450).pan(.58)
).room(.28).orbit(1)

const cavernAir = s("brown")
  .attack(.9).decay(.4).sustain(.3).release(.7)
  .hpf(90).lpf(430).gain(.025).room(.2).orbit(2)

// Three brief, unevenly spaced high calls in each 32-cycle loop.
const highCries = note(`<
  ~ ~ ~ [~ ~ f5 ~] ~ ~ ~ ~
  ~ ~ ~ ~ ~ [~ eb5 ~ ~] ~ ~
  ~ ~ ~ ~ ~ ~ ~ ~
  [~ ~ ~ g5] ~ ~ ~ ~ ~ ~ ~
>`).s('sawtooth')
  .vowel('a').vib(6).vibmod(.65)
  .penv(9).panchor(0).pattack(.09).pdecay(.38)
  .attack(.065).decay(.35).sustain(0).release(.2)
  .hpf(650).lpf(2700).gain(highCryLevel)
  .pan("<.28 .7>").room(.45)
  .delay(.13).delaytime(.43).delayfeedback(.2).orbit(3)

// Lower, rough calls answer from a different part of the cavern.
const deepCries = note(`<
  ~ ~ ~ ~ ~ ~ [~ a1 ~ ~] ~
  ~ ~ [~ ~ c2 ~] ~ ~ ~ ~ ~
  ~ ~ ~ [~ ~ ~ g1] ~ ~ ~ ~
  ~ ~ ~ ~ [~ bb1 ~ ~] ~ ~ ~
>`).s('sawtooth')
  .vowel('o').vib(4).vibmod(.4)
  .penv(7).panchor(0).pattack(.12).pdecay(.65)
  .attack(.13).decay(.7).sustain(0).release(.3)
  .hpf(70).lpf(1100).gain(deepCryLevel)
  .pan("<.72 .32>").room(.5)
  .delay(.12).delaytime(.61).delayfeedback(.2).orbit(4)

stack(
  distantDrums,
  cavernAir,
  highCries,
  deepCries
)
