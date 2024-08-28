using RealCarNames;
using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace MatchingDates
{
    // what do I need here ?

    // __we need infos about the cars and the corresponding dates__
    // Add hooks in "real names" to get info ?

    // __Upkeep__

    // copy readme structure

    public class Main
    {
        static ModEntry.ModLogger logger;

        // "Everything Works So Far"
        static int ewsfCount;

        static bool Load(ModEntry modEntry)
        {
            ewsfCount = 0;
            logger = modEntry.Logger;

            //

            return true;
        }

        public static void Log(string message) => logger.Log(message);

        public static void LogEwSF()
        {
            ewsfCount++;
            Log("EwSF (" + ewsfCount + ")");
        }
    }
}