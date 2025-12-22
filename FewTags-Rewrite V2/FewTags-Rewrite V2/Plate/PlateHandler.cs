using UnityEngine;

namespace FewTags.FewTags
{
    internal class PlateHandlers
    {
        internal static string s_stringInstance { get; set; }
        internal static PlateStatic? platestatic { get; set; }

        /// <summary>
        /// Creates Tags If Found For The Referenced Player.
        /// </summary>
        internal static void PlateHandler(VRC.Player vrcPlayer)
        {
            try
            {
                if (!FewTags.FewTagsEnabled) return;
                if (vrcPlayer == null) return;
                if (FewTags.s_tags == null) return;
                var apiuser = vrcPlayer.APIUser;
                if (apiuser == null) return;
                string uid = apiuser.id;
                if (string.IsNullOrEmpty(uid)) return;
                PlateFunctions.ClearPlatesForPlayer(uid);
                var founduser = FewTags.s_tags?.records.FirstOrDefault(x => x.UserID == uid);
                ///
                /// For Here Just Do Whatever Logic You'd Want, In Other Words Remove SnaxyTags and Abyss and Water As Those Are Just Things I Locally Add
                /// 
                bool inExternalLists = LocalTags.LocallyTagged.ContainsKey(uid) || LocalTags.LocallyTaggedByID.ContainsKey(uid);
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
                staticPlatesForUser.Add(_platestatic);

                s_stringInstance = founduser.Size ?? ""; // set size to be empty if there is no defined size

                var textID = _platestatic.TextID;

                textID.SetTextSafe("<color=#ffffff>[</color><color=#808080>" + founduser.id + "</color><color=#ffffff>]</color>"); // id
                textID.SetOverlay();

                var textM = _platestatic.TextM;

                textM.SetTextSafe(founduser.Malicious ? FewTags.MaliciousStr : FewTags.FewTagsStr);
                textM.SetOverlay();

                var textBP = _platestatic.TextBP;
                var BPTextstr = founduser.PlateBigText;
                var BPTextActive = founduser.BigTextActive;

                if (FewTags.DisableBigPlates) // if we have big plates disabled based on config, disable them
                {
                    textBP.gameObject.SetActive(false);
                    textBP.enabled = false;
                }
                else
                {
                    textBP.enabled = BPTextActive; // enable or disable plate text based on our bool
                    textBP.gameObject.SetActive(BPTextActive); // enable or disable plate object based on our bool
                    textBP.SetOverlay();

                    if (FewTags.CleanseTags)
                    {
                        // if size isn't empty
                        if (!string.IsNullOrEmpty(s_stringInstance))
                        {
                            s_stringInstance = TagCleanser.FixSize(s_stringInstance, vrcPlayer, FewTags.MaxPlateSize, FewTags.FallbackSize); // ensure we are in set size limit, if past it use fallback size
                        }

                        BPTextstr = TagCleanser.CleanseBigPlate(BPTextstr, vrcPlayer);
                    }

                    // check for newlines & length
                    int newlineCount = BPTextstr.Count(c => c == '\n' || c == '\v');
                    bool exceedsNewlines = newlineCount >= FewTags.MaxNewlinesPerPlate;
                    bool exceedsLength = founduser.PlateBigText.Length >= FewTags.MaxTagLength;

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
                            textBP.gameObject.SetActive(false);
                            textBP.enabled = false;
                        }

                        textBP.SetTextSafe(s_stringInstance + BPTextstr);
                    }
                    else // valid or allowed through aids
                    {
                        textBP.SetTextSafe(s_stringInstance + BPTextstr); // set text
                    }

                    if (Utils.NeedsAnimator(BPTextstr, out var applyAnim))
                    {
                        try
                        {
                            var animator = _platestatic._gameObjectBP.GetComponent<TagAnimator>() ?? _platestatic._gameObjectBP.AddComponent<TagAnimator>();
                            animator.originalText = Utils.ReplaceAniNames(textBP.text);
                            applyAnim?.Invoke(animator);
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogErrorToConsole($"Failed To Add Animator To Big Plate: {ex.Message}");
                        }
                    }
                    else if (!FewTags.EnableAnimations)
                    {
                        textBP.SetTextSafe(Utils.ReplaceAniNames(textBP.text));
                    }
                }

                if (founduser.Tag == null)
                    founduser.Tag = Array.Empty<string>();

                var currentTags = new List<string>();
                bool hasLocalTags = false;
                ///
                /// Here Is Where You Want To Do Other Checking Of Any Other Tags You Want To Appear Before FewTags (However As Part Of FewTags)
                ///
                if (LocalTags.LocallyTaggedByID.TryGetValue(uid, out var labels)) // LOCAL TAGS !!
                {
                    for (int i = 0; i < labels.Count; i++)
                    {
                        var tag = labels[i];
                        if (string.IsNullOrEmpty(tag)) continue;
                        currentTags.Add(Utils.AddLocalTagPrefix(tag));
                    }
                    hasLocalTags = true;
                }
                ///
                /// End
                ///
                if (founduser.Tag != null) currentTags.AddRange(founduser.Tag);

