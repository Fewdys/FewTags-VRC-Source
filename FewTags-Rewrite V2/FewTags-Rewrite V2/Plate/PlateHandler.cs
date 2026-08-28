using FewTags.FewTags_Rewrite_V2.Util;
using HarmonyLib;
using Nyup_FewTags._Plate;
using UnityEngine;

namespace FewTags.FewTags
{
    internal class PlateHandlers
    {
        internal static bool VerboseLogging = false;
        internal static string s_stringInstance { get; set; }
        internal static PlateStatic? platestatic { get; set; }

        /// <summary>
        /// Creates Tags If Found For The Referenced Player.
        /// </summary>
        internal static void PlateHandler(VRC.Player vrcPlayer)
        {
            
                if (vrcPlayer == null) return;
                var apiuser = vrcPlayer.APIUser;
                if (apiuser == null) return;
                string uid = apiuser.id;
                if (string.IsNullOrEmpty(uid)) return;

                try
                {
                    PlateFunctions.ClearPlatesForPlayer(uid, true);
                }
                catch (Exception fuckingretardederror)
                {
                    LogManager.LogErrorToConsole($"Failed To Clear Plates For UserID: {uid}\nError: {fuckingretardederror}");
                }

                if (!FewTags.FewTagsEnabled) return;
                if (FewTags.s_tags == null) return;

            try
            {
                FewTagsUpdater._tagLookup.TryGetValue(uid, out var founduser);
                bool inExternalLists;
                ///
                /// For Here Just Do Whatever Logic You'd Want, In Other Words Remove SnaxyTags and Abyss and Water As Those Are Just Things I Locally Add
                /// 
                lock (LocalTags.Lock)
                {
                    inExternalLists = LocalTags.LocallyTagged.ContainsKey(uid) || LocalTags.LocallyTaggedByID.ContainsKey(uid);
                }
                if (inExternalLists && founduser == null)
                {
                    founduser = new Jsons.Json.Tags
                    {
                        UserID = uid,
                        Active = true,
                        id = Utils.GetUniqueNegativeId(FewTags.s_tags),
                        Tag = Array.Empty<string>(),
                        PlateBigText = string.Empty,
                        BigTextActive = false,
                        TextActive = true,
                        Malicious = false,
                        Size = string.Empty,
                        PlateText = string.Empty,
                    };
                }
                if (founduser == null) return;
                if (!founduser.Active) return;
                if (FewTags.BlacklistedUserIDs.Contains(founduser.UserID)) return;

                FewTagsUpdater.lastBigPlateText[uid] = founduser.PlateBigText; // set before we change

                var staticPlatesForUser = new List<PlateStatic>();
                var _platestatic = new PlateStatic(vrcPlayer);
                bool isExpanded = _platestatic.IsExpandedInfo;
                staticPlatesForUser.Add(_platestatic);

                s_stringInstance = founduser.Size ?? ""; // set size to be empty if there is no defined size

                var textID = _platestatic.TextID;
                textID?.SetTextSafe("<color=#ffffff>[</color><color=#808080>" + founduser.id + "</color><color=#ffffff>]</color>", true);
                textID?.SetOverlay();
                _platestatic.RefreshBackgroundID();

                var textM = _platestatic.TextM;
                textM?.SetTextSafe(founduser.Malicious ? FewTags.MaliciousStr : FewTags.FewTagsStr, true);
                textM?.SetOverlay();
                // Same reasoning as ID above.
                _platestatic.RefreshBackgroundM();

                var textBP = _platestatic.TextBP;
                var BPTextstr = founduser.PlateBigText;
                var BPTextActive = founduser.BigTextActive;

                if (FewTags.DisableBigPlates) // if we have big plates disabled based on config, disable them
                {
                    textBP?.SetPlateActive(false);
                }
                else
                {
                    textBP?.SetPlateActive(BPTextActive);
                    textBP?.SetOverlay();
                    textBP?.SetColor(Color.white);

                    // check for newlines & length
                    int newlineCount = BPTextstr.Count(c => c == '\n' || c == '\v');
                    bool exceedsNewlines = newlineCount >= FewTags.MaxNewlinesPerPlate;
                    bool exceedsLength = founduser.PlateBigText.Length >= FewTags.MaxTagLength;

                    // if size isn't empty and limit enabled
                    if (!string.IsNullOrEmpty(s_stringInstance) && FewTags.LimitSize)
                        s_stringInstance = TagCleanser.FixSize(s_stringInstance, vrcPlayer, FewTags.MaxPlateSize, FewTags.FallbackSize);

                    BPTextstr = TagCleanser.CleanseBigPlate(BPTextstr, vrcPlayer, FewTags.LimitSize, FewTags.RemoveInvalidSpaceTags, FewTags.RemoveAlphaTags);

                    if (exceedsNewlines || exceedsLength)
                    {
                        if (FewTags.ReplaceInsteadOfSkip) // Replace with error message
                        {
                            if (exceedsNewlines)
                                founduser.PlateBigText = FewTags.TooManyLines;
                            else if (exceedsLength)
                                founduser.PlateBigText = FewTags.TooLargeStr;
                        }
                        else if (FewTags.LimitNewLineOrLength) // skip/hide the plate entirely
                        {
                            textBP?.SetPlateActive(false);
                        }

                        textBP?.SetTextSafe(s_stringInstance + BPTextstr);
                    }
                    else // valid or allowed through aids
                    {
                        textBP?.SetTextSafe(s_stringInstance + BPTextstr); // set text
                    }

                    if (Utils.NeedsAnimator(BPTextstr, out var applyAnim))
                    {
                        try
                        {
                            TagAnimator tagAnimator = Utils.GetOrAddAnimator(_platestatic, true);
                            if (tagAnimator != null)
                            {
                                tagAnimator.originalText = Utils.ReplaceAniNames(Utils.GetText(textBP));
                                applyAnim?.Invoke(tagAnimator);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogErrorToConsole($"Failed To Add Animator To Big Plate: {ex.Message}");
                        }
                    }
                    else if (!FewTags.EnableAnimations)
                    {
                        textBP?.SetTextSafe(Utils.ReplaceAniNames(Utils.GetText(textBP)), true);
                    }
                }

                if (founduser.Tag == null)
                    founduser.Tag = Array.Empty<string>();

                var currentTags = new List<string>();
                bool hasLocalTags = false;
                List<string> labelsCopy = null;
                ///
                /// Here Is Where You Want To Do Other Checking Of Any Other Tags You Want To Appear Before FewTags (However As Part Of FewTags)
                ///
                lock (LocalTags.Lock)
                {
                    if (LocalTags.LocallyTaggedByID.TryGetValue(uid, out var labels)) // LOCAL TAGS !!
                    {
                        labelsCopy = new List<string>(labels);
                        hasLocalTags = true;
                    }
                }
                ///
                /// End
                ///
                if (labelsCopy != null)
                {
                    for (int i = 0; i < labelsCopy.Count; i++)
                    {
                        var tag = labelsCopy[i];
                        if (string.IsNullOrEmpty(tag)) continue;
                        currentTags.Add(Utils.AddLocalTagPrefix(tag));
                    }
                }

                if (founduser.Tag != null) currentTags.AddRange(founduser.Tag);

                currentTags = currentTags
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                FewTagsUpdater.lastAppliedTags[uid] = currentTags.ToArray(); // set before me modify

                if (currentTags.Count == 0 && !hasLocalTags)
                    return;

                bool hasBigPlate = !FewTags.DisableBigPlates && founduser.BigTextActive;
                int tagCount = Math.Min(currentTags.Count, FewTags.MaxPlatesPerUser);

                // Baseline Y anchor — ExpandedInfo gets its own tunable set so it can be
                // centered and spaced above/below independently of the Quick Stats layout.
                float baseY = FewTags.PositionTags;

                if (VRC.Player.prop_Player_0 != null && VRC.Player.prop_Player_0 == vrcPlayer && !FewTags.UnderNameplate)
                    baseY = FewTags.PositionSelf;
                else if (apiuser != null && apiuser.isFriend && !FewTags.UnderNameplate)
                    baseY = FewTags.PositionFriend;
                else if (!FewTags.UnderNameplate)
                    baseY = FewTags.PositionOther;

                float spacing = isExpanded ? FewTags.SpacingExpanded : 28f;
                const float bigPlateOffset = 500f;

                var movingElements = new List<Transform>();

                if (!FewTags.UnderNameplate)
                {
                    // Use the movable (pill-aware) transform so ExpandedInfo actually shifts
                    // the visible pill instead of an invisible/mis-anchored root object.
                    if (hasBigPlate && _platestatic._gameObjectBP != null)
                        movingElements.Add(_platestatic._gameObjectBP.transform);
                    if (_platestatic._gameObjectID != null)
                        movingElements.Add(_platestatic._gameObjectID.transform);
                    if (_platestatic._gameObjectM != null)
                        movingElements.Add(_platestatic._gameObjectM.transform);

                    if (hasBigPlate)
                        _platestatic?.UpdatePosition(baseY + bigPlateOffset, false, true, false);

                    _platestatic?.UpdatePosition(baseY + spacing, false, false, true);
                    _platestatic?.UpdatePosition(baseY, true, false, false);
                }

                var platesForUser = new List<Plate>(tagCount); // pre-sized

                for (int i = 0; i < tagCount; i++)
                {
                    var tag = currentTags[i];
                    if (string.IsNullOrEmpty(tag)) continue;

                    if ((tag.Contains("Known Ripper/Reuploader") || tag == "Known Ripper/Reuploader") && FewTags.BeepOnReuploaderDetected)
                        ConsoleUtils.AmongUsBeep(); // feel free to change or add you're own logic

                    var RemovedHTML = Utils.RemoveHtmlTags(tag);

                    if (FewTags.NoHTMLForMain)
                        tag = RemovedHTML;

                    if (!FewTags.EnableAnimations)
                        tag = Utils.ReplaceAniNames(tag);

                    // check for newlines & length
                    int newlineCount = tag.Count(c => c == '\n' || c == '\v');
                    bool isTooLong = tag.Length >= FewTags.MaxTagLength;
                    bool hasTooManyLines = newlineCount >= FewTags.MaxNewlinesPerPlate;

                    tag = TagCleanser.CleansePlate(tag, vrcPlayer, FewTags.LimitSize, FewTags.RemoveInvalidSpaceTags, FewTags.RemoveAlphaTags);

                    if (FewTags.ReplaceInsteadOfSkip) // replace if needed
                    {
                        if (isTooLong) tag = FewTags.TooLargeStr;
                        if (hasTooManyLines) tag = FewTags.TooManyLines;
                    }
                    else if (FewTags.LimitNewLineOrLength)
                    {
                        if (isTooLong || hasTooManyLines) continue; // skip tag -- disable
                    }

                    if (!FewTags.UnderNameplate)
                    {
                        foreach (var t in movingElements)
                            t.localPosition += new Vector3(0f, spacing, 0f);
                    }

                    bool needsAnim = Utils.NeedsAnimator(tag, out var applyAnim);
                    string displayTag = needsAnim ? Utils.ReplaceAniNames(tag) : tag;
                    bool isScroll = needsAnim && tag.ToLower().StartsWith(".scroll.");

                    float plateY = isExpanded && FewTags.UnderNameplate ? baseY - (i * spacing) - 84f : FewTags.UnderNameplate ? baseY - (i * spacing) : baseY;
                    Plate plate = new Plate(vrcPlayer, plateY, displayTag);
                    var plateText = plate.Text;
                    plateText?.SetTextSafe(displayTag, true);
                    plateText?.SetOverlay();
                    plateText?.SetColor(Color.white, true);

                    if (isScroll && plate.Extender != null && plate.Text != null)
                    {
                        string sample = new string('A', TagAnimator.ScrollMaxWindowChars - 10);
                        Vector2 preferred = plate.Extender.GetPreferredValues(plate.Text, sample, 0f, 0f);

                        plate.Extender.extendWidth = false;
                        var fixedSize = plate.Extender.Clone.sizeDelta;
                        fixedSize.x = preferred.x + plate.Extender.paddingLeft + plate.Extender.paddingRight;
                        plate.Extender.Clone.sizeDelta = fixedSize;
                    }
                    else
                    {
                        plate?.RefreshBackground();
                    }

                    if (needsAnim)
                    {
                        plate.Animator = Utils.GetOrAddAnimator(plate);
                        if (plate.Animator != null)
                        {
                            // needs the marker present to know which effect + do its own stripping
                            plate.Animator.originalText = tag;
                            applyAnim?.Invoke(plate.Animator);
                        }
                    }

                    if (plate._gameObject == null)
                    {
                        LogManager.LogErrorToConsole($"Plate gameObject is null for tag: {tag}, skipping.");
                        continue;
                    }

                    platesForUser.Add(plate);
                    movingElements.Add(plate.MovableTransform ?? plate._gameObject.transform);
                }

                FewTags.playerPlates[uid] = platesForUser;
                FewTags.playerStaticPlates[uid] = staticPlatesForUser;

                lock (FewTags.Lock)
                {
                    if (!FewTags.p.Contains(vrcPlayer))
                        FewTags.p.Add(vrcPlayer);
                }

                PlateFunctions.NameplateESP(vrcPlayer);

                if (!FewTags.UnderNameplate)
                {
                    foreach (var plate in movingElements)
                    {
                        var detect = plate.gameObject.AddComponent<MenuDetector>();
                        detect.player = vrcPlayer;
                        detect.IsExtendedInfo = isExpanded;
                        detect.isFriend = /* however you check if user is a friend*/;
                    }
                }
            }
            catch (Exception ex)
            {
                string safeUid = "unknown";
                try { safeUid = uid ?? "unknown"; } catch { }
                LogManager.LogErrorToConsole($"Error Handling Plates For UserID: {safeUid}\nError: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }
    }
}
