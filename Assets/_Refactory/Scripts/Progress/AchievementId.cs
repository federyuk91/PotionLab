using System;

namespace ProgressSystem
{
    public enum AchievementId
    {
        None = 0,
        TheGoodnightPotion = 1,
        TheMage = 2,
        Perfectionist = 3,
        TheClassic = 4,
        TimeToThink = 5,
        UnderTheSea = 6,
        UdunFlame = 7,
        Roar = 8,
        WetWizardLuckyWizard = 9,
        ThankYouLara = 10,
        AdvanceKnowledge = 11,
        AdvanceTactics = 12,
        ExoticInteraction = 13,
        PizzaExpress3000 = 14,
        SylvanusBlessing = 15,
        CookingMama = 16,
        Blob = 17,
        FalconPunch = 18,
        OldToby = 19,
        IceTwice = 20,
        FlounderIsDeath = 21,
        MastroLindo = 22,
        GrayWizardSacrifice = 23,
        CasualDrinker = 24,
        RegularDrinker = 25,
        HardcoreDrinker = 26,
        MasterDrinker = 27,
        AlmostAProblemDrinker = 28,
        GodOfLibations = 29,
        BurnBabyBurn = 30,
        Shapeshifter = 31,
        DittosFollower = 32,
        ManaBurn = 33,
        NotAWaster = 34,
        Spammer = 35,
        FreshAndClean = 36,
        NecroticDeath = 37,
        ShakyShaky = 38,
        PileOfGround = 39,
        HolyPileOfGround = 40,
        SmartButFart = 41
    }

    public static class AchievementRequestHub
    {
        public static event Action<AchievementId> AchievementRequested;

        public static void Request(AchievementId achievementId)
        {
            if (achievementId != AchievementId.None)
            {
                AchievementRequested?.Invoke(achievementId);
            }
        }
    }
}
