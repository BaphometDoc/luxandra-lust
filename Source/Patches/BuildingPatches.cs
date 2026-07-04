using HarmonyLib;
using RimWorld;
using Verse;

namespace LuxandraLust
{
    [HarmonyPatch(typeof(Designator_Build), "Visible", MethodType.Getter)]
    public static class Patch_Designator_Build_Visible
    {
        public static void Postfix(Designator_Build __instance, ref bool __result)
        {
            // If the button is already hidden by vanilla logic, don't waste time checking
            if (!__result) return;

            // Check if the thing being built is Luxandra's specific monument
            if (IsLuxandraMonument(__instance.PlacingDef))
            {
                // Only show the building if Luxandra is the active storyteller
                if (!LuxandraStorytellerCheck.IsActive())
                {
                    __result = false;
                }
            }
        }

        private static bool IsLuxandraMonument (BuildableDef def)
        {
            if(def == null || def.defName == null) return false;

            if (def.defName.Contains("Luxandra_SacredMonument")) return true;

            return false;
    }
    }
}