using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FewTags.FewTags
{
    public class TagCleanser
    {
        /// <summary>
        /// Normalizes <size> tags in the input string to ensure font sizes do not exceed the specified maximum,
        /// replacing oversized values with a fallback size or removing invalid sizes.
        /// </summary>
        /// <remarks>If a <size> tag specifies a size less than or equal to zero, the tag and its contents
        /// are removed. If a tag specifies a size greater than or equal to the maximum, it is replaced with the
        /// fallback size. The method supports multiple units (pt, px, em, rem, %), converting them to points for
        /// comparison. A warning is logged if an oversized tag is encountered.</remarks>
        /// <param name="input">The input string containing <size> tags to be validated and adjusted.</param>
        /// <param name="vrcPlayer">The VRC.Player instance associated with the operation, used for logging purposes when an oversized tag is
        /// encountered.</param>
        /// <param name="maxSize">The maximum allowed font size, in points. Any <size> tag specifying a size equal to or greater than this
        /// value will be replaced with the fallback size.</param>
        /// <param name="fallbackSize">The font size, in points, to use as a replacement when an oversized <size> tag is detected.</param>
        /// <returns>A string with all <size> tags adjusted so that font sizes do not exceed the specified maximum. Oversized or
        /// invalid sizes are replaced or removed as appropriate.</returns>
        public static string FixSize(string input, VRC.Player vrcPlayer, double maxSize, double fallbackSize)
        {
            string result = Regex.Replace(input, @"<size=([-]?\d+(\.\d+)?)(px|em|pt|%|rem)?>", match =>
            {
                string replacement;

                string fullMatch = match.Value;
                string valueStr = match.Groups[1].Value;
                string unit = match.Groups[3].Success ? match.Groups[3].Value.ToLower() : "pt";

                if (!double.TryParse(valueStr, out double sizeVal))
                {
                    replacement = fullMatch;
                }
                else
                {
                    double ptSize = sizeVal;
                    switch (unit)
                    {
                        case "px":
                            ptSize = sizeVal / 96 * 72;
                            break;
                        case "em":
                        case "rem":
                            ptSize = sizeVal * 20;
                            break;
                        case "%":
                            ptSize = sizeVal * 20 / 100;
                            break;
                        case "pt":
                        default:
                            break;
                    }

                    if (ptSize <= 0)
                    {
                        var endTag = "</size>";
                        if (input.Contains(endTag))
                        {
                            int start = match.Index + match.Length;
                            int end = input.IndexOf(endTag, start);
                            if (end > start)
                            {
                                string inner = input.Substring(start, end - start);
                                replacement = inner;
                            }
                            else
                            {
                                replacement = "";
                            }
                        }
                        else
                        {
                            replacement = "";
                        }
                    }
                    else if (ptSize >= maxSize)
                    {
                        LogManager.LogWarningToConsole($"{vrcPlayer.APIUser?.displayName}'s Tag Size Is Too Large ({sizeVal}{unit}). Defaulted to {fallbackSize}pt");
                        replacement = $"<size={fallbackSize}>";
                    }
                    else
                    {
                        replacement = fullMatch;
                    }
                }

                return replacement;
            }, RegexOptions.IgnoreCase);

            return result;
        }

        /// <summary>
        /// Cleans and normalizes a large tag string for display, applying formatting and validation rules specific to
        /// big plates.
        /// </summary>
        /// <remarks>This method applies several transformations to ensure the tag string is properly
        /// formatted for big plate display, including newline normalization, size adjustments, and removal of invalid
        /// formatting tags. The formatting may depend on the provided player context.</remarks>
        /// <param name="bigTag">The tag string to be cleansed and formatted.</param>
        /// <param name="vrcPlayer">The player context used to determine formatting constraints and validation rules.</param>
        /// <returns>A cleansed and formatted string suitable for use as a big plate tag.</returns>
        public static string CleanseBigPlate(string bigTag, VRC.Player vrcPlayer, bool _FixSize, bool _RemoveInvalidSpaceTags, bool _RemoveAlphaTags)
        {
            string result = bigTag;
            result = ReplaceNewlineTokens(result); // always replace newlines
            if (_FixSize) result = FixSize(result, vrcPlayer, FewTags.MaxPlateSize, FewTags.FallbackSize); // only being called here aswell to check for extra <size=xxx> inside the actual string for the tag
            if (_RemoveInvalidSpaceTags) result = RemoveInvalidSpaceTags(result, vrcPlayer, isBig: true);
            if (_RemoveAlphaTags) result = CleanAlphaTags(result);
            return result;
        }

        /// <summary>
        /// Cleans and normalizes a user nameplate string for display in VRChat, applying formatting and validation
        /// rules.
        /// </summary>
        /// <remarks>This method applies several transformations to ensure the nameplate string is valid
        /// for display, including replacing newline tokens, adjusting text size, removing invalid space tags, and
        /// cleaning alpha tags. The resulting string is intended to comply with VRChat's nameplate formatting
        /// requirements.</remarks>
        /// <param name="tag">The original nameplate string to be cleansed and formatted.</param>
        /// <param name="vrcPlayer">The VRChat player whose nameplate is being processed. Used to determine formatting and validation context.</param>
        /// <returns>A cleansed and formatted string suitable for use as a VRChat nameplate.</returns>
        public static string CleansePlate(string tag, VRC.Player vrcPlayer, bool _FixSize, bool _RemoveInvalidSpaceTags, bool _RemoveAlphaTags)
        {
            string result = tag;
            result = ReplaceNewlineTokens(result); // always replace newlines
            if (_FixSize)
            {
                int maxSize = Math.Max(1, (int)Math.Ceiling(FewTags.MaxPlateSize / 2.0));
                result = FixSize(result, vrcPlayer, maxSize, 20); // 20 is actualy default text size for vrc nameplate
            }
            if (_RemoveInvalidSpaceTags) result = RemoveInvalidSpaceTags(result, vrcPlayer, isBig: false);
            if (_RemoveAlphaTags) result = CleanAlphaTags(result);
            return result;
        }

        /// <summary>
        /// Removes invalid or out-of-range <space> tags from the input string and normalizes large space values to a
        /// default value.
        /// </summary>
        /// <remarks>This method removes <space> tags at the start or end of the input string and replaces
        /// any <space> tag with a value of 101 or greater with <space=10>. If a large space value is normalized, a
        /// warning is logged using the provided VRC.Player information.</remarks>
        /// <param name="input">The input string potentially containing <space> tags to be validated and cleaned.</param>
        /// <param name="vrcPlayer">The VRC.Player instance associated with the operation, used for logging purposes when large space values are
        /// encountered.</param>
        /// <param name="isBig">A value indicating whether the tag is considered 'big', which affects the log message when normalizing large
        /// space values.</param>
        /// <returns>A string with invalid or out-of-range <space> tags removed or replaced with a default value.</returns>
        private static string RemoveInvalidSpaceTags(string input, VRC.Player vrcPlayer, bool isBig)
        {
            string result = input;
            result = Regex.Replace(result, @"^(<space=-?\d+>)", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(<space=-?\d+>)$", "", RegexOptions.IgnoreCase);

            result = Regex.Replace(result, @"<space=(\d+)>", match =>
            {
                string replacement;
                if (ushort.TryParse(match.Groups[1].Value, out ushort spaceVal) && spaceVal >= 101)
                {
                    LogManager.LogWarningToConsole($"{vrcPlayer.APIUser?.displayName} Has A {(isBig ? "Big" : "Regular")} Tag With Spaces Of {spaceVal}. Defaulted To 10");
                    replacement = "<space=10>";
                }
                else
                {
                    replacement = match.Value;
                }
                return replacement;
            }, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return result;
        }

        /// <summary>
        /// Replaces recognized newline and vertical tab token sequences in the specified string with their
        /// corresponding control characters.
        /// </summary>
        /// <remarks>This method recognizes both literal and escaped forms of newline and vertical tab
        /// tokens. It is useful for processing text that uses tokenized representations of control
        /// characters.</remarks>
        /// <param name="input">The input string in which to replace newline ("@*n", "\n", "\\n") and vertical tab ("@*v", "\v", "\\v")
        /// tokens.</param>
        /// <returns>A new string with all recognized newline and vertical tab tokens replaced by their respective control
        /// characters. Returns the original string if it is null or empty.</returns>
        private static string ReplaceNewlineTokens(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace("@*n", "\n").Replace("\\n", "\n").Replace("\\\\n", "\n").Replace("@*v", "\v").Replace("\\v", "\v").Replace("\\\\v", "\v");
        }

        /// <summary>
        /// Replaces newline tokens in each string of the specified array with actual newline characters.
        /// </summary>
        /// <param name="input">An array of strings in which to replace newline tokens. Can be null.</param>
        /// <returns>A new array of strings with newline tokens replaced by newline characters, or null if the input array is
        private static string[] ReplaceNewlineTokens(string[] input)
        {
            if (input == null)
                return null;

            return input.Select(ReplaceNewlineTokens).ToArray();
        }

        /// <summary>
        /// Removes <alpha> tags from the input string when the specified alpha value indicates full transparency.
        /// </summary>
        /// <remarks>An <alpha> tag is considered fully transparent if its hexadecimal value ends with
        /// '00'. Only such tags are removed; all others remain unchanged. The method performs a case-insensitive search
        /// for <alpha> tags.</remarks>
        /// <param name="input">The input string that may contain <alpha> tags with hexadecimal alpha values.</param>
        /// <returns>A string with <alpha> tags removed if their alpha value is fully transparent; otherwise, the original tags
        /// are preserved.</returns>
        private static string CleanAlphaTags(string input)
        {
            string result = Regex.Replace(input, @"<alpha=(#([0-9a-fA-F]+))>.*?</alpha>", match =>
            {
                string alphaValue = match.Groups[1].Value;

                if (alphaValue.StartsWith("#"))
                {
                    string hex = alphaValue.Substring(1); // get the hex without the #

                    if (hex.Length >= 2) // check if the length of the hex string is at least 2 otherwise we don't care bc it's invalid
                    {
                        string alphaHex = hex.Length >= 2 ? hex.Substring(hex.Length - 2) : hex; // get the last 2 characters aka alpha or transparency

                        if (alphaHex == "00")
                        {
                            return "";
                        }
                    }
                }

                return match.Value;
            }, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return result;
        }

    }
}
