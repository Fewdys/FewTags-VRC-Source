using HarmonyLib;
using System;

namespace FewTags.FewTags.Patches
{
    public class OnPlayer
    {

        public static HarmonyLib.Harmony Instance = new HarmonyLib.Harmony("FewdyWasHere"); // feel free to change to so desired name
        public static void PatchOnPlayer()
        {
            try
            {
                Instance.Patch(typeof(NetworkManager).GetMethod(nameof(NetworkManager/*.JoinMethod*/)), new HarmonyMethod(AccessTools.Method(typeof(OnPlayer), nameof(OnPlayerJoin))));
                Instance.Patch(typeof(NetworkManager).GetMethod(nameof(NetworkManager/*.LeaveMethod*/)), new HarmonyMethod(AccessTools.Method(typeof(OnPlayer), nameof(OnPlayerLeave))));
            }
            catch (Exception e)
            {
                // log exception here
            }
        }

        public static void OnPlayerJoin(ref VRC.Player __0)
        {
            VRC.Player localPlayer = __0;
            if (localPlayer == null) return;
            if (localPlayer.prop_APIUser_0 == null) return;

            Utils.GetAllPlayers(); // might be redundant to call this here, but it's better than having it be called every frame as it's not needed every frame
            JoinLeaveManager.DoBasicJoinCheck(__0);

            return;
        }

        public static void OnPlayerLeave(ref VRC.Player __0)
        {
            if (__0.prop_APIUser_0 == null) return;

            Utils.GetAllPlayers();
            JoinLeaveManager.DoBasicLeaveCheck(__0);

            return;
        }
    }
}
