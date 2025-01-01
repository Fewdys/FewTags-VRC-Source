using UnityEngine;
using UnityEngine.UI;

namespace FewTags
{
    internal class KeybindManager
    {
        public static void Update()
        {
            if (Input.GetKeyDown(KeyCode.F) && Input.GetKey(KeyCode.L))
            {
                Entry.UpdateTags();
            }

            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.O))
            {
                if (GameObject.Find("Canvas_MainMenu(Clone)/Container/MMParent/Modal_MM_Keyboard").gameObject.GetComponent<GraphicRaycaster>().enabled != true)
                {
                    Entry.overlay = !Entry.overlay;
                    Entry.NameplateOverlay(Entry.overlay);
                    if (Entry.p.Count != 0)
                    {
                        try
                        {
                            foreach (var player in Entry.p)
                            {
                                if (player != null)
                                {
                                    Entry.NameplateESP(player);
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
