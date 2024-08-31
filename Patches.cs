using HarmonyLib;
using RealCarNames;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MatchingDates
{
    [HarmonyPatch(typeof(CarChooserHelper), "Init")]
    static class CarChooserHelper_Init_Patch
    {
        public static int detectedYear { get; private set; }

        static List<string> originalList;
        static List<string> truncatedList;

        static void Prefix()
        {
            if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER)
                return;

            // reset car selection index
            SaveGame.SetInt(GameModeManager.GetSeasonDataCurrentGameMode().CarClass.ToString(), 0);
        }

        static void Postfix(CarChooserHelper __instance)
        {
            if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER)
                return;

            // remove unavailable cars from selection list
            originalList = new List<string>(__instance.CarButton.stringList);
            truncatedList = new List<string>(__instance.CarButton.stringList);

            detectedYear = int.Parse(__instance.GroupTitle.Text.text.Split(new string[] { "  |  " }, StringSplitOptions.None)[1]);
            List<string> toRemove = new List<string>();

            originalList.ForEach(carName =>
            {
                if (!Main.IsCarValid(carName, detectedYear))
                    toRemove.Add(carName);
            });

            string debug = string.Empty;
            toRemove.ForEach(carName =>
            {
                truncatedList.Remove(carName);
                debug += "\n- " + carName + " (" + CarNameProvider.years[CarNameProvider.DetectCarName(carName)] + ")";
            });
            Main.Log("Removed cars :" + debug);

            // set list of cars for selection
            __instance.CarButton.stringList = truncatedList;
            __instance.CarButton.stringListLength = truncatedList.Count - 1;

            // updated UI stats and name
            __instance.CarButton.GetType().GetMethod("UpdateCarSpecs", BindingFlags.NonPublic | BindingFlags.Instance).
                Invoke(__instance.CarButton, null);
            __instance.CarButton.UpdateOptionTextAndArrows();

            // reselect car model
            UIManager.Instance.PanelManager.CarChooserManager.SelectCarInClass(GameModeManager.GetSeasonDataCurrentGameMode().CarClass, 0);
            Main.Log("Reselected car " + truncatedList[0]);
        }

        public static int AdjustedToNormalIndex(int carIndex)
        {
            if (originalList == null)
                return carIndex;

            return originalList.IndexOf(truncatedList[carIndex]);
        }
    }

    [HarmonyPatch(typeof(CarManager), nameof(CarManager.GetCurrentCarsListForClass))]
    static class CarManager_GetCurrentCarsListForClass_Patch
    {
        static void Postfix(ref List<Car> __result)
        {
            if (!Main.enabled || CarChooserHelper_Init_Patch.detectedYear == 0)
                return;

            List<Car> toRemove = new List<Car>();
            __result.ForEach(car =>
            {
                if (!Main.IsCarValid(car.name, CarChooserHelper_Init_Patch.detectedYear))
                    toRemove.Add(car);
            });

            foreach (Car car in toRemove)
                __result.Remove(car);
        }
    }

    [HarmonyPatch(typeof(CarChooserManager), nameof(CarChooserManager.SelectCarInClass))]
    static class CarChooserManager_SelectCarInClass_Patch
    {
        // this is called when we change cars
        static void Prefix(CarChooserManager __instance, Car.CarClass carClass, ref int index)
        {
            if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER || CarChooserHelper_Init_Patch.detectedYear == 0)
                return;

            index = CarChooserHelper_Init_Patch.AdjustedToNormalIndex(index);
        }
    }
}