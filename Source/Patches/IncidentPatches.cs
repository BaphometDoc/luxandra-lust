using HarmonyLib;
using RimWorld;
using Verse;

namespace LuxandraLust
{
    // Detect if the active narrator is in fact Luxandra
    [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
    public static class Patch_IncidentWorker_TryExecute
    {
        public static void Prefix(IncidentParms parms)
        {
            if (!LuxandraStorytellerCheck.IsActive())
                return;

            // Debug trigger BEFORE the incident fires
            Log.Message("[Luxandra] Event fired");
        }
    }
}