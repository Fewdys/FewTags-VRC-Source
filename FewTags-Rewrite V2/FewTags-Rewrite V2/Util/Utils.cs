using System.Text.RegularExpressions;
using FewTags.FewTags.Wrappers;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace FewTags.FewTags
{
    public static class Utils
    {
        public static VRC.Player[] AllPlayers;
        internal static System.Random random = new System.Random();
        internal static readonly HashSet<long> usedNegativeIds = new HashSet<long>();
        public static List<string> ObjectsToDestroy = new List<string> { "Trust Icon", "Performance Icon", "Performance Text", "Friend Anchor Stats", "Reason", "Shared Connections Icon", "Shared Connections Text", "Spacing", "Earmuffs Icon", "Age Verification Icon", "Performance Rank Icon", "Group Icon" };

        private static readonly Regex RemoveHtmlRegex = new Regex(@"<color=[^>]*>|</color>|<b>|</b>|<i>|</i>|<mark=[^>]*>|</mark>|<space=[^>]*>|</space>|<size=[^>]*>|</size>|<voffset=[^>]*>|</voffset>|<width=[^>]*>|</width>|<rotate=[^>]*>|</rotate>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RemoveHtmlRegexNoSize = new Regex(@"<color=[^>]*>|</color>|<b>|</b>|<i>|</i>|<mark=[^>]*>|</mark>|<space=[^>]*>|</space>|<voffset=[^>]*>|</voffset>|<width=[^>]*>|</width>|<rotate=[^>]*>|</rotate>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static GameObject GetPlayerNameplateContainer(this VRC.Player player)
        {
            var vrcplayer = player.gameObject.GetComponent<VRCPlayer>();
            if (vrcplayer == null || vrcplayer.Pointer == IntPtr.Zero)
                return null;

            var klass = vrcplayer.GetIl2CppType();
            if (klass == null || klass.Pointer == IntPtr.Zero)
            {
                LogManager.LogErrorToConsole("Could Not Get Class For VRCPlayer");
                return null;
            }

            GameObject wantedobj = null;

            var fields = klass.GetFields();
            foreach (var field in fields)
            {
                try
                {
                    if (!field.FieldType.Name.Contains("GameObject"))
                        continue;

                    var value = field.GetValue(vrcplayer);
                    var gameObject = value?.TryCast<GameObject>();

                    if (gameObject == null || gameObject.Pointer == IntPtr.Zero)
                        continue;

                    string name;
                    try
                    {
                        name = gameObject.name;
                    }
                    catch
                    {
                        continue;
                    }

                    if (name == "NameplateContainer")
                    {
                        if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("Found NameplateContainer");
                        wantedobj = gameObject;
                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (wantedobj == null)
            {

                LogManager.LogErrorToConsole("Failed To Find NameplateContainer!");
            }

            return wantedobj;
        }
        /// <summary>
        /// Sets the color of a TMP_Text component.
        /// </summary>
        /// <param name="tmp">The TMP_Text component.</param>
        /// <param name="color">The target color.</param>
        /// <param name="preserveAlpha">If true, keeps the current alpha value.</param>
        public static void SetColor(this TMP_Text tmp, Color color, bool preserveAlpha = false)
        {
            if (tmp == null) return;

            if (preserveAlpha)
            {
                color.a = tmp.color.a;
            }

            tmp.color = color;
        }

        public static void GetAllPlayers()
        {
            var players = PlayerWrapper.GetAllVRCPlayers();
            if (players == null) return;
            AllPlayers = players.ToArray();
        }

        /// <summary>
        /// Sets the color using a hex string (e.g., "FF00FF" or "FF00FF80").
        /// </summary>
        /// <param name="tmp">The TMP_Text component.</param>
        /// <param name="hex">Hex string of the color.</param>
        public static void SetColorHex(this TMP_Text tmp, string hex)
        {
            if (tmp == null || string.IsNullOrEmpty(hex)) return;

            if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            {
                tmp.color = color;
            }
        }

        /// <summary>
        /// Sets the active state of the specified TextMeshProUGUI component and its associated GameObject.
        /// </summary>
        /// <remarks>If the provided TextMeshProUGUI component is null, the method does nothing. This
        /// method also enables or disables the TextMeshProUGUI component based on the Active parameter.</remarks>
        /// <param name="tmp">The TextMeshProUGUI component whose active state is to be set. This parameter cannot be null.</param>
        /// <param name="Active">A boolean value indicating whether to activate or deactivate the TextMeshProUGUI component and its
        /// GameObject. <see langword="true"/> activates the component; otherwise, it deactivates it.</param>
        public static void SetPlateActive(this TextMeshProUGUI tmp, bool Active)
        {
            if (tmp == null) return;
            var obj = tmp.gameObject;
            if (obj != null) obj.SetActive(Active);
            tmp.enabled = Active;
        }

        public static string GetText(this TextMeshProUGUI tmp)
        {
            if (tmp == null) return string.Empty;
            return tmp.text;
        }

        public static Color GetBackgroundColor(this GameObject obj)
        {
            if (obj == null) return Color.white;
            var imagethreeslice = obj.GetComponentInChildren<ImageThreeSlice>();
            if (imagethreeslice != null) return imagethreeslice.color;
            return Color.white;
        }

        public static Color GetMainPlateColor(this PlayerNameplate nameplate)
        {
            if (nameplate == null) return Color.white;

            return nameplate.field_Public_Color_0;
        }

        public static Color GetMainPlateColorByImageThreeSlice(this PlayerNameplate nameplate)
        {
            if (nameplate == null) return Color.white;
            var background = nameplate.mainContainer?.transform.Find("Background");
            if (background != null)
            {
                var imagethreeslice = background.GetComponent<ImageThreeSlice>();
                if (imagethreeslice != null) return imagethreeslice.color;
            }

            return Color.white;
        }

        /// <summary>
        /// Gets an existing TagAnimator component or adds a new one to the specified PlateStatic object, targeting the
        /// big plate, ID plate, or malicious plate GameObject based on the provided flags.
        /// </summary>
        /// <remarks>Only one of the plate type flags (BigPlate, IDPlate, MaliciousPlate) should be set to
        /// true at a time. If multiple flags are true, the method prioritizes them in the order: BigPlate, IDPlate,
        /// MaliciousPlate.</remarks>
        /// <param name="plate">The PlateStatic instance to which the TagAnimator component will be added or from which it will be
        /// retrieved.</param>
        /// <param name="BigPlate">true to add or retrieve the TagAnimator from the big plate GameObject; otherwise, false.</param>
        /// <param name="IDPlate">true to add or retrieve the TagAnimator from the ID plate GameObject; otherwise, false. The default is
        /// false.</param>
        /// <param name="MaliciousPlate">true to add or retrieve the TagAnimator from the malicious plate GameObject; otherwise, false. The default
        /// is false.</param>
        /// <returns>The TagAnimator component associated with the specified PlateStatic object and plate type, or null if no
        /// component could be added or found.</returns>
        public static TagAnimator GetOrAddAnimator(this PlateStatic plate, bool BigPlate, bool IDPlate = false, bool MaliciousPlate = false)
        {
            if (BigPlate)
            {
                if (plate.AnimatorBP != null) return plate.AnimatorBP;
                plate.AnimatorBP = plate._gameObjectBP?.AddComponent<TagAnimator>();
                return plate.AnimatorBP;
            }
            if (IDPlate)
            {
                if (plate.AnimatorID != null) return plate.AnimatorID;
                plate.AnimatorID = plate._gameObjectID?.AddComponent<TagAnimator>();
                return plate.AnimatorID;
            }
            if (MaliciousPlate)
            {
                if (plate.AnimatorM != null) return plate.AnimatorM;
                plate.AnimatorM = plate._gameObjectM?.AddComponent<TagAnimator>();
                return plate.AnimatorM;
            }
            return null;
        }

        /// <summary>
        /// Gets the existing TagAnimator component associated with the specified Plate, or adds a new one if none
        /// exists.
        /// </summary>
        /// <remarks>If the Plate does not already have an Animator, a new TagAnimator component is added
        /// to its GameObject. If an Animator exists, the method returns the existing TagAnimator component. This method
        /// requires that the Plate's GameObject is not null.</remarks>
        /// <param name="plate">The Plate instance to retrieve or add the TagAnimator component for. Cannot be null.</param>
        /// <returns>The TagAnimator component associated with the Plate, or null if the Plate's GameObject is null or the
        /// component cannot be added.</returns>
        public static TagAnimator GetOrAddAnimator(this Plate plate)
        {
            // Capture the tuple result
            return plate._gameObject?.AddComponent<TagAnimator>();
        }

        /// <summary>
        /// Sets isOverlay for TMP.
        /// </summary>
        public static void SetOverlay(this TMP_Text tmp)
        {
            if (tmp == null) return;
            tmp.isOverlay = FewTags.isOverlay;
        }

        /// <summary>
        /// Colors UsersNameplate (Doesn't Exactly Work Anymore)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="SpeakingAndGlowColor"></param>
        public static void ColorPlate(VRC.Player player, Color SpeakingAndGlowColor)
        {
            if (player == null || player.Pointer == IntPtr.Zero || !FewTags.FewTagsEnabled) return;

            var components = player.GetComponent<VRCPlayer>();
            if (components == null) return;
            Color pc = Color.white; // GET RANK COLOR HERE!
            if (pc != null)
            {
                _ = ColorUsersNameplate(player, pc, SpeakingAndGlowColor);
            }
        }

        public static async Task ColorUsersNameplate(VRC.Player player, Color color, Color speakcolor)
        {
            if (player == null || color == null || speakcolor == null) return;
            if (color.a == 0f) color.a = 1f;
            if (speakcolor.a == 0f) speakcolor.a = 1f;

            float alpha = color.a;
            Color hdrColor = new Color(color.r + 0.3f, color.g + 0.3f, color.b + 0.3f, alpha);

            float alpha2 = speakcolor.a;
            Color hdrSpeakColor = new Color(speakcolor.r, speakcolor.g, speakcolor.b, alpha2);

            //Color ldrColor = new Color(Mathf.Clamp01(color.r * 2f), Mathf.Clamp01(color.g * 2f), Mathf.Clamp01(color.b * 2f), alpha);

            var vrcplayer = player.GetComponent<VRCPlayer>();
            while (vrcplayer == null)
            {
                await Task.Delay(500);
                if (player == null || player.Pointer == IntPtr.Zero) return;
                vrcplayer = player.GetComponent<VRCPlayer>();
            }

            var nameplate = vrcplayer.Nameplate;
            while (nameplate == null)
            {
                await Task.Delay(500);
                if (player == null || player.Pointer == IntPtr.Zero) return;
                nameplate = vrcplayer.Nameplate;
            }

            var contents = nameplate.gameObject.transform.Find("Contents") ?? nameplate.gameObject.transform.Find("NameplateFragment");
            while (contents == null || contents.gameObject == null)
            {
                await Task.Delay(500);
                if (player == null || player.Pointer == IntPtr.Zero) return;
                contents = nameplate.gameObject.transform.Find("Contents") ?? nameplate.gameObject.transform.Find("NameplateFragment");
            }

            ImageThreeSlice[] threeslices;
            while (true)
            {
                threeslices = contents.GetComponentsInChildren<ImageThreeSlice>(true);
                if (threeslices.Length >= 4) break;
                await Task.Delay(500);
                if (player == null || player.Pointer == IntPtr.Zero) return;
            }

            foreach (var image in threeslices)
            {
                if (image == null || image.gameObject == null) continue;
                string name = image.gameObject.name;

                if (name.Contains("Background") && image.transform?.parent?.name == "Main" || name.Contains("Background") && image.transform?.parent?.name == "Icon")
                {
                    image.CrossFadeColor(hdrColor, 0, true, true, true);
                }
                else if (name.Contains("Status Icon") || name.Contains("Platform") || name.Contains("Pronoun") ||
                    name.Contains("Stats") || name.Contains("Info") || name.Contains("Few") ||
                    name.Contains("Nyup") || name.Contains("InteractBackground"))
                {
                    image.CrossFadeColor(hdrColor, 0, true, true, true);
                }
                else if (name.Contains("Glow") || name.Contains("Pulse") || name.Contains("Border"))
                {
                    image.color = hdrSpeakColor;
                }
                else if (name == "Icon")
                {
                    image.color = hdrColor;
                }
                /*else
                {
                    image.color = hdrColor;
                }*/
            }
        }

        /// <summary>
        /// Safely sets the text of a TextMeshPro or TextMeshProUGUI object.
        /// Can be called directly on a TMP_Text instance.
        /// </summary>
        public static void SetTextSafe(this TMP_Text tmp, string text, bool requireRebuild = false, bool overflow = true)
        {
            if (tmp == null) return;
            if (!tmp.richText) tmp.richText = true;
            if (tmp.enableAutoSizing) tmp.enableAutoSizing = false;
            if (!tmp.enableCulling) tmp.enableCulling = true;
            string safeText = text ?? string.Empty;
            bool textChanged = !string.Equals(tmp.text, safeText, System.StringComparison.Ordinal);
            if (textChanged) tmp.text = safeText;
            if (overflow && tmp.overflowMode != TextOverflowModes.Overflow) tmp.overflowMode = TextOverflowModes.Overflow;
            if (requireRebuild && textChanged) tmp.ForceMeshUpdate(true, true);

            var parent = tmp.transform.parent;
            var parentsparent = parent?.parent;
            if (parentsparent != null)
            {
                var layoutGroup = parentsparent.GetComponent<HorizontalLayoutGroup>();
                if (layoutGroup != null)
                {
                    layoutGroup.childAlignment = TextAnchor.MiddleCenter; // fixes cancer with text not wanting to center on nameplate
                }
            }
        }

        /// <summary>
        /// Adds the specified player to the blacklist, preventing they're tags from showing
        /// </summary>
        /// <param name="player">The player to be added to the blacklist. Must not be null.</param>
        public static void AddToBlacklist(this VRC.Player player)
        {
            if (player == null) return;
            string userid = player.GetPlayersUserID();
            if (!string.IsNullOrEmpty(userid))
            {
                if (FewTags.BlacklistedUserIDs.Contains(userid))
                {
                    LogManager.LogWarningToConsole($"UserID: {userid} Already In Blacklist");
                    return;
                }

                FewTags.BlacklistedUserIDs.Add(userid);

                var json = JSON.JSON.Parse(File.ReadAllText(FewTagsConfigLoader.ConfigPath));

                json["BlacklistedUserIDs"].Add(userid);

                FewTagsConfigLoader.Save();
                FewTagsUpdater.UpdatePlayerTags(player);
            }
        }

        /// <summary>
        /// Removes the specified player from the blacklist if they are currently blacklisted.
        /// </summary>
        /// <remarks>If the player is not found in the blacklist, a warning is logged to the console. The
        /// configuration file is updated to reflect the removal of the player from the blacklist.</remarks>
        /// <param name="player">The player to remove from the blacklist. This parameter cannot be null.</param>
        public static void RemoveFromBlacklist(this VRC.Player player)
        {
            if (player == null) return;
            string userid = player.GetPlayersUserID();
            if (!string.IsNullOrEmpty(userid))
            {
                if (!FewTags.BlacklistedUserIDs.Contains(userid))
                {
                    LogManager.LogWarningToConsole($"UserID: {userid} Is Not In The Blacklist");
                    return;
                }

                FewTags.BlacklistedUserIDs.Remove(userid);

                var json = JSON.JSON.Parse(File.ReadAllText(FewTagsConfigLoader.ConfigPath));

                json["BlacklistedUserIDs"].Remove(userid);

                FewTagsConfigLoader.Save();
                FewTagsUpdater.UpdatePlayerTags(player);
            }
        }

        /// <summary>
        /// Retrieves the user ID associated with the specified player.
        /// </summary>
        /// <remarks>If the player does not have an associated API user or the user ID is null or empty,
        /// the method returns an empty string.</remarks>
        /// <param name="player">The player instance from which to obtain the user ID. This parameter must not be null.</param>
        /// <returns>The user ID of the player if available; otherwise, an empty string.</returns>
        public static string GetPlayersUserID(this VRC.Player player)
        {
            var apiuser = player.APIUser;
            if (apiuser != null && !string.IsNullOrEmpty(apiuser.id)) return apiuser.id;

            return string.Empty;
        }


        /// <summary>
        /// Safely sets the text on a GameObject by finding a TMP_Text component on it.
        /// </summary>
        public static void SetTextSafe(this GameObject go, string text, bool requireRebuild = false, bool overflow = false)
        {
            if (go == null) return;

            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                if (!tmp.richText) tmp.richText = true;
                if (tmp.enableAutoSizing) tmp.enableAutoSizing = false;
                if (!tmp.enableCulling) tmp.enableCulling = true;
                string safeText = text ?? string.Empty;
                bool textChanged = !string.Equals(tmp.text, safeText, System.StringComparison.Ordinal);
                if (textChanged) tmp.text = safeText;
                if (overflow && tmp.overflowMode != TextOverflowModes.Overflow) tmp.overflowMode = TextOverflowModes.Overflow;
                if (requireRebuild && textChanged) tmp.ForceMeshUpdate(true, true);

                var parent = tmp.transform.parent;
                var parentsparent = parent?.parent;
                if (parentsparent != null)
                {
                    var layoutGroup = parentsparent.GetComponent<HorizontalLayoutGroup>();
                    if (layoutGroup != null)
                    {
                        layoutGroup.childAlignment = TextAnchor.MiddleCenter; // fixes cancer with text not wanting to center on nameplate
                    }
                }
            }
        }

        /// <summary>
        /// Replaces Animation Prefixes Found In Text.
        /// </summary>
        internal static string ReplaceAniNames(string text)
        {
            return text.Replace(".LBL.", "").Replace(".CYLN.", "").Replace(".RAIN.", "")
                .Replace(".SR.", "").Replace(".PULSE.", "").Replace(".JUMP.", "")
                .Replace(".SHAKE.", "").Replace(".GT.", "").Replace(".BLINK.", "").Replace(".GLITCH.", "").Replace(".SCROLL.", "");
        }

        static string CleanTag(string t) => t?.Trim().Replace("\r\n", "\n").Replace("\r", "\n").Replace("\u200B", "").ToLowerInvariant() ?? "";

        /// <summary>
        /// Normalizes a collection of tag strings by trimming whitespace, converting to lowercase, and sorting them in
        /// ascending order.
        /// </summary>
        /// <param name="tags">The collection of tag strings to normalize. Each tag will be trimmed and converted to lowercase. Cannot be
        /// null.</param>
        /// <returns>An array of normalized tag strings, sorted in ascending order. The array will be empty if the input
        /// collection contains no tags.</returns>
        internal static string[] NormalizeTags(IEnumerable<string> tags) => tags?.Select(CleanTag).Where(t => !string.IsNullOrEmpty(t)).OrderBy(t => t).ToArray() ?? System.Array.Empty<string>();

        /// <summary>
        /// Adds a local tag prefix to the specified text, inserting it after a recognized animation marker if present.
        /// </summary>
        /// <remarks>If the text contains a recognized animation marker (such as ".LBL." or ".CYLN."), the
        /// local tag prefix "[L] " is inserted immediately after the first occurrence of such a marker. Otherwise, the
        /// prefix is added at the beginning of the text.</remarks>
        /// <param name="text">The text to which the local tag prefix will be added. Can be null or empty.</param>
        /// <returns>A string with the local tag prefix inserted. If the input is null or empty, returns "[LocalTag]".</returns>
        internal static string AddLocalTagPrefix(string text) // i pressed tab and the note for this was created, thanks co-pilot :3
        {
            if (string.IsNullOrEmpty(text))
                return "[LocalTag]";

            string[] aniNames = new string[]
            {
                ".LBL.", ".CYLN.", ".RAIN.", ".SR.", ".PULSE.",
                ".JUMP.", ".SHAKE.", ".GT.", ".BLINK.", ".GLITCH.", ".SCROLL."
            };

            int index = aniNames.Select(name => (text.IndexOf(name), name)).Where(t => t.Item1 >= 0).OrderBy(t => t.Item1).Select(t => t.Item1).FirstOrDefault(-1);

            if (index >= 0)
            {
                int endOfAni = index + aniNames.First(n => text.IndexOf(n) == index).Length;
                return text.Insert(endOfAni, "[L] ");
            }
            else
            {
                return "[L] " + text;
            }
        }


        /// <summary>
        /// Checks If The Passed Through String For A Tag Needs An Animator When Allowed.
        /// </summary>
        internal static bool NeedsAnimator(string tag, out System.Action<TagAnimator> applyAnim)
        {
            applyAnim = null;
            if (!FewTags.EnableAnimations) return false;

            var lowerTag = tag.ToLower();

            applyAnim = lowerTag switch
            {
                var t when t.StartsWith(".lbl.") => a => a.LetterByLetter = true,
                var t when t.StartsWith(".cyln.") => a => a.Bounce = true,
                var t when t.StartsWith(".rain.") => a => a.Rainbow = true,
                var t when t.StartsWith(".sr.") => a => a.SmoothRainbow = true,
                var t when t.StartsWith(".pulse.") => a => a.Pulse = true,
                var t when t.StartsWith(".jump.") => a => a.Jump = true,
                var t when t.StartsWith(".shake.") => a => a.Shake = true,
                var t when t.StartsWith(".gt.") => a => a.GhostTrail = true,
                var t when t.StartsWith(".blink.") => a => a.Blink = true,
                var t when t.StartsWith(".glitch.") => a => a.Glitch = true,
                var t when t.StartsWith(".scroll.") => a => a.Scroll = true,
                _ => null
            };

            return applyAnim != null;
        }

        /// <summary>
        /// Removes Most If Not All Html Tags From A String.
        /// </summary>
        internal static string RemoveHtmlTags(string text, bool excludesize = false)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return excludesize ? RemoveHtmlRegexNoSize.Replace(text, string.Empty) : RemoveHtmlRegex.Replace(text, string.Empty);
        }

        /// <summary>
        /// Gets A Unique Negative Int.
        /// This Can Be Used For Asigning A Unique ID For FewTags Through Per-Say Local Tags Loaded Through A Text File.
        /// </summary>
        internal static long GetUniqueNegativeId(Jsons.Json._Tags? s_tags)
        {
            long id;
            HashSet<long> existingIds = s_tags != null
                ? new HashSet<long>(s_tags.records.Select(r => r.id))
                : new HashSet<long>();

            do
            {
                id = -random.Next(1, int.MaxValue); // -1 to -2,147,483,647
            } while (existingIds.Contains(id) || usedNegativeIds.Contains(id));

            usedNegativeIds.Add(id);
            return id;
        }

        /// <summary>
        /// Clears Generated Negative IDs.
        /// </summary>
        internal static void ClearGeneratedIDValues()
        {
            if (usedNegativeIds != null)
                usedNegativeIds.Clear();
        }

        /// <summary>
        /// Recursively Finds Child.
        /// </summary>
        public static Transform RecursiveFindChild(Transform parent, string childName, bool ContainsName = false, bool StartsWith = false)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (ContainsName && !StartsWith)
                {
                    if (child.name.EndsWith(childName))
                        return child;
                }
                else if (ContainsName && StartsWith)
                {
                    if (child.name.StartsWith(childName))
                        return child;
                }
                else
                {
                    if (child.name == childName)
                        return child;
                }

                var result = RecursiveFindChild(child, childName, ContainsName, StartsWith);
                if (result != null)
                    return result;
            }
            return parent.Find(childName);
        }

        /// <summary>
        /// Recursively finds all descendant transforms whose name contains any of the
        /// given keywords (case-insensitive). Unlike RecursiveFindChild, which stops
        /// at the first match, this walks the entire subtree and returns every match —
        /// used to count elements added by other client modules under the nameplate,
        /// so FewTags plates can offset around them without needing to know exactly
        /// what those modules are or how many there are ahead of time.
        /// </summary>
        public static void RecursiveFindAllContaining(Transform parent, string[] keywords, List<Transform> results)
        {
            if (parent == null || keywords == null || results == null) return;

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string nameLower = child.name.ToLowerInvariant();

                for (int k = 0; k < keywords.Length; k++)
                {
                    if (nameLower.Contains(keywords[k]))
                    {
                        results.Add(child);
                        break; // don't double-count a child matching multiple keywords
                    }
                }

                RecursiveFindAllContaining(child, keywords, results);
            }
        }

        /// <summary>
        /// Destroys Children When Creating A Plate That Is Not Needed.
        /// </summary>
        public static void DestroyChildren(GameObject? obj)
        {
            if (obj == null) return;
            var pill = Utils.RecursiveFindChild(obj.transform, "GroupPill", true, true);
            if (pill != null)
            {
                pill.gameObject.SetActive(true);

                var canvasgroup = obj.GetComponent<CanvasGroup>();
                if (canvasgroup != null) canvasgroup.enabled = false;
            }

            foreach (var name in ObjectsToDestroy)
            {
                var find = Utils.RecursiveFindChild(obj?.transform, name, true, true);
                if (find != null)
                {
                    if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"Destroying '{find.name}' (matched '{name}'), childCount={find.childCount}");
                    find.gameObject?.SetActive(false);
                    find.gameObject?.Destroy();
                }
            }
        }
    }
}
