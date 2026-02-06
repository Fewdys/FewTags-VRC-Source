using FewTags.FewTags.JSON;
using FewTags.FewTags_Rewrite_V2.Managers;
using Il2CppSystem;
using UnityEngine;

namespace FewTags.FewTags
{
    public class FewTagsUpdater
    {
        internal static readonly Dictionary<string, string[]> lastAppliedTags = new();
        internal static readonly Dictionary<string, string> lastBigPlateText = new();

        internal static string url = "https://raw.githubusercontent.com/Fewdys/FewTags/main/FewTags.json";
        internal static float updateInterval = 0f;
        internal static readonly HttpClient httpClient = new HttpClient();
        internal static System.Threading.CancellationTokenSource cancellationTokenSource;
        //private static readonly List<string> tagsList = new List<string>();
        internal static bool isUpdating = false;

        static FewTagsUpdater()
        {
            httpClient.Timeout = System.TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "FewTags/1.0");
        }

        /// <summary>
        /// Main Update Loop For Fetching Tags (Put This On Main Update Loop Or A Monobehaviour Update).
        /// </summary>
        internal static void DoUpdate()
        {

            if (Time.realtimeSinceStartup >= updateInterval)
            {
                FewTagsConfigLoader.Load();
                updateInterval = Time.realtimeSinceStartup + (FewTags.UpdateIntervalMinutes * 60f);
                UpdateTagsAsync();
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
        internal static void UpdateTagsAsync()
        {
            if (isUpdating) return;

            isUpdating = true;

            Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(url))
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            LoadTags(System.IO.File.ReadAllText("C:\\Users\\Fewdy\\source\\repos\\FewTags\\FewTags.json"));
                        });
                        return;
                    }

                    cancellationTokenSource = new System.Threading.CancellationTokenSource();

                    using (var response = await httpClient.GetAsync(url, cancellationTokenSource.Token))
                    {
                        response.EnsureSuccessStatusCode();
                        var result = await response.Content.ReadAsStringAsync(cancellationTokenSource.Token);

                        if (!string.IsNullOrEmpty(result))
                        {
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                LoadTags(result);
                            });
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    LogManager.LogErrorToConsole("Download was cancelled");
                }
                catch (HttpRequestException ex)
                {
                    LogManager.LogErrorToConsole($"HTTP request error: {ex.Message}");
                }
                catch (System.Exception ex)
                {
                    LogManager.LogErrorToConsole($"Update error: {ex.Message}");
                }
                finally
                {
                    isUpdating = false;
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                }
            });
        }

        internal static void LoadTags(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson))
            {
                LogManager.LogErrorToConsole("Raw JSON is null or empty.");
                return;
            }

            FewTags.s_rawTags = rawJson;

            JSONNode jsonNode = JSON.JSON.Parse(rawJson);
            if (jsonNode == null)
            {
                LogManager.LogErrorToConsole("JSON parse failed.");
                return;
            }

            if (FewTags.s_tags == null)
                FewTags.s_tags = new Jsons.Json._Tags { records = new List<Jsons.Json.Tags>() };
            else
                FewTags.s_tags.records.Clear();

            JSONArray records;
            if (jsonNode is JSONArray array)
            {
                records = array;
            }
            else if (jsonNode["records"] != null && jsonNode["records"].IsArray)
            {
                records = jsonNode["records"].AsArray;
            }
            else
            {
                LogManager.LogErrorToConsole("Invalid JSON format: no records found");
                return;
            }

            if (records == null)
            {
                LogManager.LogErrorToConsole("No valid 'records' array.");
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var tagArray = record["Tag"].AsArray;

                var tagsList = new List<string>();
                if (tagArray != null)
                {
                    for (int j = 0; j < tagArray.Count; j++)
                        tagsList.Add(tagArray[j].Value);
                }

                FewTags.s_tags.records.Add(new Jsons.Json.Tags
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
                });
            }
        }


        /// <summary>
        /// Check If Players Tags Have Changed, If They Have Run PlateHandler On Player.
        /// </summary>
        internal static void UpdatePlayerTags(VRC.Player vrcPlayer, bool Forced = false)
        {
            if (vrcPlayer == null || vrcPlayer.APIUser == null) return;
            string uid = vrcPlayer.APIUser.id;
            if (string.IsNullOrEmpty(uid)) return;
            var records = FewTags.s_tags?.records;
            if (records == null) return;
            Jsons.Json.Tags[] snapshot;
            try
            {
                snapshot = records.ToArray();
            }
            catch { return; }

            var record = snapshot.FirstOrDefault(r => r.UserID == uid);
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

            if (changed || bigChanged || Forced)
            {
                ///
                /// These Two Log Messages Are For Debugging You Can Comment Them Out!!
                /// 
                LogManager.LogWarningToConsole($"Tags changed for {uid}: prev=[{string.Join(",", prevTags ?? System.Array.Empty<string>())}], curr=[{string.Join(",", currentTags ?? System.Array.Empty<string>())}]");
                LogManager.LogWarningToConsole($"BigPlate changed for {uid}: prev=[{prevBig ?? "null"}], curr=[{bigPlate ?? "null"}]");
                ///
                /// End
                ///
                PlateHandlers.PlateHandler(vrcPlayer);
            }
        }

        internal static void UpdatePlayerTags(VRCPlayer vrcPlayer, bool Forced = false)
        {
            if (vrcPlayer == null) return;
            var player = vrcPlayer.gameObject.GetComponent<VRC.Player>();
            if (player == null) return;
            var apiuser = player.APIUser;
            if (apiuser == null) return;
            string uid = apiuser.id;
            if (string.IsNullOrEmpty(uid)) return;

            var records = FewTags.s_tags?.records;
            if (records == null) return;
            Jsons.Json.Tags[] snapshot;
            try
            {
                snapshot = records.ToArray();
            }
            catch { return; }

            var record = snapshot.FirstOrDefault(r => r.UserID == uid);
            bool hasExternal = LocalTags.LocallyTagged.ContainsKey(uid) || LocalTags.LocallyTaggedByID.ContainsKey(uid);

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

            if (changed || bigChanged || Forced)
            {
                ///
                /// These Two Log Messages Are For Debugging You Can Comment Them Out
                /// 
                LogManager.LogWarningToConsole($"Tags changed for {uid}: prev=[{string.Join(",", prevTags ?? System.Array.Empty<string>())}], curr=[{string.Join(",", currentTags ?? System.Array.Empty<string>())}]");
                LogManager.LogWarningToConsole($"BigPlate changed for {uid}: prev=[{prevBig ?? "null"}], curr=[{bigPlate ?? "null"}]");
                ///
                /// End
                ///
                PlateHandlers.PlateHandler(player);
            }
        }

        /// <summary>
        /// Update All Players Tags In The Instance.
        /// Call Me 
        /// </summary>
        internal static void UpdateAllPlayersTagsLive()
        {
            var allPlayers = Utils.AllPlayers;
            if (allPlayers == null || allPlayers.Length == 0) return;
            for (int i = 0; i < allPlayers.Length; i++)
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
