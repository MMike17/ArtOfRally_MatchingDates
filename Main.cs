using HarmonyLib;
using RealCarNames;
using System;
using System.Reflection;
using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace MatchingDates
{
    static class Main
    {
        public static bool enabled { get; private set; }
        public static Settings settings { get; private set; }

        static ModEntry.ModLogger logger;

        static bool Load(ModEntry modEntry)
        {
            logger = modEntry.Logger;
            settings = ModSettings.Load<Settings>(modEntry);

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = entry => settings.Draw(modEntry);
            modEntry.OnSaveGUI = entry => settings.Save(modEntry);

            Harmony harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }

        static bool OnToggle(ModEntry modEntry, bool state)
        {
            enabled = state;
            return true;
        }

        public static void RefreshCarLocks()
        {
            // what are the original car locks ?
            // what are the new car locks ?

            // set unlock date
            // make sure the car is unlocked
        }

        public static bool IsCarValid(string carName, int rallyYear) => rallyYear >= CarNameProvider.GetCarYear(carName);

        public static void Log(string message) => logger.Log(message);

        public static void Try(Action callback)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }
    }
}