using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    // Detect if the active storyteller is in fact Luxandra
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

    // Necessary to prevent a recursive call
    public static class LuxandraExecutionGuard
    {
        public static bool InLuxandraExecution = false;
    }

    // Intercept the storyteller event, and re-evaluate it based on how much
    // fucking has been going on
    [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
    public static class Patch_IncidentExecute
    {
        [HarmonyPrefix]
        public static bool Prefix(IncidentWorker __instance, IncidentParms parms)
        {
            Log.Message($"[Luxandra DEBUG] Incident firing: {__instance.def?.defName}");
            // FAILSAFE: recursion guard
            if (LuxandraExecutionGuard.InLuxandraExecution)
                return true;

            // Ignore quest threats
            bool isFromQuest = parms.quest != null || parms.forced;
            if (isFromQuest)
                return true; 

            var def = __instance.def;
            if (!LuxandraStorytellerCheck.IsActive())
                return true;

            Log.Message("[Luxandra DEBUG] TryExecute intercepted correctly");

            // Failsafe if I fucked up something somewhere
            var n = GameComponent_LuxandraLust.Instance?.narrator;
            if (n == null)
                return true;

            var eventType = def.category;
            Log.Message($"[Luxandra DEBUG] Event type: {eventType}");

            bool isNegative = eventType == IncidentCategoryDefOf.ThreatBig || eventType == IncidentCategoryDefOf.ThreatSmall;

            Log.Message("[Luxandra DEBUG] number of sex events detected: " + n.sexActionCounter);

            if (isNegative && n.sexActionCounter > 1)
            {
                try
                {
                    LuxandraExecutionGuard.InLuxandraExecution = true;
                    Log.Message($"[Luxandra] Attempting to suppress hostile event: {def.defName}");

                    // replace with a positive event if there is a valid one
                    IncidentParms safeParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, parms.target);
                    IncidentDef replacement;
                    bool foundValid = DefDatabase<IncidentDef>.AllDefs
                        .Where(x => x.category == IncidentCategoryDefOf.Misc && x.letterDef == LetterDefOf.PositiveEvent && x.Worker.CanFireNow(safeParms))
                        .TryRandomElement(out replacement);

                    if (foundValid)
                    {
                        Log.Message($"[Luxandra] Reroll successful: {replacement.defName}");

                        Find.LetterStack.ReceiveLetter(
                            "Luxandra Intervention",
                            "A hostile event was suppressed by Luxandra’s influence.",
                            LetterDefOf.PositiveEvent
                        );

                        replacement.Worker.TryExecute(safeParms);
                    }
                    else
                    {
                        // If no valid positive event is found, log a warning and suppress the raid anyway
                        Log.Warning("[Luxandra] Could not find a valid positive event to fire. Suppressing raid anyway.");
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[Luxandra] Error during reroll: {ex}");
                }
                finally
                { 
                    LuxandraExecutionGuard.InLuxandraExecution = false;
                    n.ResetSexCounters();
                }

                // block original event
                return false;
            }

            // Otherwise continue as expected
            n.ResetSexCounters();
            return true;
        }
    }
}