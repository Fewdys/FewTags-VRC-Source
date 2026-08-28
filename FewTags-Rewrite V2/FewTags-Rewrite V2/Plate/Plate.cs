using FewTags.FewTags_Rewrite_V2.Plate;
using TMPro;
using UnityEngine;

namespace FewTags.FewTags
{
    public class Plate
    {
        public TextMeshProUGUI? Text { get; set; } = null;
        public GameObject? _gameObject { get; set; } = null;
        public TagAnimator? Animator { get; set; } = null;

        /// <summary>
        /// Duplicated + resized background for this plate. Null when
        /// FewTags.DisableBackgrounds is true or no background was found.
        /// </summary>
        public BackgroundExtender? Extender { get; set; } = null;

        /// <summary>
        /// True if this plate's "qs" anchor resolved via the ExpandedInfo nameplate fragment
        /// instead of the normal Quick Stats one.
        /// </summary>
        public bool IsExpandedInfo { get; private set; } = false;
        public Transform? MovableTransform => _gameObject?.transform;

        public void Cleanup()
        {
            ClearRefs();
        }

        public void ClearRefs()
        {
            Animator = null;
            Text = null;
            _gameObject = null;
            Extender = null;
        }

        /// <summary>
        /// Re-runs the background extension against the current text. Call this
        /// if you change Text.text after construction and want the background
        /// clone to catch up.
        /// </summary>
        public void RefreshBackground()
        {
            Extender?.ExtendBackground("RefreshBackground.Plate");
        }

        public Plate(VRC.Player __0, float position, string tag)
        {
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] ctor start | tag='{tag}' | position={position}");

            if (__0 == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] __0 (VRC.Player) is null. Aborting.");
                return;
            }

            var plateobj = __0.GetPlayerNameplateContainer();
            if (plateobj == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] Nameplate is null.");
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Got nameplate container: '{plateobj.name}' (id={plateobj.GetInstanceID()})");

            var quickStatsObj = plateobj.transform.Find("PlayerNameplate/Canvas/NameplateGroup/Nameplate/Contents/Quick Stats")?.gameObject;
            var expandedInfoObj = quickStatsObj == null
                ? plateobj.transform.Find("PlayerNameplate/Canvas/NameplateGroup/NameplateFragment/ExpandedInfo")?.gameObject
                : null;
            var qs = quickStatsObj ?? expandedInfoObj;
            IsExpandedInfo = expandedInfoObj != null;

            if (qs == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] quickStats/info is null.");
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Got quickStats/info: '{qs.name}' (id={qs.GetInstanceID()}) | IsExpandedInfo={IsExpandedInfo}");

            var parent = qs.transform.parent;
            if (parent == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] contents/qs parent is null.");
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Got parent: '{parent.name}' (id={parent.GetInstanceID()})");

            _gameObject = GameObject.Instantiate(qs, parent).gameObject;
            if (_gameObject == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] Failed to instantiate plate prefab.");
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Instantiated _gameObject (id={_gameObject.GetInstanceID()})");

            //Utils.IgnoreParentLayout(_gameObject);

            _gameObject.name = "FewTagsPlate";

            var pill = _gameObject.transform.childCount > 0 ? _gameObject.transform.GetChild(0) : null;
            bool hasPillGroup = pill != null && pill.name.Contains("Group");
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Pill lookup: found={pill != null} name='{pill?.name}' isGroup={hasPillGroup}");

