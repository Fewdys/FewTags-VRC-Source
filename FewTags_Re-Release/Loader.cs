using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

///
/// <summary>
/// DELETE WHATEVER YOU DON'T NEED!
/// ALSO ADD WHATEVER USINGS YOU NEED DEPENDING ON WHAT YOUR USING
/// CODE SHOULD BE EXTREMELY EASY TO EDIT!
/// </summary>
/// 

namespace FewTags
{
    internal static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.Fewdy.FewTags";
        public const string PLUGIN_NAME = "FewTags";
        public const string PLUGIN_VERSION = "3.0.7";
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

            OnPlayer.PatchOnPlayer();
            ConfigLoader.Load();
            Main.UpdateTags();
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
            OnPlayer.PatchOnPlayer();
            ConfigLoader.Load();
            Main.UpdateTags();

            //FewTagsUpdater.DoUpdate(); // add this to either to the update of a monobehaviour or you're OnUpdate loop

            Log.Msg(ConsoleColor.Green, "FewTags Loaded!");
            Log.Msg(ConsoleColor.Green, "Tagged Players - Nameplate ESP On/Off: RightShift + O");

        }
    }
}
