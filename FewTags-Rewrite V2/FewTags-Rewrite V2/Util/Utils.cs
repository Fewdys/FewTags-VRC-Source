using System.Text.RegularExpressions;
using FewTags.FewTags.Wrappers;
using TMPro;
using UnityEngine;

namespace FewTags.FewTags
{
    public static class Utils
    {
        internal static System.Random random = new System.Random();
        internal static readonly HashSet<int> usedNegativeIds = new HashSet<int>();
        public static List<string> ObjectsToDestroy = new List<string> { "Trust Icon", "Performance Icon", "Performance Text", "Friend Anchor Stats", "Reason", "Shared Connections Icon", "Shared Connections Text" };
        public static VRC.Player[] AllPlayers;
        private static readonly Regex RemoveHtmlRegex = new Regex(@"<color=[^>]*>|</color>|<b>|</b>|<i>|</i>|<mark=[^>]*>|</mark>|<space=[^>]*>|</space>|<size=[^>]*>|</size>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RemoveHtmlRegexNoSize = new Regex(@"<color=[^>]*>|</color>|<b>|</b>|<i>|</i>|<mark=[^>]*>|</mark>|<space=[^>]*>|</space>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            if (plate.Animator == null && BigPlate)
                return plate._gameObjectBP?.AddComponent<TagAnimator>();
            else if (plate.Animator != null && BigPlate)
                return plate._gameObjectBP?.GetComponent<TagAnimator>();
            if (plate.Animator == null && IDPlate)
                return plate._gameObjectID?.AddComponent<TagAnimator>();
            else if (plate.Animator != null && IDPlate)
                return plate._gameObjectID?.GetComponent<TagAnimator>();
            if (plate.Animator == null && MaliciousPlate)
                return plate._gameObjectM?.AddComponent<TagAnimator>();
            else if (plate.Animator != null && MaliciousPlate)
                return plate._gameObjectM?.GetComponent<TagAnimator>();

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
            if (plate.Animator == null)
                return plate._gameObject?.AddComponent<TagAnimator>();
            else if (plate.Animator != null)
                return plate._gameObject?.GetComponent<TagAnimator>();

            return null;
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
        /// Applies the specified color to the ImageThreeSlice and/or Text components found within the given Transform,
        /// if present.
        /// </summary>
        /// <remarks>If ImageThreeSlice is set to <see langword="true"/>, the method attempts to locate an
        /// ImageThreeSlice component in the Transform's children and applies the specified color. If Text is set to
        /// <see langword="true"/>, the method attempts to locate a TextMeshProUGUI or TextMeshPro component in the
        /// children and applies the color. If the required component is not found, an error is logged. No action is
        /// taken if the Transform is null.</remarks>
        /// <param name="obj">The Transform whose child components will have their color updated. Cannot be null.</param>
        /// <param name="color">The color to apply to the eligible components.</param>
        /// <param name="ImageThreeSlice">true to apply the color to the ImageThreeSlice component, if found; otherwise, false. The default is true.</param>
        /// <param name="Text">true to apply the color to TextMeshProUGUI or TextMeshPro components, if found; otherwise, false. The
        /// default is false.</param>
        public static void ColorPlate(this Transform obj, Color color, bool ImageThreeSlice = true, bool Text = false)
        {
            if (obj == null) return;
            if (ImageThreeSlice)
            {
                var imagethreeslice = obj.GetComponentInChildren<ImageThreeSlice>();
                if (imagethreeslice != null && imagethreeslice.color != color) imagethreeslice.color = color;
                else if (imagethreeslice == null) LogManager.LogErrorToConsole("Failed To Find ImageThreeSlice On: " + obj.name);
            }
            if (Text)
            {
                var text = obj.GetComponentInChildren<TextMeshProUGUI>();
                if (text == null)
                {
                    var tmptext = obj.GetComponentInChildren<TextMeshPro>();
                    if (tmptext != null && tmptext.color != color) tmptext.color = color;
                    else if (tmptext == null) LogManager.LogErrorToConsole("Failed To Find A Text Component On or In Children For: " + obj.name);
                }
                else if (text != null && text.color != color)
                {
                    text.color = color;
                }
            }
        }

        /// <summary>
        /// Safely sets the text of a TextMeshPro or TextMeshProUGUI object.
        /// Can be called directly on a TMP_Text instance.
        /// </summary>
        public static void SetTextSafe(this TMP_Text tmp, string text, bool requireRebuild = false, bool overflow = false)
        {
            if (tmp == null) return;
            string safeText = text ?? string.Empty;
            bool textChanged = !string.Equals(tmp.text, safeText, System.StringComparison.Ordinal);
            if (textChanged) tmp.text = safeText;
            if (overflow && tmp.overflowMode != TextOverflowModes.Overflow) tmp.overflowMode = TextOverflowModes.Overflow;
            if (requireRebuild && textChanged) tmp.ForceMeshUpdate(true, true);
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
        public static void SetTextSafe(this GameObject go, string text, bool RequireRebuild = false, bool Overflow = false)
        {
            if (go == null) return;

            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = text ?? string.Empty;
                if (Overflow && tmp.overflowMode != TextOverflowModes.Overflow) tmp.overflowMode = TextOverflowModes.Overflow;
                if (RequireRebuild) tmp.ForceMeshUpdate(true, true);
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
        internal static int GetUniqueNegativeId(Jsons.Json._Tags? s_tags)
        {
            int id;
            HashSet<int> existingIds = s_tags != null
                ? new HashSet<int>(s_tags.records.Select(r => r.id))
                : new HashSet<int>();

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
        public static Transform RecursiveFindChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                    return child;

                var result = RecursiveFindChild(child, childName);
                if (result != null)
                    return result;
            }
            return parent.Find(childName);
        }

        /// <summary>
        /// Destroys Children When Creating A Plate That Is Not Needed.
        /// </summary>
        public static void DestroyChildren(GameObject? obj)
        {
            foreach (var name in ObjectsToDestroy)
            {
                var find = obj?.transform.Find(name);
                if (find != null)
                    find.gameObject?.SetActive(false);
            }
        }
    }
}
