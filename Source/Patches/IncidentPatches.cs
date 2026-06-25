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

                LuxandraDebugActions.DebugLogMessage("TryExecute intercepted correctly");
                LuxandraDebugActions.DebugLogMessage($"Incident about to happen: {__instance.def?.defName}");

                // FAILSAFE: recursion guard
                if (LuxandraExecutionGuard.InLuxandraExecution)
                    return true;

                // Ignore quest threats as well as the weekly cycle from Luxandra herself
                bool isFromQuest = parms.quest != null || parms.forced;
                if (isFromQuest)
                {
                    LuxandraDebugActions.DebugLogMessage("Event is from quest or forced, skipping the reroll.");
                    return true;
                }

                var def = __instance.def;

                // Failsafe if I fucked up something somewhere
                var n = GameComponent_LuxandraLust.Instance;
                if (n == null)
                    return true;

                var eventType = def.category;
                LuxandraDebugActions.DebugLogMessage($"Event type: {eventType}");

                bool isNegative = eventType == IncidentCategoryDefOf.ThreatBig || eventType == IncidentCategoryDefOf.ThreatSmall;

                LuxandraDebugActions.DebugLogMessage("Number of sex events detected before the event: " + n.sexActionCounterForRerolls);

                // Determine the threshold for the event conversion
                Map targetMap = parms.target as Map ?? Find.CurrentMap;
                int adultColonistCount = targetMap.mapPawns.FreeColonistsSpawned.Count(p => LuxandraUtilities.IsAdult(p));
                int adultSlavesCount = targetMap.mapPawns.SlavesOfColonySpawned.Count(p => LuxandraUtilities.IsAdult(p));

                // Apply the threshold multiplier from settings
                float settingsMultiplier = LuxandraModSettings.eventThresholdMultiplier;

                int totalThreshold = (int)((adultColonistCount * 2 + adultSlavesCount) * settingsMultiplier);
                LuxandraDebugActions.DebugLogMessage("Event threshold: Adults (" + adultColonistCount + ") * 2 + Slaves (" + adultSlavesCount + ") = " + totalThreshold);

                // Luxandra will only suppress negative events for her gimmick
                if (isNegative)
                {
                    var multipleColonistsPresent = LuxandraUtilities.HasMultipleAdultColonists(targetMap);
                    if (n.sexActionCounterForRerolls == 0 && !multipleColonistsPresent)
                    {
                        Find.LetterStack.ReceiveLetter(
                                    "Luxandra's Lustful Gaze",
                                    "Luxandra is disappointed at your lack of activity. However, seeing you're alone, she chose to show mercy and won't punish you. For now.\n\n " +
                                    "You should look for more people before she changes her mind...",
                                    LetterDefOf.NeutralEvent
                                );
                        LuxandraDebugActions.DebugLogMessage($"0 Sexual activity detected but only one colonist. Skipping the punishment.");
                    }

                    // 0 Sexual Activity Detected - force a negative sexual event to punish the player
                    if (n.sexActionCounterForRerolls == 0 && multipleColonistsPresent)
                    {
                        LuxandraDebugActions.DebugLogMessage($"0 Sexual activity detected");
                        LuxandraDebugActions.DebugLogMessage($"Attempting to suppress hostile event: {def.defName}");

                        bool successfullyRerolled = false;
                        try
                        {
                            LuxandraExecutionGuard.InLuxandraExecution = true;

                            List<IncidentDef> sexEvents = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.NegativeIncidents);
                            bool foundValidSex = sexEvents
                                .Where(x => x.Worker.CanFireNow(parms))
                                .TryRandomElement(out IncidentDef replacement);

                            if (foundValidSex)
                            {
                                LuxandraDebugActions.DebugLogMessage($"Sexual reroll successful, replacement found: {replacement.defName}");

                                Find.LetterStack.ReceiveLetter(
                                    "Luxandra's Lustful Gaze",
                                    "Luxandra is disappointed at your lack of activity. She chose to punish you.\n\n" +
                                    "Maybe it would be time to find more partners so her opinion of your colony improves...",
                                    LetterDefOf.NegativeEvent
                                );

                                replacement.Worker.TryExecute(parms);
                                successfullyRerolled = true;
                            }
                            else
                            {
                                Log.Warning("[Luxandra Debug] Could not find a valid event to fire. Letting the negative event play.");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"[Luxandra Debug] Error during reroll: {ex}");
                        }
                        finally
                        {
                            LuxandraExecutionGuard.InLuxandraExecution = false;
                            if (successfullyRerolled)
                            {
                                // Only reset if the event actually successfully swapped!
                                n.ResetSexCountersForRerolls();
                            }
                        }

                        // If we rerolled, return false to block vanilla. If we failed, return true to let vanilla play out.
                        return !successfullyRerolled;
                    }

                    // High Sexual Activity Passed Threshold - reroll to a sexual event or a positive/neutral event to suppress the negative one
                    else if (n.sexActionCounterForRerolls > totalThreshold)
                    {
                        LuxandraDebugActions.DebugLogMessage($"Threshold was passed!");
                        LuxandraDebugActions.DebugLogMessage($"Attempting to suppress hostile event: {def.defName}");

                        try
                        {
                            LuxandraExecutionGuard.InLuxandraExecution = true;

                            List<IncidentDef> sexEvents = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.AllIncidents);
                            bool foundValidSex = sexEvents
                                .Where(x => x.Worker.CanFireNow(parms))
                                .TryRandomElement(out IncidentDef replacement);

                            if (foundValidSex)
                            {
                                LuxandraDebugActions.DebugLogMessage($"Sexual reroll successful, replacement found: {replacement.defName}");

                                Find.LetterStack.ReceiveLetter(
                                    "Luxandra's Lustful Gaze",
                                    "Luxandra is pleased by your colony!\n" +
                                    "To repay the joy she felt watching you, she has turned the wheels of fate, and turned a dangerous event into something more...interesting.\n" +
                                    "Though her concept of 'interesting' isn't always devoid of danger...",
                                    LetterDefOf.PositiveEvent
                                );

                                replacement.Worker.TryExecute(parms);
                            }
                            else
                            {
                                LuxandraDebugActions.DebugLogMessage($"Sexual reroll failed, attempting to reroll in a positive or neutral event.");
                                IncidentParms safeParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, parms.target);
                                List<IncidentDef> positiveEvents = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.PositiveIncidents);
                                bool foundValid = positiveEvents
                                    .Where(x => x.Worker.CanFireNow(safeParms))
                                    .TryRandomElement(out replacement);

                                if (foundValid)
                                {
                                    LuxandraDebugActions.DebugLogMessage($"Reroll successful, replacement found: {replacement.defName}");

                                    Find.LetterStack.ReceiveLetter(
                                        "Luxandra's Lustful Gaze",
                                        "Luxandra is pleased by your colony!\n" +
                                        "To repay the joy she felt watching you, she has turned the wheels of fate, and turned a dangerous event into something helpful.",
                                        LetterDefOf.PositiveEvent
                                    );

                                    replacement.Worker.TryExecute(safeParms);
                                }
                                else
                                {
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
                            n.ResetSexCountersForRerolls();
                        }

                        // Block the original event
                        return false;
                    }

                    // Threshold was not passed - continue
                    else
                    {
                        LuxandraDebugActions.DebugLogMessage($"Threshold was not passed, event continues as usual.");
                        return true;
                    }
                }

                LuxandraDebugActions.DebugLogMessage($"Event was not considered negative, continue as usual.");
                return true;
            }
        }
    }
}