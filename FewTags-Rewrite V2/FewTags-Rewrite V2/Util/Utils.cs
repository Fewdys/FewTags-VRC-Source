using FewTags.FewTags.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VRC.SDKBase;

namespace FewTags.FewTags
{
    public static class Utils
    {
        internal static System.Random random = new System.Random();
        internal static readonly HashSet<int> usedNegativeIds = new HashSet<int>();
        public static List<string> ObjectsToDestroy = new List<string> { "Trust Icon", "Performance Icon", "Performance Text", "Friend Anchor Stats", "Reason", "Shared Connections Icon", "Shared Connections Text" };
        private static readonly object _beepLock = new();
        private static Queue<(int frequency, int duration)> _patternQueue;
        private static Timer _timer;
        public static VRC.Player[] AllPlayers;

        public static void Beep(int frequency, int duration)
        {
            // frequency in Hertz (37 to 32767), duration in milliseconds
            Console.Beep(frequency, duration);
        }

        public static void AmongUsBeep()
        {
            Task.Run(() => // consider not using a task, they cause memory leaks
            {
                lock (_beepLock)
                {
                    PlayPattern();
                }
            });
        }

        public static void GetAllPlayers()
        {
            var players = PlayerWrapper.GetAllVRCPlayers();
            if (players == null) return;
            AllPlayers = players.ToArray();
        }

        public static void AmongUsBeepAlt()
        {
            lock (_beepLock)
            {
                Thread thread = new Thread(PlayPattern);
                thread.IsBackground = true;
                thread.Start();
                //GC.KeepAlive(thread);
            }
        }

        private static void PlayPattern()
        {
            Beep(300, 400);
            Thread.Sleep(20);

            for (int i = 0; i < 7; i++)
            {
                Beep(750, 100);
                Thread.Sleep(40);
            }

            Thread.Sleep(20);
            Beep(750, 100);
            Thread.Sleep(10);
            Beep(700, 100);
            Thread.Sleep(10);
            Beep(750, 100);

            Beep(400, 180);
            Thread.Sleep(100);
            Beep(400, 180);
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
        /// Sets isOverlay for TMP.
        /// </summary>
        public static void SetOverlay(this TMP_Text tmp)
        {
            if (tmp == null) return;
            tmp.isOverlay = FewTags.isOverlay;
        }

        /// <summary>
        /// Safely sets the text of a TextMeshPro or TextMeshProUGUI object.
        /// Can be called directly on a TMP_Text instance.
        /// </summary>
        public static void SetTextSafe(this TMP_Text tmp, string text, bool RequireRebuild = false, bool Overflow = false)
        {
            if (tmp == null) return;
            tmp.text = text ?? string.Empty;
            if (Overflow && tmp.overflowMode != TextOverflowModes.Overflow) tmp.overflowMode = TextOverflowModes.Overflow;
            if (RequireRebuild) tmp.ForceMeshUpdate(true, true); // hopefully fixes issue with MelonLoader
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
                if (RequireRebuild) tmp.ForceMeshUpdate(true, true); // hopefully fixes issue with MelonLoader
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

        /// <summary>
        /// Cleans and normalizes a tag string by trimming whitespace, standardizing line endings, removing zero-width
        /// spaces, and converting to lowercase.
        /// </summary>
        /// <param name="t">The tag string to clean and normalize. Can be null.</param>
        /// <returns>A cleaned and normalized version of the tag. Returns an empty string if <paramref name="t"/> is null.</returns>
        static string CleanTag(string t) =>t?.Trim().Replace("\r\n", "\n").Replace("\r", "\n").Replace("\u200B", "").ToLowerInvariant() ?? "";

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
        /// <remarks>If the text contains a recognized animation marker (such as ".LBL." or ".CYLN." for example), the
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
            string pattern = excludesize ? @"<color=[^>]*>|</color>|<b>|</b>|<i>|</i>|<mark=[^>]*>|</mark>|<space=[^>]*>|</space>" : @"<color=[^>]*>|</color>|<b>|</b>|<i>|</i>|<mark=[^>]*>|</mark>|<space=[^>]*>|</space>|<size=[^>]*>|</size>";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.Replace(text, string.Empty);
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
