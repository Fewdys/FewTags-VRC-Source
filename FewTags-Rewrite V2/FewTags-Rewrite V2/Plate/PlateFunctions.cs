using System;
using System.Collections.Generic;
using System.Linq;
using FewTags.FewTags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRC.UI.Elements;

namespace Nyup_FewTags._Plate
{
    internal class PlateFunctions
    {
        private static Dictionary<string, GraphicRaycaster> raycasterMap;
        private static GraphicRaycaster qmRaycaster;
        private static GraphicRaycaster bmRaycaster;
        private static GraphicRaycaster kbRaycaster;

        /// <summary>
        /// Ensures GeneratedIDs and Plates are Cleared.
        /// </summary>
        internal static void WorldChangeCall()
        {
            Utils.ClearGeneratedIDValues();
            ClearAllPlates();
        }

        /// <summary>
        /// Clears or Removes All Plates From All Players That Have Plates.
        /// </summary>
        internal static void ClearAllPlates()
        {
            FewTags.FewTags.FewTags.playerPlates.Clear();
            FewTags.FewTags.FewTags.playerStaticPlates.Clear();
        }

        /// <summary>
        /// Clears or Removes All Plates From UserID Entered If They Have Plates.
        /// </summary>
        internal static void ClearPlatesForPlayer(string uid, bool Destroy = true)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return;

            // ---- Dynamic plates ----
            var platesDict = FewTags.FewTags.FewTags.playerPlates;
            if (platesDict != null && platesDict.Count > 0)
            {
                if (platesDict.TryRemove(uid, out var oldPlates) && oldPlates != null)
                {
                    foreach (var plate in oldPlates)
                    {
                        if (plate == null) continue;
                        try
                        {
                            if (Destroy && plate._gameObject != null)
                                UnityEngine.Object.Destroy(plate._gameObject);
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogErrorToConsole(
                                $"[ClearPlates] Cleanup failed (dynamic) | uid={uid} | plate={plate} | {ex}"
                            );
                        }
                        finally
                        {
                            plate.ClearRefs();
                        }
                    }
                }
            }

            // ---- Static plates ----
            var staticDict = FewTags.FewTags.FewTags.playerStaticPlates;
            if (staticDict != null && staticDict.Count > 0)
            {
                if (staticDict.TryRemove(uid, out var oldStaticPlates) && oldStaticPlates != null)
                {
                    foreach (var plate in oldStaticPlates)
                    {
                        if (plate == null) continue;
                        try
                        {
                            if (Destroy)
                            {
                                if (plate._gameObjectBP != null)
                                    UnityEngine.Object.Destroy(plate._gameObjectBP);
                                if (plate._gameObjectM != null)
                                    UnityEngine.Object.Destroy(plate._gameObjectM);
                                if (plate._gameObjectID != null)
                                    UnityEngine.Object.Destroy(plate._gameObjectID);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogErrorToConsole(
                                $"[ClearPlates] Cleanup failed (static) | uid={uid} | plate={plate} | {ex}"
                            );
                        }
                        finally
                        {
                            plate.ClearRefs();
                        }
                    }
                }
            }

            // ---- Cached state ----
            FewTagsUpdater.lastAppliedTags?.TryRemove(uid, out _);
            FewTagsUpdater.lastBigPlateText?.TryRemove(uid, out _);
        }

