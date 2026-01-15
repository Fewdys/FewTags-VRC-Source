using FewTags.FewTags.Wrappers;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace FewTags.FewTags
{
    internal class PlateFunctions
    {
        private static GameObject qm;
        private static GameObject bm;
        private static GameObject kb;
        private static GraphicRaycaster qmRaycaster;
        private static GraphicRaycaster bmRaycaster;
        private static GraphicRaycaster kbRaycaster;

        /// <summary>
        /// Ensures GeneratedIDs and Plates are Cleared.
        /// </summary>
        internal static void WorldChangeCall()
        {
            Utils.ClearGeneratedIDValues();
            //ClearAllPlates(); // this is not needed and calling so could cause issues, this is pretty much already handled by unity itself
        }

        /// <summary>
        /// Clears or Removes All Plates From All Players That Have Plates.
        /// </summary>
        internal static void ClearAllPlates()
        {
            var keys1 = FewTags.playerPlates.Keys.ToArray();
            for (int i = 0; i < keys1.Length; i++)
            {
                var key = keys1[i];
                var plates = FewTags.playerPlates[key];
                for (int j = 0; j < plates.Count; j++)
                {
                    plates[j]?.Cleanup();
                }
            }
            FewTags.playerPlates.Clear();

            var keys2 = FewTags.playerStaticPlates.Keys.ToArray();
            for (int i = 0; i < keys2.Length; i++)
            {
                var key = keys2[i];
                var plates = FewTags.playerStaticPlates[key];
                for (int j = 0; j < plates.Count; j++)
                {
                    plates[j]?.Cleanup();
                }
            }
            FewTags.playerStaticPlates.Clear();
        }

        /// <summary>
        /// Clears or Removes All Plates From UserID Entered If They Have Plates.
        /// </summary>
        internal static void ClearPlatesForPlayer(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return;

            if (FewTags.playerPlates.TryGetValue(uid, out var oldPlates))
            {
                for (int i = 0; i < oldPlates.Count; i++)
                {
                    oldPlates[i]?.Cleanup();
                }

                FewTags.playerPlates.Remove(uid);
            }

            if (FewTags.playerStaticPlates.TryGetValue(uid, out var oldStaticPlates))
            {
                for (int i = 0; i < oldStaticPlates.Count; i++)
                {
                    oldStaticPlates[i]?.Cleanup();
                }

                FewTags.playerStaticPlates.Remove(uid);
            }

            FewTagsUpdater.lastAppliedTags.Remove(uid);
            FewTagsUpdater.lastBigPlateText.Remove(uid);
        }

        /// <summary>
        /// Function Call For Changing Weather Or Not To Hide All Tags.
        /// </summary>
        internal static void ChangeNameplates(bool value)
        {
            if (FewTags.p.Count != 0)
            {
                var allPlayers = Utils.AllPlayers; // or assign whatever you're playerlist is
                if (allPlayers == null || allPlayers.Length == 0) return;
                for (int i = 0; i < allPlayers.Length; i++)
                {
                    var user = allPlayers[i];
                    VRC.Player player = user?.gameObject?.GetComponent<VRC.Player>();
                    if (player == null) continue;

                    for (int j = 0; j < FewTags.p.Count; j++)
                    {
                        VRC.Player p = FewTags.p[j];
                        if (p != null && p == player)
                        {
                            ChangePlayerTag(p, value);
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Function Call For Changing Weather Or Not To Hide All Tags On A Specific Player.
        /// </summary>
        internal static void ChangePlayerTag(VRC.Player player, bool value)
        {
            var nameplate = player._vrcplayer?.Nameplate;
            if (nameplate == null) return;

            var transforms = nameplate.GetComponentsInChildren<Transform>(true);
            if (transforms == null || transforms.Length == 0) return;
            for (int i = 0; i < transforms.Length; i++)
            {
                var t = transforms[i];
                if (t.name.IndexOf("fewtag", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    t.gameObject.SetActive(value);
                }
            }

        }

        /// <summary>
        /// Toggles NameplateESP For Tagged Users.
        /// </summary>
        internal static void NameplateESP(VRC.Player player)
        {
            if (player._vrcplayer?.Nameplate?.quickStats != null && player._vrcplayer?.Nameplate.field_Public_TextMeshProUGUIEx_4 != null)
            {
                player._vrcplayer.Nameplate.field_Public_TextMeshProUGUIEx_4.SetOverlay();
            }
        }

        public static bool AreMenusOpen(bool ExcludQM = false, bool ExcludeKeyboard = false)
        {
            // QuickMenu cache
            if (qm == null)
            {
                qm = Resources.FindObjectsOfTypeAll<GraphicRaycaster>()
                    .FirstOrDefault(x => x != null && x.name.StartsWith("Canvas_QuickMenu"))?.gameObject;
                if (qm != null) qmRaycaster = qm.GetComponent<GraphicRaycaster>();
            }

            // MainMenu cache
            if (bm == null)
            {
                bm = Resources.FindObjectsOfTypeAll<GraphicRaycaster>()
                    .FirstOrDefault(x => x != null && x.name.StartsWith("Canvas_MainMenu"))?.gameObject;
                if (bm != null) bmRaycaster = bm.GetComponent<GraphicRaycaster>();
            }

            // Keyboard cache
            if (kb == null && bm != null)
            {
                var kbTransform = bm.transform.Find("Modal_MM_Keyboard(Clone)");
                kb = kbTransform?.gameObject;
                if (kb != null) kbRaycaster = kb.GetComponent<GraphicRaycaster>();
            }

            // Check states
            if (!ExcludeKeyboard && kbRaycaster != null && kbRaycaster.enabled)
                return true;
            if (bmRaycaster != null && bmRaycaster.enabled)
                return true;
            if (!ExcludQM && qmRaycaster != null && qmRaycaster.enabled)
                return true;

            return false;
        }

        /// <summary>
        /// Checks For If NameplateESP Was Toggled.
        /// </summary>
        internal static void CheckNameplateESPBind()
        {
            if (!AreMenusOpen())
            {
                FewTags.isOverlay = !FewTags.isOverlay;
                LogManager.LogWarningToConsole($"Nameplate Overlay Was {(FewTags.isOverlay ? "Enabled" : "Disabled")}");
                if (FewTags.p.Count != 0)
                {
                    var allPlayers = Utils.AllPlayers; // or assign whatever you're playerlist is
                    if (allPlayers == null || allPlayers.Length == 0) return;

                    for (int i = 0; i < allPlayers.Length; i++)
                    {
                        var user = allPlayers[i];
                        VRC.Player player = user?.gameObject?.GetComponent<VRC.Player>();
                        if (player == null) continue;

                        for (int j = 0; j < FewTags.p.Count; j++)
                        {
                            VRC.Player p = FewTags.p[j];
                            if (p != null && p == player)
                            {
                                PlateFunctions.NameplateESP(p);
                            }
                        }
                    }
                }

            }
        }
    }
}
