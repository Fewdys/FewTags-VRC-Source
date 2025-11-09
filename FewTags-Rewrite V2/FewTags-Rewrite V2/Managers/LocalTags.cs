using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    }
}
