using TMPro;
using UnityEngine;

namespace FewTags.FewTags
{
    public class Plate
    {
        public TextMeshProUGUI? Text { get; set; }
        public GameObject? _gameObject { get; set; }
        public TagAnimator? Animator { get; set; }

        public void Cleanup()
        {
            if (_gameObject != null && _gameObject) UnityEngine.Object.DestroyImmediate(_gameObject);
            _gameObject = null;

            if (Animator != null && Animator) UnityEngine.Object.DestroyImmediate(Animator);
            Animator = null;

            Text = null;
        }

        public Plate(VRC.Player __0, float position, string tag)
        {
            if (__0 == null) return;
            var vecPos = new Vector3(0, position, 0);
            var plateBase = __0._vrcplayer?.Nameplate;
            if (plateBase == null)
            {
                LogManager.LogErrorToConsole("Nameplate is null.");
                return;
            }

            if (plateBase.quickStats == null)
            {
                LogManager.LogErrorToConsole("quickStats is null.");
                return;
            }

            if (plateBase.contents == null)
            {
                LogManager.LogErrorToConsole("contents is null.");
                return;
            }

            ///
            /// ASSIGN COLOR HERE
            ///
            var color = Color.white; // white is a placeholder!
            ///
            /// END
            ///

            _gameObject = GameObject.Instantiate(plateBase.quickStats, plateBase.contents.transform).gameObject;

            if (_gameObject == null)
            {
                LogManager.LogErrorToConsole("Failed to instantiate plate prefab.");
                return;
            }

            string lowerTag = Utils.RemoveHtmlTags(tag);
            bool needsAnim = Utils.NeedsAnimator(lowerTag, out var applyAnim) && FewTags.EnableAnimations;

            if (needsAnim)
            {
                Animator = _gameObject.AddComponent<TagAnimator>();
                Animator.originalText = tag;
                applyAnim?.Invoke(Animator);
            }

            _gameObject.name = "FewTagsPlate";
            _gameObject.transform.localPosition = vecPos;
            _gameObject?.SetActive(true);

            var obj_t = _gameObject?.transform;
            if (obj_t == null)
            {
                LogManager.LogErrorToConsole("obj_t is null.");
                Cleanup();
                return;
            }

            obj_t.ColorPlate(color);

            Text = obj_t.Find("Trust Text").GetComponent<TextMeshProUGUI>();
            if (Text == null)
            {
                var trustText = Utils.RecursiveFindChild(obj_t, "Trust Text");
                if (trustText == null)
                {
                    LogManager.LogErrorToConsole("Couldn't find 'Trust Text' transform.");
                    Cleanup();
                    return;
                }

                Text = trustText.GetComponent<TextMeshProUGUI>();
                if (Text == null)
                {
                    LogManager.LogErrorToConsole("'Trust Text' exists, but has no TextMesh component.");
                    Cleanup();
                    return;
                }
            }

            Utils.DestroyChildren(_gameObject);

            if (Text != null)
            {
                Text.SetTextSafe(tag);
                Text.SetOverlay();
            }
            else
            {
                LogManager.LogErrorToConsole("Text component is null!");
                Cleanup();
                return;
            }
        }
    }

    public class PlateStatic
    {
        public TextMeshProUGUI? TextBP { get; set; }
        public TextMeshProUGUI? TextM { get; set; }
        public TextMeshProUGUI? TextID { get; set; }
        public GameObject? _gameObjectBP { get; set; }
        public GameObject? _gameObjectM { get; set; }
        public GameObject? _gameObjectID { get; set; }

        public TagAnimator? Animator { get; set; }

        public void Cleanup()
        {
            if (_gameObjectBP != null && _gameObjectBP)
                UnityEngine.Object.DestroyImmediate(_gameObjectBP);
            if (_gameObjectM != null && _gameObjectM)
                UnityEngine.Object.DestroyImmediate(_gameObjectM);
            if (_gameObjectID != null && _gameObjectID)
                UnityEngine.Object.DestroyImmediate(_gameObjectID);

            _gameObjectBP = null;
            _gameObjectM = null;
            _gameObjectID = null;

            TextBP = null;
            TextM = null;
            TextID = null;
        }

        public void UpdatePosition(float position, bool mainobj = false, bool bigplate = false, bool idplate = false)
        {
            var vecPos = new Vector3(0, position, 0);
            if (mainobj && _gameObjectM != null)
                _gameObjectM.transform.localPosition = vecPos;
            if (bigplate && _gameObjectBP != null)
                _gameObjectBP.transform.localPosition = vecPos;
            if (idplate && _gameObjectID != null)
                _gameObjectID.transform.localPosition = vecPos;
        }

