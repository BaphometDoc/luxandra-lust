using HarmonyLib;
using RimWorld;

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
            if (__instance.PlacingDef?.defName == "Luxandra_SacredMonument")
            {
                // Only show the building if Luxandra is the active storyteller
                if (!LuxandraStorytellerCheck.IsActive())
                {
                    __result = false;
                }
            }
        }
    }
}