            // Centered (x=0) regardless of ExpandedInfo — the pill's own anchoring inside its
            // parent handles horizontal placement, we just set the Y anchor.
            var vecPos = new Vector3(0f, IsExpandedInfo && FewTags.UnderNameplate ? position - (FewTags.SpacingExpanded + 22f) : position, 0);
            var backgroundGameObject = hasPillGroup ? pill?.Find("Background")?.gameObject : null;
            if (FewTags.DisableBackgrounds)
            {
                if (backgroundGameObject != null) backgroundGameObject.SetActive(false);
                var img = hasPillGroup ? pill?.GetComponentInChildren<ImageThreeSlice>() : _gameObject.GetComponentInChildren<ImageThreeSlice>();
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] DisableBackgrounds=true, ImageThreeSlice Found={img != null}, BackgroundGameObject Found={backgroundGameObject != null}");
                if (img != null) img.enabled = false;
            }

            _gameObject.transform.localPosition = vecPos;

            _gameObject?.SetActive(true);
            if (hasPillGroup)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Activating pill group '{pill.name}'");
                pill.gameObject.SetActive(true);

                //var rect = _gameObject.GetComponent<RectTransform>();
                //if (rect != null)
                //    rect.sizeDelta = new Vector2(30, rect.sizeDelta.y);
            }

            var obj_t = _gameObject?.transform;
            if (obj_t == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] obj_t is null.");
                Cleanup();
                return;
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Calling DestroyChildren on '{_gameObject.name}' (id={_gameObject.GetInstanceID()})");
            Utils.DestroyChildren(_gameObject);

            var TextTransform = Utils.RecursiveFindChild(obj_t, "Trust Text", true) ?? Utils.RecursiveFindChild(obj_t, "Name", true);
            if (TextTransform == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] Couldn't find 'Text' transform.");
                Cleanup();
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Found TextTransform: '{TextTransform.name}' (id={TextTransform.GetInstanceID()}) fullPath (approx from obj_t)");

            Text = TextTransform.GetComponent<TextMeshProUGUI>();
            if (Text == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] 'Text' exists, but has no TextMesh component.");
                Cleanup();
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Got Text component (id={Text.GetInstanceID()}) on gameObject '{Text.gameObject.name}' (id={Text.gameObject.GetInstanceID()})");

            Text.horizontalAlignment = HorizontalAlignmentOptions.Center;

            _gameObject.transform.localScale = Vector3.one;

            if (Text != null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Before SetTextSafe: Text.gameObject.activeInHierarchy={Text.gameObject.activeInHierarchy}, activeSelf={Text.gameObject.activeSelf}");
                Text.SetTextSafe(tag);
                Text.SetOverlay();
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] After SetTextSafe: Text.text='{Text.text}' (expected tag='{tag}')");
            }
            else
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] Text component is null!");
                Cleanup();
                return;
            }

            // ── Background extension ──────────────────────────────────────
            // Only the pill layout gets the duplicate + resize treatment.
            // Non-pill layout is left exactly as it already is — no clone, no
            // repositioning.
            //
            // backgroundGameObject was captured before DestroyChildren; Destroy()
            // is deferred so the reference is still valid this frame (same pattern
            // this codebase already relies on for TextTransform above).
            if (hasPillGroup && !FewTags.DisableBackgrounds && backgroundGameObject != null && Text != null)
            {
                var bgRect = backgroundGameObject.GetComponent<RectTransform>();
                if (bgRect != null)
                {
                    Extender = new BackgroundExtender
                    {
                        sourceBackground = bgRect,
                        targetText = Text,
                        extendWidth = true,
                        extendHeight = true,
                    };
                    Extender.ExtendBackground("Plate.ctor");

                    // The clone lands exactly on top of the original (Instantiate
                    // keeps local position when parent is unchanged), so hide the
                    // original to avoid a double-background.
                    backgroundGameObject.SetActive(false);

                    if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] Extended background clone created (id={Extender.Clone?.gameObject.GetInstanceID()})");
                }
                else
                {
                    if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[Plate] backgroundGameObject has no RectTransform, skipping extension.");
                }
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[Plate] ctor complete for tag='{tag}' | _gameObject id={_gameObject.GetInstanceID()} | Text id={Text?.GetInstanceID()}");
        }
    }

    public class PlateStatic
    {
        public TextMeshProUGUI? TextBP { get; set; } = null;
        public TextMeshProUGUI? TextM { get; set; } = null;
        public TextMeshProUGUI? TextID { get; set; } = null;
        public GameObject? _gameObjectBP { get; set; } = null;
        public GameObject? _gameObjectM { get; set; } = null;
        public GameObject? _gameObjectID { get; set; } = null;
        public TagAnimator? AnimatorBP { get; set; } = null;
        public TagAnimator? AnimatorM { get; set; } = null;
        public TagAnimator? AnimatorID { get; set; } = null;

        /// <summary>
        /// Duplicated + resized backgrounds for the ID and Malicious/Normal (M)
        /// sub-plates. BigPlate (BP) is intentionally excluded since its
        /// background is always disabled elsewhere in this constructor.
        /// </summary>
        public BackgroundExtender? ExtenderID { get; set; } = null;
        public BackgroundExtender? ExtenderM { get; set; } = null;

        /// <summary>
        /// True if this player's "qs" anchor resolved via the ExpandedInfo nameplate fragment
        /// instead of the normal Quick Stats one. Set once in the constructor and reused by
        /// PlateHandlers for spacing/centering decisions so it isn't recomputed per-plate.
        /// </summary>
        public bool IsExpandedInfo { get; private set; } = false;

        public void Cleanup()
        {
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic] Cleanup() called - clearing all refs.");
            ClearRefs();
        }

        public void ClearRefs()
        {
            AnimatorBP = null;
            AnimatorM = null;
            AnimatorID = null;

            TextBP = null;
            TextM = null;
            TextID = null;

            _gameObjectBP = null;
            _gameObjectM = null;
            _gameObjectID = null;

            ExtenderID = null;
            ExtenderM = null;
        }

        /// <summary>
        /// Re-runs background extension for ID after its text changes post-construction
        /// (e.g. once PlateHandlers assigns the real ID string).
        /// </summary>
        public void RefreshBackgroundID()
        {
            ExtenderID?.ExtendBackground("RefreshBackgroundID.Plate");
        }

        /// <summary>
        /// Re-runs background extension for M after its text changes post-construction
        /// (e.g. once PlateHandlers assigns the real Malicious/Normal tag string).
        /// </summary>
        public void RefreshBackgroundM()
        {
            ExtenderM?.ExtendBackground("RefreshBackgroundM.Plate");
        }

        /// <summary>
        /// Repositions the requested sub-plate(s). Always centered (x=0); moves the pill
        /// transform under ExpandedInfo instead of the root, so it actually lands where told.
        /// </summary>
        public void UpdatePosition(float position, bool mainobj = false, bool bigplate = false, bool idplate = false)
        {
            var vecPos = new Vector3(0f, position, 0);

            if (mainobj)
            {
                var t = _gameObjectM?.transform;
                if (t != null) t.localPosition = vecPos;
            }
            if (bigplate)
            {
                var t = _gameObjectBP?.transform;
                if (t != null) t.localPosition = vecPos;
            }
            if (idplate)
            {
                var t = _gameObjectID?.transform;
                if (t != null) t.localPosition = vecPos;
            }
        }

        public PlateStatic(VRC.Player __0)
        {
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic] ctor start");

            var plateobj = __0.GetPlayerNameplateContainer();
            if (plateobj == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic] Nameplate is null.");
                return;
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic] Got Nameplate: '{plateobj.name}' (id={plateobj.GetInstanceID()})");

            var quickStatsObj = plateobj.transform.Find("PlayerNameplate/Canvas/NameplateGroup/Nameplate/Contents/Quick Stats")?.gameObject;
            var expandedInfoObj = quickStatsObj == null
                ? plateobj.transform.Find("PlayerNameplate/Canvas/NameplateGroup/NameplateFragment/ExpandedInfo")?.gameObject
                : null;
            var qs = quickStatsObj ?? expandedInfoObj;
            IsExpandedInfo = expandedInfoObj != null;

            if (qs == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic] quickStats/info is null.");
                return;
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic] Got QuickStats/Info: '{qs.name}' (id={qs.GetInstanceID()}) | IsExpandedInfo={IsExpandedInfo}");

            var parent = qs.transform.parent;
            if (parent == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic] contents/qs parent is null.");
                return;
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic] Got Parent: '{parent.name}' (id={parent.GetInstanceID()})");

            // ==================== ID ====================
            _gameObjectID = GameObject.Instantiate(qs, parent).gameObject;
            if (_gameObjectID == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][ID] Failed to instantiate ID plate.");
                return;
            }

            //Utils.IgnoreParentLayout(_gameObjectID);

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] Created Object (id={_gameObjectID.GetInstanceID()})");

            _gameObjectID.name = "FewTagsID";
            var ID_obj_t = _gameObjectID.transform;
            if (ID_obj_t == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][ID] ID_obj_t is null.");
                Cleanup();
                return;
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] Calling DestroyChildren on '{_gameObjectID.name}' (id={_gameObjectID.GetInstanceID()})");
            Utils.DestroyChildren(_gameObjectID);

            var TextTransformID = Utils.RecursiveFindChild(ID_obj_t, "Trust Text", true) ?? Utils.RecursiveFindChild(ID_obj_t, "Name", true);
            if (TextTransformID == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][ID] Couldn't find 'Text' transform.");
                Cleanup();
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] Found TextTransform: '{TextTransformID.name}' (id={TextTransformID.GetInstanceID()})");

            TextID = TextTransformID.GetComponent<TextMeshProUGUI>();
            if (TextID == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][ID] 'Text' exists, but has no TextMesh component.");
                Cleanup();
                return;
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] Got TextID component (id={TextID.GetInstanceID()}) on gameObject '{TextID.gameObject.name}' (id={TextID.gameObject.GetInstanceID()})");

            TextID.horizontalAlignment = HorizontalAlignmentOptions.Center;

            _gameObjectID.transform.localScale = Vector3.one;

            var pillID = ID_obj_t.childCount > 0 ? ID_obj_t.GetChild(0) : null;
            bool hasPillGroupID = pillID != null && pillID.name.Contains("Group");
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] Pill lookup: found={pillID != null} name='{pillID?.name}' isGroup={hasPillGroupID}");

            var backgroundGameObjectID = hasPillGroupID ? pillID?.Find("Background")?.gameObject : null;
            if (FewTags.DisableBackgrounds)
            {
                if (backgroundGameObjectID != null) backgroundGameObjectID.SetActive(false);
                var img = _gameObjectID.GetComponentInChildren<ImageThreeSlice>();
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] DisableBackgrounds=true, ImageThreeSlice Found={img != null}, BackgroundGameObject Found={backgroundGameObjectID != null}");
                if (img != null) img.enabled = false;
            }

            ID_obj_t.localPosition = new Vector3(0f, IsExpandedInfo && FewTags.UnderNameplate ? FewTags.PositionID - 84f : IsExpandedInfo ? FewTags.PositionID : FewTags.PositionID, 0);

            _gameObjectID.SetActive(true);
            if (hasPillGroupID)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] Activating pill group '{pillID.name}'");
                pillID.gameObject.SetActive(true);

                //var rect = _gameObjectID.GetComponent<RectTransform>();
                //if (rect != null)
                //    rect.sizeDelta = new Vector2(30, rect.sizeDelta.y);
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] Before SetTextSafe: activeInHierarchy={TextID.gameObject.activeInHierarchy}, activeSelf={TextID.gameObject.activeSelf}");
            TextID.SetTextSafe("");
            TextID.SetOverlay();
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] After SetTextSafe: TextID.text='{TextID.text}'");

            // ── Background extension (ID) ──────────────────────────────────
            // Only apply for the pill layout; non-pill is left untouched.
            if (hasPillGroupID && !FewTags.DisableBackgrounds && backgroundGameObjectID != null && TextID != null)
            {
                var bgRectID = backgroundGameObjectID.GetComponent<RectTransform>();
                if (bgRectID != null)
                {
                    ExtenderID = new BackgroundExtender
                    {
                        sourceBackground = bgRectID,
                        targetText = TextID,
                        extendWidth = true,
                        extendHeight = true,
                    };
                    ExtenderID.ExtendBackground("Plate.ctor");
                    backgroundGameObjectID.SetActive(false);

                    if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][ID] Extended background clone created (id={ExtenderID.Clone?.gameObject.GetInstanceID()})");
                }
            }

            // ==================== Malicious Or Normal Tag (M) ====================
            _gameObjectM = GameObject.Instantiate(qs, parent).gameObject;
            if (_gameObjectM == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][M] Failed to instantiate main plate.");
                Cleanup();
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] Created Object (id={_gameObjectM.GetInstanceID()})");

            //Utils.IgnoreParentLayout(_gameObjectM);

            _gameObjectM.name = "FewTagsMalicious";
            var M_obj_t = _gameObjectM.transform;
            if (M_obj_t == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][M] M_obj_t is null.");
                Cleanup();
                return;
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] Calling DestroyChildren on '{_gameObjectM.name}' (id={_gameObjectM.GetInstanceID()})");
            Utils.DestroyChildren(_gameObjectM);

            var TextTransformM = Utils.RecursiveFindChild(M_obj_t, "Trust Text", true) ?? Utils.RecursiveFindChild(M_obj_t, "Name", true);
            if (TextTransformM == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][M] Couldn't find 'Text' transform.");
                Cleanup();
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] Found TextTransform: '{TextTransformM.name}' (id={TextTransformM.GetInstanceID()})");

            TextM = TextTransformM.GetComponent<TextMeshProUGUI>();
            if (TextM == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][M] 'Text' exists, but has no TextMesh component.");
                Cleanup();
                return;
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] Got TextM component (id={TextM.GetInstanceID()}) on gameObject '{TextM.gameObject.name}' (id={TextM.gameObject.GetInstanceID()})");

            TextM.horizontalAlignment = HorizontalAlignmentOptions.Center;

            _gameObjectM.transform.localScale = Vector3.one;

            var pillM = M_obj_t.childCount > 0 ? M_obj_t.GetChild(0) : null;
            bool hasPillGroupM = pillM != null && pillM.name.Contains("Group");
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] Pill lookup: found={pillM != null} name='{pillM?.name}' isGroup={hasPillGroupM}");

            var backgroundGameObjectM = hasPillGroupM ? pillM?.Find("Background")?.gameObject : null;
            if (FewTags.DisableBackgrounds)
            {
                if (backgroundGameObjectM != null) backgroundGameObjectM.SetActive(false);
                var img = hasPillGroupM ? pillM?.GetComponentInChildren<ImageThreeSlice>() : _gameObjectM.GetComponentInChildren<ImageThreeSlice>();
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] DisableBackgrounds=true, ImageThreeSlice Found={img != null}, BackgroundGameObject Found={backgroundGameObjectM != null}");
                if (img != null) img.enabled = false;
            }

            M_obj_t.localPosition = new Vector3(0f, IsExpandedInfo && FewTags.UnderNameplate ? -260.95f : IsExpandedInfo ? -176.95f : FewTags.Position, 0);

            _gameObjectM.SetActive(true);
            if (hasPillGroupM)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] Activating pill group '{pillM.name}'");
                pillM.gameObject.SetActive(true);

                //var rect = _gameObjectM.GetComponent<RectTransform>();
                //if (rect != null)
                //    rect.sizeDelta = new Vector2(30, rect.sizeDelta.y);
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] Before SetTextSafe: activeInHierarchy={TextM.gameObject.activeInHierarchy}, activeSelf={TextM.gameObject.activeSelf}");
            TextM.SetTextSafe("");
            TextM.SetOverlay();
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] After SetTextSafe: TextM.text='{TextM.text}'");

            // ── Background extension (M) ───────────────────────────────────
            // Only apply for the pill layout; non-pill is left untouched.
            if (hasPillGroupM && !FewTags.DisableBackgrounds && backgroundGameObjectM != null && TextM != null)
            {
                var bgRectM = backgroundGameObjectM.GetComponent<RectTransform>();
                if (bgRectM != null)
                {
                    ExtenderM = new BackgroundExtender
                    {
                        sourceBackground = bgRectM,
                        targetText = TextM,
                        extendWidth = true,
                        extendHeight = true,
                    };
                    ExtenderM.ExtendBackground("Plate.ctor");
                    backgroundGameObjectM.SetActive(false);

                    if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][M] Extended background clone created (id={ExtenderM.Clone?.gameObject.GetInstanceID()})");
                }
            }

            // ==================== BigPlate (BP) ====================
            _gameObjectBP = GameObject.Instantiate(qs, parent).gameObject;
            if (_gameObjectBP == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][BP] Failed to instantiate big plate.");
                Cleanup();
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][BP] Created Object (id={_gameObjectBP.GetInstanceID()})");

            //Utils.IgnoreParentLayout(_gameObjectBP);

            _gameObjectBP.name = "FewTagsBigPlate";
            var BP_obj_t = _gameObjectBP.transform;
            if (BP_obj_t == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][BP] BP_obj_t is null.");
                Cleanup();
                return;
            }

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][BP] Calling DestroyChildren on '{_gameObjectBP.name}' (id={_gameObjectBP.GetInstanceID()})");
            Utils.DestroyChildren(_gameObjectBP);

            var TextTransformB = Utils.RecursiveFindChild(BP_obj_t, "Trust Text", true) ?? Utils.RecursiveFindChild(BP_obj_t, "Name", true);
            if (TextTransformB == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][BP] Couldn't find 'Text' transform.");
                Cleanup();
                return;
            }
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][BP] Found TextTransform: '{TextTransformB.name}' (id={TextTransformB.GetInstanceID()})");

            TextBP = TextTransformB.GetComponent<TextMeshProUGUI>();
            if (TextBP == null)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole("[PlateStatic][BP] 'Text' exists, but has no TextMesh component.");
                Cleanup();
                return;
            }

            TextBP.horizontalAlignment = HorizontalAlignmentOptions.Center;

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][BP] Got TextBP component (id={TextBP.GetInstanceID()}) on gameObject '{TextBP.gameObject.name}' (id={TextBP.gameObject.GetInstanceID()})");

            _gameObjectBP.transform.localScale = Vector3.one;

            var pillBP = BP_obj_t.childCount > 0 ? BP_obj_t.GetChild(0) : null;
            bool hasPillGroupBP = pillBP != null && pillBP.name.Contains("Group");
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][BP] Pill lookup: found={pillBP != null} name='{pillBP?.name}' isGroup={hasPillGroupBP}");

            BP_obj_t.localPosition = new Vector3(0f, IsExpandedInfo && FewTags.UnderNameplate ? FewTags.PositionBigText + FewTags.BigPlateOffsetExpanded : FewTags.PositionBigText, 0);
            var bpImg = hasPillGroupBP ? pillBP?.GetComponentInChildren<ImageThreeSlice>() : _gameObjectBP.GetComponent<ImageThreeSlice>();
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][BP] ImageThreeSlice on BP root found? {bpImg != null}");
            if (bpImg != null) bpImg.enabled = false;
            _gameObjectBP.SetActive(true);
            var backgroundGameObjectBP = hasPillGroupBP ? pillBP?.Find("Background")?.gameObject : null;
            if (backgroundGameObjectBP != null)
            {
                backgroundGameObjectBP.gameObject.SetActive(false);
            }
            if (hasPillGroupBP)
            {
                if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][BP] Activating pill group '{pillBP.name}'");
                pillBP.gameObject.SetActive(true);

                //var rect = _gameObjectBP.GetComponent<RectTransform>();
                //if (rect != null)
                //    rect.sizeDelta = new Vector2(30, rect.sizeDelta.y);
            }


            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][BP] Before SetTextSafe: activeInHierarchy={TextBP.gameObject.activeInHierarchy}, activeSelf={TextBP.gameObject.activeSelf}");
            TextBP.SetTextSafe("");
            TextBP.SetOverlay();
            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic][BP] After SetTextSafe: TextBP.text='{TextBP.text}'");

            if (PlateHandlers.VerboseLogging) LogManager.LogToConsole($"[PlateStatic] ctor complete | ID={TextID?.GetInstanceID()} M={TextM?.GetInstanceID()} BP={TextBP?.GetInstanceID()} | IsExpandedInfo={IsExpandedInfo}");
        }
    }
}