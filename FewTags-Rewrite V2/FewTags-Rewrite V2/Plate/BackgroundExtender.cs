using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FewTags.FewTags;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FewTags.FewTags_Rewrite_V2.Plate
{
    public class BackgroundExtender
    {
        public RectTransform sourceBackground;
        public TMP_Text targetText;

        public float paddingLeft = 18f;
        public float paddingRight = 18f;
        public float paddingTop = 13f;
        public float paddingBottom = 13f;

        public bool extendWidth = true;
        public bool extendHeight = true;

        public float minWidth = 0f;
        public float maxWidth = 0f;
        public float minHeight = 56f;
        public float maxHeight = 0f;

        public Transform parentOverride;
        public bool reuseClone = true;

        public RectTransform Clone { get; private set; }

        public RectTransform ExtendBackground(string callSite = "unspecified")
        {
            if (sourceBackground == null || targetText == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[BackgroundExtender] Missing sourceBackground or targetText.");
                return null;
            }

            EnsureClone();
            AlignLeftAndExtend(Clone, callSite);
            return Clone;
        }

        public Vector2 GetPreferredValues(TMP_Text obj, string text, float width, float height)
        {
            return obj.GetPreferredValues(text, width, height);
        }

        public RectTransform ExtendInPlace(string callSite = "unspecified")
        {
            if (sourceBackground == null || targetText == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[BackgroundExtender] Missing sourceBackground or targetText.");
                return null;
            }

            AlignLeftAndExtend(sourceBackground, callSite);
            Clone = sourceBackground;
            return Clone;
        }

        public RectTransform ForceNewClone(string callSite = "unspecified")
        {
            if (Clone != null)
            {
                UnityEngine.Object.Destroy(Clone.gameObject);
                Clone = null;
            }
            return ExtendBackground(callSite);
        }

        private void EnsureClone()
        {
            if (Clone != null && reuseClone)
                return;

            Transform parent = parentOverride != null ? parentOverride : sourceBackground.parent;

            GameObject instance = GameObject.Instantiate(sourceBackground.gameObject, parent);
            instance.name = sourceBackground.name + "_Extended";

            Clone = instance.GetComponent<RectTransform>();
            Clone.SetSiblingIndex(sourceBackground.GetSiblingIndex());

            var layoutElement = Clone.GetComponent<UnityEngine.UI.LayoutElement>();
            if (layoutElement == null)
                layoutElement = Clone.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.ignoreLayout = true;

            var ownLayoutGroup = Clone.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (ownLayoutGroup != null)
                ownLayoutGroup.enabled = false;
        }

        /// <summary>
        /// DIAGNOSTIC ONLY. Remove once the cause is found — this is not meant to ship.
        /// </summary>
        private static void LogLayoutChain(Transform startTransform, Transform stopAt, string callSite)
        {
            if (startTransform == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[BGExt:{callSite}] LogLayoutChain: startTransform is null.");
                return;
            }

            var current = startTransform;
            int depth = 0;
            while (current != null && depth < 15)
            {
                var rt = current.GetComponent<RectTransform>();
                string rectInfo = rt != null ? $"rectWidth={rt.rect.width:F2} sizeDelta={rt.sizeDelta} anchoredPos={rt.anchoredPosition}" : "no RectTransform";
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[BGExt:{callSite}] [{depth}] '{current.name}' | {rectInfo}");

                var hlg = current.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    if (PlateHandlers.VerboseLogging) LogManager.LogToConsole(
                        $"[BGExt:{callSite}]   HorizontalLayoutGroup: enabled={hlg.enabled} " +
                        $"childControlWidth={hlg.childControlWidth} childControlHeight={hlg.childControlHeight} " +
                        $"childForceExpandWidth={hlg.childForceExpandWidth} childForceExpandHeight={hlg.childForceExpandHeight} " +
                        $"childScaleWidth={hlg.childScaleWidth} childScaleHeight={hlg.childScaleHeight} " +
                        $"spacing={hlg.spacing} " +
                        $"padding=(L{hlg.padding.left},R{hlg.padding.right},T{hlg.padding.top},B{hlg.padding.bottom}) " +
                        $"childAlignment={hlg.childAlignment}"
                    );
                }

                var vlg = current.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    if (PlateHandlers.VerboseLogging) LogManager.LogToConsole(
                        $"[BGExt:{callSite}]   VerticalLayoutGroup: enabled={vlg.enabled} " +
                        $"childControlWidth={vlg.childControlWidth} childControlHeight={vlg.childControlHeight} " +
                        $"childForceExpandWidth={vlg.childForceExpandWidth} childForceExpandHeight={vlg.childForceExpandHeight}"
                    );
                }

                var le = current.GetComponent<LayoutElement>();
                if (le != null)
                {
                    if (PlateHandlers.VerboseLogging) LogManager.LogToConsole(
                        $"[BGExt:{callSite}]   LayoutElement: ignoreLayout={le.ignoreLayout} " +
                        $"minWidth={le.minWidth} preferredWidth={le.preferredWidth} flexibleWidth={le.flexibleWidth}"
                    );
                }

                var csf = current.GetComponent<ContentSizeFitter>();
                if (csf != null)
                {
                    if (PlateHandlers.VerboseLogging) LogManager.LogToConsole(
                        $"[BGExt:{callSite}]   ContentSizeFitter: horizontalFit={csf.horizontalFit} verticalFit={csf.verticalFit}"
                    );
                }

                if (current == stopAt) break;
                current = current.parent;
                depth++;
            }
        }

        private void AlignLeftAndExtend(RectTransform rt, string callSite = "unspecified")
        {
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[BGExt:{callSite}] ===== BEGIN (pre-rebuild) =====");
            LogLayoutChain(targetText.transform, rt.parent, callSite);

            Transform walk = targetText.rectTransform.parent;
            RectTransform layoutRoot = null;
            while (walk != null)
            {
                var hlg = walk.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    layoutRoot = walk.GetComponent<RectTransform>();
                    break;
                }
                if (walk == rt.parent)
                {
                    layoutRoot = walk.GetComponent<RectTransform>();
                    break;
                }
                walk = walk.parent;
            }

            if (layoutRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);

            targetText.ForceMeshUpdate(true, true);

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[BGExt:{callSite}] ===== AFTER ForceRebuildLayoutImmediate (target={layoutRoot?.name}) =====");
            LogLayoutChain(targetText.transform, rt.parent, callSite);

            Bounds textBounds = targetText.GetTextBounds(true);

            if (textBounds.size.x < 0f || string.IsNullOrEmpty(targetText.text))
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[BGExt:{callSite}] Skipping extend — empty/invalid text bounds.");
                return;
            }

            float textScaleX = targetText.transform.lossyScale.x;
            float textScaleY = targetText.transform.lossyScale.y;
            float rtScaleX = rt.lossyScale.x;
            float rtScaleY = rt.lossyScale.y;

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole(
                $"[BGExt:{callSite}] text='{targetText.text}' " +
                $"boundsMin={textBounds.min.x:F2} boundsMax={textBounds.max.x:F2} boundsSize={textBounds.size.x:F2} " +
                $"textRectWidth={targetText.rectTransform.rect.width:F2} " +
                $"textRectAnchoredPos={targetText.rectTransform.anchoredPosition} " +
                $"textScaleX={textScaleX:F4} textScaleY={textScaleY:F4} " +
                $"rtScaleX={rtScaleX:F4} rtScaleY={rtScaleY:F4} " +
                $"overflow={targetText.overflowMode} " +
                $"activeInHierarchy={targetText.gameObject.activeInHierarchy}"
            );

            Vector2 size = rt.sizeDelta;

            if (extendWidth)
            {
                float rawTextWidth = textBounds.size.x;
                float worldTextWidth = rawTextWidth * targetText.transform.lossyScale.x;
                float localTextWidth = worldTextWidth / rt.lossyScale.x;

                size.x = ClampAxis(
                    localTextWidth + paddingLeft + paddingRight,
                    minWidth,
                    maxWidth
                );
            }

            if (extendHeight)
            {
                float rawTextHeight = textBounds.size.y;
                float worldTextHeight = rawTextHeight * targetText.transform.lossyScale.y;
                float localTextHeight = worldTextHeight / rt.lossyScale.y;

                size.y = ClampAxis(
                    localTextHeight + paddingTop + paddingBottom,
                    minHeight,
                    maxHeight
                );
            }

            float localCenterX = textBounds.center.x;
            Vector3 localCenter = new Vector3(localCenterX, 0f, 0f);
            Vector3 worldCenter = targetText.transform.TransformPoint(localCenter);

            rt.pivot = new Vector2(0.5f, rt.pivot.y);
            rt.anchorMin = rt.pivot;
            rt.anchorMax = rt.pivot;

            Vector3 newWorldPos = rt.position;
            newWorldPos.x = worldCenter.x;
            rt.position = newWorldPos;

            rt.sizeDelta = size;

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole(
                $"[BGExt:{callSite}] RESULT rt='{rt.name}' finalSizeDelta={rt.sizeDelta} finalWorldPos={rt.position} " +
                $"finalPivot={rt.pivot} finalAnchorMin={rt.anchorMin}"
            );
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[BGExt:{callSite}] ===== END =====");
        }


        private static float ClampAxis(float value, float min, float max)
        {
            if (min > 0f && value < min) value = min;
            if (max > 0f && value > max) value = max;
            return value;
        }

        public void SetAnchoredPosition(Vector2 position)
        {
            if (Clone != null)
                Clone.anchoredPosition = position;
        }

        public void SetAnchorsAndPivot(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            if (Clone == null) return;
            Clone.anchorMin = anchorMin;
            Clone.anchorMax = anchorMax;
            Clone.pivot = pivot;
        }
    }
}
