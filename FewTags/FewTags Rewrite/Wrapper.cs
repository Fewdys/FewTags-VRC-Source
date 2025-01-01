using System.Collections.Generic;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using MelonLoader;
using System.Linq;

namespace FewTags
{
    public static class PlayerWrapper
    {
        public static APIUser apiUser;
        public static Dictionary<int, Player> PlayersActorID = new Dictionary<int, Player>();
        public static VRC.Player LocalPlayer() => VRC.Player.prop_Player_0;
        public static VRCPlayer LocalVRCPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;
        public static float GetFrames(this VRC.Player player) => (player._playerNet.prop_Byte_0 != 0) ? Mathf.Floor(1000f / (float)player._playerNet.prop_Byte_0) : -1f;
        public static short GetPing(this VRC.Player player) => player._playerNet.field_Private_Int16_0;
        public static bool IsBot(this VRC.Player player) => player.GetPing() <= 0 && player.GetFrames() <= 0 || player.transform.position == Vector3.zero;
        public static UnityEngine.Color GetTrustColor(this VRC.Player player) => VRCPlayer.Method_Public_Static_Color_APIUser_0(player.field_Private_APIUser_0);
        public static APIUser GetAPIUser(this VRCPlayer Instance) => Instance.GetPlayer().GetAPIUser();
        public static VRCPlayerApi GetVRCPlayerApi(this VRC.Player Instance) => Instance?.prop_VRCPlayerApi_0;
        public static bool GetIsMaster(this VRC.Player Instance) => Instance.GetVRCPlayerApi().isMaster;
        public static int GetActorNumber(this VRC.Player player) => player.GetVRCPlayerApi() != null ? player.GetVRCPlayerApi().playerId : -1;
        public static bool IsFriendsWith(this APIUser apiUser) => APIUser.CurrentUser.friendIDs.Contains(apiUser.id);
        public static VRCPlayer GetLocalVRCPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;
        public static bool IsMe(this Player p) => p.name == GetLocalVRCPlayer().name;
        public static bool CheckFriend(this Player vrcPlayer) => VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_Player_0.field_Private_APIUser_0.friendIDs.Contains(vrcPlayer.field_Private_APIUser_0.id);
        public static APIUser GetAPIUser(this VRC.Player player)
        {
            return player.field_Private_APIUser_0;
        }
        public static VRC.Player GetPlayer(this VRCPlayer vrcPlayer)
        {
            return vrcPlayer._player;
        }
        public static System.Random nr = new System.Random();
        public static string RandomString(int length)
        {
            char[] array = "abcdefghlijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToArray<char>();
            string empty = string.Empty;
            for (int index = 0; index < length; ++index)
                empty += array[nr.Next(array.Length)].ToString();
            return empty;
        }
    }
}
