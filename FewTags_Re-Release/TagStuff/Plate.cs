using TMPro;
using UnityEngine;

namespace FewTags
{
    public static class ObjectUtils
    {
        public static List<string> ObjectsToDestroy = new List<string> { "Trust Icon", "Performance Icon", "Performance Text", "Friend Anchor Stats", "Reason" };

        public static void DestroyChildren(GameObject? obj) // actually just hiding but yea lol
        {
            foreach (var name in ObjectsToDestroy)
            {
                var find = obj?.transform.Find(name);
                if (find != null)
                    find.gameObject?.SetActive(false);
            }
        }

        public static Transform RecursiveFindChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                    return child;

                var result = RecursiveFindChild(child, childName);
                if (result != null)
                    return result;
            }
            return parent.Find(childName);
        }
    }
    public class Plate
    {
        public TextMeshProUGUI? Text { get; set; }
        public GameObject? _gameObject { get; set; }
        public TagAnimator? Animator { get; set; }

        ~Plate()
        {
            Cleanup();
        }

        public void Cleanup()
        {
            _gameObject = null;
            Text = null;
            Animator = null;
        }

        public Plate(VRC.Player __0, float position, string tag)
        {
            if (__0 == null) return;
            var vecPos = new UnityEngine.Vector3(0, position, 0);
            var plateBase = __0._vrcplayer?.Nameplate;
            if (plateBase == null)
            {
                string Error = "Nameplate is null.";
                // log error here
                return;
            }

            if (plateBase.quickStats == null)
            {
                string Error2 = "quickStats is null.";
                // log error here
                return;
            }

            if (plateBase.contents == null)
            {
                string Error3 = "contents is null.";
                // log error here
                return;
            }

            _gameObject = GameObject.Instantiate(plateBase.quickStats, plateBase.contents.transform).gameObject;

            if (_gameObject == null)
            {
                string Failed = "Failed to instantiate plate prefab.";
                // log error here
                return;
            }

            string lowerTag = Main.RemoveHtmlTags(tag).ToLower();
            bool needsAnimator = Main.EnableAnimations &&
            (
                lowerTag.StartsWith(".lbl.") ||
                lowerTag.StartsWith(".cyln.") ||
                lowerTag.StartsWith(".rain.") ||
                lowerTag.StartsWith(".sr.") ||
                lowerTag.StartsWith(".pulse.") ||
                lowerTag.StartsWith(".jump.") ||
                lowerTag.StartsWith(".shake.") ||
                lowerTag.StartsWith(".gt.") ||
                lowerTag.StartsWith(".blink.") ||
                lowerTag.StartsWith(".glitch.")
            );

            if (needsAnimator)
            {
                Animator = _gameObject.AddComponent<TagAnimator>();
                Animator.originalText = tag;
                if (lowerTag.StartsWith(".lbl.")) Animator.LetterByLetter = true;
                else if (lowerTag.StartsWith(".cyln.")) Animator.Bounce = true;
                else if (lowerTag.StartsWith(".rain.")) Animator.Rainbow = true;
                else if (lowerTag.StartsWith(".sr.")) Animator.SmoothRainbow = true;
                else if (lowerTag.StartsWith(".pulse.")) Animator.Pulse = true;
                else if (lowerTag.StartsWith(".jump.")) Animator.Jump = true;
                else if (lowerTag.StartsWith(".shake.")) Animator.Shake = true;
                else if (lowerTag.StartsWith(".gt.")) Animator.GhostTrail = true;
                else if (lowerTag.StartsWith(".blink.")) Animator.Blink = true;
                else if (lowerTag.StartsWith(".glitch.")) Animator.Glitch = true;
            }

            _gameObject.name = "FewTagsPlate";
            _gameObject.transform.localPosition = vecPos;
            _gameObject?.SetActive(true);

            Text = _gameObject?.transform.Find("Trust Text").GetComponent<TextMeshProUGUI>();
            if (Text == null)
            {
                var trustText = ObjectUtils.RecursiveFindChild(_gameObject?.transform, "Trust Text");
                if (trustText == null)
                {
                    string Error4 = "Couldn't find 'Trust Text' transform.";
                    // log error here
                    Cleanup();
                    return;
                }

                Text = trustText.GetComponent<TextMeshProUGUI>();
                if (Text == null)
                {
                    string Error5 = "'Trust Text' exists, but has no TextMesh component.";
                    // log error here
                    Cleanup();
                    return;
                }
            }

            ObjectUtils.DestroyChildren(_gameObject);

            if (Text != null)
            {
                Text.text = tag;
                Text.isOverlay = Main.isOverlay;
            }
            else
            {
                string Failed2 = "Text component is null!";
                // log error here
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

        ~PlateStatic()
        {
            Cleanup();
        }

        public void Cleanup()
        {
            _gameObjectID = null;
            _gameObjectM = null;
            _gameObjectBP = null;
            TextBP = null;
            TextM = null;
            TextID = null;
            Animator = null;
        }

        public PlateStatic(VRC.Player __0, string tag = null)
        {
            if (__0?._vrcplayer?.Nameplate?.quickStats == null || __0._vrcplayer.Nameplate.contents == null)
            {
                string Error = "Required nameplate components are null.";
                // log error here
                return;
            }

            // ID
            _gameObjectID = GameObject.Instantiate(__0._vrcplayer.Nameplate.quickStats, __0._vrcplayer.Nameplate.contents.transform).gameObject;
            if (_gameObjectID == null)
            {
                string Error2 = "Failed to instantiate ID plate.";
                // log error here
                return;
            }

            _gameObjectID.name = "FewTags";
            TextID = _gameObjectID.transform.Find("Trust Text").GetComponent<TextMeshProUGUI>();
            if (TextID == null)
            {
                var trustText = ObjectUtils.RecursiveFindChild(_gameObjectID.transform, "Trust Text");
                if (trustText == null)
                {
                    string Error3 = "Couldn't find 'Trust Text' transform.";
                    // log error here
                    Cleanup();
                    return;
                }

                TextID = trustText.GetComponent<TextMeshProUGUI>();
                if (TextID == null)
                {
                    string Error4 = "'Trust Text' exists, but has no TextMesh component.";
                    // log error here
                    Cleanup();
                    return;
                }
            }
            ObjectUtils.DestroyChildren(_gameObjectID);
            _gameObjectID.transform.localPosition = new UnityEngine.Vector3(0, Main.PositionID, 0);
            _gameObjectID.SetActive(true);
            TextID.text = "";
            TextID.isOverlay = Main.isOverlay;

            // Malicious Or Normal Tag
            _gameObjectM = GameObject.Instantiate(__0._vrcplayer.Nameplate.quickStats, __0._vrcplayer.Nameplate.contents.transform).gameObject;
            if (_gameObjectM == null)
            {
                string Failed = "Failed to instantiate main plate.";
                // log error here
                Cleanup();
                return;
            }

            _gameObjectM.name = "FewTags";
            TextM = _gameObjectM.transform.Find("Trust Text").GetComponent<TextMeshProUGUI>();
            if (TextM == null)
            {
                var trustText = ObjectUtils.RecursiveFindChild(_gameObjectM.transform, "Trust Text");
                if (trustText == null)
                {
                    string Error5 = "Couldn't find 'Trust Text' transform.";
                    // log error here
                    Cleanup();
                    return;
                }

                TextM = trustText.GetComponent<TextMeshProUGUI>();
                if (TextM == null)
                {
                    string Error6 = "'Trust Text' exists, but has no TextMesh component.";
                    // log error here
                    Cleanup();
                    return;
                }
            }
            ObjectUtils.DestroyChildren(_gameObjectM);
            _gameObjectM.transform.localPosition = new UnityEngine.Vector3(0, Main.Position, 0);
            _gameObjectM.SetActive(true);
            TextM.text = "";
            TextM.isOverlay = Main.isOverlay;

            // BigPlate
            _gameObjectBP = GameObject.Instantiate(__0._vrcplayer.Nameplate.quickStats, __0._vrcplayer.Nameplate.contents.transform).gameObject;
            if (_gameObjectBP == null)
            {
                string Failed2 = "Failed to instantiate big plate.";
                // log error here
                Cleanup();
                return;
            }
            if (!string.IsNullOrEmpty(tag))
            {
                string lowerTag = Main.RemoveHtmlTags(tag).ToLower();
                bool needsAnimator = Main.EnableAnimations &&
                (
                    lowerTag.StartsWith(".lbl.") ||
                    lowerTag.StartsWith(".cyln.") ||
                    lowerTag.StartsWith(".rain.") ||
                    lowerTag.StartsWith(".sr.") ||
                    lowerTag.StartsWith(".pulse.") ||
                    lowerTag.StartsWith(".jump.") ||
                    lowerTag.StartsWith(".shake.") ||
                    lowerTag.StartsWith(".gt.") ||
                    lowerTag.StartsWith(".blink.") ||
                    lowerTag.StartsWith(".glitch.")
                );
    
                if (needsAnimator)
                {
                    Animator = _gameObjectBP.AddComponent<TagAnimator>();
                    Animator.originalText = tag;
                    if (lowerTag.StartsWith(".lbl.")) Animator.LetterByLetter = true;
                    else if (lowerTag.StartsWith(".cyln.")) Animator.Bounce = true;
                    else if (lowerTag.StartsWith(".rain.")) Animator.Rainbow = true;
                    else if (lowerTag.StartsWith(".sr.")) Animator.SmoothRainbow = true;
                    else if (lowerTag.StartsWith(".pulse.")) Animator.Pulse = true;
                    else if (lowerTag.StartsWith(".jump.")) Animator.Jump = true;
                    else if (lowerTag.StartsWith(".shake.")) Animator.Shake = true;
                    else if (lowerTag.StartsWith(".gt.")) Animator.GhostTrail = true;
                    else if (lowerTag.StartsWith(".blink.")) Animator.Blink = true;
                    else if (lowerTag.StartsWith(".glitch.")) Animator.Glitch = true;
                }
            }

            _gameObjectBP.name = "FewTagsBigPlate";
            TextBP = _gameObjectBP.transform.Find("Trust Text").GetComponent<TextMeshProUGUI>();
            if (TextBP == null)
            {
                var trustText = ObjectUtils.RecursiveFindChild(_gameObjectBP.transform, "Trust Text");
                if (trustText == null)
                {
                    string Error7 = "Couldn't find 'Trust Text' transform.";
                    // log error here
                    Cleanup();
                    return;
                }

                TextBP = trustText.GetComponent<TextMeshProUGUI>();
                if (TextBP == null)
                {
                    string Error8 = "'Trust Text' exists, but has no TextMesh component.";
                    // log error here
                    Cleanup();
                    return;
                }
            }
            ObjectUtils.DestroyChildren(_gameObjectBP);
            _gameObjectBP.transform.localPosition = new UnityEngine.Vector3(0, Main.PositionBigText, 0);
            _gameObjectBP.transform.GetComponent<ImageThreeSlice>().enabled = false;
            _gameObjectBP.SetActive(true);
            TextBP.text = tag;
            TextBP.isOverlay = Main.isOverlay;
        }
    }

}


