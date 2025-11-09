using FewTags.FewTags.JSON;
using FewTags.FewTags.Wrappers;
using Il2CppSystem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEngine;

namespace FewTags.FewTags
{
    public class FewTagsUpdater
    {
        internal static readonly Dictionary<string, string[]> lastAppliedTags = new();
        internal static readonly Dictionary<string, string> lastBigPlateText = new();

        internal static string url = "https://raw.githubusercontent.com/Fewdys/FewTags/main/FewTags.json";
        internal static float updateInterval = 0f;

        /// <summary>
        /// Main Update Loop For Fetching Tags (Put This On Main Update Loop Or A Monobehaviour Update).
        /// </summary>
        internal static void DoUpdate()
        {

            if (Time.realtimeSinceStartup >= updateInterval)
            {
                FewTagsConfigLoader.Load();
                updateInterval = Time.realtimeSinceStartup + (FewTags.UpdateIntervalMinutes * 60f);
                UpdateTags();
                UpdateAllPlayersTagsLive();
            }

            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.O))
            {
                PlateFunctions.CheckNameplateESPBind();
            }
        }

        /// <summary>
        /// Fetches Tag Database.
        /// </summary>
        internal static void UpdateTags()
        {

            try
            {

                if (string.IsNullOrEmpty(url))
                {
                    using (WebClient wc = new WebClient())
                    {
                        FewTags.s_rawTags = File.ReadAllText("C:\\Users\\Fewdy\\source\\repos\\FewTags\\FewTags.json"); // change path if you downloaded the json either manually or to a specific place and want to use local file

                        if (string.IsNullOrEmpty(FewTags.s_rawTags))
                        {
                            return;
                        }

                        JSONNode jsonNode = JSON.JSON.Parse(FewTags.s_rawTags);
                        if (jsonNode == null)
                        {
                            return;
                        }

                        FewTags.s_tags = new Jsons.Json._Tags { records = new List<Jsons.Json.Tags>() };

                        var records = jsonNode["records"].AsArray;
                        for (int i = 0; i < records.Count; i++)
                        {
                            var record = records[i];
                            List<string> tagsList = new List<string>();

                            var tagArray = record["Tag"].AsArray;
                            for (int j = 0; j < tagArray.Count; j++)
                            {
                                tagsList.Add(tagArray[j].Value);
                            }

                            Jsons.Json.Tags tagEntry = new Jsons.Json.Tags
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

                            FewTags.s_tags.records.Add(tagEntry);
                        }

                        //LogManager.LogToConsole($"Record Count: {FewTags.s_tags.records.Count}");
                        return;
                    }
                }
                else
                {

                    using (WebClient wc = new WebClient())
                    {
                        FewTags.s_rawTags = wc.DownloadString(url); // do FewTags.url instead (current url is set for my local thingy)

                        if (string.IsNullOrEmpty(FewTags.s_rawTags))
                        {
                            LogManager.LogErrorToConsole("s_rawTags is Null");
                            return;
                        }

                        JSONNode jsonNode = JSON.JSON.Parse(FewTags.s_rawTags);
                        if (jsonNode == null)
                        {
                            LogManager.LogErrorToConsole("JsonNode is Null");
                            return;
                        }

                        FewTags.s_tags = new Jsons.Json._Tags { records = new List<Jsons.Json.Tags>() };

                        var records = jsonNode["records"].AsArray; // jsonNode.AsArray if you're reading the file without the records part
                        for (int i = 0; i < records.Count; i++)
                        {
                            var record = records[i];
                            List<string> tagsList = new List<string>();

                            var tagArray = record["Tag"].AsArray;
                            for (int j = 0; j < tagArray.Count; j++)
                            {
                                tagsList.Add(tagArray[j].Value);
                            }

                            Jsons.Json.Tags tagEntry = new Jsons.Json.Tags
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

                            FewTags.s_tags.records.Add(tagEntry);
                        }

                        //LogManager.LogToConsole($"Record Count: {FewTags.s_tags.records.Count}");
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                url = string.Empty;
                LogManager.LogErrorToConsole($"Error in UpdateTags: {ex.Message}\n{ex.StackTrace}\n{ex}");
            }
        }

        /// <summary>
        /// Check If Players Tags Have Changed, If They Have Run PlateHandler On Player.
        /// </summary>
        internal static void UpdatePlayerTags(VRC.Player vrcPlayer)
        {
            if (vrcPlayer == null || vrcPlayer.APIUser == null) return;
            string uid = vrcPlayer.APIUser.id;
            if (string.IsNullOrEmpty(uid)) return;

            var record = FewTags.s_tags?.records?.FirstOrDefault(r => r.UserID == uid);
            /// 
            /// Custom Tag Checking Here If Wanted
            /// 
            bool hasExternal = LocalTags.LocallyTagged.ContainsKey(uid)|| LocalTags.LocallyTaggedByID.ContainsKey(uid); // LOCAL TAGS !!
            ///
            /// End
            /// 

            if (record == null && !hasExternal) return;

            var effectiveTags = new List<string>();
            /// 
            /// Custom Tag Checking Here If Wanted
            /// 
            if (LocalTags.LocallyTaggedByID.TryGetValue(uid, out var localTags)) // LOCAL TAGS !!
            {
                for (int i = 0; i < localTags.Count; i++)
                {
                    var tag = localTags[i];
                    if (string.IsNullOrEmpty(tag)) continue;
                    effectiveTags.Add(Utils.AddLocalTagPrefix(tag));
                }
            }
            ///
            /// End
            /// 
            if (record?.Tag != null) effectiveTags.AddRange(record.Tag);
            effectiveTags = effectiveTags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(System.StringComparer.OrdinalIgnoreCase).ToList();

            string[] currentTags = effectiveTags.ToArray();
            string bigPlate = record?.PlateBigText ?? string.Empty;

            bool changed = !lastAppliedTags.TryGetValue(uid, out var prevTags) || !Utils.NormalizeTags(prevTags).SequenceEqual(Utils.NormalizeTags(currentTags));

            bool bigChanged = !lastBigPlateText.TryGetValue(uid, out var prevBig) || !string.Equals(prevBig?.Trim(), bigPlate?.Trim(), System.StringComparison.OrdinalIgnoreCase);

            if (changed || bigChanged)
            {
                ///
                /// These Two Log Messages Are For Debugging You Can Comment Them Out
                /// 
                LogManager.LogWarningToConsole($"Tags changed for {uid}: prev=[{string.Join(",", prevTags ?? System.Array.Empty<string>())}], curr=[{string.Join(",", currentTags ?? System.Array.Empty<string>())}]");
                LogManager.LogWarningToConsole($"BigPlate changed for {uid}: prev=[{prevBig ?? "null"}], curr=[{bigPlate ?? "null"}]");
                ///
                /// End
                ///
                PlateHandlers.PlateHandler(vrcPlayer);
            }
        }

        /// <summary>
        /// Update All Players Tags In The Instance.
        /// Call Me 
        /// </summary>
        internal static void UpdateAllPlayersTagsLive()
        {
            var allPlayers = PlayerWrapper.GetAllPlayers();
            for (int i = 0; i < allPlayers.Count; i++)
            {
                var player = allPlayers[i];
                if (player == null) continue;

                var vrcPlayer = player.gameObject?.GetComponent<VRC.Player>();
                if (vrcPlayer != null)
                {
                    UpdatePlayerTags(vrcPlayer);
                }
            }
        }
    }
}
