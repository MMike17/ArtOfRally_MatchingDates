using UnityEngine;
using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace MatchingDates
{
    public class Settings : ModSettings, IDrawable
    {
        [Draw(DrawType.Toggle)]
        [Header("The default behaviour of this mod is to set cars unlock dates to their production year.")]
        public bool hideLockedInMenu;
        [Draw(DrawType.Toggle)]
        [Header("This option will change the cars of other racers in the leaderboards to comply with the current year.")]
        public bool replaceInLeaderboards;

        [Header("Debug")]
        [Draw(DrawType.Toggle)]
        public bool disableInfoLogs;

        public void OnChange()
        {
            Main.RefreshCarLocks();
        }

        public override void Save(ModEntry modEntry) => Save(this, modEntry);
    }
}
