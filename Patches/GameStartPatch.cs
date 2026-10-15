using EFT;
using SPTushonka.Reflection.Patching;
using System;
using System.Reflection;

namespace Radar.Patches
{
    /// <summary>Installs the radar once a raid actually starts.</summary>
    public class GameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
            // An exception escaping a native->managed trampoline kills the process, so every
            // patch entry point is guarded.
            try
            {
                RadarPlugin.Log.LogInfo("Game started, loading radar hud");
                __instance.gameObject.AddComponent<InRaidRadarManager>();
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogError($"GameStartPatch swallowed an exception: {e}");
            }
        }
    }
}
