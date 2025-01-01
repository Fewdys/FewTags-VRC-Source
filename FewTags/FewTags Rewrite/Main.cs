using Il2CppInterop.Common.XrefScans;
using Il2CppInterop.Runtime.XrefScans;
using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using static FewTags.Json;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace FewTags
{
    public class Main : MelonMod
    {
        private static Json._Tags? s_tags {  get; set; }
        private static string? s_rawTags { get; set; }
        public static bool SnaxyTagsLoaded { get; private set; }
        public static bool AbyssClientLoaded { get; private set; }
        public static bool NameplateStatsLoaded { get; private set; }
        internal static float Position { get; set; }
        internal static float Position2 { get; set; }
        internal static float Position3 { get; set; }
        internal static float PositionID { get; set; }
        internal static float PositionBigText { get; set; }
        private static string? s_stringInstance { get; set; }
        public static bool overlay = false;

        private delegate IntPtr userJoined(IntPtr _instance, IntPtr _user, IntPtr _methodinfo);
        private static userJoined? s_userJoined;
        public static List<VRC.Player> players = new List<VRC.Player>();

        public static MelonLogger.Instance Log = new("FewTags", System.Drawing.Color.Red);

        public override void OnApplicationStart()
        {
            Log.Msg(ConsoleColor.Yellow, "Starting FewTags...");
            new WaitForSeconds(3f);
            MelonCoroutines.Start(_OnPlayer.InitializeJoinLeaveHooks());
            UpdateTags();
            // ^ Used To Be OnApplicationLateStart() ^ //

            Chatbox.CreateShit();
            SnaxyTagsLoaded = MelonTypeBase<MelonMod>.RegisteredMelons.Any(m => m.Info.Name.ToLower().Contains("snaxytags")); // put whatever mod names i guess if you intend on changing code in which other mods take up room below nameplate :shrug:
            NameplateStatsLoaded = MelonMod.RegisteredMelons.Any(m => m.Info.Name.ToLower().Contains("nameplatestats")); // put whatever mod names i guess if you intend on changing code in which other mods take up room below nameplate :shrug:

            Log.Msg(ConsoleColor.Green, "FewTags Loaded!");
            Log.Msg(ConsoleColor.DarkCyan, "To ReFetch Tags Press L + F (Rejoin Required)");
            Log.Msg(ConsoleColor.Green, "Tagged Players - Nameplate ESP On/Off: RightShift + O (Rejoin Required)");

            if (!NameplateStatsLoaded && !SnaxyTagsLoaded)
            {
                PositionID = -75.95f;
                Position = -131.65f;
                PositionBigText = 273.75f;
            }
            else if (FewTags.Main.NameplateStatsLoaded || FewTags.Main.SnaxyTagsLoaded)
            {
                PositionID = -102.95f;
                Position = -161.65f;
                PositionBigText = 273.75f;
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            try
            {
                Chatbox.GetColors();
            }
            catch { }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F) && Input.GetKey(KeyCode.L))
            {
                UpdateTags();
            }
        }

        public override void OnLateUpdate()
        {
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.O))
            {
                var mainMenu = GameObject.Find("Canvas_MainMenu(Clone)/Container/MMParent/Modal_MM_Keyboard");
                if (mainMenu?.GetComponent<GraphicRaycaster>()?.enabled != true)
                {
                    overlay = !overlay;
                    NameplateOverlay(overlay);

                    if (players.Count != 0)
                    {
                        try
                        {
                            for (int i = 0; i < players.Count; i++)
                            {
                                NameplateESP(players[i]);
                            }
                        }
                        catch (Exception exp) { Log.Msg(ConsoleColor.Red, "Error Changing Overlay/Nameplate ESP For A Player" + "\nError: " + exp.Message + "\nException StackTrace: " + exp.StackTrace + "\nException Data: " + exp.Data); }
                    }
                }
            }
        }

        public static void NameplateOverlay(bool set)
        {
            if (platestatic.TextID.isActiveAndEnabled)
            {
                overlay = set;
                s_plate.Text.isOverlay = set;
                platestatic.TextID.isOverlay = set;
                platestatic.TextM.isOverlay = set;
                platestatic.TextBP.isOverlay = set;
            }

            Log.Msg(set ? ConsoleColor.Green : ConsoleColor.Red, $"(Tagged Players) Nameplate ESP {(set ? "On" : "Off")}");
        }

        public static void UpdateTags()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    s_rawTags = wc.DownloadString("https://raw.githubusercontent.com/Fewdys/FewTags/main/FewTags.json");
                    s_tags = JsonConvert.DeserializeObject<Json._Tags>(s_rawTags);
                }
                Log.Msg(ConsoleColor.Yellow, "Fetching Tags (If L + F Was Pressed This Could Potentially Cause Some Lag)");
            }
            catch (Exception ex)
            {
                Log.Msg(ConsoleColor.Red, $"Error Fetching Tags: {ex.Message}\nYou Can Try Re-Fetching Tags If You Think This Might Be Github Having Issues");
            }
        }

        private static Plate s_plate { get; set; }
        private static PlateStatic platestatic { get; set; }
        private static Json.Tags[]? s_tagsArr { get; set; } // no longer used

        public static void NameplateESP(VRC.Player player)
        {
            var nameplate = player._vrcplayer.field_Public_PlayerNameplate_0?.field_Public_GameObject_5;
            if (nameplate != null)
            {
                nameplate.transform.Find("Trust Text")?.GetComponent<NameplateTextMeshProUGUI>().isOverlay = overlay;
            }
        }

        // didn't test if this would work properly but i'd assume it does, thx _Unreal for some of the small optimization tips here
        public static void PlateHandler(VRC.Player vrcPlayer, bool overlay)
        {
            try
            {
                if (s_tags == null) return;
                platestatic = new PlateStatic(vrcPlayer, overlay);
                //s_tagsArr = s_tags.records.Where(x => x.UserID == vrcPlayer.field_Private_APIUser_0.id).ToArray();
                string uid = vrcPlayer.field_Private_APIUser_0.id;

                for (int i = 0; i < s_tags.records.Count/*s_tagsArr.Length*/; i++)
                {
                    var user = s_tags.records[i];
                    if (user.UserID != uid) continue;
                    if (!user.Active) continue;

                    s_stringInstance = user.Size ?? "";
                    for (int g = 0; g < user.Tag.Length/*s_tagsArr[i].Tag.Length*/; g++)
                    {
                        s_plate = new Plate(vrcPlayer, Main.Position - (g * 28f), overlay);
                        s_plate.Text.text = user.Tag[g];
                        s_plate.Text.enabled = user.TextActive; // enable or disable plate text based on our bool
                        s_plate.Text.gameObject.SetActive(user.TextActive); // enable or disable plate based on our bool
                        s_plate.Text.isOverlay = overlay; // not needed as all plates overlay when you overlay one of them
                    }

                    platestatic.TextID.text = "<color=#ffffff>[</color><color=#808080>" + user.id + "</color><color=#ffffff>]"; // id
                    platestatic.TextID.isOverlay = overlay; // not needed as all plates overlay when you overlay one of them

                    platestatic.TextM.text = user.Malicious ? "<color=#ff0000>Malicious User</color>" : "<b><color=#ff0000>-</color> <color=#ff7f00>F</color><color=#ffff00>e</color><color=#80ff00>w</color><color=#00ff00>T</color><color=#00ff80>a</color><color=#00ffff>g</color><color=#0000ff>s</color> <color=#8b00ff>-</color><color=#ffffff></b>";
                    platestatic.TextM.isOverlay = overlay; // not needed as all plates overlay when you overlay one of them

                    platestatic.TextBP.text = s_stringInstance + user.PlateBigText;
                    platestatic.TextBP.enabled = user.BigTextActive; // enable or disable plate text based on our bool
                    platestatic.TextBP.gameObject.SetActive(user.BigTextActive); // enable or disable plate based on our bool
                    platestatic.TextBP.isOverlay = overlay; // not needed as all plates overlay when you overlay one of them
                }
            }
            catch (Exception ex)
            {
                Log.Msg(ConsoleColor.Red, "Error Handling Plates For UserID:" + vrcPlayer.field_Private_APIUser_0.id + "\nError: " + ex.Message + "\nException StackTrace: " + ex.StackTrace + "\nException Data: " + ex.Data);
            }
        }
    }
}
