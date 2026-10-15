using EFT.Interactive;
using SPTushonka.Reflection.Patching;
using System;
using System.Reflection;

namespace Radar.Patches
{
    /// <summary>
    /// Registers loot the game adds mid-raid: player drops, airdrops, gear from freshly killed bots.
    /// </summary>
    /// <remarks>
    /// IL2CPP generics: DictionaryListHydra&lt;int, T&gt; shares a single method body across every
    /// reference-type instantiation, so this patch also fires for non-loot dictionaries in the
    /// game and "value" is only sometimes an actual LootItem. An exception escaping a
    /// native->managed trampoline kills the whole process, so every entry point is guarded.
    /// </remarks>
    internal class LootItemAddPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(DictionaryListHydra<int, LootItem>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public)!;

        [PatchPostfix]
        private static void PostFix(int key, LootItem value)
        {
            try
            {
                // Runtime type guard: for other instantiations the argument is a different class.
                if (value == null || value.TryCast<LootItem>() == null)
                    return;

                // The transform has not settled at this point, hence lazyUpdate.
                InRaidRadarManager.LiveRadar?.AddLoot(value.ItemId, value.Item, value.TrackableTransform, true);
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"LootItemAddPatch swallowed an exception: {e.Message}");
            }
        }
    }

    /// <summary>Drops the blip for loot that has been picked up or otherwise removed from the world.</summary>
    internal class LootItemRemovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(DictionaryListHydra<int, LootItem>).GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public)!;

        [PatchPrefix]
        private static void PreFix(int key)
        {
            try
            {
                // Prefix, because the item still has to be resolvable by key. Foreign keys (other
                // DictionaryListHydra instantiations) are skipped inside RemoveByKey; nothing may
                // escape here - a thrown exception crosses the trampoline and crashes the game.
                InRaidRadarManager.LiveRadar?.RemoveLootByKey(key);
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"LootItemRemovePatch swallowed an exception: {e.Message}");
            }
        }
    }
}
