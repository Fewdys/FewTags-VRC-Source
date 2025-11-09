using FewTags.FewTags.JSON;

namespace FewTags.FewTags
{
    internal class FewTagsConfigLoader
    {
        private static readonly string ConfigPath = Path.Combine("FewTags", "config.json");

        /// <summary>
        /// Loads The FewTags Config.
        /// </summary>
        internal static void Load() // load me on startup
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    var defaultConfig = new JSONObject
                    {
                        ["CleanseTags"] = true,
                        ["ReplaceInsteadOfSkip"] = true,
                        ["EnableAnimations"] = true,
                        ["IsOverlay"] = false,
                        ["BeepOnReuploaderDetected"] = false,
                        ["DisableBigPlates"] = false,
                        ["FewTagsEnabled"] = true,
                        ["NoHTMLForMain"] = false,
                        ["SendTaggedIdiots"] = false,
                        ["LimitNewLineOrLength"] = false,
                        ["MaxNewlinesPerPlate"] = 4,
                        ["MaxPlatesPerUser"] = 30,
                        ["MaxPlateSize"] = 691,
                        ["FallbackSize"] = 80,
                        ["MaxTagLength"] = 5000,
                        ["UpdateIntervalMinutes"] = 1,
                        ["BlacklistedUserIDs"] = new JSONArray()
                    };

                    string directory = Path.GetDirectoryName(ConfigPath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    File.WriteAllText(ConfigPath, defaultConfig.ToString(2));
                }

                var json = JSON.Parse(File.ReadAllText(ConfigPath));
                if (json == null) return;

                FewTags.CleanseTags = json["CleanseTags"].AsBool;
                FewTags.ReplaceInsteadOfSkip = json["ReplaceInsteadOfSkip"].AsBool;
                FewTags.EnableAnimations = json["EnableAnimations"].AsBool;
                FewTags.isOverlay = json["IsOverlay"].AsBool;
                FewTags.BeepOnReuploaderDetected = json["BeepOnReuploaderDetected"].AsBool;
                FewTags.NoHTMLForMain = json["NoHTMLForMain"].AsBool;
                FewTags.DisableBigPlates = json["DisableBigPlates"].AsBool;
                FewTags.FewTagsEnabled = json["FewTagsEnabled"].AsBool;
                FewTags.SendTaggedIdiots = json["SendTaggedIdiots"].AsBool;
                FewTags.LimitNewLineOrLength = json["LimitNewLineOrLength"].AsBool;
                FewTags.MaxPlatesPerUser = json["MaxPlatesPerUser"].AsInt;
                FewTags.MaxNewlinesPerPlate = json["MaxNewlinesPerPlate"].AsInt;
                FewTags.MaxPlateSize = json["MaxPlateSize"].AsInt;
                FewTags.FallbackSize = json["FallbackSize"].AsInt;
                FewTags.MaxTagLength = json["MaxTagLength"].AsInt;
                FewTags.UpdateIntervalMinutes = json["UpdateIntervalMinutes"].AsInt;

                FewTags.BlacklistedUserIDs.Clear();
                if (json["BlacklistedUserIDs"] is JSONArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        FewTags.BlacklistedUserIDs.Add(array[i].Value);
                    }
                }

                //LogManager.LogToConsole($"FewTags Config Loaded!");
            }
            catch (Exception ex)
            {
                LogManager.LogErrorToConsole($"{ex}");
            }
        }

        /// <summary>
        /// Saves The FewTags Config.
        /// </summary>
        internal static void Save() // save current settings to config
        {
            try
            {
                var config = new JSONObject
                {
                    ["CleanseTags"] = FewTags.CleanseTags,
                    ["ReplaceInsteadOfSkip"] = FewTags.ReplaceInsteadOfSkip,
                    ["EnableAnimations"] = FewTags.EnableAnimations,
                    ["IsOverlay"] = FewTags.isOverlay,
                    ["BeepOnReuploaderDetected"] =  FewTags.BeepOnReuploaderDetected,
                    ["DisableBigPlates"] = FewTags.DisableBigPlates,
                    ["FewTagsEnabled"] = FewTags.FewTagsEnabled,
                    ["NoHTMLForMain"] = FewTags.NoHTMLForMain,
                    ["SendTaggedIdiots"] = FewTags.SendTaggedIdiots,
                    ["LimitNewLineOrLength"] = FewTags.LimitNewLineOrLength,
                    ["MaxNewlinesPerPlate"] = FewTags.MaxNewlinesPerPlate,
                    ["MaxPlatesPerUser"] = FewTags.MaxPlatesPerUser,
                    ["MaxPlateSize"] = FewTags.MaxPlateSize,
                    ["FallbackSize"] = FewTags.FallbackSize,
                    ["MaxTagLength"] = FewTags.MaxTagLength,
                    ["UpdateIntervalMinutes"] = FewTags.UpdateIntervalMinutes,
                    ["BlacklistedUserIDs"] = new JSONArray()
                };

                var blacklisted = FewTags.BlacklistedUserIDs.ToList();
                for (int i = 0; i < blacklisted.Count; i++)
                {
                    config["BlacklistedUserIDs"].AsArray.Add(blacklisted[i]);
                }

                string directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(ConfigPath, config.ToString(2));

                Load();
            }
            catch
            {

            }
        }
    }
}
