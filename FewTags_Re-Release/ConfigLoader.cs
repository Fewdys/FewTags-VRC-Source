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
                        ["MaxNewlinesPerPlate"] = 4,
                        ["MaxPlatesPerUser"] = 30,
                        ["MaxPlateSize"] = 691,
                        ["FallbackSize"] = 80,
                        ["MaxTagLength"] = 25000,
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
                Main.DisableBigPlates = json["DisableBigPlates"].AsBool;
                Main.BeepOnReuploaderDetected = json["BeepOnReuploaderDetected"].AsBool;
                Main.MaxPlatesPerUser = json["MaxPlatesPerUser"].AsInt;
                Main.MaxNewlinesPerPlate = json["MaxNewlinesPerPlate"].AsInt;
                Main.MaxPlateSize = json["MaxPlateSize"].AsInt;
                Main.FallbackSize = json["FallbackSize"].AsInt;
                Main.MaxTagLength = json["MaxTagLength"].AsInt;

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
}
