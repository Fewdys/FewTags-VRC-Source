using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace FewTags.FewTags
{
    public class TagAnimator : MonoBehaviour
    {
        private float rainbowTime;
        private float smoothRainbowTime;

        private float bounceTimer;
        private int bounceIndex;
        private bool bounceForward = true;

        private int letterIndex;
        private bool letterGoingForward = true;
        private float letterTimer;

        private float glitchTimer;
        private float glitchOffset;
        private int glitchCharIndex;

        public float ScrollSpeed = 8f;
        private float scrollOffset;

        public const int ScrollMaxWindowChars = 26;

        public bool LetterByLetter = false;
        public bool SmoothRainbow = false;
        public bool Rainbow = false;
        public bool Bounce = false;
        public bool Jump = false;
        public bool Pulse = false;
        public bool Shake = false;
        public bool GhostTrail = false;
        public bool Blink = false;
        public bool Glitch = false;
        public bool Scroll = false;

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

        private const int ANIM_LBL = 0;
        private const int ANIM_BOUNCE = 1;
        private const int ANIM_RAINBOW = 2;
        private const int ANIM_SMOOTH_RAINBOW = 3;
        private const int ANIM_PULSE = 4;
        private const int ANIM_JUMP = 5;
        private const int ANIM_SHAKE = 6;
        private const int ANIM_GHOST = 7;
        private const int ANIM_BLINK = 8;
        private const int ANIM_GLITCH = 9;
        private const int ANIM_SCROLL = 10;

        private int _renderAnimation = -1;

        private bool _lastLetterByLetter;
        private bool _lastSmoothRainbow;
        private bool _lastRainbow;
        private bool _lastBounce;
        private bool _lastJump;
        private bool _lastPulse;
        private bool _lastShake;
        private bool _lastGhostTrail;
        private bool _lastBlink;
        private bool _lastGlitch;
        private bool _lastScroll;

        private float _frameDeltaTime;
        private float _frameTime;

        private static readonly Color32[] rainbowColors =
        {
            new Color32(255,   0,   0, 255),
            new Color32(255, 127,   0, 255),
            new Color32(255, 255,   0, 255),
            new Color32(  0, 255,   0, 255),
            new Color32(  0,   0, 255, 255),
            new Color32( 75,   0, 130, 255),
            new Color32(148,   0, 211, 255)
        };

        private static readonly string[] rainbowHex =
        {
            "FF0000",
            "FF7F00",
            "FFFF00",
            "00FF00",
            "0000FF",
            "4B0082",
            "9400D3"
        };

        private static readonly char[] HexChars = "0123456789ABCDEF".ToCharArray();

        private TextMeshProUGUI _cachedTMP;
        private bool _tmpLookupFailed;

        private readonly StringBuilder _sb = new StringBuilder(512);
        private readonly Stack<string> _tagStack = new Stack<string>(8);
        private readonly Stack<string> _openTags = new Stack<string>(8);
        private readonly Stack<float> _sizeStack = new Stack<float>(4);
        private readonly List<TagData> _activeTagsList = new List<TagData>(8);
        private readonly Stack<TagData> _activeTagStack = new Stack<TagData>(8);
        private readonly Stack<TagData> _tempTagStack = new Stack<TagData>(8);
        private readonly Stack<string> _tempOpenStack = new Stack<string>(8);

        private string _cachedStrippedText_LBL;
        private string _cachedRaw_LBL;

        private string _cachedStrippedText_CYLN;
        private string _cachedRaw_CYLN;

        private string _cachedStrippedText_RAIN;
        private string _cachedRaw_RAIN;

        private string _cachedStrippedText_SR;
        private string _cachedRaw_SR;

        private string _cachedStrippedText_PULSE;
        private string _cachedRaw_PULSE;

        private string _cachedStrippedText_JUMP;
        private string _cachedRaw_JUMP;

        private string _cachedStrippedText_SHAKE;
        private string _cachedRaw_SHAKE;

        private string _cachedStrippedText_GT;
        private string _cachedRaw_GT;

        private string _cachedStrippedText_BLINK;
        private string _cachedRaw_BLINK;

        private string _cachedStrippedText_GLITCH;
        private string _cachedRaw_GLITCH;

        private string _cachedStrippedText_SCROLL;
        private string _cachedRaw_SCROLL;

        private TextPart[] _partsLBL;
        private string _partsRawLBL;
        private int _lengthLBL;

        private TextPart[] _partsCYLN;
        private string _partsRawCYLN;
        private int _lengthCYLN;

        private TextPart[] _partsRAIN;
        private string _partsRawRAIN;
        private int _lengthRAIN;

        private TextPart[] _partsSR;
        private string _partsRawSR;
        private int _lengthSR;

        private TextPart[] _partsPULSE;
        private string _partsRawPULSE;
        private int _lengthPULSE;

        private TextPart[] _partsJUMP;
        private string _partsRawJUMP;
        private int _lengthJUMP;

        private TextPart[] _partsSHAKE;
        private string _partsRawSHAKE;
        private int _lengthSHAKE;

        private TextPart[] _partsGT;
        private string _partsRawGT;
        private int _lengthGT;

        private TextPart[] _partsBLINK;
        private string _partsRawBLINK;
        private int _lengthBLINK;

        private TextPart[] _partsGLITCH;
        private string _partsRawGLITCH;
        private int _lengthGLITCH;

        private TextPart[] _partsSCROLL;
        private string _partsRawSCROLL;
        private int _lengthSCROLL;

        private List<string> _scrollCharList;
        private string _scrollCachedText;
        private int _scrollVisibleTotal;

        private static readonly Regex OpenSizeRegex =
            new Regex(
                @"<size=([+-]?\d+)%?>",
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled);

        private static readonly Regex TagRegex =
            new Regex(
                @"<[^>]+>|[^<]+",
                RegexOptions.Compiled);

        private static readonly Regex ScrollCharRegex =
            new Regex(
                @"(<.*?>)|(.{1})",
                RegexOptions.Singleline |
                RegexOptions.Compiled);

        private readonly System.Random _rng = new System.Random();

        private string GetStripped(string raw, string marker, ref string cachedRaw, ref string cachedStripped, bool removeHtmlTags = false)
        {
            if (raw == cachedRaw && cachedStripped != null)
            {
                return cachedStripped;
            }

            cachedRaw = raw;

            string result = raw.Replace(marker, "");

            if (removeHtmlTags)
            {
                result = Utils.RemoveHtmlTags(result, true);
            }

            cachedStripped = result;

            return result;
        }

        private TextPart[] GetCachedParts(string text, ref TextPart[] cache, ref string cachedText, ref int cachedLength)
        {
            if (text == cachedText && cache != null)
            {
                return cache;
            }

            cachedText = text;

            cache = ParseTextWithTags(text);

            cachedLength = 0;

            for (int i = 0; i < cache.Length; i++)
            {
                if (!cache[i].IsStyled) cachedLength += cache[i].Length;
            }

            return cache;
        }

        private bool AnimationFlagsChanged()
        {
            return
                _lastLetterByLetter != LetterByLetter ||
                _lastSmoothRainbow != SmoothRainbow ||
                _lastRainbow != Rainbow ||
                _lastBounce != Bounce ||
                _lastJump != Jump ||
                _lastPulse != Pulse ||
                _lastShake != Shake ||
                _lastGhostTrail != GhostTrail ||
                _lastBlink != Blink ||
                _lastGlitch != Glitch ||
                _lastScroll != Scroll;
        }

        private void RefreshRenderAnimation()
        {
            if (!AnimationFlagsChanged())
                return;

            _lastLetterByLetter = LetterByLetter;
            _lastSmoothRainbow = SmoothRainbow;
            _lastRainbow = Rainbow;
            _lastBounce = Bounce;
            _lastJump = Jump;
            _lastPulse = Pulse;
            _lastShake = Shake;
            _lastGhostTrail = GhostTrail;
            _lastBlink = Blink;
            _lastGlitch = Glitch;
            _lastScroll = Scroll;

            if (Scroll)
                _renderAnimation = ANIM_SCROLL;
            else if (Glitch)
                _renderAnimation = ANIM_GLITCH;
            else if (Blink)
                _renderAnimation = ANIM_BLINK;
            else if (GhostTrail)
                _renderAnimation = ANIM_GHOST;
            else if (Shake)
                _renderAnimation = ANIM_SHAKE;
            else if (Jump)
                _renderAnimation = ANIM_JUMP;
            else if (Pulse)
                _renderAnimation = ANIM_PULSE;
            else if (SmoothRainbow)
                _renderAnimation = ANIM_SMOOTH_RAINBOW;
            else if (Rainbow)
                _renderAnimation = ANIM_RAINBOW;
            else if (Bounce)
                _renderAnimation = ANIM_BOUNCE;
            else if (LetterByLetter)
                _renderAnimation = ANIM_LBL;
            else
                _renderAnimation = -1;
        }

        public void ResetAnimator()
        {
            _cachedTMP = null;
            _tmpLookupFailed = false;

            LetterByLetter = false;
            SmoothRainbow = false;
            Rainbow = false;
            Bounce = false;
            Jump = false;
            Pulse = false;
            Shake = false;
            GhostTrail = false;
            Blink = false;
            Glitch = false;
            Scroll = false;

            _lastLetterByLetter = false;
            _lastSmoothRainbow = false;
            _lastRainbow = false;
            _lastBounce = false;
            _lastJump = false;
            _lastPulse = false;
            _lastShake = false;
            _lastGhostTrail = false;
            _lastBlink = false;
            _lastGlitch = false;
            _lastScroll = false;

            rainbowTime = 0f;
            smoothRainbowTime = 0f;

            bounceTimer = 0f;
            bounceIndex = 0;
            bounceForward = true;

            letterIndex = 0;
            letterGoingForward = true;
            letterTimer = 0f;

            glitchTimer = 0f;
            glitchOffset = 0f;
            glitchCharIndex = 0;

            scrollOffset = 0f;

            _renderAnimation = -1;

            _scrollCharList = null;
            _scrollCachedText = null;
            _scrollVisibleTotal = 0;

            ClearParsedCaches();
        }

        private void ClearParsedCaches()
        {
            _partsLBL = null;
            _partsRawLBL = null;
            _lengthLBL = 0;

            _partsCYLN = null;
            _partsRawCYLN = null;
            _lengthCYLN = 0;

            _partsRAIN = null;
            _partsRawRAIN = null;
            _lengthRAIN = 0;

            _partsSR = null;
            _partsRawSR = null;
            _lengthSR = 0;

            _partsPULSE = null;
            _partsRawPULSE = null;
            _lengthPULSE = 0;

            _partsJUMP = null;
            _partsRawJUMP = null;
            _lengthJUMP = 0;

            _partsSHAKE = null;
            _partsRawSHAKE = null;
            _lengthSHAKE = 0;

            _partsGT = null;
            _partsRawGT = null;
            _lengthGT = 0;

            _partsBLINK = null;
            _partsRawBLINK = null;
            _lengthBLINK = 0;

            _partsGLITCH = null;
            _partsRawGLITCH = null;
            _lengthGLITCH = 0;

            _partsSCROLL = null;
            _partsRawSCROLL = null;
            _lengthSCROLL = 0;
        }

        public void Start()
        {
        }

        public void Update()
        {
            try
            {
                if (this == null)
                    return;

                RefreshRenderAnimation();

                if (_renderAnimation < 0)
                    return;

                if (_tmpLookupFailed)
                    return;

                if (_cachedTMP == null)
                {
                    Transform textTransform = Utils.RecursiveFindChild(transform, "Trust Text", true, false);

                    if (textTransform == null)
                    {
                        textTransform = Utils.RecursiveFindChild(transform, "Name", true, false);
                    }

                    try
                    {
                        _cachedTMP = textTransform?.GetComponent<TextMeshProUGUI>();
                    }
                    catch
                    {
                        _tmpLookupFailed = true;
                        return;
                    }

                    if (_cachedTMP == null)
                    {
                        _tmpLookupFailed = true;
                        return;
                    }
                }

                TextMeshProUGUI textComponent = _cachedTMP;

                if (textComponent == null)
                {
                    _cachedTMP = null;
                    return;
                }

                string raw = originalText;

                if (string.IsNullOrEmpty(raw))
                    return;

                _frameDeltaTime = Time.unscaledDeltaTime;

                _frameTime = Time.unscaledTime;

                switch (_renderAnimation)
                {
                    case ANIM_LBL:
                        LetterByLetterAnimation(textComponent, GetStripped(raw, ".LBL.", ref _cachedRaw_LBL, ref _cachedStrippedText_LBL));
                        break;

                    case ANIM_BOUNCE:
                        BounceAnimation(textComponent, GetStripped(raw, ".CYLN.", ref _cachedRaw_CYLN, ref _cachedStrippedText_CYLN));
                        break;

                    case ANIM_RAINBOW:
                        RainbowAnimation(textComponent, GetStripped(raw, ".RAIN.", ref _cachedRaw_RAIN, ref _cachedStrippedText_RAIN, true));
                        break;

                    case ANIM_SMOOTH_RAINBOW:
                        SmoothRainbowAnimation(textComponent, GetStripped(raw, ".SR.", ref _cachedRaw_SR, ref _cachedStrippedText_SR, true));
                        break;

                    case ANIM_PULSE:
                        PopPulseAnimation(textComponent, GetStripped(raw, ".PULSE.", ref _cachedRaw_PULSE, ref _cachedStrippedText_PULSE));
                        break;

                    case ANIM_JUMP:
                        JumpAnimation(textComponent, GetStripped(raw, ".JUMP.", ref _cachedRaw_JUMP, ref _cachedStrippedText_JUMP));
                        break;

                    case ANIM_SHAKE:
                        ShakeAnimation(textComponent, GetStripped(raw, ".SHAKE.", ref _cachedRaw_SHAKE, ref _cachedStrippedText_SHAKE));
                        break;

                    case ANIM_GHOST:
                        GhostTrailAnimation(textComponent, GetStripped(raw, ".GT.", ref _cachedRaw_GT, ref _cachedStrippedText_GT));
                        break;

                    case ANIM_BLINK:
                        BlinkAnimation(textComponent, GetStripped(raw, ".BLINK.", ref _cachedRaw_BLINK, ref _cachedStrippedText_BLINK, true));
                        break;

                    case ANIM_GLITCH:
                        GlitchAnimation(textComponent, GetStripped(raw, ".GLITCH.", ref _cachedRaw_GLITCH, ref _cachedStrippedText_GLITCH));
                        break;

                    case ANIM_SCROLL:
                        ScrollAnimation(textComponent, GetStripped(raw, ".SCROLL.", ref _cachedRaw_SCROLL, ref _cachedStrippedText_SCROLL));
                        break;
                }
            }
            catch (Exception e)
            {
                LogManager.LogErrorToConsole(
                    "Failed To Update A Animated Plate!\n" +
                    e);
            }
        }

        private TextPart[] ParseTextWithTags(string text)
        {
            MatchCollection matches =
                TagRegex.Matches(text);

            int count =
                matches.Count;

            TextPart[] parts =
                new TextPart[count];

            for (int i = 0; i < count; i++)
            {
                string value =
                    matches[i].Value;

                bool isStyled =
                    value.Length >= 2 &&
                    value[0] == '<' &&
                    value[value.Length - 1] == '>';

                TextPart part =
                    new TextPart
                    {
                        Text = value,
                        Length = value.Length,
                        IsStyled = isStyled,
                        IsClosingTag = isStyled && value.Length > 2 && value[1] == '/',
                        SizeScale = 0f,
                        CloseSizeCount = 0
                    };

                if (isStyled)
                {
                    Match openSize = OpenSizeRegex.Match(value);

                    if (openSize.Success && float.TryParse(openSize.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float size))
                    {
                        part.SizeScale = size / 30f;
                    }

                    part.CloseSizeCount = CountOccurrences(value, "</size>");
                }

                parts[i] = part;
            }

            return parts;
        }

        private static int CountOccurrences(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return 0;
            }

            int count = 0;
            int index = 0;

            while ((index = source.IndexOf(value, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private string BuildVisibleTextWithTags(TextPart[] parts, int visibleCharCount)
        {
            int capacity = visibleCharCount * 2 + parts.Length * 3;

            _sb.Clear();

            if (_sb.Capacity < capacity)
                _sb.EnsureCapacity(capacity);

            int writtenChars = 0;

            _tagStack.Clear();

            for (int i = 0; i < parts.Length; i++)
            {
                TextPart part =
                    parts[i];

                if (part.IsStyled)
                {
                    _sb.Append(part.Text);

                    if (!part.IsClosingTag)
                    {
                        _tagStack.Push(
                            ExtractTagName(
                                part.Text));
                    }
                    else if (_tagStack.Count > 0 && _tagStack.Peek() == ExtractTagName(part.Text))
                    {
                        _tagStack.Pop();
                    }

                    continue;
                }

                int remaining = visibleCharCount - writtenChars;

                if (remaining <= 0)
                    break;

                int charsToWrite = Mathf.Min(part.Length, remaining);

                _sb.Append(part.Text, 0, charsToWrite);

                writtenChars += charsToWrite;

                if (charsToWrite < part.Length)
                    break;
            }

            while (_tagStack.Count > 0)
            {
                string tag = _tagStack.Pop();

                _sb.Append("</");
                _sb.Append(tag);
                _sb.Append('>');
            }

            return _sb.ToString();
        }

        private static string ExtractTagName(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return string.Empty;

            int start = 1;

            if (start < tag.Length && tag[start] == '/')
            {
                start++;
            }

            int end = start;

            while (end < tag.Length)
            {
                char c = tag[end];

                if (c == ' ' ||
                    c == '>' ||
                    c == '\t' ||
                    c == '=')
                {
                    break;
                }

                end++;
            }

            if (end <= start)
                return string.Empty;

            return tag.Substring(start, end - start).ToLowerInvariant();
        }

        internal void ScrollAnimation(TextMeshProUGUI textMeshPro, string text, int minWindowChars = 14, int maxWindowChars = ScrollMaxWindowChars)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textToScroll = text;
            string prefix = string.Empty;

            if (textToScroll.StartsWith("[L]"))
            {
                prefix = "[L] ";

                textToScroll = textToScroll.Substring(4);
            }

            if (!textToScroll.EndsWith(" "))
                textToScroll += " ";

            if (textToScroll != _scrollCachedText)
            {
                _scrollCachedText = textToScroll;

                if (_scrollCharList == null)
                {
                    _scrollCharList = new List<string>(textToScroll.Length);
                }
                else
                {
                    _scrollCharList.Clear();
                }

                foreach (Match match in ScrollCharRegex.Matches(textToScroll))
                {
                    _scrollCharList.Add(match.Value);
                }

                _scrollVisibleTotal = 0;

                for (int i = 0;
                     i < _scrollCharList.Count;
                     i++)
                {
                    string item = _scrollCharList[i];

                    if (!IsTag(item))
                        _scrollVisibleTotal++;
                }

                scrollOffset = 0f;
            }

            List<string> charList = _scrollCharList;

            if (charList == null || charList.Count == 0)
            {
                return;
            }

            int totalChars = charList.Count;

            int targetWindowChars;

            if (_scrollVisibleTotal <= minWindowChars)
            {
                targetWindowChars = _scrollVisibleTotal;
            }
            else if (_scrollVisibleTotal <= maxWindowChars)
            {
                targetWindowChars = Mathf.Max(minWindowChars, Mathf.CeilToInt(_scrollVisibleTotal * 0.7f));
            }
            else
            {
                targetWindowChars = maxWindowChars;
            }

            scrollOffset += _frameDeltaTime * ScrollSpeed;

            if (scrollOffset >= totalChars)
            {
                scrollOffset %= totalChars;
            }

            int startIndex = Mathf.FloorToInt(scrollOffset) % totalChars;

            int windowSize = 0;
            int visibleCount = 0;

            for (int i = 0; i < totalChars; i++)
            {
                int idx = (startIndex + i) % totalChars;

                windowSize++;

                if (!IsTag(charList[idx]))
                {
                    visibleCount++;

                    if (visibleCount >= targetWindowChars)
                    {
                        break;
                    }
                }
            }

            _activeTagStack.Clear();

            for (int i = 0; i < startIndex; i++)
            {
                string item = charList[i];

                if (!IsTag(item))
                    continue;

                string tagName = ExtractTagName(item);

                if (!item.StartsWith("</"))
                {
                    _activeTagStack.Push(
                        new TagData
                        {
                            TagName = tagName,
                            FullTag = item
                        });
                }
                else
                {
                    _tempTagStack.Clear();

                    bool found = false;

                    while (_activeTagStack.Count > 0)
                    {
                        TagData top = _activeTagStack.Pop();

                        if (top.TagName ==
                            tagName &&
                            !found)
                        {
                            found = true;
                            break;
                        }

                        _tempTagStack.Push(top);
                    }

                    while (_tempTagStack.Count > 0)
                    {
                        _activeTagStack.Push(_tempTagStack.Pop());
                    }
                }
            }

            _sb.Clear();

            _sb.Append(prefix);

            bool insideColorAtStart = false;

            foreach (TagData tag in _activeTagStack)
            {
                if (tag.TagName == "color")
                {
                    insideColorAtStart = true;
                    break;
                }
            }

            if (!insideColorAtStart)
            {
                _sb.Append("<color=#ffffff>");
                _openTags.Clear();
                _openTags.Push("color");
            }
            else
            {
                _openTags.Clear();
            }

            _activeTagsList.Clear();

            foreach (TagData tag in _activeTagStack)
                _activeTagsList.Add(tag);

            for (int i =
                     _activeTagsList.Count - 1;
                 i >= 0;
                 i--)
            {
                TagData activeTag =
                    _activeTagsList[i];

                _sb.Append(
                    activeTag.FullTag);

                _openTags.Push(
                    activeTag.TagName);
            }

            for (int i = 0; i < windowSize; i++)
            {
                int idx = (startIndex + i) % totalChars;

                string item = charList[idx];

                if (!IsTag(item))
                {
                    _sb.Append(item);
                    continue;
                }

                if (!item.StartsWith("</"))
                {
                    _sb.Append(item);

                    _openTags.Push(
                        ExtractTagName(item));
                }
                else
                {
                    string tagName =
                        ExtractTagName(item);

                    _sb.Append(item);

                    if (tagName == "color")
                    {
                        _sb.Append("<color=#ffffff>");
                        _openTags.Push("color");
                    }

                    _tempOpenStack.Clear();

                    bool found = false;

                    while (_openTags.Count > 0)
                    {
                        string top =
                            _openTags.Pop();

                        if (top == tagName &&
                            !found)
                        {
                            found = true;
                            break;
                        }

                        _tempOpenStack.Push(top);
                    }

                    while (_tempOpenStack.Count > 0) _openTags.Push(_tempOpenStack.Pop());
                }
            }

            bool hasColor = false;

            foreach (string tag in _openTags)
            {
                if (tag == "color")
                {
                    hasColor = true;
                    break;
                }
            }

            if (!hasColor)
            {
                _sb.Append("<color=#ffffff>");
                _openTags.Push("color");
            }

            while (_openTags.Count > 0)
            {
                string tagName =
                    _openTags.Pop();

                _sb.Append("</");
                _sb.Append(tagName);
                _sb.Append('>');
            }

            textMeshPro.SetTextSafe(
                _sb.ToString(),
                true,
                true);
        }

        private static bool IsTag(string value)
        {
            return value.Length > 1 &&
                   value[0] == '<' &&
                   value[value.Length - 1] == '>';
        }

        internal void LetterByLetterAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation =
                    textForAnimation.Substring(4);
            }

            letterTimer += _frameDeltaTime;

            if (letterTimer < LETTER_DELAY)
                return;

            letterTimer -= LETTER_DELAY;

            TextPart[] parts =
                GetCachedParts(
                    textForAnimation,
                    ref _partsLBL,
                    ref _partsRawLBL,
                    ref _lengthLBL);

            if (letterGoingForward)
            {
                letterIndex++;

                if (letterIndex > _lengthLBL)
                {
                    letterIndex = _lengthLBL;
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

            _sb.Clear();
            _sb.Append(prefix);
            _sb.Append(result);

            textMeshPro.SetTextSafe(
                _sb.ToString());
        }

        internal void JumpAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation =
                    textForAnimation.Substring(4);
            }

            TextPart[] parts = GetCachedParts(textForAnimation, ref _partsJUMP, ref _partsRawJUMP, ref _lengthJUMP);

            _sb.Clear();

            int requiredCapacity = text.Length * 3;

            if (_sb.Capacity < requiredCapacity)
                _sb.EnsureCapacity(requiredCapacity);

            _sb.Append(prefix);

            int visibleCharIndex = 0;

            float t = _frameTime * JUMP_SPEED;

            int mid = _lengthJUMP / 2;

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];

                if (part.IsStyled)
                {
                    _sb.Append(part.Text);
                    continue;
                }

                for (int i = 0; i < part.Length; i++)
                {
                    float offsetDistance = Mathf.Abs(visibleCharIndex - mid);

                    float voffset = Mathf.Sin(t - offsetDistance * 0.3f) * 6f;

                    _sb.Append("<voffset=");
                    _sb.Append(voffset.ToString("F1", CultureInfo.InvariantCulture));
                    _sb.Append("px>");
                    _sb.Append(part.Text[i]);
                    _sb.Append("</voffset>");

                    visibleCharIndex++;
                }
            }

            textMeshPro.SetTextSafe(_sb.ToString(), true);
        }

        internal void BlinkAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4);
            }

            float alpha = Mathf.Abs(Mathf.Sin(_frameTime * BLINK_SPEED * Mathf.PI));

            _sb.Clear();

            _sb.Append(prefix);
            _sb.Append("<color=#");

            AppendColorHex(_sb, new Color(1f, 1f, 1f, alpha));

            _sb.Append('>');
            _sb.Append(textForAnimation);
            _sb.Append("</color>");

            textMeshPro.SetTextSafe(_sb.ToString());
        }

        internal void PopPulseAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4);
            }

            TextPart[] parts = GetCachedParts(textForAnimation, ref _partsPULSE, ref _partsRawPULSE, ref _lengthPULSE);

            _sb.Clear();

            int requiredCapacity = text.Length * 3;

            if (_sb.Capacity < requiredCapacity)
                _sb.EnsureCapacity(requiredCapacity);

            _sb.Append(prefix);

            float t = _frameTime * PULSE_SPEED;

            int visibleCharIndex = 0;

            _sizeStack.Clear();
            _sizeStack.Push(1f);

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];

                if (part.IsStyled)
                {
                    if (part.SizeScale > 0f)
                        _sizeStack.Push(
                            part.SizeScale);

                    for (int i = 0;
                         i < part.CloseSizeCount;
                         i++)
                    {
                        if (_sizeStack.Count > 1)
                            _sizeStack.Pop();
                    }

                    _sb.Append(part.Text);
                    continue;
                }

                float baseSize = _sizeStack.Peek();

                for (int i = 0; i < part.Length; i++)
                {
                    float pulse = 1f + Mathf.Sin(t - visibleCharIndex * 0.3f) * 0.2f;

                    float finalSize = baseSize * pulse;

                    _sb.Append("<size=");
                    _sb.Append(((int)(finalSize * 100f)).ToString(CultureInfo.InvariantCulture));
                    _sb.Append("%>");
                    _sb.Append(part.Text[i]);
                    _sb.Append("</size>");

                    visibleCharIndex++;
                }
            }

            textMeshPro.SetTextSafe(_sb.ToString());
        }

        internal void ShakeAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4);
            }

            TextPart[] parts = GetCachedParts(textForAnimation, ref _partsSHAKE, ref _partsRawSHAKE, ref _lengthSHAKE);

            _sb.Clear();

            int requiredCapacity = text.Length * 3;

            if (_sb.Capacity < requiredCapacity)
                _sb.EnsureCapacity(requiredCapacity);

            _sb.Append(prefix);

            float t = _frameTime * SHAKE_SPEED;

            int visibleCharIndex = 0;

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];

                if (part.IsStyled)
                {
                    _sb.Append(part.Text);
                    continue;
                }

                for (int i = 0; i < part.Length; i++)
                {
                    float rot = Mathf.Sin(t * 0.1f + visibleCharIndex * 0.2f) * 20f;

                    _sb.Append("<rotate=");
                    _sb.Append(rot.ToString("F1", CultureInfo.InvariantCulture));
                    _sb.Append('>');
                    _sb.Append(part.Text[i]);
                    _sb.Append("</rotate>");

                    visibleCharIndex++;
                }
            }

            textMeshPro.SetTextSafe(_sb.ToString());
        }

        internal void GhostTrailAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4);
            }

            TextPart[] parts = GetCachedParts(textForAnimation, ref _partsGT, ref _partsRawGT, ref _lengthGT);

            _sb.Clear();

            int requiredCapacity = text.Length * 4;

            if (_sb.Capacity < requiredCapacity)
                _sb.EnsureCapacity(requiredCapacity);

            _sb.Append(prefix);

            float t = _frameTime * GHOST_SPEED;

            int visibleIndex = 0;

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];

                if (part.IsStyled)
                {
                    _sb.Append(part.Text);
                    continue;
                }

                for (int i = 0; i < part.Length; i++)
                {
                    float pulse = Mathf.Clamp01(Mathf.Sin(t - visibleIndex * 0.3f) * 0.5f + 0.5f);

                    byte alpha =
                        (byte)(pulse * 255f);

                    _sb.Append("<color=#FFFFFF");
                    _sb.Append(HexChars[alpha >> 4]);
                    _sb.Append(HexChars[alpha & 0xF]);
                    _sb.Append('>');
                    _sb.Append(part.Text[i]);
                    _sb.Append("</color>");

                    visibleIndex++;
                }
            }

            textMeshPro.SetTextSafe(_sb.ToString());
        }

        internal void BounceAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            bounceTimer += _frameDeltaTime;

            if (bounceTimer < BOUNCE_DELAY)
                return;

            bounceTimer -= BOUNCE_DELAY;

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4);
            }

            TextPart[] parts = GetCachedParts(textForAnimation, ref _partsCYLN, ref _partsRawCYLN, ref _lengthCYLN);

            int visibleLength = _lengthCYLN;

            if (visibleLength <= 0)
                return;

            _sb.Clear();

            int requiredCapacity = text.Length * 2;

            if (_sb.Capacity < requiredCapacity)
                _sb.EnsureCapacity(requiredCapacity);

            _sb.Append(prefix);

            int charIndex = 0;

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];

                if (part.IsStyled)
                {
                    _sb.Append(part.Text);
                    continue;
                }

                for (int j = 0; j < part.Length; j++)
                {
                    if (charIndex == bounceIndex)
                    {
                        _sb.Append("<color=#FF0000>");
                        _sb.Append(part.Text[j]);
                        _sb.Append("</color>");
                    }
                    else
                    {
                        _sb.Append(part.Text[j]);
                    }

                    charIndex++;
                }
            }

            textMeshPro.SetTextSafe(_sb.ToString());

            bounceIndex += bounceForward ? 1 : -1;

            if (bounceIndex >= visibleLength)
            {
                bounceIndex = visibleLength - 1;

                bounceForward = false;
            }
            else if (bounceIndex < 0)
            {
                bounceIndex = 0;
                bounceForward = true;
            }
        }

        internal void RainbowAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4);
            }

            rainbowTime += _frameDeltaTime * RAINBOW_SPEED;

            if (rainbowTime >= 1f)
            {
                rainbowTime -= Mathf.Floor(rainbowTime);
            }

            TextPart[] parts = GetCachedParts(textForAnimation, ref _partsRAIN, ref _partsRawRAIN, ref _lengthRAIN);

            int colorCount = rainbowColors.Length;

            int colorOffset = Mathf.FloorToInt(rainbowTime * colorCount);

            _sb.Clear();

            int requiredCapacity = text.Length * 3;

            if (_sb.Capacity < requiredCapacity)
                _sb.EnsureCapacity(requiredCapacity);

            _sb.Append(prefix);

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];

                if (part.IsStyled)
                {
                    _sb.Append(part.Text);
                    continue;
                }

                for (int i = 0; i < part.Length; i++)
                {
                    int colorIndex = (i + colorOffset) % colorCount;

                    _sb.Append("<color=#");
                    _sb.Append(rainbowHex[colorIndex]);
                    _sb.Append('>');
                    _sb.Append(part.Text[i]);
                    _sb.Append("</color>");
                }
            }

            textMeshPro.SetTextSafe(_sb.ToString());
        }

        internal void SmoothRainbowAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4);
            }

            smoothRainbowTime += _frameDeltaTime * SMOOTH_RAINBOW_SPEED;

            if (smoothRainbowTime >= 1f)
            {
                smoothRainbowTime -= Mathf.Floor(smoothRainbowTime);
            }

            TextPart[] parts = GetCachedParts(textForAnimation, ref _partsSR, ref _partsRawSR, ref _lengthSR);

            _sb.Clear();

            int requiredCapacity = text.Length * 3;

            if (_sb.Capacity < requiredCapacity)
                _sb.EnsureCapacity(requiredCapacity);

            _sb.Append(prefix);

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];

                if (part.IsStyled)
                {
                    _sb.Append(part.Text);
                    continue;
                }

                int len = Mathf.Max(1, part.Length);

                for (int i = 0; i < part.Length; i++)
                {
                    float hue = Mathf.Repeat(smoothRainbowTime + (float)i / len, 1f);

                    Color color = Color.HSVToRGB(hue, 1f, 1f);

                    _sb.Append("<color=#");

                    AppendColorHex(_sb, color);

                    _sb.Append('>');
                    _sb.Append(part.Text[i]);
                    _sb.Append("</color>");
                }
            }

            textMeshPro.SetTextSafe(_sb.ToString());
        }

        internal void GlitchAnimation(TextMeshProUGUI textMeshPro, string text)
        {
            if (textMeshPro == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            string textForAnimation = text;
            string prefix = string.Empty;

            if (textForAnimation.StartsWith("[L]"))
            {
                prefix = "[L] ";
                textForAnimation = textForAnimation.Substring(4);
            }

            glitchTimer += _frameDeltaTime * GLITCH_SPEED;

            TextPart[] parts = GetCachedParts(textForAnimation, ref _partsGLITCH, ref _partsRawGLITCH, ref _lengthGLITCH);

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

            float random1 = (float)_rng.NextDouble();

            float random2 = (float)_rng.NextDouble();

            float random3 = (float)_rng.NextDouble();

            float random4 = (float)_rng.NextDouble();

            if (_lengthGLITCH > 0 && shouldCorrupt)
            {
                glitchCharIndex = (glitchCharIndex + 1) % _lengthGLITCH;
            }

            if (shouldShift)
            {
                glitchOffset = Mathf.Sin(glitchTimer * 1.5f) * GLITCH_INTENSITY * 0.5f;
            }
            else
            {
                glitchOffset = Mathf.Lerp(glitchOffset, 0f, _frameDeltaTime * 8f);
            }

            const string corruptChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?~`";

            const string glitchChars = "0123456789ABCDEF!@#$%^&*()_+-=[]{}|;':\",./<>?~`";

            _sb.Clear();

            int requiredCapacity = text.Length * 6;

            if (_sb.Capacity < requiredCapacity)
                _sb.EnsureCapacity(requiredCapacity);

            _sb.Append(prefix);

            int visibleCharIndex = 0;

            for (int p = 0; p < parts.Length; p++)
            {
                TextPart part = parts[p];

                if (part.IsStyled)
                {
                    _sb.Append(part.Text);
                    continue;
                }

                for (int i = 0; i < part.Length; i++)
                {
                    char currentChar = part.Text[i];

                    bool hasColorTag = false;
                    bool hasPositionTag = false;
                    bool hasRotationTag = false;
                    bool hasSizeTag = false;
                    bool hasBoldTag = false;

                    float charRandom = (random1 + visibleCharIndex * 0.10f) % 1f;

                    float charRandom2 = (random2 + visibleCharIndex * 0.15f) % 1f;

                    float charRandom3 = (random3 + visibleCharIndex * 0.20f) % 1f;

                    float charRandom4 = (random4 + visibleCharIndex * 0.25f) % 1f;

                    if (shouldCorrupt && visibleCharIndex == glitchCharIndex)
                    {
                        currentChar = corruptChars[Mathf.FloorToInt(glitchTimer * 15f) % corruptChars.Length];
                    }
                    else if (shouldGlitch && charRandom < 0.25f)
                    {
                        currentChar = glitchChars[Mathf.FloorToInt(charRandom * glitchChars.Length)];
                    }

                    if (shouldCorrupt && visibleCharIndex == glitchCharIndex)
                    {
                        Color gc = new Color(0.5f + charRandom * 0.5f, charRandom2 * 0.5f, 0.5f + charRandom3 * 0.5f, 1f);

                        _sb.Append("<color=#");
                        AppendColorHex(_sb, gc);
                        _sb.Append('>');

                        hasColorTag = true;
                    }
                    else if (shouldGlitch && charRandom < 0.4f)
                    {
                        Color gc = new Color(0.8f + charRandom * 0.2f, charRandom2 * 0.2f, 0.8f + charRandom3 * 0.2f, 1f);

                        _sb.Append("<color=#");
                        AppendColorHex(_sb, gc);
                        _sb.Append('>');

                        hasColorTag = true;
                    }

                    if (shouldShift ||
                        Mathf.Abs(glitchOffset) > 0.1f)
                    {
                        float yOffset = Mathf.Cos(visibleCharIndex * 0.3f + glitchTimer * 1.5f) * glitchOffset * 0.3f;

                        _sb.Append("<voffset=");
                        _sb.Append(yOffset.ToString("F1", CultureInfo.InvariantCulture));
                        _sb.Append("px>");

                        hasPositionTag = true;
                    }

                    if (shouldRotate || (shouldGlitch && charRandom2 < 0.3f))
                    {
                        float rotation = -45f + charRandom * 90f;

                        _sb.Append("<rotate=");
                        _sb.Append(rotation.ToString("F1", CultureInfo.InvariantCulture));
                        _sb.Append('>');

                        hasRotationTag = true;
                    }

                    if (shouldSize || (shouldGlitch && charRandom3 < 0.25f))
                    {
                        float size = 0.5f + charRandom * 1.5f;

                        _sb.Append("<size=");
                        _sb.Append(((int)(size * 100f)).ToString(CultureInfo.InvariantCulture));
                        _sb.Append("%>");

                        hasSizeTag = true;
                    }

                    if (shouldFlicker || (shouldGlitch && charRandom4 < 0.35f))
                    {
                        float al = 0.2f + charRandom * 0.8f;

                        _sb.Append("<color=#");

                        AppendColorHex(_sb, new Color(1f, 1f, 1f, al));

                        _sb.Append('>');

                        hasColorTag = true;
                    }

                    if (shouldBold || (shouldGlitch && charRandom < 0.2f))
                    {
                        _sb.Append("<b>");
                        hasBoldTag = true;
                    }

                    _sb.Append(currentChar);

                    if (hasSizeTag)
                        _sb.Append("</size>");

                    if (hasRotationTag)
                        _sb.Append("</rotate>");

                    if (hasPositionTag)
                        _sb.Append("</voffset>");

                    if (hasBoldTag)
                        _sb.Append("</b>");

                    if (hasColorTag)
                        _sb.Append("</color>");

                    visibleCharIndex++;
                }
            }

            textMeshPro.SetTextSafe(_sb.ToString());
        }

        private static void AppendColorHex(
            StringBuilder sb,
            Color color)
        {
            byte r =
                (byte)(
                    Mathf.Clamp01(color.r) *
                    255f);

            byte g =
                (byte)(
                    Mathf.Clamp01(color.g) *
                    255f);

            byte b =
                (byte)(
                    Mathf.Clamp01(color.b) *
                    255f);

            byte a =
                (byte)(
                    Mathf.Clamp01(color.a) *
                    255f);

            sb.Append(HexChars[r >> 4]);
            sb.Append(HexChars[r & 0xF]);

            sb.Append(HexChars[g >> 4]);
            sb.Append(HexChars[g & 0xF]);

            sb.Append(HexChars[b >> 4]);
            sb.Append(HexChars[b & 0xF]);

            sb.Append(HexChars[a >> 4]);
            sb.Append(HexChars[a & 0xF]);
        }

        private struct TextPart
        {
            public string Text;
            public int Length;
            public bool IsStyled;
            public bool IsClosingTag;

            public float SizeScale;
            public int CloseSizeCount;
        }

        private struct TagData
        {
            public string TagName;
            public string FullTag;
        }

        public void OnDestroy()
        {
            _cachedTMP = null;
            _tmpLookupFailed = false;

            originalText = string.Empty;

            _renderAnimation = -1;

            _scrollCharList = null;
            _scrollCachedText = null;
            _scrollVisibleTotal = 0;

            ClearParsedCaches();

            _sb.Clear();

            _tagStack.Clear();
            _openTags.Clear();
            _sizeStack.Clear();

            _activeTagsList.Clear();
            _activeTagStack.Clear();
            _tempTagStack.Clear();
            _tempOpenStack.Clear();
        }
    }
}
