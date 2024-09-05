using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

using static MatchingDates.Settings;

namespace MatchingDates
{
    // used for init and caching valid cars
    [HarmonyPatch(typeof(CarChooserHelper), nameof(CarChooserHelper.InitHideClass))]
    static class CarChooserHelper_InitHideClass_Patch
    {
        public static bool isReady => detectedYear > 0 && originalList != null && truncatedList != null;
        public static int detectedYear { get; private set; }

        static List<string> originalList;
        static List<string> truncatedList;

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
            if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER || Main.settings.mode != Mode.hide_in_menu)
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
                Main.settings.mode != Mode.hide_in_menu)
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
                Main.settings.mode != Mode.hide_in_menu)
                return;

            int newIndex = index;
            Main.Try(() => newIndex = CarChooserHelper_InitHideClass_Patch.ToOriginalIndex(newIndex));
            index = newIndex;
        }
    }

    // makes sure we save the correct car
    [HarmonyPatch(typeof(CarManager), nameof(CarManager.SetChosenCar), new[] { typeof(int) })]
    static class CarManager_SetChosenCar_Patch
    {
        static void Prefix(ref int index)
        {
            if (!Main.enabled ||
                GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER ||
                !CarChooserHelper_InitHideClass_Patch.isReady ||
                Main.settings.mode != Mode.hide_in_menu)
                return;

            int newIndex = index;
            Main.Try(() => newIndex = CarChooserHelper_InitHideClass_Patch.ToOriginalIndex(newIndex));
            index = newIndex;
        }
    }
}