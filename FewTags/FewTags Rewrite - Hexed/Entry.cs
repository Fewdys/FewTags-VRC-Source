using CoreRuntime.Interfaces;
using CoreRuntime.Manager;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace FewTags
{
    public class Entry : HexedCheat // Define the Main Class for the Loader
    {
        private static Json._Tags s_tags { get; set; }
        public static string s_rawTags { get; set; }
        internal static float Position { get; set; }
        internal static float PositionID { get; set; }
        internal static float PositionBigText { get; set; }
        private static string s_stringInstance { get; set; }
        public static bool overlay = false;
        public static List<VRC.Player> p = new List<VRC.Player>();
        public override void OnLoad(string[] args = null)
        {
            // Entry thats getting called by HexedLoader 
            UpdateTags();
            //Console.WriteLine("Started FewTags");
            //Console.WriteLine("To ReFetch Tags Press L + F (Rejoin Required)");
            //Console.WriteLine("Finished Fetching Tags (This Message Doesn't Appear When Tags Are ReFetched)");
            //Console.WriteLine("Tagged Players - Nameplate ESP On/Off: RightShift + O (Rejoin Required)");

            // Positions For HexedClient Not Loaded
            PositionID = -75.95f;
            Position = -101.95f;

            // Positions For HexedClient Loaded
            //PositionID = -102.95f;
            //Position = -130.95f;

            PositionBigText = 273.75f; // big plate position the same for both

            // Specify our main function hooks to let the loader know about the games base functions, it takes any method that matches the original unity function struct
            MonoManager.PatchUpdate(typeof(VRCApplication).GetMethod(nameof(VRCApplication.Update))); // Update is needed to work with IEnumerators, hooking it will enable the CoroutineManager
            MonoManager.PatchOnApplicationQuit(typeof(VRCApplicationSetup).GetMethod(nameof(VRCApplicationSetup.OnApplicationQuit))); // Optional Hook to enable the OnApplicationQuit callback

            CoroutineManager.RunCoroutine(_OnPlayer.InitializeJoinLeaveHooks());
        }

        public override void OnApplicationQuit()
        {
        }

        public override void OnUpdate()
        {
            KeybindManager.Update();
        }

        public static void NameplateOverlay(bool set)
        {
            if (platestatic?.TextID.isActiveAndEnabled == true)
            {
                overlay = set;
                s_plate.Text.isOverlay = set;
                platestatic.TextID.isOverlay = set;
                platestatic.TextM.isOverlay = set;
                platestatic.TextBP.isOverlay = set;
            }
            /*if (set == true)
            {
                Console.WriteLine("(Tagged Players) Nameplate ESP On");
            }
            else if (set == false)
            {
                Console.WriteLine("(Tagged Players) Nameplate ESP Off");
            }*/
        }

        public static void UpdateTags()
        {

            using (WebClient wc = new WebClient())
            {
                s_rawTags = wc.DownloadString("https://raw.githubusercontent.com/Fewdys/FewTags/main/FewTags.json");
                s_tags = JsonSerializer.Deserialize<Json._Tags>(s_rawTags);
            }
            //Console.WriteLine("Fetching Tags (If L + F Was Pressed This Could Potentially Cause Some Lag)");
        }

        private static Plate s_plate { get; set; }
        private static PlateStatic platestatic { get; set; }
        private static Json.Tags[] s_tagsArr { get; set; }

        public static void NameplateESP(VRC.Player player)
        {
            if (player._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_5 != null)
            {
                player._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_5.transform.FindChild("Trust Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().isOverlay = overlay; // Why The Fuck Was I Searching For The Specific VRC Class When I Don't Do That Anywhere Else
            }
        }

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
                        // HexedClient Loaded
                        //s_plate = new Plate(vrcPlayer, -158.75f - (g * 28f), overlay);

                        // HexedClient Not Loaded
                        s_plate = new Plate(vrcPlayer, -128.75f - (g * 28f), overlay);
                        
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
