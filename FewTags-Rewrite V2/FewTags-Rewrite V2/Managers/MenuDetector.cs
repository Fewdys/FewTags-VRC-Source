using Nyup_FewTags._Plate;
using UnityEngine;
using VRC.UI.Elements;

namespace FewTags.FewTags
{
    public class MenuDetector : MonoBehaviour
    {
        static GameObject qm;
        static Canvas qmcanvas;

        public bool IsExtendedInfo = false;
        public VRC.Player player;
        public bool isFriend;

        Vector3 originalLocalPos;
        float currentOffset = 0f;

        const int CHECK_EVERY_FRAMES = 10;
        int frameCounter;

        // Expensive nameplate hierarchy scans only happen this often (seconds).
        const float STATE_CHECK_INTERVAL = 10f;

        float _lastStateCheck = -1f;
        int _cachedExternalCount;
        bool _cachedGroupActive;
        VRC.Player _cachedPlayer;

        public MenuDetector(IntPtr ptr) : base(ptr)
        {
        }

        public MenuDetector() : base(IntPtr.Zero)
        {
        }

        public void Start()
        {
            originalLocalPos = transform.localPosition;
        }

        static readonly string DefaultKeyword = string.Empty; // default name to always include for you're build

        static readonly HashSet<string> _keywordSet =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // insert any names needed to move the plate up whenever above nameplate for example: VRTool, Nyup, Abyss e.g (or whatever you name your existing plates contain if any go above nameplate)
            };

        static string[] _keywordsSnapshot = null;
        static bool _keywordsDirty = true;

        // Group-related objects we need to detect.
        // These are static because they never change (if they do change, change them here).
        static readonly string[] _groupKeywords =
        {
            "group info",
            "banner_expanded",
            "group banner"
        };

        static string[] _combinedKeywords = null;
        static bool _combinedKeywordsDirty = true;

        static readonly List<Transform> _externalMatchBuffer = new List<Transform>(8);

        /// <summary>
        /// Registers an additional GameObject-name keyword (case-insensitive)
        /// to watch for under the nameplate when counting external module
        /// additions. Safe to call multiple times / with duplicates — they're
        /// deduplicated automatically. Persists to config immediately.
        /// </summary>
        public static void AddKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return;

            keyword = keyword.Trim();

