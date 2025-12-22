using UnityEngine;

namespace FewTags.FewTags
{
    // please note this will need to be modifed per client depending on how you do you're nameplates / nameplate stats
    // don't forget to register this in il2cpp
    internal class MenuDetector : MonoBehaviour
    {
        static GameObject qm;
        static Canvas qmcanvas;

        public VRC.Player player;

        Vector3 originalLocalPos;
        float currentOffset = 0f;

        void Start()
        {
            originalLocalPos = transform.localPosition;
        }

        void Update()
        {
            if (FewTags.UnderNameplate) return;

            if (qm == null)
                qm = GameObject.Find("Canvas_QuickMenu(Clone)");

            if (qm == null)
                return;

            if (qmcanvas == null)
                qmcanvas = qm.GetComponent<Canvas>();

            if (qmcanvas == null)
                return;

            if (player == null || player._vrcplayer == null)
                return;

            float targetOffset = 0f;

            if (qmcanvas.enabled)
            {
                if (VRC.Player.prop_Player_0 != null && VRC.Player.prop_Player_0 == player)
                    targetOffset = 28f; // move up once -- ideally you don't actually see you're own nameplate sooooo
                else
                    targetOffset = 76f; // move up twice and + 20 because of how nameplate is
            }

            else if (IsOtherStuffInactiveOrLODCheck(out int count))
            {
                if (count == 1)
                    targetOffset = -28f; // move down once something is disable based on lod
                /*else if (count == 2)
                    targetOffset = -56f; // move down twice, two things are disable based on lod
                else if (count == 3)
                    targetOffset = -84f;*/ // move down three times everything was disabled based on lod

                // and so on and so forth
            }

            if (!Mathf.Approximately(currentOffset, targetOffset))
            {
                transform.localPosition = originalLocalPos + Vector3.up * targetOffset;
                currentOffset = targetOffset;
            }
        }

        bool IsOtherStuffInactiveOrLODCheck(out int count) // this is simple an example
        {
            int localCount = 0;

            if (player == null || player._vrcplayer == null)
            {
                count = 0;
                return false;
            }

            var nameplate = player._vrcplayer.Nameplate?.gameObject;
            if (nameplate == null)
            {
                count = 0;
                return false;
            }

            // logic to detect something disabled

            ///
            /// for example
            /// 

            /*

            var stats = nameplate.GetComponent<NamePlates>();
            if (stats == null)
            {
                count = 0;
                return false;
            }

            if (stats.stats != null && !stats.stats.gameObject.activeSelf)
                localCount++;
            */

            count = localCount;
            return localCount > 0;
        }
    }
}
