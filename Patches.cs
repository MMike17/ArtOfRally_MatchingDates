using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

using static MatchingDates.Settings;
using Random = UnityEngine.Random;

namespace MatchingDates
{
    // used for init and caching valid cars
    [HarmonyPatch(typeof(CarChooserHelper), nameof(CarChooserHelper.InitHideClass))]
    static class CarChooserHelper_InitHideClass_Patch
    {
        public static bool isReady => detectedYear > 0 && originalList != null && truncatedList != null;
        public static int detectedYear { get; private set; }

        public static List<string> originalList;
        public static List<string> truncatedList;

        static CarChooserHelper instance;

        // gets called every time we open the car selection menu
        static void Prefix(CarChooserHelper __instance)
        {
            if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER)
                return;

            instance = __instance;

            Main.Try(() =>
            {
                // force unlock cars for this season
                detectedYear = int.Parse(__instance.GroupTitle.Text.text.Split(new string[] { "  |  " }, StringSplitOptions.None)[1]);
                CarManager.AllCarsList.ForEach(car => Main.SetCarUnlockState(car, car.carStats.YearUnlocked < detectedYear));

                Main.Log("Locked/unlocked cars for season " + detectedYear);
            });
        }

        static void Postfix(CarChooserHelper __instance)
        {
            if (!Main.enabled ||
                GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER ||
                (!Main.settings.hideLockedInMenu &&
                !Main.settings.replaceInLeaderboards))
                return;

            Main.Try(() =>
            {
                // store original and curated list
                originalList = new List<string>(__instance.CarButton.stringList);
                truncatedList = new List<string>(__instance.CarButton.stringList);

                List<string> toRemove = new List<string>();
                List<Car> currentCarsList = CarManager.GetCurrentCarsListForClass(GameModeManager.GetSeasonDataCurrentGameMode().CarClass);

                for (int i = 0; i < truncatedList.Count; i++)
                {
                    Car car = currentCarsList[i];

                    if (!Main.IsCarValid(car, detectedYear))
                        toRemove.Add(car.name);
                }

                toRemove.ForEach(car => truncatedList.Remove(car));

                if (truncatedList.Count == 0)
                {
                    detectedYear = 0;
                    Main.Log("New list is empty. Skipping feature.");
                    return;
                }

                __instance.CarButton.index = 0;
                __instance.CarButton.stringList = truncatedList;
                __instance.CarButton.stringListLength = truncatedList.Count - 1;

                // updated UI stats and name
                __instance.CarButton.GetType().GetMethod("UpdateTextures", BindingFlags.NonPublic | BindingFlags.Instance).
                    Invoke(__instance.CarButton, null);
                __instance.CarButton.GetType().GetMethod("UpdateCarSpecs", BindingFlags.NonPublic | BindingFlags.Instance).
                    Invoke(__instance.CarButton, null);

                __instance.CarButton.UpdateOptionTextAndArrows();

                // reselect first car of new list
                UIManager.Instance.PanelManager.CarChooserManager.SelectCarInClass(
                    GameModeManager.GetSeasonDataCurrentGameMode().CarClass,
                    originalList.IndexOf(truncatedList[0])
                );

                Main.Log("Cached adjusted list of cars.");
            });
        }

        public static int ToOriginalIndex(int index)
        {
            if (originalList == null)
            {
                Main.Log("originalList is null");
                return index;
            }

            if (truncatedList == null)
            {
                Main.Log("truncatedList is null");
                return index;
            }

            return originalList.IndexOf(truncatedList[index]);
        }

