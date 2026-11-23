using AlchemistMod.Survivor;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AlchemistMod.Effects
{
    /// <summary>
    /// The Alchemist's scaling: every damage-over-time tick she owns is multiplied by how long
    /// that enemy has been continuously afflicted by her. Starts weak, ends brutal.
    ///
    ///   0s  : 0.4x
    ///   10s : 1.9x
    ///   24s+: 4.0x (cap)
    ///
    /// The enemy shows a Festering buff whose stack count is the seconds elapsed.
    /// If none of her DoTs tick on it for 2s, the timer resets.
    /// </summary>
    internal class Fester : MonoBehaviour
    {
        public const float StartMultiplier = 0.4f;
        public const float GainPerSecond = 0.15f;
        public const float MaxMultiplier = 4f;
        public const float ResetAfter = 2f;

        private CharacterBody body;
        private float afflictedTime;
        private float lastTickTime;

        public float Multiplier => Mathf.Min(MaxMultiplier, StartMultiplier + GainPerSecond * afflictedTime);

        public static void Init(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(HealthComponent), nameof(HealthComponent.TakeDamage), new[] { typeof(DamageInfo) });
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(Fester), nameof(TakeDamagePrefix)));
        }

        /// <summary>Server: scale the Alchemist's DoT ticks before they land.</summary>
        private static void TakeDamagePrefix(HealthComponent __instance, DamageInfo damageInfo)
        {
            if (!NetworkServer.active || damageInfo == null || damageInfo.dotIndex == DotController.DotIndex.None) return;
            if (!damageInfo.attacker || !__instance || !__instance.body) return;
            if (!IsAlchemist(damageInfo.attacker)) return;

            Fester fester = For(__instance.body);
            fester.lastTickTime = Time.fixedTime;
            damageInfo.damage *= fester.Multiplier;
        }

        public static bool IsAlchemist(GameObject attacker)
        {
            var attackerBody = attacker ? attacker.GetComponent<CharacterBody>() : null;
            return attackerBody && attackerBody.bodyIndex == AlchemistSurvivor.BodyIndex;
        }

        public static Fester For(CharacterBody victim)
        {
            var fester = victim.GetComponent<Fester>();
            if (!fester)
            {
                fester = victim.gameObject.AddComponent<Fester>();
                fester.body = victim;
                fester.lastTickTime = Time.fixedTime;
            }
            return fester;
        }

        /// <summary>Current multiplier for a victim without creating a tracker (1x if none).</summary>
        public static float MultiplierFor(CharacterBody victim)
        {
            var fester = victim ? victim.GetComponent<Fester>() : null;
            return fester ? fester.Multiplier : StartMultiplier;
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || !body) return;

            if (Time.fixedTime - lastTickTime > ResetAfter)
            {
                afflictedTime = 0f;
                if (body.HasBuff(Debuffs.Festering)) body.SetBuffCount(Debuffs.Festering.buffIndex, 0);
                return;
            }

            afflictedTime += Time.fixedDeltaTime;
            int seconds = Mathf.Min(99, Mathf.FloorToInt(afflictedTime));
            if (body.GetBuffCount(Debuffs.Festering) != seconds)
                body.SetBuffCount(Debuffs.Festering.buffIndex, seconds);
        }
    }
}
