using HarmonyLib;
using RealCarNames;
using System.Reflection;

using static UnityModManagerNet.UnityModManager;

namespace MatchingDates
{
    static class Main
    {
        public static bool enabled { get; private set; }

        static ModEntry.ModLogger logger;

        static bool Load(ModEntry modEntry)
        {
            logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;

            Harmony harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }

        static bool OnToggle(ModEntry modEntry, bool state)
        {
            enabled = state;
            return true;
        }

        public static bool IsCarValid(string carName, string rallyYear)
        {
            return int.Parse(rallyYear) >= int.Parse(CarNameProvider.years[CarNameProvider.DetectCarName(carName)]);
        }

        public static void Log(string message) => logger.Log(message);
    }
}