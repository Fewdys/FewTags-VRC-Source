using HarmonyLib;

namespace FewTags
{
    internal class OnPlayer
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

            EnsureTagsAreAdded(__0);

            return;
        }

        public static void OnPlayerLeave(ref VRC.Player __0)
        {
            if (__0.prop_APIUser_0 == null) return;

            if (Main.p.Contains(__0))
            {
                Main.p.Remove(__0);
            }
            return;
        }

        public static void EnsureTagsAreAdded(VRC.Player __0)
        {

            if (Main.s_tags == null)
            {
                string Warning = "s_tags is not initialized. Force-updating...";
                // log warning
                Main.UpdateTags(); // force a synchronous update
            }

            if (Main.s_rawTags != null && Main.s_rawTags.Contains(__0.field_Private_APIUser_0.id)) // ensure userid is in rawtags
            {
                Main.PlateHandler(__0);
                if (!Main.p.Contains(__0))
                {
                    Main.p.Add(__0);
                }
                Main.NameplateESP(__0);
            }
        }
    }
}