                currentTags = currentTags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                founduser.Tag = currentTags.ToArray();

                FewTagsUpdater.lastAppliedTags[uid] = currentTags.ToArray(); // set before me modify

                if (founduser.Tag.Length == 0 && !hasLocalTags)
                    return;

                bool hasBigPlate = !FewTags.DisableBigPlates && founduser.BigTextActive;
                int tagCount = Math.Min(currentTags.Count, FewTags.MaxPlatesPerUser);
                float baseY = FewTags.PositionTags;
                if (VRC.Player.prop_Player_0 != null && VRC.Player.prop_Player_0 == vrcPlayer && !FewTags.UnderNameplate)
                {
                    baseY = 196.05f; // Add + 28 if needed -- 196 was correct?... whoops, double commit
                }
                ///
                /// ONLY NEEDED IF YOU DO SOME SPECIAL NAMEPLATE THAT IS SELF / FRIEND RELATED
                /// 
                /*else if (vrcPlayer.APIUser != null && vrcPlayer.APIUser.isFriend && !FewTags.UnderNameplate) // or whatever other friend checking logic
                    baseY = 147.05f;*/
                ///
                /// END
                /// 
                else if (!FewTags.UnderNameplate)
                    baseY = 119.05f; // default pos above nameplates feel free to add 28 as many times as needed for however many custom plates you have and or modify MenuUpdator to handle it
                const float spacing = 28f;
                const float bigPlateOffset = 500f;

                var movingElements = new List<Transform>();

                if (!FewTags.UnderNameplate)
                {
                    if (hasBigPlate) movingElements.Add(_platestatic._gameObjectBP.transform);
                    movingElements.Add(_platestatic._gameObjectID.transform);
                    movingElements.Add(_platestatic._gameObjectM.transform);

                    if (hasBigPlate)
                        _platestatic.UpdatePosition(baseY + bigPlateOffset, false, true, false);

                    _platestatic.UpdatePosition(baseY + spacing, false, false, true);
                    _platestatic.UpdatePosition(baseY, true, false, false);
                }

                var platesForUser = new List<Plate>();

                for (int i = 0; i < tagCount; i++)
                {
                    var tag = currentTags[i];
                    if ((tag.Contains("Known Ripper/Reuploader") || tag == "Known Ripper/Reuploader") && FewTags.BeepOnReuploaderDetected)
                        Utils.AmongUsBeepAlt();

                    var RemovedHTML = Utils.RemoveHtmlTags(tag);

                    if (string.IsNullOrEmpty(tag)) continue;

                    if (FewTags.NoHTMLForMain)
                    {
                        tag = RemovedHTML;
                    }

                    if (FewTags.CleanseTags)
                    {
                        tag = TagCleanser.CleansePlate(tag, vrcPlayer);
                    }

                    if (!FewTags.EnableAnimations)
                    {
                        tag = Utils.ReplaceAniNames(tag);
                    }

                    // check for newlines & length
                    int newlineCount = tag.Count(c => c == '\n' || c == '\v');
                    bool isTooLong = tag.Length >= FewTags.MaxTagLength;
                    bool hasTooManyLines = newlineCount >= FewTags.MaxNewlinesPerPlate;

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

                    float plateY = FewTags.UnderNameplate ? baseY - (i * spacing) : baseY;
                    Plate plate = new Plate(vrcPlayer, plateY, tag);
                    var plateText = plate.Text;
                    plateText?.SetTextSafe(tag);
                    plateText?.SetOverlay();
                    plateText?.SetColor(Color.white);

                    bool needsAnim = Utils.NeedsAnimator(tag, out var applyAnim);

                    var plateAnimator = plate.Animator;

                    if (needsAnim)
                    {
                        if (plateAnimator == null)
                        {
                            // no animator exists, create one
                            plateAnimator = plate._gameObject.AddComponent<TagAnimator>();
                            plateAnimator.originalText = tag;
                            applyAnim?.Invoke(plateAnimator);
                        }
                    }

                    platesForUser.Add(plate);
                    movingElements.Add(plate._gameObject.transform);
                }

                FewTags.playerPlates[uid] = platesForUser;
                FewTags.playerStaticPlates[uid] = staticPlatesForUser;

                if (!FewTags.p.Contains(vrcPlayer))
                {
                    FewTags.p.Add(vrcPlayer);
                }
                PlateFunctions.NameplateESP(vrcPlayer);

                if (!FewTags.UnderNameplate)
                {
                    foreach (var plate in movingElements)
                    {
                        var detect = plate.gameObject.AddComponent<MenuDetector>();
                        detect.player = vrcPlayer;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogErrorToConsole("Error Handling Plates For UserID:" + vrcPlayer.APIUser.id + "\nError: " + ex.Message + "\nException StackTrace: " + ex.StackTrace + "\nException Data: " + ex.Data);
            }
        }
    }
}
