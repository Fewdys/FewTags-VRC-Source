using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;

namespace FewTags
{
    public class FewTagsUpdater
    {
        private static float timeSinceLastUpdate = 0f;
        private static float updateInterval = 0f;

        public static void DoUpdate() // add this to either to the update of a monobehaviour or you're update loop
        {
            timeSinceLastUpdate += Time.deltaTime;

            if (Time.realtimeSinceStartup >= updateInterval)
            {
                updateInterval = Time.realtimeSinceStartup + (Main.UpdateIntervalMinutes * 60f);
                Main.UpdateTags();
                ConfigLoader.Load();
            }

            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.O))
            {
                var modalMMKeyboard = GameObject.Find("Canvas_MainMenu(Clone)/Container/MMParent/HeaderOffset/Modal_MM_Keyboard").gameObject.GetComponent<GraphicRaycaster>();
                if (!modalMMKeyboard.enabled)
                {
                    Main.isOverlay = !Main.isOverlay;
                    string msg = $"Nameplate Overlay Was {(Main.isOverlay ? "Enabled" : "Disabled")}";
                    // log message here
                    if (Main.p.Count != 0)
                    {
                        foreach (var user in VRCPlayerApi.AllPlayers)
                        {
                            foreach (VRC.Player p in Main.p)
                            {
                                VRC.Player player = user?.gameObject?.GetComponent<VRC.Player>();
                                if (p == player)
                                {
                                    if (player != null && p != null)
                                    {
                                        Main.NameplateESP(p);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
