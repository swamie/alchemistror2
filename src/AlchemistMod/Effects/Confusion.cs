using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace AlchemistMod.Effects
{
    /// <summary>
    /// Thrown Elbow's confusion. For a few seconds the enemy is moved to the neutral team
    /// (so friendly fire rules let it hurt its old allies) and its AI is pointed at the
    /// nearest former ally. Bosses and player-controlled bodies are only stunned.
    /// </summary>
    internal class Confusion : MonoBehaviour
    {
        private CharacterBody body;
        private CharacterMaster master;
        private TeamIndex originalTeam;
        private float timer;
        private float retargetTimer;
        private bool active;

        public static void Apply(CharacterBody victim, float duration)
        {
            if (!NetworkServer.active || !victim || victim.isBoss || victim.isPlayerControlled) return;
            if (!victim.master || victim.master.aiComponents == null || victim.master.aiComponents.Length == 0) return;
            if (!victim.teamComponent) return;

            var confusion = victim.GetComponent<Confusion>();
            if (!confusion) confusion = victim.gameObject.AddComponent<Confusion>();
            confusion.Begin(victim, duration);
        }

        private void Begin(CharacterBody victim, float duration)
        {
            timer = Mathf.Max(timer, duration);
            victim.AddTimedBuff(Debuffs.Confused, duration);
            if (active) return;

            body = victim;
            master = victim.master;
            originalTeam = victim.teamComponent.teamIndex;
            SetTeam(TeamIndex.Neutral);
            active = true;
            retargetTimer = 0f;
        }

        private void FixedUpdate()
        {
            if (!active) return;
            timer -= Time.fixedDeltaTime;
            if (timer <= 0f || !body || !body.healthComponent || !body.healthComponent.alive)
            {
                End();
                return;
            }

            retargetTimer -= Time.fixedDeltaTime;
            if (retargetTimer > 0f) return;
            retargetTimer = 0.5f;

            CharacterBody target = FindNearestFormerAlly();
            if (!target) return;
            foreach (BaseAI ai in master.aiComponents)
            {
                if (!ai) continue;
                ai.currentEnemy.gameObject = target.gameObject;
                ai.enemyAttention = 1f;
            }
        }

        private CharacterBody FindNearestFormerAlly()
        {
            CharacterBody best = null;
            float bestDist = 60f * 60f;
            foreach (TeamComponent member in TeamComponent.GetTeamMembers(originalTeam))
            {
                CharacterBody other = member ? member.body : null;
                if (!other || other == body || !other.healthComponent || !other.healthComponent.alive) continue;
                float dist = (other.corePosition - body.corePosition).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = other;
                }
            }
            return best;
        }

        private void SetTeam(TeamIndex team)
        {
            if (master) master.teamIndex = team;
            if (body && body.teamComponent) body.teamComponent.teamIndex = team;
        }

        private void End()
        {
            if (active)
            {
                SetTeam(originalTeam);
                if (master)
                {
                    foreach (BaseAI ai in master.aiComponents)
                    {
                        if (ai) ai.currentEnemy.Reset();
                    }
                }
            }
            active = false;
            Destroy(this);
        }

        private void OnDestroy()
        {
            if (active) SetTeam(originalTeam);
        }
    }
}
