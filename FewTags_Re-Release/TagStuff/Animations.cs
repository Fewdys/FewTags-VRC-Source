using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace FewTags.TagStuff
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
        
        private List<TextPart> cachedParts = null;
        private string cachedOriginalText = "";
        private int cachedVisibleLength = -1;
        
        
        public bool LetterByLetter = false, SmoothRainbow = false, Rainbow = false, Bounce = false, Jump = false, Pulse = false, Shake = false, GhostTrail = false, Blink = false, Glitch = false;
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
        private const float UPDATE_INTERVAL = 0.010f;
        private int updateCounter = 0;
        private const int UPDATE_SKIP_THRESHOLD = 3;
        
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
                RainbowAnimation(textComponent, Main.RemoveHtmlTags(originalText).Replace(".RAIN.", ""));
    
            if (SmoothRainbow)
                SmoothRainbowAnimation(textComponent, Main.RemoveHtmlTags(originalText).Replace(".SR.", ""));
    
            if (Pulse)
                PopPulseAnimation(textComponent, originalText.Replace(".PULSE.", ""));
    
            if (Jump)
                JumpAnimation(textComponent, originalText.Replace(".JUMP.", ""));
    
            if (Shake)
                ShakeAnimation(textComponent, originalText.Replace(".SHAKE.", ""));
    
            if (GhostTrail)
                GhostTrailAnimation(textComponent, originalText.Replace(".GT.", ""));
    
            if (Blink)
                BlinkAnimation(textComponent, Main.RemoveHtmlTags(originalText.Replace(".BLINK.", ""), true));
    
            if (Glitch)
                GlitchAnimation(textComponent, originalText.Replace(".GLITCH.", ""));
        }
    
        private static StringBuilder GetStringBuilder(int capacity = 256)
        {
            return new StringBuilder(capacity);
        }
    
    
    
        private List<TextPart> GetCachedParts(string text)
        {
            if (text != cachedOriginalText || cachedParts == null)
            {
                cachedParts = ParseTextWithTags(text);
                cachedOriginalText = text;
                cachedVisibleLength = cachedParts.Where(p => !p.IsStyled).Sum(p => p.Text.Length);
            }
            return cachedParts;
        }
    
        private static string BuildVisibleTextWithTags(List<TextPart> parts, int visibleCharCount)
        {
            int estimatedCapacity = visibleCharCount * 2 + parts.Count * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            int visibleSoFar = 0;
            var openTags = new Stack<string>();
    
            foreach (var part in parts)
            {
                if (part.IsStyled)
                {
                    sb.Append(part.Text);
    
                    if (!part.Text.StartsWith("</"))
                    {
                        string tagName = ExtractTagName(part.Text);
                        openTags.Push(tagName);
                    }
                    else
                    {
                        if (openTags.Count > 0)
                        {
                            string closingTagName = ExtractTagName(part.Text);
                            if (openTags.Peek() == closingTagName)
                                openTags.Pop();
                        }
                    }
                }
                else
                {
                    int remaining = visibleCharCount - visibleSoFar;
                    if (remaining <= 0)
                        break;
    
                    int count = Mathf.Min(part.Text.Length, remaining);
                    sb.Append(part.Text.Substring(0, count));
                    visibleSoFar += count;
    
                    if (count < part.Text.Length)
                        break;
                }
            }
    
            while (openTags.Count > 0)
            {
                string tagName = openTags.Pop();
                sb.Append($"</{tagName}>");
            }
    
            string result = sb.ToString();
            return result;
        }
    
        private static string ExtractTagName(string tag)
        {
            int start = tag.IndexOf('<') + 1;
            int end = tag.IndexOfAny(new char[] { ' ', '>', '\t' }, start);
            if (end < 0) end = tag.Length - 1;
    
            if (tag[start] == '/')
                start++;
    
            return tag.Substring(start, end - start).ToLower();
        }
    
        internal void LetterByLetterAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;
    
            letterTimer += Time.deltaTime;
            if (letterTimer < LETTER_DELAY) return;
    
            var parts = GetCachedParts(originalText);
    
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
            textMeshPro.text = result;
    
            letterTimer = 0f;
        }
    
        internal void JumpAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;
    
            var parts = GetCachedParts(originalText);
            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            int visibleCharIndex = 0;
            float t = Time.time * JUMP_SPEED;
            int mid = cachedVisibleLength / 2;
    
            foreach (var part in parts)
            {
                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                }
                else
                {
                    for (int i = 0; i < part.Text.Length; i++)
                    {
                        float dist = Mathf.Abs(visibleCharIndex - mid);
                        float intensity = Mathf.Sin(t - dist * 0.3f) * 6f;
                        sb.Append($"<voffset={intensity:F1}px>{part.Text[i]}</voffset>");
                        visibleCharIndex++;
                    }
                }
            }
    
            textMeshPro.text = sb.ToString();
        }
    
        internal void BlinkAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;
    
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * BLINK_SPEED * Mathf.PI));
            Color color = new Color(1f, 1f, 1f, alpha);
            string hex = ColorToHex(color);
            textMeshPro.text = $"<color=#{hex}>{originalText}</color>";
        }
    
        internal void PopPulseAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;
    
            var parts = GetCachedParts(originalText);
            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            float t = Time.time * PULSE_SPEED;
            int visibleCharIndex = 0;
    
            foreach (var part in parts)
            {
                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                }
                else
                {
                    for (int i = 0; i < part.Text.Length; i++)
                    {
                        float scale = 1f + Mathf.Sin(t - visibleCharIndex * 0.3f) * 0.2f;
                        sb.Append($"<size={(scale * 100):F0}%>{part.Text[i]}</size>");
                        visibleCharIndex++;
                    }
                }
            }
    
            textMeshPro.text = sb.ToString();
        }
    
        internal void ShakeAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;
    
            var parts = GetCachedParts(originalText);
            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            float t = Time.time * SHAKE_SPEED;
            int visibleCharIndex = 0;
    
            foreach (var part in parts)
            {
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
    
            textMeshPro.text = sb.ToString();
        }
    
        internal void GhostTrailAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;
    
            float t = Time.time * GHOST_SPEED;
            int estimatedCapacity = originalText.Length * 4;
            var sb = GetStringBuilder(estimatedCapacity);
            var parts = GetCachedParts(originalText);
            int visibleIndex = 0;
    
            foreach (var part in parts)
            {
                if (part.IsStyled)
                {
                    sb.Append(part.Text);
                }
                else
                {
                    for (int i = 0; i < part.Text.Length; i++)
                    {
                        float alpha = Mathf.Clamp01(Mathf.Sin(t - visibleIndex * 0.3f) * 0.5f + 0.5f);
                        byte a = (byte)(alpha * 255);
                        string alphaHex = a.ToString("X2");
                        sb.Append($"<color=#FFFFFF{alphaHex}>{part.Text[i]}</color>");
                        visibleIndex++;
                    }
                }
            }
    
            textMeshPro.text = sb.ToString();
        }
    
        internal void BounceAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            bounceTimer += Time.deltaTime;
            if (bounceTimer < BOUNCE_DELAY) return;
            bounceTimer = 0f;
    
            var parts = GetCachedParts(originalText);
            int estimatedCapacity = originalText.Length * 2;
            var sb = GetStringBuilder(estimatedCapacity);
            int charIndex = 0;
            string bounceRedHex = ColorUtility.ToHtmlStringRGB(Color.red);
    
            foreach (var part in parts)
            {
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
    
            textMeshPro.text = sb.ToString();
    
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
    
            int colorCount = rainbowColors.Length;
            rainbowTime += Time.deltaTime * RAINBOW_SPEED;
            rainbowTime %= 1.0f;
    
            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            var parts = GetCachedParts(originalText);
    
            foreach (var part in parts)
            {
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
    
            textMeshPro.text = sb.ToString();
        }
    
        internal void SmoothRainbowAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;
    
            smoothRainbowTime += Time.deltaTime * SMOOTH_RAINBOW_SPEED;
            smoothRainbowTime %= 1f;
    
            int estimatedCapacity = originalText.Length * 3;
            var sb = GetStringBuilder(estimatedCapacity);
            var parts = GetCachedParts(originalText);
    
            foreach (var part in parts)
            {
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
    
            textMeshPro.text = sb.ToString();
        }
    
        internal void GlitchAnimation(TextMeshProUGUI textMeshPro, string originalText)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(originalText)) return;
    
            if (updateCounter % 4 == 0) return;
            
            glitchTimer += Time.deltaTime * GLITCH_SPEED;
            
            var parts = GetCachedParts(originalText);
            int estimatedCapacity = originalText.Length * 6;
            var sb = GetStringBuilder(estimatedCapacity);
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
                glitchOffset = Mathf.Lerp(glitchOffset, 0f, Time.deltaTime * 8f);
            }
    
            foreach (var part in parts)
            {
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
    
            textMeshPro.text = sb.ToString();
        }
    
        private static string ColorToHex(Color color)
        {
            byte r = (byte)(Mathf.Clamp01(color.r) * 255f);
            byte g = (byte)(Mathf.Clamp01(color.g) * 255f);
            byte b = (byte)(Mathf.Clamp01(color.b) * 255f);
            byte a = (byte)(Mathf.Clamp01(color.a) * 255f);
            return $"{r:X2}{g:X2}{b:X2}{a:X2}";
        }
    
        private static List<TextPart> ParseTextWithTags(string text)
        {
            var parts = new List<TextPart>();
            var matches = Regex.Matches(text, @"<[^>]+>|[^<]+");
    
            foreach (Match match in matches)
            {
                var part = new TextPart
                {
                    Text = match.Value,
                    IsStyled = match.Value.StartsWith("<") && match.Value.EndsWith(">"),
                };
                parts.Add(part);
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
            cachedParts?.Clear();
            cachedParts = null;
            cachedOriginalText = "";
            cachedVisibleLength = -1;
        }
    }
}

