using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using FewTags.FewTags_Rewrite_V2;
using FewTags.FewTags_Rewrite_V2.Managers;
using Nyup_FewTags._Plate;
using UnityEngine;

namespace FewTags.FewTags
{
    public class FewTagsUpdater
    {
        internal static readonly ConcurrentDictionary<string, string[]> lastAppliedTags = new();
        internal static readonly ConcurrentDictionary<string, string> lastBigPlateText = new();

        private static readonly ConcurrentDictionary<string, string[]> _normalizedTagCache = new();

        internal static readonly ConcurrentQueue<System.Action> _mainThreadQueue = new();

        //internal static string url = "http://localhost:5000/tags";
        internal static string url = "https://raw.githubusercontent.com/Fewdys/FewTags/main/FewTags.json";
        internal static float updateInterval = 0f;
        internal static float LastTime = 0f;

        private static float _cachedIntervalSeconds = -1f;
        private static string _lastETag = null;
        private static bool firstinit = true;

        private const float GitHubMinIntervalSeconds = 90f;
        private static bool IsGitHubUrl => !string.IsNullOrEmpty(url) &&
            (url.Contains("raw.githubusercontent.com") || url.Contains("github.com"));

        internal static readonly ConcurrentDictionary<string, Jsons.Json.Tags> _tagLookup = new();

        // HttpClient reused across requests for connection pooling.
        // Called synchronously via .GetAwaiter().GetResult() on a null-SynchronizationContext thread.
        private static HttpClient _httpClient;
        private static readonly object _httpClientLock = new object();
        private static bool _warmed = false;
        private static readonly object _lock = new object();