            if (_keywordSet.Add(keyword))
            {
                _keywordsDirty = true;
                _combinedKeywordsDirty = true;

                SaveKeywordsToConfig();
            }
        }

        /// <summary>
        /// Removes a previously-registered keyword, if present. Persists to config
        /// immediately.
        /// </summary>
        public static void RemoveKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return;

            keyword = keyword.Trim();

            if (_keywordSet.Remove(keyword))
            {
                _keywordsDirty = true;
                _combinedKeywordsDirty = true;

                SaveKeywordsToConfig();
            }
        }

        /// <summary>
        /// Removes every registered keyword except the hardcoded default, then
        /// persists the reset to config.
        /// </summary>
        public static void ClearKeywordsToDefault()
        {
            _keywordSet.Clear();
            _keywordSet.Add(DefaultKeyword);

            _keywordsDirty = true;
            _combinedKeywordsDirty = true;

            SaveKeywordsToConfig();
        }

        static string[] GetKeywordsSnapshot()
        {
            if (!_keywordsDirty && _keywordsSnapshot != null)
                return _keywordsSnapshot;

            _keywordsSnapshot = new string[_keywordSet.Count];

            int i = 0;

            foreach (string kw in _keywordSet)
                _keywordsSnapshot[i++] = kw.ToLowerInvariant();

            _keywordsDirty = false;

            return _keywordsSnapshot;
        }

        static string[] GetCombinedKeywords()
        {
            if (!_combinedKeywordsDirty && _combinedKeywords != null)
                return _combinedKeywords;

            string[] externalKeywords = GetKeywordsSnapshot();

            int totalCount =
                externalKeywords.Length +
                _groupKeywords.Length;

            _combinedKeywords = new string[totalCount];

            int index = 0;

            for (int i = 0; i < externalKeywords.Length; i++)
                _combinedKeywords[index++] = externalKeywords[i];

            for (int i = 0; i < _groupKeywords.Length; i++)
                _combinedKeywords[index++] = _groupKeywords[i];

            _combinedKeywordsDirty = false;

            return _combinedKeywords;
        }

        /// <summary>
        /// Writes the current keyword set to the FewTags config file under the
        /// "MenuDetectorKeywords" key, following the same JSON read/mutate/save
        /// pattern used elsewhere (e.g. AddToBlacklist).
        /// </summary>
        static void SaveKeywordsToConfig()
        {
            try
            {
                var json = JSON.JSON.Parse(
                    File.ReadAllText(FewTagsConfigLoader.ConfigPath));

                var arr = new JSON.JSONArray();

                foreach (string kw in _keywordSet)
                    arr.Add(kw);

                json["MenuDetectorKeywords"] = arr;

                File.WriteAllText(
                    FewTagsConfigLoader.ConfigPath,
                    json.ToString());
            }
            catch (Exception ex)
            {
                LogManager.LogErrorToConsole(
                    $"Failed To Save MenuDetector Keywords: {ex.Message}");
            }
        }

        public static void LoadKeywordsFromConfig()
        {
            try
            {
                if (!File.Exists(FewTagsConfigLoader.ConfigPath))
                    return;

                var json = JSON.JSON.Parse(
                    File.ReadAllText(FewTagsConfigLoader.ConfigPath));

                if (!json.HasKey("MenuDetectorKeywords"))
                {
                    LogManager.LogToConsole(
                        "[MenuDetector] 'MenuDetectorKeywords' missing from config — adding with default.");

                    SaveKeywordsToConfig();
                    return;
                }

                var arr = json["MenuDetectorKeywords"].AsArray;

                if (arr == null)
                    return;

                bool changed = false;

                foreach (var entry in arr.Children)
                {
                    string kw = entry?.Value;

                    if (string.IsNullOrWhiteSpace(kw))
                        continue;

                    kw = kw.Trim();

                    if (_keywordSet.Add(kw))
                        changed = true;
                }

                if (changed)
                {
                    _keywordsDirty = true;
                    _combinedKeywordsDirty = true;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogErrorToConsole(
                    $"Failed To Load MenuDetector Keywords: {ex.Message}");
            }
        }

        // 
        // Performs BOTH:
        //  - external module counting
        //  - group active detection
        // 
        void GetNameplateState(
            out int externalCount,
            out bool groupActive)
        {
            externalCount = 0;
            groupActive = false;

            if (player == null)
                return;

            var nameplate =
                Utils.GetPlayerNameplateContainer(player);

            if (nameplate == null)
                return;

            string[] externalKeywords =
                GetKeywordsSnapshot();

            if (externalKeywords == null ||
                externalKeywords.Length == 0)
                return;

            _externalMatchBuffer.Clear();

            Utils.RecursiveFindAllContaining(
                nameplate.transform,
                GetCombinedKeywords(),
                _externalMatchBuffer);

            for (int i = 0; i < _externalMatchBuffer.Count; i++)
            {
                Transform t = _externalMatchBuffer[i];

                if (t == null)
                    continue;

                GameObject obj = t.gameObject;

                if (!obj.activeInHierarchy)
                    continue;

                string objectName = obj.name;

                bool externalMatch = false;

                for (int k = 0; k < externalKeywords.Length; k++)
                {
                    if (objectName.IndexOf(
                            externalKeywords[k],
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        externalMatch = true;
                        break;
                    }
                }

                if (externalMatch)
                    externalCount++;

                if (!groupActive)
                {
                    for (int k = 0; k < _groupKeywords.Length; k++)
                    {
                        if (objectName.IndexOf(
                                _groupKeywords[k],
                                StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            groupActive = true;
                            break;
                        }
                    }
                }
            }
        }

        void GetCachedNameplateState(
            out int externalCount,
            out bool groupActive)
        {
            VRC.Player currentPlayer = player;

            if (currentPlayer == null)
            {
                _cachedExternalCount = 0;
                _cachedGroupActive = false;
                _cachedPlayer = null;

                externalCount = 0;
                groupActive = false;

                return;
            }

            float time = Time.time;

            if (_cachedPlayer != currentPlayer)
            {
                _cachedPlayer = currentPlayer;
                _lastStateCheck = -1f;
                _cachedExternalCount = 0;
                _cachedGroupActive = false;
            }

            if (_lastStateCheck < 0f ||
                time - _lastStateCheck >= STATE_CHECK_INTERVAL)
            {
                _lastStateCheck = time;

                GetNameplateState(
                    out _cachedExternalCount,
                    out _cachedGroupActive);
            }

            externalCount = _cachedExternalCount;
            groupActive = _cachedGroupActive;
        }

        public void Update()
        {
            if (FewTags.UnderNameplate)
                return;

            if (++frameCounter < CHECK_EVERY_FRAMES)
                return;

            frameCounter = 0;

            if (qm == null)
            {
                var raycaster = PlateFunctions.FindRaycaster(new string[] { "Canvas_QuickMenu" });
                if (raycaster == null) return;
                qm ??= raycaster.gameObject;

                if (qm == null) return;
            }

            if (qmcanvas == null)
            {
                qmcanvas = qm.GetComponent<Canvas>();

                if (qmcanvas == null)
                    return;
            }

            if (player == null)
                return;

            float targetOffset = GetCurrentOffset(qmcanvas);

            if (Mathf.Approximately(
                    currentOffset,
                    targetOffset))
                return;

            transform.localPosition =
                originalLocalPos +
                Vector3.up * targetOffset;

            currentOffset = targetOffset;
        }

        float GetCurrentOffset(Canvas canvas) // I don't like math I lost braincells lets not talk about it
        {
            if (canvas == null)
                return 0f;

            GetCachedNameplateState(
                out int externalCount,
                out bool groupActive);

            bool extended = IsExtendedInfo;

            VRC.Player localPlayer = VRC.Player.prop_Player_0;

            bool isLocal =
                localPlayer != null &&
                localPlayer == player;

            bool quickMenuEnabled = canvas.enabled;

            float offset = extended
                ? (externalCount * 100f) + 102f
                : (externalCount * 28f) - 182f;

            if (quickMenuEnabled) // when quick menu enabled...
            {
                if (extended)
                {
                    if (isLocal)
                        return offset;

                    if (groupActive) // beta with group active
                    {
                        if (isFriend)
                            return offset + 134f;

                        return offset + 148f; //274
                    }

                    // beta without group active
                    if (isFriend)
                        return offset + 90f;

                    return offset + 98f;
                }

                // non-beta
                if (groupActive) // non-beta with group active
                {
                    if (isLocal)
                        return offset + 100f;

                    return offset + 204f;
                }

                // non-beta without group active
                if (isLocal)
                    return offset + 20f;

                return offset + 124f;
            }

            // Quick menu closed
            if (!isLocal && extended)
            {
                if (!groupActive) // beta with group inactive and qm closed
                {
                    if (isFriend)
                        return offset - 11f;

                    return offset;
                }

                // beta with group active and qm closed
                if (isFriend)
                    return offset + 78f;

                return offset + 100f; //118
            }

            if (!isLocal && !extended && !groupActive) // non-beta without group active and qm closed
            {
                return offset + 94f;
            }

            return offset;
        }
    }
}
