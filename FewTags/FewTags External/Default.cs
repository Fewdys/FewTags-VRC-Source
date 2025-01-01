using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FewTags
{
    internal class Default
    {
        private static string? id;
        private static string? UserID;
        private static string[]? Tag;
        private static string? PlateBigText;
        private static string? Malicious;
        private static long LastReadOffset;

        private static FewTags.Json._Tags? s_tags { get; set; }
        public static string? s_rawTags { get; set; }
        public static FewTags.Json.Tags[]? s_tagsArr { get; set; }

        private static void UpdateTags()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Fetching Tags...");
                using (WebClient webClient = new WebClient())
                {
                    s_rawTags = webClient.DownloadString("https://raw.githubusercontent.com/Fewdys/FewTags/main/ExternalTags.json");
                    if (!string.IsNullOrEmpty(s_rawTags))
                    {
                        s_tags = JsonConvert.DeserializeObject<FewTags.Json._Tags>(s_rawTags);
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("Finished Fetching Tags");
                        Console.ResetColor();
                        Console.WriteLine("Please Note: Colors May Not Be Correct Do To Limitations Of Console");
                    }
                    else
                    {
                        Console.WriteLine("Failed to fetch tags: Response is null or empty.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while updating tags: {ex.Message}");
            }
        }

        private static Random random = new Random();

        static void UpdateConsoleTitle()
        {
            // Choose a random title from the list
            string newTitle = GenerateRandomString(GetRandomNumber(13, 36));

            // Change the console title
            Console.Title = newTitle;
        }

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        public static int GetRandomNumber(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue + 1);
        }

        public static void Main()
        {
            //Randomize Console Title
            UpdateConsoleTitle();
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            UpdateTags();
            ScanLog();
        }

        private static void ScanLog()
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low\\VRChat\\VRChat");
                if (directoryInfo == null || !directoryInfo.Exists) return;
                FileInfo fileInfo = null;
                foreach (FileInfo file in directoryInfo.GetFiles("output_log_*.txt", SearchOption.TopDirectoryOnly))
                {
                    if (fileInfo == null || file.LastWriteTime.CompareTo(fileInfo.LastWriteTime) >= 0)
                        fileInfo = file;
                }
                if (fileInfo == null) return;
                Process[] processesByName = Process.GetProcessesByName("VRChat");
                if (processesByName == null || processesByName.Length == 0) return;
                Process process = processesByName[0];
                ReadNewLines(fileInfo.FullName);
                while (!process.HasExited)
                {
                    ReadLog(fileInfo.FullName);
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while scanning log: {ex.Message}");
            }
        }

        private static void ReadLog(string Path)
        {
            var lines = ReadNewLines(Path);
            try
            {
                foreach (var line in lines)
                {
                    if (line.Contains("OnPlayerJoined "))
                    {
                        //Randomize Console Title
                        UpdateConsoleTitle();
                        string[] parts = line.Split(new[] { "OnPlayerJoined " }, StringSplitOptions.None);
                        string DisplayName = parts[1].Trim();

                        // Check if the username is found in rawtags
                        if (s_rawTags.Contains(DisplayName))
                        {
                            //Randomize Console Title
                            UpdateConsoleTitle();
                            s_tagsArr = s_tags.records.Where(x => x.DisplayName == DisplayName).ToArray();
                            foreach (var tag in s_tagsArr)
                            {
                                id = tag.id.ToString();
                                UserID = tag.UserID;
                                PlateBigText = tag.PlateBigText;
                                Malicious = tag.Malicious.ToString();
                                Tag = tag.Tag;

                                if ((bool)tag.Active)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] [FewTags] {DisplayName} ({UserID}), Tags Found:");
                                    Console.ForegroundColor = ConsoleColor.Magenta;
                                    Console.WriteLine($"[FewTags] ID: {id}");
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"[FewTags] Malicious: {Malicious}");
                                    Console.ResetColor();
                                    if ((bool)tag.BigTextActive && PlateBigText != null)
                                    {
                                        // Replace <b>, <i>, </b>, </i> with empty strings
                                        string processedTag = Regex.Replace(PlateBigText, @"<\/?b>|<\/?i>|</color>", "");

                                        // Replace color tags and perform console color change
                                        ProcessAndPrintTag(processedTag);
                                    }
                                    if (Tag != null && Tag.Length > 0)
                                    {
                                        foreach (var _tag in Tag)
                                        {
                                            // Replace <b>, <i>, </b>, </i> with empty strings
                                            string processedTag = Regex.Replace(_tag, @"<\/?b>|<\/?i>|</color>", "");

                                            // Replace color tags and perform console color change
                                            ProcessAndPrintTag(processedTag);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("[FewTags] No Tags");
                                    }
                                    Console.WriteLine();
                                }
                                Console.ResetColor();
                            }
                        }
                    }
                    if (line.Contains("OnPlayerLeft "))
                    {
                        //Randomize Console Title
                        UpdateConsoleTitle();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading log: {ex.Message}");
            }
        }

        private static List<string> ReadNewLines(string filePath)
        {
            List<string> lines = new();

            try
            {
                if (filePath == null)
                {
                    Console.WriteLine("File path is null.");
                }

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        streamReader.BaseStream.Seek(LastReadOffset, SeekOrigin.Begin);
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            if (line.Contains("User Authenticated:"))
                            {
                                string pattern = "Authenticated:\\s+(.*?)\\s+\\(";
                                Match match = Regex.Match(line, pattern);
                                if (match.Success)
                                {
                                    string name = match.Groups[1].Value;
                                    if (s_rawTags.Contains(name))
                                    {
                                        s_tagsArr = s_tags.records.Where(x => x.DisplayName == name).ToArray();
                                        foreach (var tag in s_tagsArr)
                                        {
                                            id = tag.id.ToString();
                                            UserID = tag.UserID;
                                            Tag = tag.Tag;
                                            PlateBigText = tag.PlateBigText;
                                            Malicious = tag.Malicious.ToString();
                                            if ((bool)tag.Active)
                                            {
                                                Console.ForegroundColor = ConsoleColor.Yellow;
                                                Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] [FewTags] Welcome {name} ({UserID}), Tags Found:");
                                                Console.ForegroundColor = ConsoleColor.Magenta;
                                                Console.WriteLine($"[FewTags] ID: {id}");
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine($"[FewTags] Malicious: {Malicious}");
                                                Console.ResetColor();
                                                if ((bool)tag.BigTextActive && PlateBigText != null)
                                                {
                                                    // Replace <b>, <i>, </b>, </i> with empty strings
                                                    string processedTag = Regex.Replace(PlateBigText, @"<\/?b>|<\/?i>|</color>", "");

                                                    // Replace color tags and perform console color change
                                                    ProcessAndPrintTag(processedTag);
                                                }
                                                if (Tag != null && Tag.Length > 0)
                                                {
                                                    foreach (var _tag in Tag)
                                                    {
                                                        // Replace <b>, <i>, </b>, </i> with empty strings
                                                        string processedTag = Regex.Replace(_tag, @"<\/?b>|<\/?i>|</color>", "");

                                                        // Replace color tags and perform console color change
                                                        ProcessAndPrintTag(processedTag);
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine("[FewTags] No Tags");
                                                }
                                                Console.WriteLine();
                                            }
                                            Console.ResetColor();
                                        }
                                    }
                                    else
                                    {

                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"[FewTags] Welcome {name}");
                                        Console.ForegroundColor = ConsoleColor.Magenta;
                                        Console.WriteLine($"[FewTags] {name} Was Not Found In The Database (No Tags)");
                                    }
                                }
                            }
                            lines.Add(line);
                        }
                        LastReadOffset = streamReader.BaseStream.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading new lines: {ex.Message}");
            }

            return lines;
        }



        static void DoConsoleColor(string hexColor)
        {
            // Convert hexadecimal color to ConsoleColor
            ConsoleColor bgColor = GetConsoleColorFromHex(hexColor);
            Console.BackgroundColor = bgColor;
            if (bgColor == ConsoleColor.Black)
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Black;
            }
        }

        static void ProcessAndPrintTag(string tag)
        {
            // Match color tags
            MatchCollection colorMatches = Regex.Matches(tag, @"<color=#[0-9A-Fa-f]{6}>|</color>");
            List<string> hexColors = new List<string>();

            // Process each color tag
            foreach (Match colorMatch in colorMatches)
            {
                if (colorMatch.Value.StartsWith("<color="))
                {
                    string hexColor = colorMatch.Value.Replace("<color=", "").Replace(">", "");
                    hexColors.Add(hexColor);
                }
                else
                {
                    // Reset console color
                    Console.ResetColor();
                }
            }

            // Remove color tags from the tag
            tag = Regex.Replace(tag, @"<\/?color=#[0-9A-Fa-f]{6}>", "");

            // Print the tag with color changes
            foreach (string hexColor in hexColors)
            {
                // Set console color based on hex color
                DoConsoleColor(hexColor);
            }

            // Print the tag without color tags
            Console.WriteLine($"[FewTags] {tag}");
        }

        static ConsoleColor GetConsoleColorFromHex(string hexColor)
        {
            // Convert hexadecimal color to RGB
            System.Drawing.Color color = System.Drawing.ColorTranslator.FromHtml(hexColor);

            // Match to the closest ConsoleColor
            ConsoleColor closestColor = ConsoleColor.Black;
            double minDistance = double.MaxValue;

            foreach (ConsoleColor enumColor in Enum.GetValues(typeof(ConsoleColor)))
            {
                System.Drawing.Color consoleColor = System.Drawing.Color.FromName(enumColor.ToString());
                double distance = Math.Sqrt(Math.Pow(color.R - consoleColor.R, 2) + Math.Pow(color.G - consoleColor.G, 2) + Math.Pow(color.B - consoleColor.B, 2));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestColor = enumColor;
                }
            }

            return closestColor;
        }
    }
}
