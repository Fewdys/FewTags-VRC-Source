namespace FewTags.FewTags
{
    internal static class LocalTags
    {
        internal static readonly Dictionary<string, string> LocallyTagged = new(StringComparer.OrdinalIgnoreCase);
        internal static readonly Dictionary<string, List<string>> LocallyTaggedByID = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, HashSet<string>> _cache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new();

        /// <summary>
        /// Called On Application Start To Load/Create File For Local Tagging
        /// </summary>
        internal static void LoadLocalTags()
        {
            LocalTags.LoadAndWatchKeyValue("FewTags/LocallyTagged.txt");
        }

        internal static void LoadAndWatchSet(string relativePath, HashSet<string> targetSet)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            string dir = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(fullPath))
                File.Create(fullPath).Dispose();

            var set = LoadFromFile(fullPath);
            lock (_lock)
            {
                targetSet.Clear();
                targetSet.UnionWith(set);
                _cache[fullPath] = set;

                if (!_watchers.ContainsKey(fullPath))
                {
                    var watcher = new FileSystemWatcher(dir ?? ".", Path.GetFileName(fullPath))
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
                    };

                    watcher.Changed += (_, __) => ReloadSet(fullPath, targetSet);
                    watcher.Created += (_, __) => ReloadSet(fullPath, targetSet);
                    watcher.Renamed += (_, __) => ReloadSet(fullPath, targetSet);
                    watcher.Deleted += (_, __) => ReloadSet(fullPath, targetSet);

                    watcher.EnableRaisingEvents = true;
                    _watchers[fullPath] = watcher;
                }
            }
        }

        private static void ReloadSet(string fullPath, HashSet<string> targetSet)
        {
            lock (_lock)
            {
                try
                {
                    var set = LoadFromFile(fullPath);
                    targetSet.Clear();
                    targetSet.UnionWith(set);
                    _cache[fullPath] = set;
                    LogManager.LogToConsole($"[LocalTags] Reloaded {Path.GetFileName(fullPath)}");
                }
                catch (Exception ex)
                {
                    LogManager.LogErrorToConsole($"[LocalTags] Failed to reload {fullPath}: {ex.Message}");
                }
            }
        }

        private static HashSet<string> LoadFromFile(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path)
                                .Select(l => l.Trim())
                                .Where(l => !string.IsNullOrEmpty(l));
                return new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                LogManager.LogErrorToConsole($"[LocalTags] Error reading {path}: {ex.Message}");
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        internal static void LoadAndWatchKeyValue(string relativePath)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            string dir = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(fullPath))
                File.Create(fullPath).Dispose();

            ReloadKeyValue(fullPath);

            lock (_lock)
            {
                if (!_watchers.ContainsKey(fullPath))
                {
                    var watcher = new FileSystemWatcher(dir ?? ".", Path.GetFileName(fullPath))
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
                    };

                    watcher.Changed += (_, __) => ReloadKeyValue(fullPath);
                    watcher.Created += (_, __) => ReloadKeyValue(fullPath);
                    watcher.Renamed += (_, __) => ReloadKeyValue(fullPath);
                    watcher.Deleted += (_, __) => ReloadKeyValue(fullPath);

                    watcher.EnableRaisingEvents = true;
                    _watchers[fullPath] = watcher;
                }
            }
        }

        private static void ReloadKeyValue(string fullPath)
        {
            lock (_lock)
            {
                try
                {
                    var dict = LoadKeyValueFromFile(fullPath);
                    _cache[fullPath] = new HashSet<string>(dict.Values.SelectMany(l => l), StringComparer.OrdinalIgnoreCase);

                    LocallyTaggedByID.Clear();
                    LocallyTagged.Clear();
                    foreach (var kv in dict)
                    {
                        LocallyTaggedByID[kv.Key] = new List<string>(kv.Value);
                        foreach (var label in kv.Value)
                        {
                            LocallyTagged[label] = kv.Key;
                        }
                    }

                    LogManager.LogToConsole($"[LocalTags] Reloaded {Path.GetFileName(fullPath)}");
                }
                catch (Exception ex)
                {
                    LogManager.LogErrorToConsole($"[LocalTags] Failed to reload key-value {fullPath}: {ex.Message}");
                }
            }
        }

        private static Dictionary<string, List<string>> LoadKeyValueFromFile(string path)
        {
            var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    var parts = trimmed.Split(':', 2);
                    if (parts.Length != 2) continue;

                    string labelPart = parts[0].Trim();
                    string id = parts[1].Trim();

                    var labels = labelPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(l => l.Trim())
                                          .ToList();

                    if (!dict.TryGetValue(id, out var list))
                    {
                        list = new List<string>();
                        dict[id] = list;
                    }
                    list.AddRange(labels);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogErrorToConsole($"[LocalTags] Error reading key-value {path}: {ex.Message}");
            }

            return dict;
        }

        internal static HashSet<string> Get(string filePath)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);

            lock (_lock)
            {
                return _cache.TryGetValue(fullPath, out var set)
                    ? set
                    : LoadFromFile(fullPath);
            }
        }

        internal static void SaveFile() // used for the main local tagging file, feel free to add functions for other tag stuff you might add.
        {
            try
            {
                using (var writer = new StreamWriter("FewTags/LocallyTagged.txt", false))
                {
                    foreach (var value in LocallyTaggedByID)
                    {
                        string userid = value.Key;
                        var tags = value.Value;
                        string tagString = string.Join(",", tags);
                        writer.WriteLine($"{tagString}:{userid}");
                    }
                }

                LogManager.LogToConsole("[LocalTags] Saved Local Tags");
            }
            catch (Exception e)
            {
                LogManager.LogErrorToConsole($"[LocalTags] Failed to save Local Tags");
            }
        }

        /// <summary>
        /// Adds a local tag to the specified player, associating the tag with the player's user ID if it is not already
        /// present.
        /// </summary>
        /// <remarks>If the player does not already have the specified tag, this method creates a new
        /// entry in the local tags collection and updates the player's tags in the system. Changes are persisted to the
        /// local tags file.</remarks>
        /// <param name="player">The player to which the local tag will be added. Cannot be null.</param>
        /// <param name="tag">The tag to associate with the player. Must be a non-empty string.</param>
        public static void AddLocalTag(this VRC.Player player, string tag)
        {
            if (player == null) return;
            string userid = player.GetPlayersUserID();
            if (!string.IsNullOrEmpty(userid))
            {
                if (!LocalTags.LocallyTaggedByID.TryGetValue(userid, out var localTags))
                {
                    localTags = new List<string>();
                    LocalTags.LocallyTaggedByID[userid] = localTags;
                }

                if (!localTags.Contains(tag))
                    localTags.Add(tag);

                string tagString = string.Join(",", localTags);
                LocalTags.LocallyTagged[tagString] = userid;

                LocalTags.SaveFile();

                FewTagsUpdater.UpdatePlayerTags(player);
            }
        }

        public static void AddLocalTag(string userid, string tag)
        {
            if (!string.IsNullOrEmpty(userid))
            {
                if (!LocalTags.LocallyTaggedByID.TryGetValue(userid, out var localTags))
                {
                    localTags = new List<string>();
                    LocalTags.LocallyTaggedByID[userid] = localTags;
                }

                if (!localTags.Contains(tag))
                    localTags.Add(tag);

                string tagString = string.Join(",", localTags);
                LocalTags.LocallyTagged[tagString] = userid;

                LocalTags.SaveFile();
            }
        }

        /// <summary>
        /// Removes the specified tag from the local tags associated with the given player.
        /// </summary>
        /// <remarks>If the player has no tags associated with their user ID after removal, the user ID
        /// will be removed from the local tags collection. The method also updates the player's tags after the removal
        /// operation.</remarks>
        /// <param name="player">The player from whom the tag will be removed. This parameter cannot be null.</param>
        /// <param name="tag">The tag to be removed from the player's local tags. This parameter cannot be null or empty.</param>
        public static void RemoveLocalTag(this VRC.Player player, string tag)
        {
            if (player == null || string.IsNullOrEmpty(tag)) return;
            string userid = player.GetPlayersUserID();
            if (!string.IsNullOrEmpty(userid))
            {
                if (LocalTags.LocallyTaggedByID.TryGetValue(userid, out var localTags))
                {
                    localTags.Remove(tag);
                    LocalTags.LocallyTagged.Remove(tag);

                    if (localTags.Count == 0)
                        LocalTags.LocallyTaggedByID.Remove(userid);

                    LocalTags.SaveFile();
                    FewTagsUpdater.UpdatePlayerTags(player);
                }
            }
        }

        public static void RemoveLocalTag(string userid, string tag)
        {
            if (!string.IsNullOrEmpty(userid))
            {
                if (LocalTags.LocallyTaggedByID.TryGetValue(userid, out var localTags))
                {
                    localTags.Remove(tag);
                    LocalTags.LocallyTagged.Remove(tag);

                    if (localTags.Count == 0)
                        LocalTags.LocallyTaggedByID.Remove(userid);

                    LocalTags.SaveFile();
                }
            }
        }

        /// <summary>
        /// Removes all locally assigned tags for the specified player.
        /// </summary>
        /// <remarks>If the player has no associated user ID or if the user ID is not found in the local
        /// tags, no action is taken. This method also saves the updated local tags to the file system after
        /// removal.</remarks>
        /// <param name="player">The player whose local tags are to be removed. This parameter cannot be null.</param>
        public static void RemoveLocalUsersTags(this VRC.Player player)
        {
            if (player == null) return;
            string userid = player.GetPlayersUserID();
            if (!string.IsNullOrEmpty(userid))
            {
                if (LocalTags.LocallyTaggedByID.TryGetValue(userid, out var localTags))
                {
                    for (int i = 0; i < localTags.Count; i++)
                    {
                        var tag = localTags[i];
                        if (tag == null || string.IsNullOrEmpty(tag)) continue;
                        localTags.Remove(tag);
                    }

                    LocalTags.LocallyTaggedByID.Remove(userid);
                    LocalTags.SaveFile();
                    FewTagsUpdater.UpdatePlayerTags(player);
                }
                else
                {
                    LogManager.LogWarningToConsole($"UserID: {userid} Was Not Found In FewTags/LocallyTagged.txt");
                }
            }
        }

        public static void RemoveLocalUsersTags(string userid)
        {
            if (!string.IsNullOrEmpty(userid))
            {
                if (LocalTags.LocallyTaggedByID.TryGetValue(userid, out var localTags))
                {
                    for (int i = 0; i < localTags.Count; i++)
                    {
                        var tag = localTags[i];
                        if (tag == null || string.IsNullOrEmpty(tag)) continue;
                        localTags.Remove(tag);
                    }

                    LocalTags.LocallyTaggedByID.Remove(userid);
                    LocalTags.SaveFile();
                }
                else
                {
                    LogManager.LogWarningToConsole($"UserID: {userid} Was Not Found In FewTags/LocallyTagged.txt");
                }
            }
        }

        /// <summary>
        /// Retrieves a list of local tags associated with the specified player.
        /// </summary>
        /// <remarks>This method checks if the player has a valid user ID and retrieves the corresponding
        /// tags from the local tag storage. If the user ID is null or empty, an empty list is returned.</remarks>
        /// <param name="player">The player instance for which to retrieve local tags. This parameter cannot be null.</param>
        /// <returns>A list of strings representing the local tags associated with the player. Returns an empty list if the
        /// player has no associated tags or if the user ID is not available.</returns>
        public static List<string> GetLocalTags(this VRC.Player player)
        {
            var userid = player.GetPlayersUserID();
            if (string.IsNullOrEmpty(userid)) return new List<string>();

            if (LocalTags.LocallyTaggedByID != null && LocalTags.LocallyTaggedByID.TryGetValue(userid, out var tags))
            {
                return new List<string>(tags);
            }
            else return new List<string>();
        }

        /// <summary>
        /// Replaces all local tags associated with the specified player with a new set of tags.
        /// </summary>
        /// <remarks>If the player has existing tags, they are removed before the new tags are applied. If
        /// the provided collection is empty or contains only null or empty strings, all local tags for the player are
        /// cleared. Duplicate tags are ignored, and tags are trimmed of whitespace before being stored.</remarks>
        /// <param name="player">The player whose local tags are to be replaced. This parameter cannot be null.</param>
        /// <param name="tags">An enumerable collection of strings representing the new tags to associate with the player. Tags that are
        /// null or empty are ignored.</param>
        public static void ReplaceLocalTags(this VRC.Player player, IEnumerable<string> tags)
        {
            if (player == null) return;
            var userid = player.GetPlayersUserID();

            if (!string.IsNullOrEmpty(userid))
            {
                var newTags = tags?.Where(t => !string.IsNullOrEmpty(t)).Select(t => t.Trim()).Distinct().ToList() ?? new List<string>();

                if (LocalTags.LocallyTaggedByID.TryGetValue(userid, out var oldTags))
                {
                    foreach (var tag in oldTags)
                    {
                        LocalTags.LocallyTagged.Remove(tag);
                    }
                }

                if (newTags.Count > 0)
                    LocalTags.LocallyTaggedByID[userid] = newTags;
                else
                    LocalTags.LocallyTaggedByID.Remove(userid);

                var updatedlist = new List<string>();
                foreach (var tag in newTags)
                {
                    updatedlist.Add(tag.Trim());
                }

                string tagString = string.Join(",", updatedlist);
                LocalTags.LocallyTagged[tagString] = userid;

                LocalTags.SaveFile();
                FewTagsUpdater.UpdatePlayerTags(player);
            }
        }

        public static void ReplaceLocalTags(string userid, IEnumerable<string> tags)
        {

            if (!string.IsNullOrEmpty(userid))
            {
                var newTags = tags?.Where(t => !string.IsNullOrEmpty(t)).Select(t => t.Trim()).Distinct().ToList() ?? new List<string>();

                if (LocalTags.LocallyTaggedByID.TryGetValue(userid, out var oldTags))
                {
                    foreach (var tag in oldTags)
                    {
                        LocalTags.LocallyTagged.Remove(tag);
                    }
                }

                if (newTags.Count > 0)
                    LocalTags.LocallyTaggedByID[userid] = newTags;
                else
                    LocalTags.LocallyTaggedByID.Remove(userid);

                var updatedlist = new List<string>();
                foreach (var tag in newTags)
                {
                    updatedlist.Add(tag.Trim());
                }

                string tagString = string.Join(",", updatedlist);
                LocalTags.LocallyTagged[tagString] = userid;

                LocalTags.SaveFile();
            }
        }

        /// <summary>
        /// Adds one or more local tags to the specified player and associates them with the player's user ID.
        /// </summary>
        /// <remarks>If the player is not found or the tags parameter is invalid, the method performs no
        /// action. The tags are stored locally and persisted to a file for future reference.</remarks>
        /// <param name="player">The player to which the tags will be added. This parameter must not be null.</param>
        /// <param name="tags">A comma-separated list of tags to associate with the player. This parameter must not be null or empty.</param>
        public static void AddUserWithLocalTags(this VRC.Player player, string tags)
        {
            if (player == null || string.IsNullOrEmpty(tags)) return;
            var userid = player.GetPlayersUserID();
            if (!string.IsNullOrEmpty(userid))
            {
                var newTags = new List<string>();
                newTags.AddRange(tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)));
                LocalTags.LocallyTaggedByID[userid] = newTags;

                var updatedlist = new List<string>();
                foreach (var tag in newTags)
                {
                    updatedlist.Add(tag.Trim());
                }

                string tagString = string.Join(",", updatedlist);
                LocalTags.LocallyTagged[tagString] = userid;

                LocalTags.SaveFile();

                FewTagsUpdater.UpdatePlayerTags(player);
            }
        }

        public static void AddUserWithLocalTags(string userid, string tags)
        {
            if (!string.IsNullOrEmpty(userid))
            {
                var newTags = new List<string>();
                newTags.AddRange(tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)));
                LocalTags.LocallyTaggedByID[userid] = newTags;

                var updatedlist = new List<string>();
                foreach (var tag in newTags)
                {
                    updatedlist.Add(tag.Trim());
                }

                string tagString = string.Join(",", updatedlist);
                LocalTags.LocallyTagged[tagString] = userid;

                LocalTags.SaveFile();
            }
        }
    }
}
