using HarmonyLib;
using RimWorld;
using Verse;

namespace LuxandraLust
{
    [HarmonyPatch(typeof(StatPart_FertilityByGenderAge), "AgeFactor")]
    public static class StatPart_FertilityByGenderAgePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn pawn, ref float __result)
        {
            // Only intervene if the base game calcs have already zeroed out or reduced their age-based fertility factor
            if (__result < 1f && pawn?.health?.hediffSet != null)
            {
                // Ensure they are adults (skip children to prevent weird shit)
                if (LuxandraUtilities.IsAdult(pawn))
                {
                    // Check if they have any of the Fertility pulse hediffs active
                    if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Luxandra_PulseAdultMale")) ||
                        pawn.health.hediffSet.HasHediff(HediffDef.Named("Luxandra_PulseAdultFemale")))
                    {
                        // Force the age scaling multiplier back to 100%
                        __result = 1f;
                    }
                }
            }
        }
    }
}