        /// <summary>
        /// Function Call For Changing Weather Or Not To Hide All Tags.
        /// </summary>
        internal static void ChangeNameplates()
        {
            List<VRC.Player> pSnapshot;
            lock (FewTags.FewTags.FewTags.Lock)
            {
                if (FewTags.FewTags.FewTags.p.Count == 0) return;
                pSnapshot = new List<VRC.Player>(FewTags.FewTags.FewTags.p);
            }

            var allPlayers = Utils.AllPlayers;
            if (allPlayers == null || allPlayers.Length == 0) return;

            for (int i = 0; i < allPlayers.Length; i++)
            {
                var user = allPlayers[i];
                VRC.Player player = user?.gameObject?.GetComponent<VRC.Player>();
                if (player == null) continue;

                for (int j = 0; j < pSnapshot.Count; j++)
                {
                    VRC.Player p = pSnapshot[j];
                    if (p != null && p == player)
                    {
                        NameplateESP(p);
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
            var nameplate = Utils.GetPlayerNameplateContainer(player);
            if (nameplate == null) return;
            var qs = nameplate?.transform.Find("PlayerNameplate/Canvas/NameplateGroup/Nameplate/Contents/Quick Stats")?.gameObject ?? nameplate?.transform.Find("PlayerNameplate/Canvas/NameplateGroup/NameplateFragment/ExpandedInfo")?.gameObject;
            var tmps = qs?.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (qs != null && tmps != null && tmps.Length > 0)
            {
                foreach (var tmp in tmps)
                {
                    if (tmp == null) continue;
                    tmp.SetOverlay();
                }
            }
        }

        public static bool AreMenusOpen(bool ExcludQM = false, bool ExcludeKeyboard = false)
        {
            // QuickMenu cache
            if (qmRaycaster == null)
            {
                qmRaycaster ??= FindRaycaster(new string[] { "Canvas_QuickMenu" });
            }

            // MainMenu cache
            if (bmRaycaster == null)
            {
                bmRaycaster ??= FindRaycaster(new string[] { "Canvas_MainMenu" });
            }

            // Lazy init & cache keyboard raycaster
            if (!ExcludeKeyboard)
            {
                kbRaycaster ??= FindRaycaster(new string[] { /*"HeaderOffset", */"Modal_MM_Keyboard" });

                if (kbRaycaster != null && kbRaycaster.enabled)
                    return true;
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
        /// Finds a GraphicRaycaster whose GameObject name starts with any of the given prefixes.
        /// Uses dictionary for O(1)-ish lookup.
        /// </summary>
        public static GraphicRaycaster FindRaycaster(string[] nameStartsWith)
        {
            try
            {
                BuildRaycasterMap();

                if (nameStartsWith.Length == 0) return null;

                for (int i = 0; i < nameStartsWith.Length; i++)
                {
                    foreach (var kv in raycasterMap)
                    {
                        if (kv.Key == null) continue;
                        if (kv.Key.StartsWith(nameStartsWith[i], StringComparison.Ordinal))
                            return kv.Value;
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Builds the name-prefix → GraphicRaycaster map.
        /// Only rebuilds if dictionary is null or all cached objects are invalid.
        /// </summary>
        private static void BuildRaycasterMap()
        {
            try
            {
                if (raycasterMap != null)
                {
                    foreach (var kv in raycasterMap)
                    {
                        if (kv.Value != null)
                            return; // still valid
                    }
                }
                else
                {

                    raycasterMap = new Dictionary<string, GraphicRaycaster>();
                    CanvasGroup[] all = null;
                    try
                    {
                        all = GameObject.FindObjectsOfType<CanvasGroup>() ?? null;
                    }
                    catch (TypeInitializationException tie)
                    {
                        return; // Don't rebuild the map if type fails
                    }
                    catch (Exception ex)
                    {
                        return;
                    }

                    if (all == null || all.Length == 0)
                        return;

                    for (int i = 0; i < all.Length; i++)
                    {
                        var obj = all[i]?.gameObject;
                        if (obj == null || obj.Pointer == IntPtr.Zero) continue;

                        var gr = obj.GetComponentInChildren<GraphicRaycaster>();
                        if (gr != null && !raycasterMap.ContainsKey(obj.name))
                            raycasterMap[obj.name] = gr;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Checks For If NameplateESP Was Toggled.
        /// </summary>
        internal static void CheckNameplateESPBind()
        {
            if (!AreMenusOpen())
            {
                FewTags.FewTags.FewTags.isOverlay = !FewTags.FewTags.FewTags.isOverlay;
                LogManager.LogWarningToConsole($"Nameplate Overlay Was {(FewTags.FewTags.FewTags.isOverlay ? "Enabled" : "Disabled")}");

                // FIXED: snapshot under lock to avoid race condition
                List<VRC.Player> pSnapshot;
                lock (FewTags.FewTags.FewTags.Lock)
                {
                    pSnapshot = new List<VRC.Player>(FewTags.FewTags.FewTags.p);
                }

                if (pSnapshot.Count == 0) return;

                var allPlayers = Utils.AllPlayers;
                if (allPlayers == null || allPlayers.Length == 0) return;

                for (int i = 0; i < allPlayers.Length; i++)
                {
                    var user = allPlayers[i];
                    VRC.Player player = user?.gameObject?.GetComponent<VRC.Player>();
                    if (player == null) continue;

                    for (int j = 0; j < pSnapshot.Count; j++)
                    {
                        VRC.Player p = pSnapshot[j];
                        if (p != null && p.Pointer != IntPtr.Zero && p == player)
                        {
                            PlateFunctions.NameplateESP(p);
                        }
                    }
                }
            }
        }
    }
}