        public static int GetCurrentCarOriginalIndex() => instance != null ? ToOriginalIndex(instance.CarButton.index) : -1;
    }

    // replace returned car with currently selected
    [HarmonyPatch(typeof(CustomButtonCars), "GetCurrentCar")]
    static class CustomButtonCars_GetCurrentCar_Patch
    {
        static void Postfix(ref Car __result)
        {
            if (!Main.enabled ||
                GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER ||
                !CarChooserHelper_InitHideClass_Patch.isReady ||
                !Main.settings.hideLockedInMenu)
                return;

            Car result = __result;

            Main.Try(() =>
            {
                int adjustedIndex = CarChooserHelper_InitHideClass_Patch.GetCurrentCarOriginalIndex();
                result = CarManager.GetCurrentCarsListForClass(GameModeManager.GetSeasonDataCurrentGameMode().CarClass)[adjustedIndex];
            });

            __result = result;
        }
    }

    // replaces car selection to correct index
    [HarmonyPatch(typeof(CarChooserManager), nameof(CarChooserManager.ChangeCar))]
    static class CarChooserManager_ChangeCar_Patch
    {
        // this is called when we change cars
        static void Prefix(ref int index)
        {
            if (!Main.enabled ||
                GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER ||
                !CarChooserHelper_InitHideClass_Patch.isReady ||
                !Main.settings.hideLockedInMenu)
                return;

            int newIndex = index;
            Main.Try(() => newIndex = CarChooserHelper_InitHideClass_Patch.ToOriginalIndex(newIndex));
            index = newIndex;
        }
    }

    // makes sure we save the correct car selection
    [HarmonyPatch(typeof(CarManager), nameof(CarManager.SetChosenCar), new[] { typeof(int) })]
    static class CarManager_SetChosenCar_Patch
    {
        static void Prefix(ref int index)
        {
            if (!Main.enabled ||
                GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER ||
                !CarChooserHelper_InitHideClass_Patch.isReady ||
                !Main.settings.hideLockedInMenu)
                return;

            int newIndex = index;
            Main.Try(() => newIndex = CarChooserHelper_InitHideClass_Patch.ToOriginalIndex(newIndex));
            index = newIndex;
        }
    }

    // cache name substitution table
    [HarmonyPatch(typeof(RallyManager), nameof(RallyManager.LoadFirstStage))]
    static class RallyManager_LoadFirstStage_Patch
    {
        static Dictionary<string, string> substitutionTable;

        static void Postfix()
        {
            if (!Main.enabled ||
                GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER ||
                !CarChooserHelper_InitHideClass_Patch.isReady ||
                !Main.settings.replaceInLeaderboards)
                return;

            Main.Try(() =>
            {
                // generate substitution table
                substitutionTable = new Dictionary<string, string>();
                List<(string name, int count)> splittingTable = new List<(string, int)>();
                CarChooserHelper_InitHideClass_Patch.truncatedList.ForEach(carName => splittingTable.Add((carName, 0)));

                CarChooserHelper_InitHideClass_Patch.originalList.ForEach(name =>
                {
                    if (!CarChooserHelper_InitHideClass_Patch.truncatedList.Contains(name) && !substitutionTable.ContainsKey(name))
                    {
                        int index = 0;
                        int min = splittingTable[0].count;

                        for (int i = 0; i < splittingTable.Count; i++)
                        {
                            if (splittingTable[i].count < min)
                            {
                                min = splittingTable[i].count;
                                index = i;
                            }
                        }

                        splittingTable[index] = (splittingTable[index].name, splittingTable[index].count + 1);
                        substitutionTable.Add(name, splittingTable[index].name);
                    }
                });
            });
        }

        public static string ReplaceName(string carName)
        {
            if (!substitutionTable.ContainsKey(carName))
                return carName;

            return substitutionTable[carName];
        }
    }

    // replace car names in stage results
    [HarmonyPatch(typeof(StageResults), nameof(StageResults.UpdateStageResults))]
    static class StageResults_UpdateStageResults_Patch
    {
        static void Postfix(StageResults __instance)
        {
            Main.Log("This should be current race results");

            if (!Main.enabled ||
                GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER ||
                !CarChooserHelper_InitHideClass_Patch.isReady ||
                !Main.settings.replaceInLeaderboards)
                return;

            Main.Try(() =>
            {
                FieldInfo field = __instance.GetType().GetField("StandingsList", BindingFlags.NonPublic | BindingFlags.Instance);
                List<StageEntry> entries = (List<StageEntry>)field.GetValue(__instance);

                entries.ForEach(entry => entry.Car.text = RallyManager_LoadFirstStage_Patch.ReplaceName(entry.Car.text));
            });
        }
    }

    // replace car names in rally results
    [HarmonyPatch(typeof(StageResults), nameof(StageResults.UpdateEventResults))]
    static class StageResults_UpdateEventResults_Patch
    {
        static void Postfix(StageResults __instance)
        {
            Main.Log("This should be current rally results");

            if (!Main.enabled ||
                GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER ||
                !CarChooserHelper_InitHideClass_Patch.isReady ||
                !Main.settings.replaceInLeaderboards)
                return;

            Main.Try(() =>
            {
                FieldInfo field = __instance.GetType().GetField("StandingsList", BindingFlags.NonPublic | BindingFlags.Instance);
                List<StageEntry> entries = (List<StageEntry>)field.GetValue(__instance);

                entries.ForEach(entry => entry.Car.text = RallyManager_LoadFirstStage_Patch.ReplaceName(entry.Car.text));
            });
        }
    }

    // replace car names in season results
    [HarmonyPatch(typeof(SeasonStandingsScreen), nameof(SeasonStandingsScreen.Init))]
    static class SeasonStandingsScreen_Init_Patch
    {
        static void Postfix(SeasonStandingsScreen __instance)
        {
            Main.Log("This should be end of season screen");

            if (!Main.enabled ||
                GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER ||
                !CarChooserHelper_InitHideClass_Patch.isReady ||
                !Main.settings.replaceInLeaderboards)
                return;

            Main.Try(() =>
            {
                Transform root = __instance.transform.GetChild(1);

                foreach (Transform transform in root)
                {
                    Text carDisplay = transform.GetChild(3).GetComponent<Text>();
                    carDisplay.text = RallyManager_LoadFirstStage_Patch.ReplaceName(carDisplay.text);
                }
            });
        }
    }
}