using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace OmniLocker
{
    [BepInPlugin("com.toby.omnilocker", "OmniLocker", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("OmniLocker loaded");

            var harmony = new Harmony("com.toby.OmniLocker");
            harmony.PatchAll();
        }
    }
}