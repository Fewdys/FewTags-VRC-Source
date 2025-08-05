using System.Net;
using System.Text.RegularExpressions;
using FewTags.JSON;
using FewTags.TagStuff;
using FewTags.Utils;
using UnityEngine;

namespace FewTags
{
    public class Main
    {
        internal const string url = "https://raw.githubusercontent.com/Fewdys/FewTags/main/FewTags.json";
        internal static int MaxPlatesPerUser, MaxNewlinesPerPlate, MaxPlateSize, FallbackSize, MaxTagLength, UpdateIntervalMinutes;
        internal static HashSet<string> BlacklistedUserIDs = new HashSet<string>();
        internal static bool CleanseTags, EnableAnimations, BeepOnReuploaderDetected, ReplaceInsteadOfSkip, isOverlay, DisableBigPlates, NoHTMLForMain;
        internal const float Position = -103.95f, PositionTags = -131.95f, PositionID = -75.95f, PositionBigText = 344.75f;
        internal static List<VRC.Player> p = new List<VRC.Player>();

        internal static Json._Tags s_tags { get; set; }
        internal static string s_rawTags { get; set; }
        internal static bool SnaxyTagsLoaded { get; set; } // perhaps still around and yea :3
        internal static string s_stringInstance { get; set; }
        internal static PlateStatic? platestatic { get; set; }

        internal const string
            MaliciousStr = "<color=#ff0000>Malicious User</color>",
            FewTagsStr = "<b><color=#ff0000>-</color> <color=#ff7f00>F</color><color=#ffff00>e</color><color=#80ff00>w</color><color=#00ff00>T</color><color=#00ff80>a</color><color=#00ffff>g</color><color=#0000ff>s</color> <color=#8b00ff>-</color><color=#ffffff></b>",
            TooLargeStr = "Plate Length To Long!",
            TooManyLines = "<color=#ff0000>Plate Contains Too Many Newlines!</color>";

        internal static void UpdateTags()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    s_rawTags = wc.DownloadString(url);

                    if (string.IsNullOrEmpty(s_rawTags))
                    {
                        return;
                    }

                    JSONNode jsonNode = JSON.JSON.Parse(s_rawTags);
                    if (jsonNode == null)
                    {
                        return;
                    }

                    s_tags = new Json._Tags { records = new List<Json.Tags>() };

                    foreach (JSONNode record in jsonNode["records"].AsArray)
                    {
                        List<string> tagsList = new List<string>();
                        foreach (JSONNode tag in record["Tag"].AsArray)
                        {
                            tagsList.Add(tag.Value);
                        }

                        Json.Tags tagEntry = new Json.Tags
                        {
                            id = record["id"].AsInt,
                            UserID = record["UserID"],
                            PlateText = record["PlateText"],
                            PlateBigText = record["PlateBigText"],
                            Malicious = record["Malicious"].AsBool,
                            Active = record["Active"].AsBool,
                            TextActive = record["TextActive"].AsBool,
                            BigTextActive = record["BigTextActive"].AsBool,
                            Size = record["Size"],
                            Tag = tagsList.ToArray()
                        };

                        s_tags.records.Add(tagEntry);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                // log exceptions here
            }
        }

        internal static void NameplateESP(VRC.Player player)
        {
            if (player._vrcplayer?.field_Public_PlayerNameplate_0?.field_Public_GameObject_5 != null)
            {
                player._vrcplayer.field_Public_PlayerNameplate_0.field_Public_TextMeshProUGUIEx_4.isOverlay = isOverlay;
            }
        }

        internal static string RemoveHtmlTags(string text, bool excludesize = false)
        {
            string pattern = excludesize ? @"<color=[^>]*>|</color>|<b>|</b>|<i>|</i>|<mark=[^>]*>|</mark>|<space=[^>]*>|</space>" : @"<color=[^>]*>|</color>|<b>|</b>|<i>|</i>|<mark=[^>]*>|</mark>|<space=[^>]*>|</space>|<size=[^>]*>|</size>";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.Replace(text, string.Empty);
        }

