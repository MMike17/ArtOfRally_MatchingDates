using HarmonyLib;
using RealCarNames;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MatchingDates
{
    // /!\ Doesn't work when we move between seasons... /!\
    // detect when we leave the screen (forward or backwards)
    // reset all data (mainly detectedYear)

    // DOESN'T WORK

    // CarChooserHelper.BeginEvent
    // CarChooserManager.UnInitForCarChooser

    //[HarmonyPatch(typeof(), nameof())]
    //static class x_x_Patch
    //{
    //    static void Postfix()
    //    {
    //        Main.Log("Post fix for \" \"");
    //    }
    //}

    //

    // Entry point =>
    // Change the list of cars in the button
    // Reroute the index when selecting to match the old indexes

    [HarmonyPatch(typeof(CarChooserHelper), nameof(CarChooserHelper.InitHideClass))]
    static class CarChooserHelper_InitHideClass_Patch
    {
        public static bool isReady => detectedYear > 0 && originalList != null && truncatedList != null;
        public static int detectedYear { get; private set; }

        public static List<string> originalList;
        static List<string> truncatedList;

        // gets called every time we open the car selection menu
        static void Prefix(CarChooserHelper __instance)
        {
            if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER)
                return;

            Main.Try(() =>
            {
                // reset car selection index
                //SaveGame.SetInt(GameModeManager.GetSeasonDataCurrentGameMode().CarClass.ToString(), 0);

                // force unlock cars for this season
                detectedYear = int.Parse(__instance.GroupTitle.Text.text.Split(new string[] { "  |  " }, StringSplitOptions.None)[1]);
                CarManager.AllCarsList.ForEach(car => Main.SetCarUnlockState(car, car.carStats.YearUnlocked < detectedYear));

                Main.Log("Locked/unlocked cars for season " + detectedYear);
            });
        }

        //static void Postfix(CarChooserHelper __instance)
        //{
        //    if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER)
        //        return;

        //    Main.Try(() =>
        //    {
        //        // store original and curated list
        //        originalList = new List<string>(__instance.CarButton.stringList);
        //        truncatedList = new List<string>(__instance.CarButton.stringList);
        //        List<string> toRemove = new List<string>();

        //        truncatedList.ForEach(carName =>
        //        {
        //            if (!Main.IsCarValid(carName, detectedYear))
        //                toRemove.Add(carName);
        //        });

        //        toRemove.ForEach(car => truncatedList.Remove(car));

        //        if (truncatedList.Count == 0)
        //        {
        //            detectedYear = 0;
        //            Main.Log("New list is empty. Skipping feature.");
        //            return;
        //        }

        //        // reselect first car of new list
        //        UIManager.Instance.PanelManager.CarChooserManager.SelectCarInClass(
        //            GameModeManager.GetSeasonDataCurrentGameMode().CarClass,
        //            originalList.IndexOf(truncatedList[0])
        //        );

        //        Main.Log("Cached adjusted list of cars.");
        //    });
        //}

        //public static int GetNextIndex(int index)
        //{
        //    //index = truncatedList.IndexOf(originalList[index]);

        //    //if (index < truncatedList.Count - 1)
        //    //    index++;

        //    index = Mathf.Min(truncatedList.IndexOf(originalList[index]) + 1, truncatedList.Count - 1);
        //    index = originalList.IndexOf(truncatedList[index]);

        //    Main.Log("Getting next index : " + index);
        //    return index;
        //}

        //public static int GetPreviousIndex(int index)
        //{
        //    //index = truncatedList.IndexOf(originalList[index]);

        //    //if (index > 0)
        //    //    index--;

        //    index = Mathf.Max(truncatedList.IndexOf(originalList[index]) - 1, 0);
        //    index = originalList.IndexOf(truncatedList[index]);

        //    Main.Log("Getting previous index : " + index);
        //    return index;
        //}
    }

    // old code

    //[HarmonyPatch(typeof(CarChooserHelper), "Init")]
    //static class CarChooserHelper_Init_Patch
    //{
    //    static void Postfix(CarChooserHelper __instance)
    //    {
    //        Main.Log("Init postfix");

    //        if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER)
    //            return;

    //        Main.Try(() =>
    //        {
    //            // remove unavailable cars from selection list
    //            originalList = new List<string>(__instance.CarButton.stringList);
    //            truncatedList = new List<string>(__instance.CarButton.stringList);

    //            string debug2 = "original list : ";
    //            foreach (string name in originalList)
    //                debug2 += "\n- " + name;
    //            Main.Log(debug2);

    //            detectedYear = int.Parse(__instance.GroupTitle.Text.text.Split(new string[] { "  |  " }, StringSplitOptions.None)[1]);
    //            List<string> toRemove = new List<string>();

    //            originalList.ForEach(carName =>
    //            {
    //                if (!Main.IsCarValid(carName, detectedYear))
    //                    toRemove.Add(carName);
    //            });

    //            string debug = string.Empty;
    //            toRemove.ForEach(carName =>
    //            {
    //                truncatedList.Remove(carName);
    //                debug += "\n- " + carName + " (" + CarNameProvider.GetCarYear(carName) + ")";
    //            });
    //            Main.Log("Removed cars :" + debug);

    //            if (truncatedList.Count == 0)
    //            {
    //                truncatedList = originalList;
    //                detectedYear = 0;
    //                Main.Log("New list is empty. Disabling feature until next call.");
    //                return;
    //            }

    //            // set list of cars for selection
    //            __instance.CarButton.stringList = truncatedList;
    //            __instance.CarButton.stringListLength = truncatedList.Count - 1;

    //            // updated UI stats and name
    //            __instance.CarButton.GetType().GetMethod("UpdateCarSpecs", BindingFlags.NonPublic | BindingFlags.Instance).
    //                Invoke(__instance.CarButton, null);
    //            __instance.CarButton.UpdateOptionTextAndArrows();

    //            // reselect car model
    //            UIManager.Instance.PanelManager.CarChooserManager.SelectCarInClass(GameModeManager.GetSeasonDataCurrentGameMode().CarClass, 0);
    //            Main.Log("Reselected car " + truncatedList[0]);
    //        });
    //    }

    //    public static int AdjustedToNormalIndex(int carIndex)
    //    {
    //        if (originalList == null)
    //            return carIndex;

    //        return originalList.IndexOf(truncatedList[carIndex]);
    //    }

    //    public static void Reset() => detectedYear = 0;
    //}

    // curates car list to only have available cars
    //[HarmonyPatch(typeof(CarManager), nameof(CarManager.GetCurrentCarsForClass))]
    //static class CarManager_GetCurrentCarsForClass_Patch
    //{
    //    static void Postfix(ref List<Car> __result)
    //    {
    //        Main.Log("GetCurrentCarsForClass Postfix");

    //        if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER || CarChooserHelper_Init_Patch.detectedYear == 0)
    //            return;

    //        List<Car> result = __result;

    //        Main.Try(() =>
    //        {
    //            List<Car> toRemove = new List<Car>();
    //            result.ForEach(car =>
    //            {
    //                if (!Main.IsCarValid(car.name, CarChooserHelper_Init_Patch.detectedYear))
    //                    toRemove.Add(car);
    //            });

    //            foreach (Car car in toRemove)
    //                result.Remove(car);
    //        });

    //        __result = result;
    //    }
    //}

    // new code

    // replaces car selection to correct index
    //[HarmonyPatch(typeof(CarChooserManager), nameof(CarChooserManager.ChangeCar))]
    static class CarChooserManager_ChangeCar_Patch
    {
        static int previousIndex = 0;

        // this is called when we change cars
        static void Prefix(CarChooserManager __instance, Car.CarClass carClass, ref int index)
        {
            if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER || !CarChooserHelper_InitHideClass_Patch.isReady)
                return;

            int newIndex = index;

            Main.Try(() =>
            {
                int deltaIndex = newIndex - previousIndex;

                Main.Log(newIndex + " - " + previousIndex + " = " + deltaIndex);

                //if (deltaIndex > 0)
                //    newIndex = CarChooserHelper_InitHideClass_Patch.GetNextIndex(previousIndex);
                //else if (deltaIndex < 0)
                //    newIndex = CarChooserHelper_InitHideClass_Patch.GetPreviousIndex(previousIndex);

                previousIndex = newIndex;
                Main.Log("Fixed index to " + newIndex + " (" + CarChooserHelper_InitHideClass_Patch.originalList[newIndex] + ")");
            });

            index = newIndex;
        }
    }
}