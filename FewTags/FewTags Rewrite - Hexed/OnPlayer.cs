using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FewTags
{
    public class _OnPlayer
    {
        public static IEnumerator InitializeJoinLeaveHooks()
        {
            while (NetworkManager.field_Internal_Static_NetworkManager_0 == null)
            {
                yield return null;
            }

            try
            {
                NetworkManager.field_Internal_Static_NetworkManager_0.OnPlayerJoinedDelegate.field_Private_HashSet_1_UnityAction_1_T_0.Add(new Action<VRC.Player>(player => OnPlayerJoined(ref player)));
                MelonLoader.MelonLogger.Msg(System.ConsoleColor.Cyan, "[Patch] OnPlayerJoined!");
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Msg(System.ConsoleColor.Red, "Failed to Hook OnPlayerJoined!\n" + e.Message);
            }
        }

        public static void CheckFewTags(VRC.Player __0)
        {
            if (!Main.s_rawTags.Contains(__0.field_Private_APIUser_0.id)) return;
            try
            {
                Main.PlateHandler(__0, Main.overlay);
            }
            catch (Exception e) { MelonLoader.MelonLogger.Msg($"Failed To Add FewTags Plates To {__0.field_Private_APIUser_0.displayName} " + e); }
            if (!Main.players.Contains(__0))
            {
                Main.players.Add(__0);
            }
            Main.NameplateESP(__0);
        }

        public static void OnPlayerJoined(ref VRC.Player __0)
        {
            if (__0.field_Private_APIUser_0 == null) return;
            CheckFewTags(__0);
            //Chatbox.UpdateMyChatBoxOnJoin(__0);
        }
    }
}
