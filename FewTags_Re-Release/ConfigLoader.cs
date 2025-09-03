using System.IO;
using FewTags.JSON;

namespace FewTags
{
    static class ConfigLoader
    {
        internal static readonly string ConfigPath = Path.Combine("FewTags", "config.json");

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
                        ["NoHTMLForMain"] = false,
                        ["LimitNewLineOrLength"] = false,
                        ["MaxNewlinesPerPlate"] = 4,
                        ["MaxPlatesPerUser"] = 30,
                        ["MaxPlateSize"] = 691,
                        ["FallbackSize"] = 80,
                        ["MaxTagLength"] = 25000,
                        ["UpdateIntervalMinutes"] = 1,
                        ["BlacklistedUserIDs"] = new JSONArray()
                    };

                    string directory = Path.GetDirectoryName(ConfigPath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    File.WriteAllText(ConfigPath, defaultConfig.ToString(2));
                }

                var json = JSON.JSON.Parse(File.ReadAllText(ConfigPath));
                if (json == null) return;

                Main.CleanseTags = json["CleanseTags"].AsBool;
                Main.ReplaceInsteadOfSkip = json["ReplaceInsteadOfSkip"].AsBool;
                Main.EnableAnimations = json["EnableAnimations"].AsBool;
                Main.isOverlay = json["IsOverlay"].AsBool;
                Main.NoHTMLForMain = json["NoHTMLForMain"].AsBool;
                Main.LimitNewLineOrLength = json["LimitNewLineOrLength"].AsBool;
                Main.DisableBigPlates = json["DisableBigPlates"].AsBool;
                Main.BeepOnReuploaderDetected = json["BeepOnReuploaderDetected"].AsBool;
                Main.MaxPlatesPerUser = json["MaxPlatesPerUser"].AsInt;
                Main.MaxNewlinesPerPlate = json["MaxNewlinesPerPlate"].AsInt;
                Main.MaxPlateSize = json["MaxPlateSize"].AsInt;
                Main.FallbackSize = json["FallbackSize"].AsInt;
                Main.MaxTagLength = json["MaxTagLength"].AsInt;
                Main.UpdateIntervalMinutes = json["UpdateIntervalMinutes"].AsInt;

                Main.BlacklistedUserIDs.Clear();
                if (json["BlacklistedUserIDs"] is JSONArray array)
                {
                    foreach (var id in array)
                        Main.BlacklistedUserIDs.Add(id.Value);
                }
            }
            catch
            {

            }
        }
    }

    internal static void Save() // save current settings to config
        {
            try
            {
                var config = new JSONObject
                {
                    ["CleanseTags"] = Main.CleanseTags,
                    ["ReplaceInsteadOfSkip"] = Main.ReplaceInsteadOfSkip,
                    ["EnableAnimations"] = Main.EnableAnimations,
                    ["IsOverlay"] = Main.isOverlay,
                    ["BeepOnReuploaderDetected"] = Main.BeepOnReuploaderDetected,
                    ["DisableBigPlates"] = Main.DisableBigPlates,
                    ["NoHTMLForMain"] = Main.NoHTMLForMain,
                    ["LimitNewLineOrLength"] = Main.LimitNewLineOrLength,
                    ["MaxNewlinesPerPlate"] = Main.MaxNewlinesPerPlate,
                    ["MaxPlatesPerUser"] = Main.MaxPlatesPerUser,
                    ["MaxPlateSize"] = Main.MaxPlateSize,
                    ["FallbackSize"] = Main.FallbackSize,
                    ["MaxTagLength"] = Main.MaxTagLength,
                    ["UpdateIntervalMinutes"] = Main.UpdateIntervalMinutes,
                    ["BlacklistedUserIDs"] = new JSONArray()
                };

                foreach (var userId in Main.BlacklistedUserIDs)
                {
                    config["BlacklistedUserIDs"].AsArray.Add(userId);
                }

                string directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(ConfigPath, config.ToString(2));
            }
            catch
            {

            }
        }
}

