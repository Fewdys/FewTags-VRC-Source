using Newtonsoft.Json;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VRC;
using VRC.Core;

namespace FewTags
{
    public class Chatbox
    {
        private static CustomJson.Custom[]? _customcolor { get; set; }
        public static CustomJson.CustomColors? colors { get; set; }
        public static string? file { get; set; }
        public static void CreateShit()
        {
            if (!Directory.Exists(@"FewTags"))
            {
                Directory.CreateDirectory(@"FewTags");
            }
            new WaitForSecondsRealtime(1f);
            string text = "{" + '\u0022' + "players" + '\u0022' + ": [ {" + '\u0022' + "UserID" + '\u0022' + ": " + '\u0022' + '\u0022' + "," + '\u0022' + "red" + '\u0022' + ":" + "0," + '\u0022' + "green" + '\u0022' + ":" + "0," + '\u0022' + "blue" + '\u0022' + ":" + "0," + '\u0022' + "alpha" + '\u0022' + ":" + "0," + '\u0022' + "textred" + '\u0022' + ":" + "0," + '\u0022' + "textgreen" + '\u0022' + ":" + "0," + '\u0022' + "textblue" + '\u0022' + ":" + "0" + "} ] }";
            if (!File.Exists(@"FewTags\CustomColors.json"))
            {
                File.Create(@"FewTags\CustomColors.json");
            }
        }
        public static void GetColors()
        {
            file = File.ReadAllText(@"FewTags\CustomColors.json");
            colors = JsonConvert.DeserializeObject<CustomJson.CustomColors>(file);
        }
        public static bool HasCustomColor(APIUser apiUser)
        {
            if (File.Exists(@"FewTags\CustomColors.json"))
            {
                if (File.ReadAllText(@"FewTags\CustomColors.json").Contains(apiUser.id))
                {
                    return true;
                }
            }

            return false;
        }
        public static void UpdateMyChatBoxOnJoin(VRC.Player vrcPlayer)
        {
            try
            {
                _customcolor = colors.players.Where(x => x.UserID == vrcPlayer.field_Private_APIUser_0.id).ToArray();
                if (HasCustomColor(vrcPlayer.field_Private_APIUser_0) == true)
                {
                    for (int i = 0; i < _customcolor?.Length; i++)
                    {
                        if (vrcPlayer.IsMe() == true)
                        {
                            VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat/ChatText").transform.gameObject.GetComponent<TMP_Text>().color = new Color(_customcolor[i].textred, _customcolor[i].textgreen, _customcolor[i].textblue, 1);
                            VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(_customcolor[i].red, _customcolor[i].green, _customcolor[i].blue, _customcolor[i].alpha);
                            VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubbleMirror/Canvas/Chat/ChatText").transform.gameObject.GetComponent<TMP_Text>().color = new Color(_customcolor[i].textred, _customcolor[i].textgreen, _customcolor[i].textblue, 1);
                            VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubbleMirror/Canvas/Chat").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(_customcolor[i].red, _customcolor[i].green, _customcolor[i].blue, _customcolor[i].alpha);
                            VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/TypingIndicator").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(_customcolor[i].red, _customcolor[i].green, _customcolor[i].blue, _customcolor[i].alpha);
                            VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/TypingIndicator/Text").transform.gameObject.GetComponent<TMP_Text>().color = new Color(_customcolor[i].textred, _customcolor[i].textgreen, _customcolor[i].textblue, 1);
                            VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubbleMirror/Canvas/TypingIndicator").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(_customcolor[i].red, _customcolor[i].green, _customcolor[i].blue, _customcolor[i].alpha);
                            VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubbleMirror/Canvas/TypingIndicator/Text").transform.gameObject.GetComponent<TMP_Text>().color = new Color(_customcolor[i].textred, _customcolor[i].textgreen, _customcolor[i].textblue, 1);
                        }
                        else
                        {
                            vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat/ChatText").gameObject.transform.gameObject.GetComponent<TMP_Text>().color = new Color(_customcolor[i].textred, _customcolor[i].textgreen, _customcolor[i].textblue, 1);
                            vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat").gameObject.transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(_customcolor[i].red, _customcolor[i].green, _customcolor[i].blue, _customcolor[i].alpha);
                            vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/TypingIndicator").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(_customcolor[i].red, _customcolor[i].green, _customcolor[i].blue, _customcolor[i].alpha);
                            vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/TypingIndicator/Text").transform.gameObject.GetComponent<TMP_Text>().color = new Color(_customcolor[i].textred, _customcolor[i].textgreen, _customcolor[i].textblue, 1);
                        }
                    }
                }
                else
                {
                    if (vrcPlayer.IsMe() == true)
                    {
                        VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat/ChatText").transform.gameObject.GetComponent<TMP_Text>().color = Color.red;
                        VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(0, 0, 0, 0.29f);
                        VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubbleMirror/Canvas/Chat/ChatText").transform.gameObject.GetComponent<TMP_Text>().color = Color.red;
                        VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubbleMirror/Canvas/Chat").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(0, 0, 0, 0.29f);
                        VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/TypingIndicator").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(0, 0, 0, 0.29f);
                        VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/TypingIndicator/Text").transform.gameObject.GetComponent<TMP_Text>().color = Color.red;
                        VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubbleMirror/Canvas/TypingIndicator").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(0, 0, 0, 0.29f);
                        VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubbleMirror/Canvas/TypingIndicator/Text").transform.gameObject.GetComponent<TMP_Text>().color = Color.red;
                    }
                    else if (vrcPlayer.field_Private_APIUser_0.isFriend)
                    {
                        vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat/ChatText").gameObject.transform.gameObject.GetComponent<TMP_Text>().color = Color.cyan;
                        vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat").gameObject.transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(0, 0, 0, 0.29f);
                        vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/TypingIndicator").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(0, 0, 0, 0.29f);
                        vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/TypingIndicator/Text").transform.gameObject.GetComponent<TMP_Text>().color = Color.cyan;
                    }
                    else
                    {
                        if (vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat").gameObject.transform.gameObject.GetComponent<ImageThreeSlice>().color != new Color(PlayerWrapper.GetTrustColor(vrcPlayer).r, PlayerWrapper.GetTrustColor(vrcPlayer).g, PlayerWrapper.GetTrustColor(vrcPlayer).b, 0.29f) || vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubbleMirror/Canvas/Chat").gameObject.transform.gameObject.GetComponent<ImageThreeSlice>().color != new Color(PlayerWrapper.GetTrustColor(vrcPlayer).r, PlayerWrapper.GetTrustColor(vrcPlayer).g, PlayerWrapper.GetTrustColor(vrcPlayer).b, 0.29f))
                        {
                            vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/Chat").gameObject.transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(PlayerWrapper.GetTrustColor(vrcPlayer).r, PlayerWrapper.GetTrustColor(vrcPlayer).g, PlayerWrapper.GetTrustColor(vrcPlayer).b, 0.29f);
                            vrcPlayer._vrcplayer.field_Public_GameObject_0.gameObject.transform.FindChild("ChatBubble/Canvas/TypingIndicator").transform.gameObject.GetComponent<ImageThreeSlice>().color = new Color(PlayerWrapper.GetTrustColor(vrcPlayer).r, PlayerWrapper.GetTrustColor(vrcPlayer).g, PlayerWrapper.GetTrustColor(vrcPlayer).b, 0.29f);
                        }
                    }
                }
            }
            catch { }
        }
    }
    public class CustomJson
    {
        [Serializable]
        public class CustomColors
        {
            public List<Custom> players { get; set; }
        }

        public class Custom
        {
            public string UserID { get; set; }
            public float red { get; set; }
            public float green { get; set; }
            public float blue { get; set; }
            public float alpha { get; set; }
            public float textred { get; set; }
            public float textgreen { get; set; }
            public float textblue { get; set; }
        }
    }
}
