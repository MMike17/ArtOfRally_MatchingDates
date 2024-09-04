using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace MatchingDates
{
    public class Settings : ModSettings, IDrawable
    {
        public enum Mode
        {
            lock_to_date,
            hide_in_selection
        }

        [Draw(DrawType.PopupList)]
        public Mode mode;

        public void OnChange()
        {
            Main.RefreshCarLocks();
        }

        public override void Save(ModEntry modEntry) => Save(this, modEntry);
    }
}
