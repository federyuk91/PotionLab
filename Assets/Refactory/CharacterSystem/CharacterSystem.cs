using System;
using System.Collections.Generic;

namespace CharacterSystem
{
    public enum DialogTrigger
    {
        PotionDrink,
        Transformation,
        StatusAdded,
        StatusRemoved,
        Explosion,
        Immunity,
        StatusTick
    }

    public struct DialogContext
    {
        public CharacterType character;
        public PotionScriptable.EffectType potion;
        public DialogTrigger trigger;
        public HashSet<Status> statuses;
    }

    // Stati elementali applicabili al personaggio
    public enum Status
    {
        None,
        Burned,
        Wet,
        Freezed,
        Poisoned,
        Grass,
        Grounded,
        Algae //Erba su stato bagnato
    }

    // Tipi di trasformazione
    [Serializable]
    public enum CharacterType
    {
        Mage,
        Balrog, //Lava su stato bruciato
        Tree, //Acqua su stato erba
        PupperFish, //Veleno su stato bagnato
        Yeti, //Ghiaccio su stato grounded
        Litch, //3 pozioni oscure consecutive senza luce o morte da oscurità
        WhiteMage, //3 pozioni di luce consecutive con luce al massimo
        Any
    }
}
