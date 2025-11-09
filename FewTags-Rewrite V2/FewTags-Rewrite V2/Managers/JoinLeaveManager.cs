using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRC.Core;

namespace FewTags.FewTags
{
    internal class JoinLeaveManager
    {
        /// <summary>
        /// Ensures Tags Are Added For Player Upon Joining.
        /// </summary>
        internal static void EnsureTagsAreAdded(VRC.Player __0)
        {

            if (FewTags.s_tags == null)
            {
                LogManager.LogToConsole("s_tags is not initialized. Force-updating...");
                FewTagsUpdater.UpdateTags(); // Force a synchronous update
            }

            PlateHandlers.PlateHandler(__0); // we no longer need to check specifically for our db since the function returns if not found in local or db

        }

        /// <summary>
        /// Basic Check Needed For OnPlayerJoined.
        /// </summary>
        internal static void DoBasicJoinCheck(VRC.Player player)
        {
            /*var apiuser = player.APIUser;
            if (apiuser != null)
            {
                FewTagsUpdater.lastAppliedTags.Remove(apiuser.id); // this is done just for the sake of local tags to ensure they are working properly
                FewTagsUpdater.lastBigPlateText.Remove(apiuser.id); // this is done just for the sake of local tags to ensure they are working properly
            }*/
            EnsureTagsAreAdded(player);
        }

        /// <summary>
        /// Basic Check Needed For OnPlayerLeft/OnPlayerLeave.
        /// </summary>
        internal static void DoBasicLeaveCheck(VRC.Player player)
        {
            if (FewTags.p.Contains(player))
            {
                FewTags.p.Remove(player);
            }
        }
    }
}
