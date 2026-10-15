using EFT;
using EFT.InventoryLogic;
using SPTushonka.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace Radar.Patches
{
    /// <summary>
    /// Lights a contact up when it fires, which is what Fire Mode displays instead of live positions.
    /// </summary>
    internal class PlayerOnMakingShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(Player).GetMethod("OnMakingShot", BindingFlags.Public | BindingFlags.Instance)!;

        [PatchPostfix]
        private static void PostFix(Player __instance, IWeapon weapon, Vector3 force)
        {
            // Patch entry points must never throw: an exception escaping the trampoline
            // terminates the game process.
            try
            {
                if (__instance == null)
                    return;

                InRaidRadarManager.LiveRadar?.UpdateFireTime(__instance.ProfileId);
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"PlayerOnMakingShotPatch swallowed an exception: {e.Message}");
            }
        }
    }
}
