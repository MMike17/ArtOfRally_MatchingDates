using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MatchingDates
{
    [HarmonyPatch(typeof(CarChooserHelper), "Init")]
    static class CarChooserHelper_Init_Patch
    {
        static void Postfix(CarChooserHelper __instance)
        {
            if (!Main.enabled || GameModeManager.GameMode != GameModeManager.GAME_MODES.CAREER)
                return;

            string rallyYear = __instance.GroupTitle.Text.text.Split(new string[] { "  |  " }, StringSplitOptions.None)[1];
            List<string> toRemove = new List<string>();

            __instance.CarButton.stringList.ForEach(carName =>
            {
                if (!Main.IsCarValid(carName, rallyYear))
                    toRemove.Add(carName);
            });

            string debug = string.Empty;
            toRemove.ForEach(carName =>
            {
                __instance.CarButton.stringList.Remove(carName);
                debug += "\n- " + carName;
            });
            Main.Log("Removed cars :" + debug);

            __instance.CarButton.stringListLength = __instance.CarButton.stringList.Count - 1;
            __instance.CarButton.GetType().GetMethod("UpdateCarSpecs", BindingFlags.NonPublic | BindingFlags.Instance).
                Invoke(__instance.CarButton, null);

            // car model problem
        }
    }
}