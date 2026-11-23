using AlchemistMod.Effects;
using R2API;
using RoR2;
using RoR2.Achievements;

namespace AlchemistMod.Survivor
{
    // Achievement ids are turned into tokens by the game: ACHIEVEMENT_<ID>_NAME / _DESCRIPTION (see Tokens).

    /// <summary>Plaguewarden: clear the game on Monsoon as the Alchemist (the standard mastery skin).</summary>
    [RegisterAchievement(Id, "Skins.SwamieAlchemist.Plaguewarden", null, 10u, null)]
    public class AlchemistMasteryAchievement : BasePerSurvivorClearGameMonsoonAchievement
    {
        public const string Id = "SwamieAlchemistMastery";
        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex(AlchemistSurvivor.BodyName);
    }

    /// <summary>Verdigris: have 5 different debuffs on one enemy at the same time.</summary>
    [RegisterAchievement(Id, "Skins.SwamieAlchemist.Verdigris", null, 5u, typeof(ServerTracker))]
    public class AlchemistDebuffAchievement : BaseAchievement
    {
        public const string Id = "SwamieAlchemistFiveDebuffs";
        public const int Required = 5;

        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex(AlchemistSurvivor.BodyName);

        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            SetServerTracked(true);
        }

        public override void OnBodyRequirementBroken()
        {
            SetServerTracked(false);
            base.OnBodyRequirementBroken();
        }

        public class ServerTracker : BaseServerAchievement
        {
            public override void OnInstall()
            {
                base.OnInstall();
                GlobalEventManager.onServerDamageDealt += OnDamage;
            }

            public override void OnUninstall()
            {
                GlobalEventManager.onServerDamageDealt -= OnDamage;
                base.OnUninstall();
            }

            private void OnDamage(DamageReport report)
            {
                if (report == null || !report.victimBody || !IsCurrentBody(report.attackerBody)) return;
                if (Debuffs.CountUniqueDebuffs(report.victimBody) >= Required) Grant();
            }
        }
    }

    /// <summary>Ashen Herbalist: kill 5 enemies with a single Transmute.</summary>
    [RegisterAchievement(Id, "Skins.SwamieAlchemist.Ashen", null, 5u, typeof(ServerTracker))]
    public class AlchemistTransmuteAchievement : BaseAchievement
    {
        public const string Id = "SwamieAlchemistTransmuteKills";
        public const int Required = 5;

        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex(AlchemistSurvivor.BodyName);

        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            SetServerTracked(true);
        }

        public override void OnBodyRequirementBroken()
        {
            SetServerTracked(false);
            base.OnBodyRequirementBroken();
        }

        public class ServerTracker : BaseServerAchievement
        {
            private int kills;
            private Run.FixedTimeStamp windowStart;

            public override void OnInstall()
            {
                base.OnInstall();
                GlobalEventManager.onCharacterDeathGlobal += OnDeath;
            }

            public override void OnUninstall()
            {
                GlobalEventManager.onCharacterDeathGlobal -= OnDeath;
                base.OnUninstall();
            }

            private void OnDeath(DamageReport report)
            {
                if (report == null || report.damageInfo == null || !IsCurrentBody(report.attackerBody)) return;
                if (!report.damageInfo.HasModdedDamageType(Debuffs.Transmute)) return;

                // One cast resolves within a single physics step; give it a small window anyway.
                if (windowStart.timeSince > 0.5f) kills = 0;
                if (kills == 0) windowStart = Run.FixedTimeStamp.now;
                kills++;
                if (kills >= Required) Grant();
            }
        }
    }
}
