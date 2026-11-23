using AlchemistMod.Content;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AlchemistMod.Potions
{
    /// <summary>
    /// Runtime-built visuals for the flask throw, inspired by Terraria rogue flasks:
    /// the bottle tumbles end over end, bursts into glass shards, and leaves drifting gas.
    /// Particles reuse a vanilla smoke material so no AssetBundle is needed.
    /// </summary>
    internal static class PotionFx
    {
        private static Material cloudMaterial;

        public static Material CloudMaterial
        {
            get
            {
                if (cloudMaterial) return cloudMaterial;
                GameObject source = GameAssets.Load<GameObject>(GameAssets.ExplosionVfx);
                if (source)
                {
                    foreach (ParticleSystemRenderer r in source.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    {
                        if (!r.sharedMaterial) continue;
                        cloudMaterial = r.sharedMaterial;
                        if (r.sharedMaterial.name.ToLowerInvariant().Contains("smoke")) break;
                    }
                }
                return cloudMaterial;
            }
        }

        /// <summary>Glass shards plus a coloured puff. Registered as an effect so every player sees it.</summary>
        public static GameObject BuildShatterEffect(string key, Color color)
        {
            var temp = new GameObject("tmp");
            GameObject effect = temp.InstantiateClone("AlchemistShatter" + key, false);
            Object.Destroy(temp);

            var component = effect.AddComponent<EffectComponent>();
            component.applyScale = false;
            component.positionAtReferencedTransform = false;
            component.soundName = "";

            var vfx = effect.AddComponent<VFXAttributes>();
            vfx.vfxPriority = VFXAttributes.VFXPriority.Medium;
            vfx.vfxIntensity = VFXAttributes.VFXIntensity.Medium;
            vfx.DoNotPool = true;

            effect.AddComponent<DestroyOnTimer>().duration = 2.5f;

            // shards: small fast pieces that fall
            AddBurst(effect, "Shards", new Color(0.9f, 0.95f, 1f, 0.9f), count: 22, size: new Vector2(0.06f, 0.16f),
                speed: new Vector2(5f, 11f), lifetime: new Vector2(0.5f, 0.9f), gravity: 2f);
            // splash of liquid
            AddBurst(effect, "Splash", new Color(color.r, color.g, color.b, 0.9f), count: 16, size: new Vector2(0.15f, 0.35f),
                speed: new Vector2(3f, 7f), lifetime: new Vector2(0.4f, 0.8f), gravity: 1.2f);
            // puff
            AddBurst(effect, "Puff", new Color(color.r, color.g, color.b, 0.5f), count: 8, size: new Vector2(2f, 3.5f),
                speed: new Vector2(0.5f, 2f), lifetime: new Vector2(0.6f, 1.1f), gravity: -0.05f);

            AlchemistContentPack.AddEffect(effect);
            return effect;
        }

        private static void AddBurst(GameObject root, string name, Color color, int count, Vector2 size, Vector2 speed, Vector2 lifetime, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count * 2;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (CloudMaterial) renderer.sharedMaterial = CloudMaterial;
        }

        /// <summary>Looping soft cloud used by the lingering gas puffs.</summary>
        public static void AddCloud(GameObject root, Color color, float radius)
        {
            var go = new GameObject("GasParticles");
            go.transform.SetParent(root.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.9f, radius * 1.5f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(color.r, color.g, color.b, 0.4f);
            main.gravityModifier = -0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 40;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius * 0.5f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (CloudMaterial) renderer.sharedMaterial = CloudMaterial;
        }

        /// <summary>Replaces a zone's hitboxes with one box of a known size centred on it.</summary>
        public static void SetHitBox(GameObject root, Vector3 size)
        {
            var group = root.GetComponent<HitBoxGroup>();
            if (!group) return;
            if (group.hitBoxes != null)
            {
                foreach (HitBox old in group.hitBoxes)
                    if (old) Object.DestroyImmediate(old.gameObject);
            }
            var go = new GameObject("AlchemistHitBox");
            go.transform.SetParent(root.transform, false);
            go.transform.localScale = size;
            group.hitBoxes = new[] { go.AddComponent<HitBox>() };
        }

        /// <summary>Removes every renderer, decal and particle from a cloned prefab, keeping the logic.</summary>
        public static void StripVisuals(GameObject root)
        {
            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true)) if (ps) ps.gameObject.SetActive(ps.gameObject == root);
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) if (r) r.enabled = false;
            foreach (Light l in root.GetComponentsInChildren<Light>(true)) if (l) l.enabled = false;
        }
    }

    /// <summary>Tumbles the flying flask end over end. Lives on the projectile ghost.</summary>
    internal class FlaskSpin : MonoBehaviour
    {
        public float degreesPerSecond = 720f;
        private Transform[] children;

        private void Awake()
        {
            children = new Transform[transform.childCount];
            for (int i = 0; i < children.Length; i++) children[i] = transform.GetChild(i);
        }

        private void Update()
        {
            float step = degreesPerSecond * Time.deltaTime;
            foreach (Transform child in children)
                if (child) child.Rotate(Vector3.right, step, Space.Self);
        }
    }

    /// <summary>
    /// Moves a gas puff outward from where the flask burst, slowing to a stop.
    /// Runs the same way on every machine from the synced spawn rotation, so no extra networking.
    /// </summary>
    internal class GasDrift : MonoBehaviour
    {
        public float startSpeed = 4f;
        public float damping = 1.6f;
        private Vector3 velocity;

        private void Start()
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            velocity = dir.normalized * startSpeed;
            // keep the cloud itself upright regardless of the spawn rotation
            transform.rotation = Quaternion.identity;
        }

        private void FixedUpdate()
        {
            transform.position += velocity * Time.fixedDeltaTime;
            velocity *= Mathf.Exp(-damping * Time.fixedDeltaTime);
        }
    }
}
