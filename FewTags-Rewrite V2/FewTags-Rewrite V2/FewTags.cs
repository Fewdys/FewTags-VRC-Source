using System.Collections.Generic;

namespace FewTags.FewTags
{
    /// <summary>
    /// This Is Pretty Much Just References Used In Other Classes Where Need Be Nowadays -> Consider Looking At Loader.cs
    /// To Those Of You Who Have No Idea What You're Doing... Just Simply Fix You're References And Learn The Absolute Basics And You Should Be Able To Build This For Whatever Loader
    /// </summary>
    public class FewTags
    {
        internal static int MaxPlatesPerUser, MaxNewlinesPerPlate, MaxPlateSize, FallbackSize, MaxTagLength, UpdateIntervalMinutes;
        internal static bool CleanseTags, ReplaceInsteadOfSkip, BeepOnReuploaderDetected, EnableAnimations, isOverlay, DisableBigPlates, NoHTMLForMain, FewTagsEnabled, LimitNewLineOrLength;
        internal static HashSet<string> BlacklistedUserIDs = new HashSet<string>();

        internal const float Position = -126.95f, PositionTags = -154.95f, PositionID = -98.95f, PositionBigText = 344.75f;

        internal static List<VRC.Player> p = new List<VRC.Player>();

        internal static Dictionary<string, List<PlateStatic>> playerStaticPlates = new();
        internal static Dictionary<string, List<Plate>> playerPlates = new();

        internal static Jsons.Json._Tags s_tags { get; set; }
        internal static string s_rawTags { get; set; }
        internal static bool SnaxyTagsLoaded { get; set; } // perhaps still around and yea :3

        internal const string
            MaliciousStr = "<color=#ff0000>Malicious User</color>",
            FewTagsStr = "<b><color=#ff0000>-</color> <color=#ff7f00>F</color><color=#ffff00>e</color><color=#80ff00>w</color><color=#00ff00>T</color><color=#00ff80>a</color><color=#00ffff>g</color><color=#0000ff>s</color> <color=#8b00ff>-</color><color=#ffffff></b>",
            TooLargeStr = "Plate Length To Long!",
            TooManyLines = "<color=#ff0000>Plate Contains Too Many Newlines!</color>";
    }
}
