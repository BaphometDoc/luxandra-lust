using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
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
                var n = GameComponent_LuxandraLust.Instance;
                if (n == null)
                    return true;

                var eventType = def.category;
                Log.Message($"[Luxandra DEBUG] Event type: {eventType}");

                bool isNegative = eventType == IncidentCategoryDefOf.ThreatBig || eventType == IncidentCategoryDefOf.ThreatSmall;

                Log.Message("[Luxandra DEBUG] number of sex events detected: " + n.sexActionCounter);

                // Determine the threshold for the event conversion
                Map targetMap = parms.target as Map ?? Find.CurrentMap;
                int adultColonistCount = targetMap.mapPawns.FreeColonistsSpawned
                    .Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);
                int adultSlavesCount = targetMap.mapPawns.SlavesOfColonySpawned
                    .Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);

                int totalThreshold = adultColonistCount * 2 + adultSlavesCount;
                Log.Message("[Luxandra DEBUG] Event threshold: Adults (" + adultColonistCount + ") * 2 + Slaves (" + adultSlavesCount + ") = " + totalThreshold);

                if (isNegative && n.sexActionCounter > totalThreshold)
                {
                    Log.Message($"[Luxandra] Threshold was passed!");
                    Log.Message($"[Luxandra] Attempting to suppress hostile event: {def.defName}");

                    try
                    {
                        LuxandraExecutionGuard.InLuxandraExecution = true;

                        // Replace with a sex-related event if there is a valid one, otherwise a positive one if there is a valid one
                        // otherwise just suppress it cause something broke but the sex must go on
                        IncidentDef replacement;

                        List<IncidentDef> sexEvents = LuxandraEventPool.GetSexRelatedIncidents();
                        bool foundValidSex = sexEvents
                            .Where(x => x.Worker.CanFireNow(parms))
                            .TryRandomElement(out replacement);

                        if (foundValidSex)
                        {
                            Log.Message($"[Luxandra] Sexual reroll successful: {replacement.defName}");

                            Find.LetterStack.ReceiveLetter(
                                "Luxandra Intervention",
                                "A hostile event was turned into a sexual event by Luxandra’s influence.",
                                LetterDefOf.PositiveEvent
                            );

                            replacement.Worker.TryExecute(parms);
                        }
                        else
                        {
                            Log.Message($"[Luxandra] Sexual reroll failed, attempting to reroll in a positive event.");
                            IncidentParms safeParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, parms.target);
                            List<IncidentDef> positiveEvents = LuxandraEventPool.GetPositiveIncidents();
                            bool foundValid = positiveEvents
                                .Where(x => x.Worker.CanFireNow(safeParms))
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
                                // If no valid sexual nor positive event is found, log a warning and suppress the raid anyway
                                Log.Warning("[Luxandra] Could not find a valid event to fire. Suppressing raid anyway.");
                            }
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
                return true;
            }
        }
    }
}