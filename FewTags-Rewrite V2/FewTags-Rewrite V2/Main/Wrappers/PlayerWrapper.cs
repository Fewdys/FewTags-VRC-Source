using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;

namespace FewTags.FewTags.Wrappers
{
    public static class PlayerWrapper
    {
        public static Il2CppSystem.Collections.Generic.List<VRCPlayerApi> GetAllPlayers() => VRCPlayerApi.AllPlayers;
        public static Il2CppSystem.Collections.Generic.List<Player> GetAllVRCPlayers() => VRC.PlayerManager.prop_PlayerManager_0?.PlayerList;
    }
}
