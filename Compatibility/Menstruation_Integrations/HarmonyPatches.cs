using HarmonyLib;
using LuxandraLust;
using RJW_Menstruation;
using System.Collections.Generic;
using Verse;

namespace Luxandra_Menstruation_Integrations
{
    public static class PulseFertilityHelper
    {
        public static bool HasFertilityPulse(HediffComp_Menstruation comp)
        {
            if (comp?.parent?.pawn == null) return false;


            Pawn pawn = comp.parent.pawn;

            // Ignore for children and pregnant pawns
            if (!LuxandraUtilities.IsAdult(pawn) || LuxandraUtilities.IsPregnant(pawn))
                return false;

            // Checks both genders for either of your custom pulse hediffs
            return pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Luxandra_PulseAdultMale", false)) ||
               pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Luxandra_PulseAdultFemale", false));
        }
    }

    // This is a bunch of patches that mainly serve the purpose
    // of overriding every single Menstruation check when Fertility Pulse
    // is active. They don't really do anything for "normal" pawns as the 1000% fertility
    // buff takes care of that, but for old age this is kinda needed

    #region Fertility Patches

    [HarmonyPatch(typeof(HediffComp_Menstruation), "ShouldBeInfertile")]
    public static class Patch_ShouldBeInfertile
    {
        public static bool Prefix(HediffComp_Menstruation __instance, ref bool __result)
        {
            if (PulseFertilityHelper.HasFertilityPulse(__instance))
            {
                __result = false; // Force them to be fertile
                return false;     // Skip original age check entirely
            }
            return true;          // Let original code handle un-affected pawns
        }
    }

    [HarmonyPatch(typeof(HediffComp_Menstruation), "CalculatedOvulationChance")]
    public static class Patch_CalculatedOvulationChance
    {
        public static bool Prefix(HediffComp_Menstruation __instance, ref float __result)
        {
            if (PulseFertilityHelper.HasFertilityPulse(__instance))
            {
                __result = System.Math.Max(50.0f, __result); ; // Force max ovulation chance
                return false;    // Bypass age limits
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(HediffComp_Menstruation), "CalculatedImplantChance")]
    public static class Patch_CalculatedImplantChance
    {
        public static bool Prefix(HediffComp_Menstruation __instance, ref float __result)
        {
            if (PulseFertilityHelper.HasFertilityPulse(__instance))
            {
                __result = System.Math.Max(50.0f, __result); // Force max implantation/conception chance
                return false;    // Bypass age limits
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(HediffComp_Menstruation), "CumCanFertilize")]
    public static class Patch_CumCanFertilize
    {
        public static bool Prefix(HediffComp_Menstruation __instance, object cum, ref bool __result)
        {
            // First check: does the mother/host have the pulse?
            if (PulseFertilityHelper.HasFertilityPulse(__instance))
            {
                __result = true; // Force max implantation/conception chance
                return false;    // Bypass age limits
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(HediffComp_Menstruation), "InterspeciesImplantFactor")]
    public static class Patch_InterspeciesImplantFactor
    {
        public static bool Prefix(HediffComp_Menstruation __instance, object fertilizer, ref float __result)
        {
            // First check: does the mother/host have the pulse?
            if (PulseFertilityHelper.HasFertilityPulse(__instance))
            {
                __result = System.Math.Max(1.0f, __result); // Disregard species type
                return false;    // Bypass species limits
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(HediffComp_Menstruation), "CalculatedImplantChance")]
    public static class Patch_GetFertilityChance
    {
        public static bool Prefix(HediffComp_Menstruation __instance, ref float __result)
        {
            // First check: does the mother/host have the pulse?
            if (PulseFertilityHelper.HasFertilityPulse(__instance))
            {
                __result = System.Math.Max(50.0f, __result); // Disregard species type
                return false;    // Bypass species limits
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(HediffComp_Menstruation), "Fertilize")]
    public static class Patch_Fertilize
    {
        [HarmonyPrefix]
        public static bool Prefix(HediffComp_Menstruation __instance, ref Pawn __result)
        {
            Map currentMap = __instance.parent?.pawn?.Map;

            // Only take over if map pulse condition is active
            if (currentMap != null && currentMap.gameConditionManager.ConditionIsActive(DefDatabase<GameConditionDef>.GetNamed("Luxandra_LustfulFertilityPulse")))
            {
                // Traverse into the instance to grab the private 'cums' list
                var cumsList = Traverse.Create(__instance).Field<List<RJW_Menstruation.Cum>>("cums").Value;

                if (cumsList == null || cumsList.Count == 0)
                {
                    __result = null; // No fluid available at all
                    return false;    // Skip original function
                }

                // Since the CumCanFertilize patch forces everything to true, 
                // every piece of fluid in the list is eligible
                List<RJW_Menstruation.Cum> eligibleCum = cumsList;

                // Log the record metric just like the original code does
                __instance.parent.pawn.records.AddTo(VariousDefOf.AmountofFertilizedEggs, 1);

                // 2. Run the exact same weight-based father selection math minus the failure roll
                float totalFertPower = 0f;
                foreach (var cum in eligibleCum) totalFertPower += cum.FertVolume;

                float selection = Rand.Range(0.0f, totalFertPower);

                foreach (var cum in eligibleCum)
                {
                    selection -= cum.FertVolume;
                    if (selection <= 0f)
                    {
                        __result = cum.pawn; // Found the father
                        return false;        // Skip original function
                    }
                }

                // Fallback for floating-point safety edge cases
                __result = eligibleCum.MaxBy(cum => cum.FertVolume).pawn;
                return false; // Skip original function
            }

            return true; // Let original function handle normal calculations when pulse is off
        }
    }
    #endregion
}