using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace FewTags.FewTags
{
    public class TagAnimator : MonoBehaviour
    {
        private float rainbowTime = 0f;
        private float smoothRainbowTime = 0f;
        private float bounceTimer = 0f;
        private int bounceIndex = 0;
        private bool bounceForward = true;
        private int letterIndex = 0;
        private bool letterGoingForward = true;
        private float letterTimer = 0f;
        private float glitchTimer = 0f;
        private float glitchOffset = 0f;
        private int glitchCharIndex = 0;
        public float ScrollSpeed = 8f;
        private float scrollOffset = 0f;

        private TextPart[] cachedParts = null;
        private string cachedOriginalText = "";
        private int cachedVisibleLength = -1;


        public bool LetterByLetter = false, SmoothRainbow = false, Rainbow = false, Bounce = false, Jump = false, Pulse = false, Shake = false, GhostTrail = false, Blink = false, Glitch = false, Scroll = false;
        public string originalText = string.Empty;

        private const float RAINBOW_SPEED = 2.5f;
        private const float SMOOTH_RAINBOW_SPEED = 0.3125f;
        private const float BOUNCE_DELAY = 0.007f;
        private const float LETTER_DELAY = 0.45f;
        private const float JUMP_SPEED = 4f;
        private const float PULSE_SPEED = 4f;
        private const float SHAKE_SPEED = 30f;
        private const float GHOST_SPEED = 5f;
        private const float BLINK_SPEED = 1.5f;
        private const float GLITCH_SPEED = 1.96f;
        private const float GLITCH_INTENSITY = 6.75f;

        private static readonly Color32[] rainbowColors = new Color32[]
        {
            new Color32(255, 0, 0, 255),
            new Color32(255, 127, 0, 255),
            new Color32(255, 255, 0, 255),
            new Color32(0, 255, 0, 255),
            new Color32(0, 0, 255, 255),
            new Color32(75, 0, 130, 255),
            new Color32(148, 0, 211, 255),
        };

        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.004f;
        private int updateCounter = 0;
        private const int UPDATE_SKIP_THRESHOLD = 3;

        private static readonly Regex OpenSizeRegex = new Regex(@"<size=([+-]?\d+)%?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CloseSizeRegex = new Regex(@"</size>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TagRegex = new Regex(@"<[^>]+>|[^<]+", RegexOptions.Compiled);


        public void ResetAnimator()
        {
            LetterByLetter = false;
            Bounce = false;
            Rainbow = false;
            SmoothRainbow = false;
            Pulse = false;
            Jump = false;
            Shake = false;
            GhostTrail = false;
            Blink = false;
            Glitch = false;
            Scroll = false;

            rainbowTime = 0;
            smoothRainbowTime = 0;
            bounceTimer = 0;
            bounceIndex = 0;
            bounceForward = true;
            letterIndex = 0;
            letterGoingForward = true;
            letterTimer = 0;
            glitchTimer = 0;
            glitchOffset = 0;
            glitchCharIndex = 0;
            updateCounter = 0;
            lastUpdateTime = 0;
            scrollOffset = 0;
        }

        void Update()
        {
            if (Time.time - lastUpdateTime < UPDATE_INTERVAL) return;
            lastUpdateTime = Time.time;

            updateCounter++;
            if (updateCounter % UPDATE_SKIP_THRESHOLD == 0) return;

            if (updateCounter > 500) updateCounter = 0;

            var textComponent = this.gameObject.transform.Find("Trust Text")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent == null) return;

            if (LetterByLetter)
                LetterByLetterAnimation(textComponent, originalText.Replace(".LBL.", ""));

            if (Bounce)
                BounceAnimation(textComponent, originalText.Replace(".CYLN.", ""));

            if (Rainbow)
                RainbowAnimation(textComponent, Utils.RemoveHtmlTags(originalText, true).Replace(".RAIN.", ""));

            if (SmoothRainbow)
                SmoothRainbowAnimation(textComponent, Utils.RemoveHtmlTags(originalText, true).Replace(".SR.", ""));

            if (Pulse)
                PopPulseAnimation(textComponent, originalText.Replace(".PULSE.", ""));

            if (Jump)
                JumpAnimation(textComponent, originalText.Replace(".JUMP.", ""));

            if (Shake)
                ShakeAnimation(textComponent, originalText.Replace(".SHAKE.", ""));

            if (GhostTrail)
                GhostTrailAnimation(textComponent, originalText.Replace(".GT.", ""));

            if (Blink)
                BlinkAnimation(textComponent, Utils.RemoveHtmlTags(originalText.Replace(".BLINK.", ""), true));

            if (Glitch)
                GlitchAnimation(textComponent, originalText.Replace(".GLITCH.", ""));

            if (Scroll)
                ScrollAnimation(textComponent, originalText.Replace(".SCROLL.", ""));
        }

        private static StringBuilder GetStringBuilder(int capacity = 256)
        {
            return new StringBuilder(capacity);
        }

        private TextPart[] GetCachedParts(string text)
        {
            if (text != cachedOriginalText || cachedParts == null)
            {
                cachedParts = ParseTextWithTags(text);
                cachedOriginalText = text;
                cachedVisibleLength = 0;

                for (int i = 0; i < cachedParts.Length; i++)
                {
                    if (!cachedParts[i].IsStyled)
                        cachedVisibleLength += cachedParts[i].Text.Length;
                }
            }

            return cachedParts;
        }

        private string BuildVisibleTextWithTags(TextPart[] parts, int visibleCharCount)
        {
            int capacity = visibleCharCount * 2 + parts.Length * 3;
            StringBuilder sb = GetStringBuilder(capacity);

            int writtenChars = 0;
            Stack<string> tagStack = new Stack<string>();

            for (int i = 0; i < parts.Length; i++)
            {
                TextPart part = parts[i];

                if (part.IsStyled)
                {
                    sb.Append(part.Text);

                    if (!part.Text.StartsWith("</"))
                    {
                        tagStack.Push(ExtractTagName(part.Text));
                    }
                    else if (tagStack.Count > 0)
                    {
                        string tag = ExtractTagName(part.Text);
                        if (tagStack.Peek() == tag)
                            tagStack.Pop();
                    }
                }
                else
                {
                    int remaining = visibleCharCount - writtenChars;
                    if (remaining <= 0) break;

                    int charsToWrite = Mathf.Min(part.Text.Length, remaining);

                    for (int j = 0; j < charsToWrite; j++)
                        sb.Append(part.Text[j]);

                    writtenChars += charsToWrite;

                    if (charsToWrite < part.Text.Length)
                        break;
                }
            }

            while (tagStack.Count > 0)
            {
                string tag = tagStack.Pop();
                sb.Append("</");
                sb.Append(tag);
                sb.Append('>');
            }

            return sb.ToString();
        }

        private string ExtractTagName(string tag)
        {
            int start = tag.IndexOf('<') + 1;
            int end = tag.IndexOfAny(new char[] { ' ', '>', '\t' }, start);
            if (end < 0) end = tag.Length - 1;

            if (tag[start] == '/')
                start++;

            return tag.Substring(start, end - start).ToLower();
        }

        internal void ScrollAnimation(TextMeshProUGUI textMeshPro, string originalText, int minWindowChars = 14, int maxWindowChars = 26)
        {

            ///
            /// IDK I GIVE UP, IF YOU USE COLOR TAGS IN UR TEXT JUST ENSURE TO MAKE THINGS WHITE FOR TEXT U WANT WHITE LOL
            /// 

            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;

            string prefix = "";
            string textToScroll = originalText.Replace(".SCROLL.", "");

            // Handle [L] prefix
            if (textToScroll.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textToScroll = textToScroll.Substring(4);
            }

            if (!textToScroll.EndsWith(" ")) textToScroll += " ";

            var charList = new List<string>();
            var regex = new Regex(@"(<.*?>)|(.{1})", RegexOptions.Singleline | RegexOptions.Compiled);
            foreach (Match m in regex.Matches(textToScroll))
                charList.Add(m.Value);

            int totalChars = charList.Count;
            if (totalChars == 0) return;

            int visibleCharTotal = 0;
            foreach (var c in charList)
                if (!(c.StartsWith("<") && c.EndsWith(">")))
                    visibleCharTotal++;

            int targetWindowChars;
            if (visibleCharTotal <= minWindowChars)
                targetWindowChars = visibleCharTotal;
            else if (visibleCharTotal <= maxWindowChars)
                targetWindowChars = Mathf.Max(minWindowChars, Mathf.CeilToInt(visibleCharTotal * 0.7f));
            else
                targetWindowChars = maxWindowChars;

            scrollOffset += Time.unscaledDeltaTime * ScrollSpeed;
            int startIndex = Mathf.FloorToInt(scrollOffset) % totalChars;

            int windowSize = 0;
            int visibleCount = 0;
            for (int i = 0; i < totalChars; i++)
            {
                int idx = (startIndex + i) % totalChars;
                windowSize++;
                if (!(charList[idx].StartsWith("<") && charList[idx].EndsWith(">")))
                {
                    visibleCount++;
                    if (visibleCount >= targetWindowChars)
                        break;
                }
            }

            var activeTagStack = new Stack<(string tagName, string fullTag)>();
            for (int i = 0; i < startIndex; i++)
            {
                string item = charList[i];
                if (item.StartsWith("<") && item.EndsWith(">"))
                {
                    if (!item.StartsWith("</"))
                    {
                        string tagName = ExtractTagName(item);
                        activeTagStack.Push((tagName, item));
                    }
                    else
                    {
                        string tagName = ExtractTagName(item);
                        var temp = new Stack<(string, string)>();
                        bool found = false;
                        while (activeTagStack.Count > 0)
                        {
                            var top = activeTagStack.Pop();
                            if (top.tagName == tagName && !found)
                            {
                                found = true;
                                break;
                            }
                            temp.Push(top);
                        }
                        while (temp.Count > 0)
                            activeTagStack.Push(temp.Pop());
                    }
                }
            }

            bool hasColorTag = charList.Any(c => c.StartsWith("<color=") || c.StartsWith("<color "));
            bool insideColorAtStart = activeTagStack.Any(t => t.tagName == "color");

            var sb = new StringBuilder();
            sb.Append(prefix);

            if (!insideColorAtStart) sb.Append("<color=#ffffff>");

            var openTags = new Stack<string>();
            if (!insideColorAtStart)
                openTags.Push("color");

            var activeTagsList = activeTagStack.ToList();
            activeTagsList.Reverse();
            foreach (var tag in activeTagsList)
            {
                sb.Append(tag.fullTag);
                openTags.Push(tag.tagName);
            }

            for (int i = 0; i < windowSize; i++)
            {
                int idx = (startIndex + i) % totalChars;
                string item = charList[idx];

                if (item.StartsWith("<") && item.EndsWith(">"))
                {
                    if (!item.StartsWith("</"))
                    {
                        string tagName = ExtractTagName(item);
                        sb.Append(item);
                        openTags.Push(tagName);
                    }
                    else
                    {
                        string tagName = ExtractTagName(item);
                        sb.Append(item);

                        if (tagName == "color")
                        {
                            sb.Append("<color=#ffffff>");
                            openTags.Push("color");
                        }

                        var temp = new Stack<string>();
                        bool found = false;
                        while (openTags.Count > 0)
                        {
                            var top = openTags.Pop();
                            if (top == tagName && !found)
                            {
                                found = true;
                                break;
                            }
                            temp.Push(top);
                        }
                        while (temp.Count > 0)
                            openTags.Push(temp.Pop());
                    }
                }
                else
                {
                    sb.Append(item);
                }
            }

            if (!openTags.Contains("color"))
            {
                sb.Append("<color=#ffffff>");
                openTags.Push("color");
            }

            while (openTags.Count > 0)
            {
                string tagName = openTags.Pop();
                sb.Append($"</{tagName}>");
            }

            textMeshPro.SetTextSafe(sb.ToString(), true, true);
        }

        internal void LetterByLetterAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;

            string textForAnimation = originalText.Replace(".LBL.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            letterTimer += Time.unscaledDeltaTime;
            if (letterTimer < LETTER_DELAY) return;

            var parts = GetCachedParts(textForAnimation);

            if (letterGoingForward)
            {
                letterIndex++;
                if (letterIndex > cachedVisibleLength)
                {
                    letterIndex = cachedVisibleLength;
                    letterGoingForward = false;
                }
            }
            else
            {
                letterIndex--;
                if (letterIndex < 0)
                {
                    letterIndex = 0;
                    letterGoingForward = true;
                }
            }

            string result = BuildVisibleTextWithTags(parts, letterIndex);
            textMeshPro.SetTextSafe(prefix + result);

            letterTimer = 0f;
        }

        internal void JumpAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;

            string textForAnimation = originalText.Replace(".JUMP.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            var parts = GetCachedParts(textForAnimation);
            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            sb.Append(prefix);
            int visibleCharIndex = 0;
            float t = Time.time * JUMP_SPEED;
            int mid = cachedVisibleLength / 2;

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];
                if (part == null) continue;

                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                    continue;
                }

                for (int i = 0; i < part.Text.Length; i++)
                {
                    float offsetDistance = Mathf.Abs(visibleCharIndex - mid);
                    float voffset = Mathf.Sin(t - offsetDistance * 0.3f) * 6f;

                    sb.Append("<voffset=");
                    sb.Append(voffset.ToString("F1"));
                    sb.Append("px>");
                    sb.Append(part.Text[i]);
                    sb.Append("</voffset>");

                    visibleCharIndex++;
                }
            }

            textMeshPro.SetTextSafe(sb.ToString());
        }

        internal void BlinkAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;

            string textForAnimation = originalText.Replace(".BLINK.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            float alpha = Mathf.Abs(Mathf.Sin(Time.time * BLINK_SPEED * Mathf.PI));
            Color color = new Color(1f, 1f, 1f, alpha);
            string hex = ColorToHex(color);
            textMeshPro.SetTextSafe(prefix + $"<color=#{hex}>{textForAnimation}</color>");
        }

        internal void PopPulseAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;

            string textForAnimation = originalText.Replace(".PULSE.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            var parts = GetCachedParts(textForAnimation);
            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            sb.Append(prefix);
            float t = Time.time * PULSE_SPEED;
            int visibleCharIndex = 0;

            Stack<float> sizeStack = new Stack<float>();
            sizeStack.Push(1.0f);

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];
                if (part == null) continue;

                if (part.IsStyled)
                {
                    foreach (Match openTag in OpenSizeRegex.Matches(part.Text))
                    {
                        if (float.TryParse(openTag.Groups[1].Value, out float parsedSize))
                            sizeStack.Push(parsedSize / 30);
                    }

                    int closeTagCount = CloseSizeRegex.Matches(part.Text).Count;
                    for (int i = 0; i < closeTagCount; i++)
                    {
                        if (sizeStack.Count > 1) // never pop default size
                            sizeStack.Pop();
                    }

                    sb.Append(part.Text);
                }
                else
                {
                    float currentBaseSize = sizeStack.Peek();

                    for (int i = 0; i < part.Text.Length; i++)
                    {
                        float pulse = 1f + Mathf.Sin(t - visibleCharIndex * 0.3f) * 0.2f;
                        float finalSize = currentBaseSize * pulse;
                        sb.Append($"<size={(finalSize * 100):F0}%>{part.Text[i]}</size>");
                        visibleCharIndex++;
                    }
                }
            }

            textMeshPro.SetTextSafe(sb.ToString());
        }


        internal void ShakeAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;

            string textForAnimation = originalText.Replace(".SHAKE.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            var parts = GetCachedParts(textForAnimation);
            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            sb.Append(prefix);
            float t = Time.time * SHAKE_SPEED;
            int visibleCharIndex = 0;

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];
                if (part == null) continue;

                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                }
                else
                {
                    for (int i = 0; i < part.Text.Length; i++)
                    {
                        float rot = Mathf.Sin(t * 0.1f + visibleCharIndex * 0.2f) * 20f;
                        sb.Append($"<rotate={rot:F1}>{part.Text[i]}</rotate>");
                        visibleCharIndex++;
                    }
                }
            }

            textMeshPro.SetTextSafe(sb.ToString());
        }

        internal void GhostTrailAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;

            string textForAnimation = originalText.Replace(".GT.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            float t = Time.time * GHOST_SPEED;
            int estimatedCapacity = originalText.Length * 4;
            var sb = GetStringBuilder(estimatedCapacity);
            var parts = GetCachedParts(textForAnimation);
            sb.Append(prefix);
            int visibleIndex = 0;

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];
                if (part == null) continue;

                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                    continue;
                }

                for (int i = 0; i < part.Text.Length; i++)
                {
                    float pulse = Mathf.Clamp01(Mathf.Sin(t - visibleIndex * 0.3f) * 0.5f + 0.5f);
                    byte alpha = (byte)(pulse * 255f);

                    sb.Append("<color=#FFFFFF");
                    sb.Append(alpha.ToString("X2"));
                    sb.Append('>');
                    sb.Append(part.Text[i]);
                    sb.Append("</color>");

                    visibleIndex++;
                }
            }

            textMeshPro.SetTextSafe(sb.ToString());
        }

        internal void BounceAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            bounceTimer += Time.unscaledDeltaTime;
            if (bounceTimer < BOUNCE_DELAY) return;
            bounceTimer = 0f;

            string textForAnimation = originalText.Replace(".CYLN.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            var parts = GetCachedParts(textForAnimation);
            int estimatedCapacity = originalText.Length * 2;
            var sb = GetStringBuilder(estimatedCapacity);
            sb.Append(prefix); // always prepend local tag
            int charIndex = 0;
            string bounceRedHex = ColorUtility.ToHtmlStringRGB(Color.red);

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];
                if (part == null) continue;

                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                }
                else
                {
                    for (int j = 0; j < part.Text.Length; j++)
                    {
                        if (charIndex == bounceIndex)
                            sb.Append($"<color=#{bounceRedHex}>{part.Text[j]}</color>");
                        else
                            sb.Append(part.Text[j]);
                        charIndex++;
                    }
                }
            }

            textMeshPro.SetTextSafe(sb.ToString());

            bounceIndex += bounceForward ? 1 : -1;

            if (bounceIndex >= cachedVisibleLength)
            {
                bounceIndex = cachedVisibleLength - 1;
                bounceForward = false;
            }
            else if (bounceIndex < 0)
            {
                bounceIndex = 0;
                bounceForward = true;
            }
        }

        internal void RainbowAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null) return;

            string textForAnimation = originalText.Replace(".RAIN.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            int colorCount = rainbowColors.Length;
            rainbowTime += Time.unscaledDeltaTime * RAINBOW_SPEED;
            rainbowTime %= 1.0f;

            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            var parts = GetCachedParts(textForAnimation);
            sb.Append(prefix);

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];
                if (part == null) continue;

                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                }
                else
                {
                    for (int i = 0; i < part.Text.Length; i++)
                    {
                        int colorIndex = (i + Mathf.FloorToInt(rainbowTime * colorCount)) % colorCount;
                        Color32 color = rainbowColors[colorIndex];
                        sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{part.Text[i]}</color>");
                    }
                }
            }

            textMeshPro.SetTextSafe(sb.ToString());
        }

        internal void SmoothRainbowAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;

            string textForAnimation = originalText.Replace(".SR.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            smoothRainbowTime += Time.unscaledDeltaTime * SMOOTH_RAINBOW_SPEED;
            smoothRainbowTime %= 1f;

            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            var parts = GetCachedParts(textForAnimation);
            sb.Append(prefix);

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];
                if (part == null) continue;

                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                }
                else
                {
                    int len = Mathf.Max(1, part.Text.Length);

                    for (int i = 0; i < part.Text.Length; i++)
                    {
                        float hue = Mathf.Repeat(smoothRainbowTime + (float)i / len, 1f);
                        Color color = Color.HSVToRGB(hue, 1f, 1f);
                        string hex = ColorToHex(color);
                        sb.Append($"<color=#{hex}>{part.Text[i]}</color>");
                    }
                }
            }

            textMeshPro.SetTextSafe(sb.ToString());
        }

        internal void GlitchAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;

            if (updateCounter % 4 == 0) return;

            string textForAnimation = originalText.Replace(".GLITCH.", "");
            string prefix = "";

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4); // remove "[L] " from animated portion
            }

            glitchTimer += Time.unscaledDeltaTime * GLITCH_SPEED;

            var parts = GetCachedParts(textForAnimation);
            int estimatedCapacity = originalText.Length * 6;
            var sb = GetStringBuilder(estimatedCapacity);
            sb.Append(prefix);
            int visibleCharIndex = 0;

            float glitchNoise = Mathf.PerlinNoise(glitchTimer * 0.5f, 0f);
            float glitchNoise2 = Mathf.PerlinNoise(glitchTimer * 0.7f, 100f);
            float glitchNoise3 = Mathf.PerlinNoise(glitchTimer * 0.3f, 200f);

            bool shouldGlitch = glitchNoise > 0.3f;
            bool shouldCorrupt = glitchNoise2 > 0.6f;
            bool shouldShift = glitchNoise3 > 0.4f;
            bool shouldFlicker = glitchNoise > 0.5f;
            bool shouldBold = glitchNoise2 > 0.7f;
            bool shouldRotate = glitchNoise3 > 0.6f;
            bool shouldSize = glitchNoise > 0.65f;

            float random1 = UnityEngine.Random.Range(0f, 1f);
            float random2 = UnityEngine.Random.Range(0f, 1f);
            float random3 = UnityEngine.Random.Range(0f, 1f);
            float random4 = UnityEngine.Random.Range(0f, 1f);

            if (shouldCorrupt)
            {
                glitchCharIndex = (glitchCharIndex + 1) % Mathf.Max(1, cachedVisibleLength);
            }

            if (shouldShift)
            {
                glitchOffset = Mathf.Sin(glitchTimer * 1.5f) * GLITCH_INTENSITY * 0.5f;
            }
            else
            {
                glitchOffset = Mathf.Lerp(glitchOffset, 0f, Time.unscaledDeltaTime * 8f);
            }

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];
                if (part == null) continue;

                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                }
                else
                {
                    for (int i = 0; i < part.Text.Length; i++)
                    {
                        char currentChar = part.Text[i];
                        bool hasColorTag = false;
                        bool hasPositionTag = false;
                        bool hasRotationTag = false;
                        bool hasSizeTag = false;
                        bool hasBoldTag = false;

                        float charRandom = (random1 + visibleCharIndex * 0.1f) % 1f;
                        float charRandom2 = (random2 + visibleCharIndex * 0.15f) % 1f;
                        float charRandom3 = (random3 + visibleCharIndex * 0.2f) % 1f;
                        float charRandom4 = (random4 + visibleCharIndex * 0.25f) % 1f;

                        if (shouldCorrupt && visibleCharIndex == glitchCharIndex)
                        {
                            string glitchChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?~`";
                            currentChar = glitchChars[Mathf.FloorToInt(glitchTimer * 15f) % glitchChars.Length];
                        }

                        if (shouldGlitch && charRandom < 0.25f)
                        {
                            string glitchChars = "0123456789ABCDEF!@#$%^&*()_+-=[]{}|;':\",./<>?~`";
                            currentChar = glitchChars[Mathf.FloorToInt(charRandom * glitchChars.Length)];
                        }

                        if (shouldCorrupt && visibleCharIndex == glitchCharIndex)
                        {
                            Color glitchColor = new Color(
                                0.5f + charRandom * 0.5f,
                                charRandom2 * 0.5f,
                                0.5f + charRandom3 * 0.5f,
                                1f
                            );
                            string hex = ColorToHex(glitchColor);
                            sb.Append($"<color=#{hex}>");
                            hasColorTag = true;
                        }
                        else if (shouldGlitch && charRandom < 0.4f)
                        {
                            Color glitchColor = new Color(
                                0.8f + charRandom * 0.2f,
                                charRandom2 * 0.2f,
                                0.8f + charRandom3 * 0.2f,
                                1f
                            );
                            string hex = ColorToHex(glitchColor);
                            sb.Append($"<color=#{hex}>");
                            hasColorTag = true;
                        }

                        if (shouldShift || (Mathf.Abs(glitchOffset) > 0.1f))
                        {
                            float yOffset = Mathf.Cos(visibleCharIndex * 0.3f + glitchTimer * 1.5f) * glitchOffset * 0.3f;
                            sb.Append($"<voffset={yOffset:F1}px>");
                            hasPositionTag = true;
                        }

                        if (shouldRotate || (shouldGlitch && charRandom2 < 0.3f))
                        {
                            float rotation = -45f + charRandom * 90f;
                            sb.Append($"<rotate={rotation:F1}>");
                            hasRotationTag = true;
                        }

                        if (shouldSize || (shouldGlitch && charRandom3 < 0.25f))
                        {
                            float size = 0.5f + charRandom * 1.5f;
                            sb.Append($"<size={(size * 100):F0}%>");
                            hasSizeTag = true;
                        }

                        if (shouldFlicker || (shouldGlitch && charRandom4 < 0.35f))
                        {
                            float alpha = 0.2f + charRandom * 0.8f;
                            Color alphaColor = new Color(1f, 1f, 1f, alpha);
                            string hex = ColorToHex(alphaColor);
                            sb.Append($"<color=#{hex}>");
                            hasColorTag = true;
                        }

                        if (shouldBold || (shouldGlitch && charRandom < 0.2f))
                        {
                            sb.Append("<b>");
                            hasBoldTag = true;
                        }

                        sb.Append(currentChar);

                        if (hasSizeTag)
                        {
                            sb.Append("</size>");
                        }
                        if (hasRotationTag)
                        {
                            sb.Append("</rotate>");
                        }
                        if (hasPositionTag)
                        {
                            sb.Append("</voffset>");
                        }
                        if (hasBoldTag)
                        {
                            sb.Append("</b>");
                        }
                        if (hasColorTag)
                        {
                            sb.Append("</color>");
                        }

                        visibleCharIndex++;
                    }
                }
            }

            textMeshPro.SetTextSafe(sb.ToString());
        }

        private string ColorToHex(Color color)
        {
            byte r = (byte)(Mathf.Clamp01(color.r) * 255f);
            byte g = (byte)(Mathf.Clamp01(color.g) * 255f);
            byte b = (byte)(Mathf.Clamp01(color.b) * 255f);
            byte a = (byte)(Mathf.Clamp01(color.a) * 255f);
            return $"{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        private TextPart[] ParseTextWithTags(string text)
        {
            var matches = TagRegex.Matches(text);
            int count = matches.Count;
            TextPart[] parts = new TextPart[count];

            for (int i = 0; i < count; i++)
            {
                string value = matches[i].Value;
                parts[i] = new TextPart
                {
                    Text = value,
                    IsStyled = value.StartsWith("<") && value.EndsWith(">")
                };
            }

            return parts;
        }

        private class TextPart
        {
            public string Text { get; set; }
            public bool IsStyled { get; set; }
        }

        void OnDestroy()
        {
            cachedParts = null;
            cachedOriginalText = "";
            cachedVisibleLength = -1;
            lastUpdateTime = 0;
            updateCounter = 0;
            originalText = string.Empty;
        }
    }
}
