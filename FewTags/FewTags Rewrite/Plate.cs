using System.Linq;
using UnityEngine;

//Code Is Still Aids But It Is What It Is Bc I Don't Feel Like Redoing The Json's

namespace FewTags
{
    internal class Plate
    {
        public TMPro.TextMeshProUGUI Text { get; set; }
        public static GameObject _gameObject { get; set; }
        private static RectTransform[] _rectTransforms { get; set; }
        ~Plate() { _rectTransforms = null; _gameObject = null; }
        public Plate(VRC.Player __0, float position, bool overlay)
        {
            //Main Plate
            _gameObject = GameObject.Instantiate(__0._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_5, __0._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_0.transform).gameObject;
            _gameObject.name = $"FewTagsPlate";
            Text = _gameObject.GetComponentsInChildren<TMPro.TextMeshProUGUI>().First(x => x.name == "Trust Text");
            _rectTransforms = _gameObject.GetComponentsInChildren<RectTransform>().Where(x => x.name != "Trust Text" && x.name != "FewTagsPlate").ToArray();
            for (int i = 0; i < _rectTransforms.Length; i++)
            {
                try
                {
                    Object.DestroyImmediate(_rectTransforms[i].gameObject);
                }
                catch { }
            }
            _gameObject.transform.localPosition = new Vector3(0, position, 0);
            _gameObject.SetActive(true);
            Text.text = "";
            Text.isOverlay = overlay;
        }
    }

    internal class PlateStatic
    {
        public TMPro.TextMeshProUGUI TextBP { get; set; }
        public TMPro.TextMeshProUGUI TextM { get; set; }
        public TMPro.TextMeshProUGUI TextID { get; set; }
        public static GameObject _gameObjectBP { get; set; }
        public static GameObject _gameObjectM { get; set; }
        public static GameObject _gameObjectID { get; set; }
        private RectTransform[] _rectTransformsBP { get; set; }
        private RectTransform[] _rectTransformsM { get; set; }
        private RectTransform[] _rectTransformsID { get; set; }
        ~PlateStatic() { _rectTransformsM = null; _gameObjectM = null; _rectTransformsBP = null; _gameObjectBP = null; _rectTransformsID = null; _gameObjectID = null; }
        public PlateStatic(VRC.Player __0, bool overlay)
        {
            //ID
            _gameObjectID = GameObject.Instantiate(__0._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_5, __0._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_0.transform).gameObject;
            _gameObjectID.name = "FewTags";
            TextID = _gameObjectID.GetComponentsInChildren<TMPro.TextMeshProUGUI>().First(x => x.name == "Trust Text");
            _rectTransformsID = _gameObjectID.GetComponentsInChildren<RectTransform>().Where(x => x.name != "Trust Text" && x.name != "FewTags").ToArray();
            for (int i = 0; i < _rectTransformsID.Length; i++)
            {
                try
                {
                    Object.DestroyImmediate(_rectTransformsID[i].gameObject);
                }
                catch { }
            }
            _gameObjectID.transform.localPosition = new Vector3(0, Main.PositionID, 0);
            _gameObjectID.SetActive(true);
            TextID.text = "";
            TextID.isOverlay = overlay;

            //Malicious Or Normal Tag
            _gameObjectM = GameObject.Instantiate(__0._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_5, __0._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_0.transform).gameObject;
            _gameObjectM.name = "FewTags";
            TextM = _gameObjectM.GetComponentsInChildren<TMPro.TextMeshProUGUI>().First(x => x.name == "Trust Text");
            _rectTransformsM = _gameObjectM.GetComponentsInChildren<RectTransform>().Where(x => x.name != "Trust Text" && x.name != "FewTags").ToArray();
            for (int i = 0; i < _rectTransformsM.Length; i++)
            {
                try
                {
                    Object.DestroyImmediate(_rectTransformsM[i].gameObject);
                }
                catch { }
            }
            _gameObjectM.transform.localPosition = new Vector3(0, Main.PositionID - 28f, 0);
            _gameObjectM.SetActive(true);
            TextM.text = "";
            TextM.isOverlay = overlay;

            //BigPlate
            _gameObjectBP = GameObject.Instantiate(__0._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_5, __0._vrcplayer.field_Public_PlayerNameplate_0.field_Public_GameObject_0.transform).gameObject;
            _gameObjectBP.name = "FewTagsBigPlate";
            TextBP = _gameObjectBP.GetComponentsInChildren<TMPro.TextMeshProUGUI>().First(x => x.name == "Trust Text");
            _rectTransformsBP = _gameObjectBP.GetComponentsInChildren<RectTransform>().Where(x => x.name != "Trust Text" && x.name != "FewTagsBigPlate").ToArray();
            for (int i = 0; i < _rectTransformsBP.Length; i++)
            {
                try
                {
                    Object.DestroyImmediate(_rectTransformsBP[i].gameObject);
                }
                catch { }
            }
            _gameObjectBP.transform.localPosition = new Vector3(0, Main.PositionBigText, 0);
            _gameObjectBP.transform.GetComponent<ImageThreeSlice>().color = new Color(1, 1, 1, 0f);
            _gameObjectBP.SetActive(true);
            TextBP.text = "";
            TextBP.isOverlay = overlay;
        }
    }
}