        public static HttpClient GetHttpClient()
        {
            if (_httpClient != null) return _httpClient;
            lock (_httpClientLock)
            {
                if (_httpClient != null) return _httpClient;

                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30),
                };
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "FewTags/1.0");
                return _httpClient;
            }
        }

        private static Thread _pollThread;
        private static CancellationTokenSource _pollCts;

        internal static void StartBackgroundPoller()
        {
            // Ensure any previous poller is fully stopped before starting a new one,
            // so we never have two threads hitting FetchTagsSync concurrently.
            StopBackgroundPoller();

            _pollCts = new CancellationTokenSource();

            _pollThread = new Thread(() => PollThreadProc(_pollCts.Token))
            {
                IsBackground = true,
                Name = "FewTagsPoller"
            };
            _pollThread.Start();
        }

        private static bool ProbeServer(string url, TimeSpan timeout)
        {
            try
            {
                var uri = new Uri(url);

                if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                    return false;

                IPAddress[] addresses;

                try
                {
                    addresses = Dns.GetHostAddresses(uri.Host);
                }
                catch
                {
                    return false;
                }

                if (addresses == null || addresses.Length == 0)
                    return false;

                int port = uri.IsDefaultPort
                    ? (uri.Scheme == Uri.UriSchemeHttps ? 443 : 80)
                    : uri.Port;

                for (int i = 0; i < addresses.Length; i++)
                {
                    Socket socket = null;
                    SocketAsyncEventArgs args = null;
                    ManualResetEventSlim completed = null;

                    try
                    {
                        socket = new Socket(
                            addresses[i].AddressFamily,
                            SocketType.Stream,
                            ProtocolType.Tcp);

                        completed = new ManualResetEventSlim(false);

                        args = new SocketAsyncEventArgs
                        {
                            RemoteEndPoint = new IPEndPoint(addresses[i], port)
                        };

                        args.Completed += (s, e) =>
                        {
                            try
                            {
                                completed.Set();
                            }
                            catch
                            {
                            }
                        };

                        bool pending = socket.ConnectAsync(args);

                        if (!pending)
                        {
                            if (args.SocketError == SocketError.Success)
                                return true;

                            continue;
                        }

                        if (!completed.Wait(timeout))
                            continue;

                        if (args.SocketError == SocketError.Success)
                            return true;
                    }
                    catch
                    {
                        // Connection refused / timeout / invalid address/etc.
                        // Treat all of them as unreachable.
                    }
                    finally
                    {
                        try { args?.Dispose(); } catch { }
                        try { socket?.Close(); } catch { }
                        try { completed?.Dispose(); } catch { }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        internal static void StopBackgroundPoller()
        {
            _pollCts?.Cancel();
            // Give the in-flight request a real chance to unwind before we let go of it.
            _pollThread?.Join(TimeSpan.FromSeconds(2));
            _pollThread = null;
            _pollCts?.Dispose();
            _pollCts = null;
        }

        private static float GetIntervalSeconds()
        {
            if (_cachedIntervalSeconds >= 0f) return _cachedIntervalSeconds;

            float configured = FewTags.UpdateIntervalMinutes * 60f;
            if (IsGitHubUrl && configured < GitHubMinIntervalSeconds)
            {
                LogManager.LogWarningToConsole($"GitHub URL detected — enforcing {GitHubMinIntervalSeconds}s minimum interval.");
                _cachedIntervalSeconds = GitHubMinIntervalSeconds;
            }
            else
            {
                _cachedIntervalSeconds = configured;
            }

            return _cachedIntervalSeconds;
        }

        private static void PollThreadProc(CancellationToken ct)
        {
            // Null out Unity's SynchronizationContext on this thread — absolute guarantee.
            // ConfigureAwait(false) is best-effort in Mono; this is not.
            SynchronizationContext.SetSynchronizationContext(null);

            ct.WaitHandle.WaitOne(3000); // startup delay

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    bool changed = FetchTagsSync(ct);
                    if (changed)
                        _mainThreadQueue.Enqueue(() => UpdateAllPlayersOnMainThread());
                }
                catch (System.Exception ex)
                {
                    LogManager.LogErrorToConsole($"Poll error: {ex.Message}");
                }

                int intervalMs = (int)(GetIntervalSeconds() * 1000f);
                ct.WaitHandle.WaitOne(intervalMs);
            }
        }

        private static bool FetchTagsSync(CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(url))
            {
                try
                {
                    var bytes = System.IO.File.ReadAllBytes("...\\FewTags.json");
                    LoadTags(bytes);
                    return true;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogErrorToConsole($"Local load error: {ex.Message}");
                    return false;
                }
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Avoid reusing a pooled connection that the local server may have already
                // closed on its end — prevents ObjectDisposedException from stale keep-alives.
                request.Headers.ConnectionClose = true;

                if (!string.IsNullOrEmpty(_lastETag))
                    request.Headers.TryAddWithoutValidation("If-None-Match", _lastETag);

                if (IsGitHubUrl)
                    request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github.v3.raw");

                if (!ProbeServer(url, TimeSpan.FromMilliseconds(100)))
                {
                    LogManager.LogWarningToConsole(
                        $"FewTags server unreachable: {url}");

                    return false;
                }

                HttpResponseMessage response;

                try
                {
                    response = GetHttpClient()
                        .SendAsync(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            ct)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (ObjectDisposedException)
                {
                    LogManager.LogErrorToConsole(
                        "Local server connection was closed. Skipping this poll.");

                    return false;
                }

                using (response)
                {
                    if (response.StatusCode == HttpStatusCode.NotModified)
                        return false;

                    if (IsGitHubUrl && (response.StatusCode == HttpStatusCode.Forbidden || (int)response.StatusCode == 429))
                    {
                        LogManager.LogErrorToConsole("Rate limited by GitHub. Increase UpdateIntervalMinutes in config.");
                        return false;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        LogManager.LogErrorToConsole($"HTTP error: {response.StatusCode}");
                        return false;
                    }

                    if (response.Headers.TryGetValues("ETag", out var etagValues))
                        _lastETag = string.Concat(etagValues);

                    byte[] body;
                    try
                    {
                        using var responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                        using var ms = new MemoryStream();
                        var buffer = new byte[8192];
                        int read;
                        while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ct.ThrowIfCancellationRequested();
                            ms.Write(buffer, 0, read);
                        }
                        body = ms.ToArray();
                    }
                    catch (ObjectDisposedException)
                    {
                        LogManager.LogErrorToConsole("Local server closed the connection mid-read. Skipping this poll.");
                        return false;
                    }

                    if (body != null && body.Length > 0)
                    {
                        LogManager.LogToConsole($"Tags changed, {body.Length} bytes{(IsGitHubUrl ? " (GitHub)" : "")}.");
                        LoadTags(body);
                        return true;
                    }

                    return false;
                }
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                // expected during shutdown/reload — not an error
                return false;
            }
            catch (TaskCanceledException)
            {
                LogManager.LogErrorToConsole("Request timed out.");
                return false;
            }
            catch (ObjectDisposedException)
            {
                LogManager.LogErrorToConsole("Connection was disposed mid-request. Skipping this poll.");
                return false;
            }
            catch (HttpRequestException ex)
            {
                LogManager.LogErrorToConsole($"HTTP request error: {ex.Message}");
                return false;
            }
            catch (System.Net.WebException ex)
            {
                // Mono's HttpClientHandler can leak raw WebException instead of
                // wrapping it in HttpRequestException — same "server not reachable" case.
                LogManager.LogErrorToConsole($"Local server unreachable: {ex.Status} — {ex.Message}");
                return false;
            }
            catch (System.Exception ex)
            {
                LogManager.LogErrorToConsole($"FetchTagsSync error: {ex.Message}");
                return false;
            }
        }

        // Force an immediate re-fetch e.g. on config reload
        internal static void UpdateFewTags(bool ReloadConfig = false)
        {
            if (ReloadConfig)
            {
                FewTagsConfigLoader.Load();
                _cachedIntervalSeconds = -1f;
            }

            var t = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(null);
                try
                {
                    bool changed = FetchTagsSync();
                    if (changed)
                        _mainThreadQueue.Enqueue(() => CoroutineHelper.RunSafe(UpdateAllPlayersOnMainThread()));
                }
                catch (System.Exception ex)
                {
                    LogManager.LogErrorToConsole($"Force update error: {ex.Message}");
                }
            })
            {
                IsBackground = true,
                Name = "FewTagsForceUpdate"
            };
            t.Start();
        }

        public static Task<bool> GetTagsSafe()
        {
            return Task.Run(() =>
            {
                SynchronizationContext.SetSynchronizationContext(null);
                try { return FetchTagsSync(); }
                catch (System.Exception ex)
                {
                    LogManager.LogErrorToConsole($"GetTagsSafe error: {ex.Message}");
                    return false;
                }
            });
        }

        internal static void DoUpdate()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
                action();

            if (Input.GetKeyDown(KeyCode.O) && Input.GetKey(KeyCode.RightShift))
                PlateFunctions.CheckNameplateESPBind();
        }

        internal static void LoadTags(byte[] rawJsonBytes)
        {
            if (rawJsonBytes == null || rawJsonBytes.Length == 0)
            {
                LogManager.LogErrorToConsole("Raw JSON bytes are null or empty.");
                return;
            }

            FewTagsResolver.EnsureRegistered();

            FewTags.s_rawTags = System.Text.Encoding.UTF8.GetString(rawJsonBytes);

            try
            {
                List<Jsons.Json.Tags> records = null;

                try
                {
                    int i = 0;
                    while (i < rawJsonBytes.Length && (rawJsonBytes[i] == ' ' || rawJsonBytes[i] == '\t' || rawJsonBytes[i] == '\r' || rawJsonBytes[i] == '\n'))
                        i++;

                    bool isArray = i < rawJsonBytes.Length && rawJsonBytes[i] == (byte)'[';

                    records = isArray ? JsonSerializer.Deserialize<List<Jsons.Json.Tags>>(rawJsonBytes) : JsonSerializer.Deserialize<Jsons.Json._Tags>(rawJsonBytes)?.records;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogErrorToConsole($"JSON deserialize failed (both formats): {ex.Message}");
                    return;
                }

                if (records == null)
                {
                    LogManager.LogErrorToConsole("Deserialized records list is null.");
                    return;
                }

                var newTags = new Jsons.Json._Tags { records = records };
                var newEntries = new List<(string uid, Jsons.Json.Tags entry)>(records.Count);

                for (int i = 0; i < records.Count; i++)
                {
                    var entry = records[i];
                    if (entry.Tag == null)
                        entry.Tag = System.Array.Empty<string>();
                    if (!string.IsNullOrEmpty(entry.UserID))
                        newEntries.Add((entry.UserID, entry));
                }

                FewTags.s_tags = newTags;

                _tagLookup.Clear();
                for (int i = 0; i < newEntries.Count; i++)
                    _tagLookup[newEntries[i].uid] = newEntries[i].entry;

                _normalizedTagCache.Clear();

                LogManager.LogToConsole($"Loaded {records.Count} records.");
            }
            catch (System.Exception ex)
            {
                LogManager.LogErrorToConsole($"LoadTags error: {ex.Message}");
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

        public static IEnumerator UpdateAllPlayersOnMainThread(bool forced = false)
        {
            yield return null;

            var allplayers = Utils.AllPlayers;
            if (allplayers == null || allplayers.Length == 0) yield break;

            var snapshot = allplayers.ToArray();

            for (int i = 0; i < snapshot.Length; i++)
            {
                var player = snapshot[i];
                if (player == null) continue;

                try
                {
                    var vrcPlayer = player?.gameObject?.GetComponent<VRC.Player>();
                    if (vrcPlayer != null)
                        UpdatePlayerTags(vrcPlayer, forced);
                }
                catch (Exception ex)
                {
                    LogManager.LogErrorToConsole($"Error updating tags for player in loop: {ex.Message}");
                }

                yield return null;
            }
        }

        public static IEnumerator ReloadPlates()
        {
            yield return null;

            var allplayers = Utils.AllPlayers;
            if (allplayers == null || allplayers.Length == 0) yield break;

            var snapshot = allplayers.ToArray();

            for (int i = 0; i < snapshot.Length; i++)
            {
                var player = snapshot[i];
                if (player == null) continue;
                PlateHandlers.PlateHandler(player);

                yield return null;
            }
        }
    }
}
