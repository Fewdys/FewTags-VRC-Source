using System.Text.RegularExpressions;

namespace FewTags
{
    public class TagCleanser
    {
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
                        string LogWarning = $"{vrcPlayer.APIUser?.displayName}'s Tag Size Is Too Large ({sizeVal}{unit}). Defaulted to {fallbackSize}pt";
                        // log warning
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

        public static string CleanseBigPlate(string bigTag, VRC.Player vrcPlayer)
        {
            string result = bigTag;
            result = ReplaceNewlineTokens(result);
            result = FixSize(result, vrcPlayer, 691, 80); // only being called here aswell to check for extra <size=xxx> inside the actual string for the tag
            result = RemoveInvalidSpaceTags(result, vrcPlayer, isBig: true);
            result = CleanAlphaTags(result);
            return result;
        }

        public static string CleansePlate(string tag, VRC.Player vrcPlayer)
        {
            string result = tag;
            result = ReplaceNewlineTokens(result);
            result = FixSize(result, vrcPlayer, 170, 25); // 20 is actualy default text size for vrc nameplate -- im using 25 as if found this means a size was trying to be used
            result = RemoveInvalidSpaceTags(result, vrcPlayer, isBig: false);
            result = CleanAlphaTags(result);
            return result;
        }

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
                    string LogWarning2 = $"{vrcPlayer.APIUser?.displayName} Has A {(isBig ? "Big" : "Regular")} Tag With Spaces Of {spaceVal}. Defaulted To 10";
                    // log warning
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

        private static string ReplaceNewlineTokens(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace("@*n", "\n").Replace("\\n", "\n").Replace("\\\\n", "\n").Replace("@*v", "\v").Replace("\\v", "\v").Replace("\\\\v", "\v");
        }

        private static string[] ReplaceNewlineTokens(string[] input)
        {
            if (input == null)
                return null;

            return input.Select(ReplaceNewlineTokens).ToArray();
        }


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