        internal static void PlateHandler(VRC.Player vrcPlayer)
        {
            try
            {
                if (vrcPlayer == null) return;
                if (s_tags == null) return;
                var apiuser = vrcPlayer.APIUser;
                if (apiuser == null) return;
                string uid = apiuser.id;
                if (uid == null) return;
                var founduser = s_tags.records.FirstOrDefault(x => x.UserID == uid);
                if (founduser == null) return;
                if (!founduser.Active) return;
                if (BlacklistedUserIDs.Contains(founduser.UserID)) return;

                platestatic = new PlateStatic(vrcPlayer);

                s_stringInstance = founduser.Size ?? ""; // set size to be empty if there is no defined size

                platestatic.TextID.text = "<color=#ffffff>[</color><color=#808080>" + founduser.id + "</color><color=#ffffff>]</color>"; // id
                platestatic.TextID.isOverlay = isOverlay;

                platestatic.TextM.text = founduser.Malicious ? MaliciousStr : FewTagsStr;
                platestatic.TextM.isOverlay = isOverlay;

                if (DisableBigPlates) // if we have big plates disabled based on config, disable them
                {
                    platestatic.TextBP.gameObject.SetActive(false);
                    platestatic.TextBP.enabled = false;
                }
                else
                {
                    platestatic.TextBP.enabled = founduser.BigTextActive; // enable or disable plate text based on our bool
                    platestatic.TextBP.gameObject.SetActive(founduser.BigTextActive); // enable or disable plate object based on our bool
                    platestatic.TextBP.isOverlay = isOverlay;

                    if (CleanseTags) // this bool was originally inverted i forget specifically why but i added a config so yea
                    {
                        // if size isn't empty
                        if (!string.IsNullOrEmpty(s_stringInstance))
                        {
                            s_stringInstance = TagCleanser.FixSize(s_stringInstance, vrcPlayer, MaxPlateSize, FallbackSize); // ensure we are in set size limit, if past it use fallback size
                        }

                        founduser.PlateBigText = TagCleanser.CleanseBigPlate(founduser.PlateBigText, vrcPlayer);
                    }

                    // check for newlines & length
                    int newlineCount = founduser.PlateBigText.Count(c => c == '\n' || c == '\v');
                    bool exceedsNewlines = newlineCount >= MaxNewlinesPerPlate;
                    bool exceedsLength = founduser.PlateBigText.Length >= MaxTagLength;

                    if (exceedsNewlines || exceedsLength)
                    {
                        if (ReplaceInsteadOfSkip)
                        {
                            if (exceedsNewlines)
                                founduser.PlateBigText = TooManyLines;
                            else if (exceedsLength)
                                founduser.PlateBigText = TooLargeStr;

                            platestatic.TextBP.text = s_stringInstance + founduser.PlateBigText;
                        }
                        else // skip/hide the plate entirely
                        {
                            platestatic.TextBP.gameObject.SetActive(false);
                            platestatic.TextBP.enabled = false;
                        }
                    }
                    else // valid or allowed through aids
                    {
                        platestatic.TextBP.text = s_stringInstance + founduser.PlateBigText;
                    }

                    string lowerTag = founduser.PlateBigText.ToLower();
                    bool needsAnimator = Main.EnableAnimations &&
                    (
                        lowerTag.StartsWith(".lbl.") ||
                        lowerTag.StartsWith(".cyln.") ||
                        lowerTag.StartsWith(".rain.") ||
                        lowerTag.StartsWith(".sr.") ||
                        lowerTag.StartsWith(".pulse.") ||
                        lowerTag.StartsWith(".jump.") ||
                        lowerTag.StartsWith(".shake.") ||
                        lowerTag.StartsWith(".gt.") ||
                        lowerTag.StartsWith(".blink.") ||
                        lowerTag.StartsWith(".glitch.")
                    );
        
                    if (needsAnimator)
                    {
                        try
                        {
                            TagAnimator animtor = platestatic._gameObjectBP.AddComponent<TagAnimator>();
                            animtor.originalText = platestatic.TextBP.text;
                            if (lowerTag.StartsWith(".lbl.")) animtor.LetterByLetter = true;
                            else if (lowerTag.StartsWith(".cyln.")) animtor.Bounce = true;
                            else if (lowerTag.StartsWith(".rain.")) animtor.Rainbow = true;
                            else if (lowerTag.StartsWith(".sr.")) animtor.SmoothRainbow = true;
                            else if (lowerTag.StartsWith(".pulse.")) animtor.Pulse = true;
                            else if (lowerTag.StartsWith(".jump.")) animtor.Jump = true;
                            else if (lowerTag.StartsWith(".shake.")) animtor.Shake = true;
                            else if (lowerTag.StartsWith(".gt.")) animtor.GhostTrail = true;
                            else if (lowerTag.StartsWith(".blink.")) animtor.Blink = true;
                            else if (lowerTag.StartsWith(".glitch.")) animtor.Glitch = true;
                        }
                        catch (Exception ex)
                        {
                            string msg = $"Failed To Add Animator To Big Plate: {ex.Message}";
                            // log msg here
                        }
                    }
                }

                if (founduser.Tag.Length == 0) return;

                Plate[] plates = new Plate[Math.Min(founduser.Tag.Length, MaxPlatesPerUser)];

                for (int i = 0; i < plates.Length; i++)
                {
                    var tag = founduser.Tag[i];
                    if ((tag.Contains("Known Ripper/Reuploader") || tag == "Known Ripper/Reuploader") && BeepOnReuploaderDetected)
                        ConsoleUtils.AmongUsBeep(); // feel free to comment out if you don't use / have a console -- off by default in config

                    //var RemovedHTML = RemoveHtmlTags(tag);

                    if (string.IsNullOrEmpty(tag)) continue;

                    if (NoHTMLForMain)
                    {
                        tag = RemoveHtmlTags(tag);
                    }

                    if (CleanseTags)
                    {
                        tag = TagCleanser.CleansePlate(tag, vrcPlayer);
                    }

                    // check for newlines & length
                    int newlineCount = tag.Count(c => c == '\n' || c == '\v');
                    bool isTooLong = tag.Length >= MaxTagLength;
                    bool hasTooManyLines = newlineCount >= MaxNewlinesPerPlate;

                    if (ReplaceInsteadOfSkip) // replace if needed
                    {
                        if (isTooLong) tag = TooLargeStr;
                        if (hasTooManyLines) tag = TooManyLines;
                    }
                    else
                    {
                        if (isTooLong || hasTooManyLines) continue; // skip tag -- disable
                    }

                    if (plates[i] == null)  // only instantiate if necessary -- not already created
                    {
                        plates[i] = new Plate(vrcPlayer, PositionTags - (i * 28f), tag);

                        plates[i].Text.text = tag;
                        plates[i].Text.enabled = true;
                        plates[i].Text.gameObject.SetActive(founduser.TextActive);
                        plates[i].Text.isOverlay = isOverlay;
                        plates[i].Text.color = Color.white;
                    }
                    else // ensure tag is set if plate exists for each
                    {
                        plates[i].Text.text = tag;
                        plates[i].Text.enabled = true;
                        plates[i].Text.gameObject.SetActive(founduser.TextActive);
                        plates[i].Text.isOverlay = isOverlay;
                        plates[i].Text.color = Color.white;
                    }
                }

            }
            catch (Exception ex)
            {
                string Msg = "Error Handling Plates For UserID:" + vrcPlayer.APIUser.id + "\nError: " + ex.Message + "\nException StackTrace: " + ex.StackTrace + "\nException Data: " + ex.Data;
                // log error using msg
            }
        }
    }
}





