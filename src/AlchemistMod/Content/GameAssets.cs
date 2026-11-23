using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AlchemistMod.Content
{
    /// <summary>
    /// Loads vanilla assets we borrow as placeholders. Everything in here is a stand-in
    /// until the real Alchemist model, potion meshes and VFX are made in Unity.
    /// </summary>
    internal static class GameAssets
    {
        // Placeholder character (hat + long coat is the closest vanilla match to a plague doctor).
        public const string BanditBody = "RoR2/Base/Bandit2/Bandit2Body.prefab";
        // Arcing grenade, same throw speed as Commando's Frag Grenade.
        public const string CommandoGrenade = "RoR2/Base/Commando/CommandoGrenadeProjectile.prefab";
        // Ground pool that ticks damage while enemies stand in it.
        public const string AcridAcidPool = "RoR2/Base/Croco/CrocoLeapAcid.prefab";
        // Bird-shaped pickup used as the crow placeholder.
        public const string WaxQuailPickup = "RoR2/Base/JumpBoost/PickupWaxQuail.prefab";
        public const string ExplosionVfx = "RoR2/Base/Common/VFX/OmniExplosionVFXQuick.prefab";

        public static T Load<T>(string key) where T : Object
        {
            T asset = null;
            try
            {
                asset = Addressables.LoadAssetAsync<T>(key).WaitForCompletion();
            }
            catch (System.Exception e)
            {
                AlchemistPlugin.Log.LogError($"Failed to load {key}: {e.Message}");
            }

            if (!asset)
                AlchemistPlugin.Log.LogError($"Missing vanilla asset {key}. A game update may have moved it.");
            return asset;
        }
    }
}
