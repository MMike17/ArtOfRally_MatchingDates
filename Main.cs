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
        const int MIN_YEAR = 1967;

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

                string forceUnlocked = "Force unlocked cars : ";
                string forceLocked = "Force locked cars : ";

                CarManager.AllCarsList.ForEach(car =>
                {
                    if (storeOriginal)
                        originalCarUnlock.Add(car, car.carStats.YearUnlocked);

                    if (enabled)
                    {
                        if (settings.mode == Mode.lock_to_date)
                        {
                            int newUnlock = CarNameProvider.GetCarYear(car.name);
                            car.carStats.YearUnlocked = newUnlock <= MIN_YEAR ? 0 : newUnlock;

                            // I'll keep this if we ever have extra cars with prior dates
                            //if (!car.carStats.IsUnlocked && car.carStats.YearUnlocked <= MIN_YEAR)
                            //{
                            //  SetCarUnlockState(car, true);
                            //  forceUnlocked += "\n- " + car.name;
                            //}

                            if (car.carStats.YearUnlocked > MIN_YEAR)
                            {
                                SetCarUnlockState(car, false);
                                forceLocked += "\n- " + car.name;
                            }
                        }
                        else
                            car.carStats.YearUnlocked = originalCarUnlock[car];
                    }
                    else
                        car.carStats.YearUnlocked = originalCarUnlock[car];
                });

                if (enabled)
                {
                    Log(forceUnlocked);
                    Log(forceLocked);

                    Log("Refreshed car locks to " + settings.mode);
                }
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

        static void SetCarUnlockState(Car car, bool state)
        {
            car.carStats.IsUnlocked = state;

            if (enabled && string.IsNullOrEmpty(car.carStats.UnlockedSaveConstant))
                car.carStats.UnlockedSaveConstant = "UNLOCKABLE_" + car.carStats.YearUnlocked;

            SaveGame.SetInt(car.carStats.UnlockedSaveConstant, state ? 1 : 0);
            SaveGame.Save();
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