using HarmonyLib;
using RealCarNames;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

using static MatchingDates.Settings;
using static UnityModManagerNet.UnityModManager;

namespace MatchingDates
{
    static class Main
    {
        public static bool enabled { get; private set; }
        public static Settings settings { get; private set; }

        static ModEntry.ModLogger logger;
        static Dictionary<Car, int> originalCarUnlock;

        static bool Load(ModEntry modEntry)
        {
            logger = modEntry.Logger;
            settings = ModSettings.Load<Settings>(modEntry);

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = entry => settings.Draw(modEntry);
            modEntry.OnSaveGUI = entry => settings.Save(modEntry);

            Harmony harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (FindMod("RealCarNames") == null)
            {
                logger.Error("Required mod \"Real car names\" cannot be found, please make sure this mod is installed.");
                return false;
            }

            return true;
        }

        static bool OnToggle(ModEntry modEntry, bool state)
        {
            enabled = state;
            RefreshCarLocks();
            return true;
        }

        public static void RefreshCarLocks()
        {
            Try(() =>
            {
                bool storeOriginal = originalCarUnlock == null;

                if (storeOriginal)
                    originalCarUnlock = new Dictionary<Car, int>();

                CarManager.AllCarsList.ForEach(car =>
                {
                    if (storeOriginal)
                        originalCarUnlock.Add(car, car.carStats.YearUnlocked);

                    if (enabled)
                    {
                        if (settings.mode == Mode.lock_to_date)
                            car.carStats.YearUnlocked = CarNameProvider.GetCarYear(car.name) - 1;
                        else
                            car.carStats.YearUnlocked = originalCarUnlock[car];
                    }
                    else
                        car.carStats.YearUnlocked = originalCarUnlock[car];
                });

                if (enabled)
                    Log("Refreshed car locks to " + settings.mode);
                else // restores locks properly
                {
                    int maxYear = 0;
                    GameModeManager.CareerManager.AllSeasons.ForEach(seasons =>
                    {
                        Season selected = seasons.FindLast(season => season.Status == Season.STATUS.COMPLETED);

                        if (selected != null)
                            maxYear = Mathf.Max(maxYear, selected.Year);
                    });

                    CarManager.AllCarsList.ForEach(car => SetCarUnlockState(car, car.carStats.YearUnlocked <= maxYear));
                    Log("Restored original car unlocks state");
                }
            });
        }

        public static void SetCarUnlockState(Car car, bool state)
        {
            Try(() =>
            {
                car.carStats.IsUnlocked = state;

                if (enabled && string.IsNullOrEmpty(car.carStats.UnlockedSaveConstant))
                    car.carStats.UnlockedSaveConstant = "UNLOCKABLE_" + car.carStats.YearUnlocked;

                if (!enabled && car.carStats.YearUnlocked == 0)
                    car.carStats.UnlockedSaveConstant = string.Empty;

                if (!string.IsNullOrEmpty(car.carStats.UnlockedSaveConstant))
                {
                    SaveGame.SetInt(car.carStats.UnlockedSaveConstant, state ? 1 : 0);
                    SaveGame.Save();
                }
            });
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