        public PlateStatic(VRC.Player __0)
        {

            if (__0?._vrcplayer?.Nameplate?.quickStats == null || __0._vrcplayer.Nameplate.contents == null)
            {
                LogManager.LogErrorToConsole("Required nameplate components are null.");
                return;
            }

            ///
            /// ASSIGN COLOR HERE
            ///
            var color = Color.white; // white is a placeholder!
            ///
            /// END
            ///

            // ID
            _gameObjectID = GameObject.Instantiate(__0._vrcplayer.Nameplate.quickStats, __0._vrcplayer.Nameplate.contents.transform).gameObject;
            if (_gameObjectID == null)
            {
                LogManager.LogErrorToConsole("Failed to instantiate ID plate.");
                return;
            }

            _gameObjectID.name = "FewTags";
            var ID_obj_t = _gameObjectID.transform;
            if (ID_obj_t == null)
            {
                LogManager.LogErrorToConsole("ID_obj_t is null.");
                Cleanup();
                return;
            }

            TextID = ID_obj_t.Find("Trust Text").GetComponent<TextMeshProUGUI>();
            if (TextID == null)
            {
                var trustText = Utils.RecursiveFindChild(ID_obj_t, "Trust Text");
                if (trustText == null)
                {
                    LogManager.LogErrorToConsole("Couldn't find 'Trust Text' transform.");
                    Cleanup();
                    return;
                }

                TextID = trustText.GetComponent<TextMeshProUGUI>();
                if (TextID == null)
                {
                    LogManager.LogErrorToConsole("'Trust Text' exists, but has no TextMesh component.");
                    Cleanup();
                    return;
                }
            }
            Utils.DestroyChildren(_gameObjectID);
            ID_obj_t.ColorPlate(color);
            ID_obj_t.localPosition = new Vector3(0, FewTags.PositionID, 0);
            _gameObjectID.SetActive(true);
            TextID.SetTextSafe("");
            TextID.SetOverlay();

            // Malicious Or Normal Tag
            _gameObjectM = GameObject.Instantiate(__0._vrcplayer.Nameplate.quickStats, __0._vrcplayer.Nameplate.contents.transform).gameObject;
            if (_gameObjectM == null)
            {
                LogManager.LogErrorToConsole("Failed to instantiate main plate.");
                Cleanup();
                return;
            }

            _gameObjectM.name = "FewTags";
            var M_obj_t = _gameObjectM.transform;
            if (M_obj_t == null)
            {
                LogManager.LogErrorToConsole("M_obj_t is null.");
                Cleanup();
                return;
            }

            TextM = M_obj_t.Find("Trust Text").GetComponent<TextMeshProUGUI>();
            if (TextM == null)
            {
                var trustText = Utils.RecursiveFindChild(M_obj_t, "Trust Text");
                if (trustText == null)
                {
                    LogManager.LogErrorToConsole("Couldn't find 'Trust Text' transform.");
                    Cleanup();
                    return;
                }

                TextM = trustText.GetComponent<TextMeshProUGUI>();
                if (TextM == null)
                {
                    LogManager.LogErrorToConsole("'Trust Text' exists, but has no TextMesh component.");
                    Cleanup();
                    return;
                }
            }
            Utils.DestroyChildren(_gameObjectM);
            M_obj_t.ColorPlate(color);
            M_obj_t.localPosition = new Vector3(0, FewTags.Position, 0);
            _gameObjectM.SetActive(true);
            TextM.SetTextSafe("");
            TextM.SetOverlay();

            // BigPlate
            _gameObjectBP = GameObject.Instantiate(__0._vrcplayer.Nameplate.quickStats, __0._vrcplayer.Nameplate.contents.transform).gameObject;
            if (_gameObjectBP == null)
            {
                LogManager.LogErrorToConsole("Failed to instantiate big plate.");
                Cleanup();
                return;
            }

            _gameObjectBP.name = "FewTagsBigPlate";
            var BP_obj_t = _gameObjectBP.transform;
            if (BP_obj_t == null)
            {
                LogManager.LogErrorToConsole("BP_obj_t is null.");
                Cleanup();
                return;
            }

            TextBP = BP_obj_t.Find("Trust Text").GetComponent<TextMeshProUGUI>();
            if (TextBP == null)
            {
                var trustText = Utils.RecursiveFindChild(BP_obj_t, "Trust Text");
                if (trustText == null)
                {
                    LogManager.LogErrorToConsole("Couldn't find 'Trust Text' transform.");
                    Cleanup();
                    return;
                }

                TextBP = trustText.GetComponent<TextMeshProUGUI>();
                if (TextBP == null)
                {
                    LogManager.LogErrorToConsole("'Trust Text' exists, but has no TextMesh component.");
                    Cleanup();
                    return;
                }
            }
            Utils.DestroyChildren(_gameObjectBP);
            BP_obj_t.ColorPlate(color);
            BP_obj_t.localPosition = new Vector3(0, FewTags.PositionBigText, 0);
            BP_obj_t.GetComponent<ImageThreeSlice>().enabled = false;
            _gameObjectBP.SetActive(true);
            TextBP.SetTextSafe("");
            TextBP.SetOverlay();
        }
    }
}