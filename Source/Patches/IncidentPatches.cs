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
        }

        // Necessary to prevent a recursive call
        public static class LuxandraExecutionGuard
        {
            public static bool InLuxandraExecution = false;
        }

        // Intercept the storyteller event, and re-evaluate it based on how much sex has been going on
        [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
        public static class Patch_IncidentExecute
        {
            [HarmonyPrefix]
            public static bool Prefix(IncidentWorker __instance, IncidentParms parms)
            {
                if (!LuxandraStorytellerCheck.IsActive())
                    return true;

                DebugActions_Luxandra.DebugLogMessage("TryExecute intercepted correctly");
                DebugActions_Luxandra.DebugLogMessage($"Incident about to happen: {__instance.def?.defName}");
                // FAILSAFE: recursion guard
                if (LuxandraExecutionGuard.InLuxandraExecution)
                    return true;

                // Ignore quest threats as well as the weekly cycle from Luxandra herself
                bool isFromQuest = parms.quest != null || parms.forced;
                if (isFromQuest)
                {
                    DebugActions_Luxandra.DebugLogMessage("Event is from quest or forced, skipping the reroll.");
                    return true;
                }

                var def = __instance.def;

                // Failsafe if I fucked up something somewhere
                var n = GameComponent_LuxandraLust.Instance;
                if (n == null)
                    return true;

                var eventType = def.category;
                DebugActions_Luxandra.DebugLogMessage($"Event type: {eventType}");

                bool isNegative = eventType == IncidentCategoryDefOf.ThreatBig || eventType == IncidentCategoryDefOf.ThreatSmall;

                DebugActions_Luxandra.DebugLogMessage("Number of sex events detected before the event: " + n.sexActionCounter);

                // Determine the threshold for the event conversion
                Map targetMap = parms.target as Map ?? Find.CurrentMap;
                int adultColonistCount = targetMap.mapPawns.FreeColonistsSpawned
                    .Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);
                int adultSlavesCount = targetMap.mapPawns.SlavesOfColonySpawned
                    .Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);

                // Apply the threshold multiplier from settings
                float settingsMultiplier = LuxandraModSettings.eventThresholdMultiplier;

                int totalThreshold = (int)((adultColonistCount * 2 + adultSlavesCount) * settingsMultiplier);
                DebugActions_Luxandra.DebugLogMessage("Event threshold: Adults (" + adultColonistCount + ") * 2 + Slaves (" + adultSlavesCount + ") = " + totalThreshold);

                if (isNegative && n.sexActionCounter > totalThreshold)
                {
                    DebugActions_Luxandra.DebugLogMessage($"Threshold was passed!");
                    DebugActions_Luxandra.DebugLogMessage($"Attempting to suppress hostile event: {def.defName}");

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
                            DebugActions_Luxandra.DebugLogMessage($"Sexual reroll successful, replacement found: {replacement.defName}");

                            Find.LetterStack.ReceiveLetter(
                                "Luxandra's Intervention",
                                "A hostile event was turned into a sexual event by Luxandra’s influence.",
                                LetterDefOf.PositiveEvent
                            );

                            replacement.Worker.TryExecute(parms);
                        }
                        else
                        {
                            DebugActions_Luxandra.DebugLogMessage($"Sexual reroll failed, attempting to reroll in a positive or neutral event.");
                            IncidentParms safeParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, parms.target);
                            List<IncidentDef> positiveEvents = LuxandraEventPool.GetPositiveIncidents();
                            bool foundValid = positiveEvents
                                .Where(x => x.Worker.CanFireNow(safeParms))
                                .TryRandomElement(out replacement);

                            if (foundValid)
                            {
                                DebugActions_Luxandra.DebugLogMessage($"Reroll successful, replacement found: {replacement.defName}");

                                Find.LetterStack.ReceiveLetter(
                                    "Luxandra's Intervention",
                                    "A hostile event was suppressed by Luxandra’s influence.",
                                    LetterDefOf.PositiveEvent
                                );

                                replacement.Worker.TryExecute(safeParms);
                            }
                            else
                            {
                                // If no valid sexual nor positive event is found, log a warning and suppress the negative event anyway
                                Log.Warning("[Luxandra Debug] Could not find a valid event to fire. Suppressing the negative event anyway.");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[Luxandra Debug] Error during reroll: {ex}");
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