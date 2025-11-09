using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using FewTags.FewTags.Patches;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;

///
/// <summary>
/// DELETE WHATEVER YOU DON'T NEED!
/// ALSO ADD WHATEVER USINGS YOU NEED DEPENDING ON WHAT YOUR USING
/// CODE SHOULD BE EXTREMELY EASY TO EDIT!
/// </summary>
/// 

namespace FewTags.FewTags
{
    internal static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.Fewdy.FewTags";
        public const string PLUGIN_NAME = "FewTags";
        public const string PLUGIN_VERSION = "3.0.8";
    }

    [BepInPlugin(PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    internal class BepInExExample : BasePlugin
    {
        internal static ManualLogSource Log;
        private Harmony harmony;
        public override void Load() // honestly this should be fine, if not, find a way to wait till game loads, than do that when game fully loads
        {
            Log = base.Log;
            //harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            ClassInjector.RegisterTypeInIl2Cpp<TagAnimator>();

            OnPlayer.PatchOnPlayer();
            FewTagsConfigLoader.Load();
            FewTagsUpdater.UpdateTags();
            LocalTags.LoadLocalTags();
            // add you're logic

            //FewTagsUpdater.DoUpdate(); // add this to either to the update of a monobehaviour or you're OnUpdate loop

            Log.LogInfo("FewTags Loaded!");
            Log.LogInfo("Tagged Players - Nameplate ESP On/Off: RightShift + O");
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
    }

    public class MelonLoaderExample : MelonMod
    {
        public static MelonLogger.Instance Log = new("FewTags", System.Drawing.Color.Red); // i forget how to actually do ml logger but something like this
        public override void OnInitializeMelon()
        {
            // ^ Used To Be OnApplicationLateStart() ^ //
            //new WaitForSeconds(3f);
            ClassInjector.RegisterTypeInIl2Cpp<TagAnimator>();

            OnPlayer.PatchOnPlayer();
            FewTagsConfigLoader.Load();
            FewTagsUpdater.UpdateTags();
            LocalTags.LoadLocalTags();

            //FewTagsUpdater.DoUpdate(); // add this to either to the update of a monobehaviour or you're OnUpdate loop

            LogManager.LogToConsole(ConsoleColor.Green, "FewTags Loaded!");
            LogManager.LogToConsole(ConsoleColor.Green, "Tagged Players - Nameplate ESP On/Off: RightShift + O");

        }
    }
}
