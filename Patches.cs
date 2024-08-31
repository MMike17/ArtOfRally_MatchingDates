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

        static void Prefix()
        {
            // reset car selection index
            SaveGame.SetInt(GameModeManager.GetSeasonDataCurrentGameMode().CarClass.ToString(), 0);
        }

        // /!\ initial car displayed is the wrong one /!\
        // what should we refresh after setting the string list ?

        // /!\ car model is stuck on old list /!\
        // Check CarChooserManager

        static void Postfix(CarChooserHelper __instance)
        {
            if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER)
                return;

            detectedYear = int.Parse(__instance.GroupTitle.Text.text.Split(new string[] { "  |  " }, StringSplitOptions.None)[1]);
            List<string> toRemove = new List<string>();

            string firstValidCarName = __instance.CarButton.stringList.Find(car => Main.IsCarValid(car, detectedYear));
            int adjustedIndex = __instance.CarButton.stringList.IndexOf(firstValidCarName);

            __instance.CarButton.stringList.ForEach(carName =>
            {
                if (!Main.IsCarValid(carName, detectedYear))
                    toRemove.Add(carName);
            });

            string debug = string.Empty;
            toRemove.ForEach(carName =>
            {
                __instance.CarButton.stringList.Remove(carName);
                debug += "\n- " + carName + " (" + CarNameProvider.years[CarNameProvider.DetectCarName(carName)] + ")";
            });
            Main.Log("Removed cars :" + debug);

            __instance.CarButton.stringListLength = __instance.CarButton.stringList.Count - 1;
            __instance.CarButton.GetType().GetMethod("UpdateCarSpecs", BindingFlags.NonPublic | BindingFlags.Instance).
                Invoke(__instance.CarButton, null);
            __instance.CarButton.UpdateOptionTextAndArrows();

            // /!\ This doesn't fix the name but fixes the model
            CarChooserManager_SelectCarInClass_Patch.manager.SelectCarInClass(
                GameModeManager.GetSeasonDataCurrentGameMode().CarClass,
                adjustedIndex
            );
        }
    }

    [HarmonyPatch(typeof(CarManager), nameof(CarManager.GetCurrentCarsListForClass))]
    static class CarManager_GetCurrentCarsListForClass_Patch
    {
        static void Postfix(ref List<Car> __result)
        {
            Main.Log("Started postfix for retrieving car list for class.");

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
        public static CarChooserManager manager { get; private set; }

        static void Postfix(CarChooserManager __instance)
        {
            // this is called when we change car

            Main.Log("Post fixing Car selection from CarChooserManager");
            // this happens before fixing list
            // call this again after fixing lists ?

            manager = __instance;
        }
    }
}