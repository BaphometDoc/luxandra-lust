using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
    public static class Patch_IncidentWorker_TryExecute
    {
        // Detect if the active storyteller is in fact Luxandra
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

        // WARNING: This is a huge mess. Read at your own risk.
        // Intercept the storyteller event, and re-evaluate it based on how much sex has been going on
        [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
        public static class Patch_IncidentExecute
        {
            [HarmonyPrefix]
            public static bool Prefix(IncidentWorker __instance, IncidentParms parms)
            {
                if (!LuxandraStorytellerCheck.IsActive())
                {
                    LuxandraDebugActions.DebugLogMessage("Luxandra was not active. Skipping the incident intercept. Why do you have her debug on again?.");
                    return true;
                }

                // Check that the system isnt straight up disabled
                bool isSexualRerollEnabled = LuxandraModSettings.eventRerollCondition != 0;
                if (!isSexualRerollEnabled)
                {
                    // Exits if the reroll is disabled
                    LuxandraDebugActions.DebugLogMessage("Sexual event rerolling is disabled. Skipping...");
                    return true;
                }

                // FAILSAFE: recursion guard
                if (LuxandraExecutionGuard.InLuxandraExecution)
                {
                    LuxandraDebugActions.DebugLogMessage("Recursion was caught. Something broke.");
                    return true;
                }

                // Ignore quest threats as well as the weekly cycle from Luxandra herself
                bool isFromQuest = parms.quest != null || parms.forced;
                if (isFromQuest)
                {
                    LuxandraDebugActions.DebugLogMessage("Event is from quest or forced, skipping the reroll.");
                    return true;
                }

                var def = __instance.def;
                Map targetMap = parms.target as Map ?? Find.CurrentMap;

                // Failsafe if I fucked up something somewhere (trust me it has happened)
                var n = GameComponent_LuxandraLust.Instance;
                if (def == null || n == null || targetMap == null)
                {
                    LuxandraDebugActions.DebugLogMessage($"The event reroll should have never entered here. You fucked up.");
                    return true;
                }

                // Determine the threshold for the event conversion
                int adultColonistCount = targetMap.mapPawns.FreeColonistsSpawned.Count(p => LuxandraUtilities.IsAdult(p));
                int adultSlavesCount = targetMap.mapPawns.SlavesOfColonySpawned.Count(p => LuxandraUtilities.IsAdult(p));

                LuxandraDebugActions.DebugLogMessage("TryExecute intercepted correctly");
                LuxandraDebugActions.DebugLogMessage($"Incident about to happen: {__instance.def?.defName}");


                var eventType = def.category;
                LuxandraDebugActions.DebugLogMessage($"Event type: {eventType}");

                bool isNegative = eventType == IncidentCategoryDefOf.ThreatBig || eventType == IncidentCategoryDefOf.ThreatSmall;

                // Event conversion condition - 0: Disabled, 1: Only Negative, 2: All Events
                int rerollConditionConfig = LuxandraModSettings.eventRerollCondition;
                // Event conversion type - 0: Match type, 1: Always positive, 2: Random
                int rerollModeConfig = LuxandraModSettings.eventConversionMode;
                // Threshold multiplier from settings
                float rerollThresholdMultiplierFromConfigs = LuxandraModSettings.eventThresholdMultiplier;
                LuxandraDebugActions.DebugLogMessage("Configs loaded:");
                LuxandraDebugActions.DebugLogMessage($"rerollConditionConfig: {rerollConditionConfig}");
                LuxandraDebugActions.DebugLogMessage($"rerollModeConfig: {rerollModeConfig}");
                LuxandraDebugActions.DebugLogMessage($"rerollThresholdMultiplierFromConfigs: {rerollThresholdMultiplierFromConfigs}");

                if (rerollConditionConfig == 1 && !isNegative)
                {
                    // Exit if the config says to only reroll negative events
                    LuxandraDebugActions.DebugLogMessage("Event not negative. Skipping...");
                    return true;
                }

                LuxandraDebugActions.DebugLogMessage("Number of sex events detected before the event: " + n.sexActionCounterForRerolls);

                int totalThreshold = (int)((adultColonistCount * 2 + adultSlavesCount) * rerollThresholdMultiplierFromConfigs);
                LuxandraDebugActions.DebugLogMessage("Event threshold: Adults (" + adultColonistCount + ") * 2 + Slaves (" + adultSlavesCount + ") = " + totalThreshold);

                // =========================================
                // ==== START OF THE PRUDE REROLL LOGIC ====
                // =========================================

                // Check if you deserve the punishment for 0 actions
                var playerIsPrude = n.sexActionCounterForRerolls == 0;
                var multipleColonistsPresent = LuxandraUtilities.HasMultipleAdultColonists(targetMap);

                // The colony only has a single adult colonist. Judgement is not served... this time
                if (playerIsPrude && !multipleColonistsPresent && isSexualRerollEnabled)
                {
                    Find.LetterStack.ReceiveLetter(
                                "Luxandra's Lustful Gaze",
                                "Luxandra is disappointed at your lack of activity. However, seeing you're alone, she chose to let fate play its cards without her influence.\n\n " +
                                "You should look for more people before she changes her mind...",
                                LetterDefOf.NeutralEvent
                            );
                    LuxandraDebugActions.DebugLogMessage($"0 Sexual activity detected but only one colonist. Skipping the punishment and letting the event continue as planned.");
                    return true;
                }

                // The colony has sex capable pawns and they're not getting it going. Luxandra is pissed.
                if (playerIsPrude && isSexualRerollEnabled)
                {
                    LuxandraDebugActions.DebugLogMessage($"0 Sexual activity detected");
                    bool successfullyPrudeRerolled = false;
                    try
                    {
                        LuxandraExecutionGuard.InLuxandraExecution = true;
                        LuxandraDebugActions.DebugLogMessage($"Attempting to suppress planned event due to prude reroll: {def.defName}");

                        List<IncidentDef> prudeRerollSexEvents = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.NegativeIncidents);
                        bool foundValidSex = prudeRerollSexEvents
                            .Where(x => x.Worker.CanFireNow(parms))
                            .TryRandomElement(out IncidentDef prudeRerollreplacement);

                        if (foundValidSex)
                        {
                            LuxandraDebugActions.DebugLogMessage($"Prude punishment reroll successful, replacement found: {prudeRerollreplacement.defName}");

                            Find.LetterStack.ReceiveLetter(
                                "Luxandra's Lustful Gaze",
                                "Luxandra is disappointed at your lack of activity. Her lust must be satiated, and you will comply, with or without your approval.\n\n" +
                                "Maybe it would be time to find more partners so her opinion of your colony improves...",
                                LetterDefOf.NegativeEvent
                            );

                            prudeRerollreplacement.Worker.TryExecute(parms);
                            successfullyPrudeRerolled = true;
                        }
                        else
                        {
                            Log.Warning("[Luxandra Debug] Could not find a valid event to fire. Letting the event play.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[Luxandra Debug] Error during prude reroll: {ex}");
                    }
                    finally
                    {
                        LuxandraExecutionGuard.InLuxandraExecution = false;
                        if (successfullyPrudeRerolled)
                        {
                            // Only reset if the event actually successfully swapped!
                            n.ResetSexCountersForRerolls();
                        }
                    }

                    LuxandraDebugActions.DebugLogMessage($"Prude reroll cycle ended, rerolled: {successfullyPrudeRerolled}");
                    return !successfullyPrudeRerolled;
                }

                // ==========================================
                // ==== START OF THE SEXUAL REROLL LOGIC ====
                // ==========================================

                // Not enough fuck was done, but more than 0 at least.
                if (n.sexActionCounterForRerolls < totalThreshold && isSexualRerollEnabled)
                {
                    LuxandraDebugActions.DebugLogMessage($"Threshold was not passed. Letting the event continue as planned.");

                    Find.LetterStack.ReceiveLetter(
                        "Luxandra's Lustful Gaze",
                        "Luxandra is not entertained by your colony.\n" +
                        "She expects more of you... You may not be a complete disappointment, but you have not proven worthy of her intervention.\n\n" +
                        "Maybe more spicy action wouldn't hurt...",
                        LetterDefOf.NeutralEvent
                    );

                    return true;
                }

                // This is a redundant check, but here for safety because there's way too many possible
                // recursions here
                if (n.sexActionCounterForRerolls >= totalThreshold && isSexualRerollEnabled)
                {
                    // High Sexual Activity Passed Threshold - reroll to a sexual event according to the settings
                    LuxandraDebugActions.DebugLogMessage($"Threshold was passed!");
                    LuxandraDebugActions.DebugLogMessage($"Attempting to suppress event: {def.defName}");

                    // ==========================================
                    // ===Negative events  (always rerollable)===
                    // ==========================================
                    if (isNegative)
                    {
                        LuxandraDebugActions.DebugLogMessage($"Event is negative. Attempting to reroll.");
                        bool successfullyRerolledNegativeEvent = false;

                        try
                        {
                            LuxandraExecutionGuard.InLuxandraExecution = true;
                            // Filter the events manually
                            IEnumerable<LuxandraIncidentDefs> incidentCollectionForNegativeRerolls = LuxandraDefsCollections.AllIncidents;
                            // Attempt to reroll in a negative event
                            if (rerollModeConfig == 0)
                                incidentCollectionForNegativeRerolls = LuxandraDefsCollections.NegativeIncidents;
                            // Attempt to reroll in a positive event
                            else if (rerollModeConfig == 1)
                                incidentCollectionForNegativeRerolls = LuxandraDefsCollections.PositiveIncidents;

                            List<IncidentDef> sexEventsForNegativeReplacement = LuxandraUtilities.ExtractIncidentsFromCollection(incidentCollectionForNegativeRerolls);
                            bool foundValidSexEventForNegativeReroll = sexEventsForNegativeReplacement
                                .Where(x => x.Worker.CanFireNow(parms))
                                .TryRandomElement(out IncidentDef replacementForNegativeReroll);

                            if (foundValidSexEventForNegativeReroll)
                            {
                                LuxandraDebugActions.DebugLogMessage($"Sexual reroll for threat successful, replacement found: {replacementForNegativeReroll.defName}");

                                if (rerollModeConfig == 0)
                                {
                                    Find.LetterStack.ReceiveLetter(
                                        "Luxandra's Lustful Gaze",
                                        "Luxandra is enthralled by your colony, and wants to watch more of your struggles!\n" +
                                        "She saw a dangerous fate incoming, and decided that raw violence is not something you should be threatened by.\n" +
                                        "She used her divine gaze to turn the danger into a more interesting event. One that may prove delightful to watch... at least for her.\n" +
                                        "Your colonists may not necessarly like what's to come...",
                                        LetterDefOf.NeutralEvent
                                    );
                                }
                                else
                                {
                                    Find.LetterStack.ReceiveLetter(
                                        "Luxandra's Lustful Gaze",
                                        "Luxandra is pleased by your colony!\n" +
                                        "She saw a dangerous fate incoming, and intervened to turn it. She instead summoned something more...interesting for your colony.\n" +
                                        "Though her concept of 'interesting' isn't always devoid of danger...",
                                        LetterDefOf.PositiveEvent
                                    );
                                }

                                replacementForNegativeReroll.Worker.TryExecute(parms);
                                successfullyRerolledNegativeEvent = true;
                            }
                            else
                            {
                                Log.Warning("[Luxandra Debug] Could not find a valid event to fire. Letting the negative event play.");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"[Luxandra Debug] Error during negative event sexual reroll: {ex}");
                        }
                        finally
                        {
                            LuxandraExecutionGuard.InLuxandraExecution = false;
                            if (successfullyRerolledNegativeEvent)
                            {
                                // Only reset if the event actually successfully swapped!
                                n.ResetSexCountersForRerolls();
                            }
                        }

                        LuxandraDebugActions.DebugLogMessage($"Negative event sexual cycle ended, rerolled: {successfullyRerolledNegativeEvent}");
                        return !successfullyRerolledNegativeEvent;
                    }
                }

                // ==========================================
                // ===  Non Negative events (config = 2)  ===
                // ==========================================
                if (!isNegative && rerollConditionConfig == 2)
                {
                    LuxandraDebugActions.DebugLogMessage($"Settings allow non-threat rerolls. Attempting to determine event type.");
                    // There's no true positive definition, I'll base it on the letter
                    bool isPositive = def.letterDef == LetterDefOf.PositiveEvent;

                    // Event is positive
                    if (isPositive)
                    {
                        LuxandraDebugActions.DebugLogMessage($"Event is positive. Attempting to reroll.");
                        bool successfullyRerolledPositiveEvent = false;

                        try
                        {
                            LuxandraExecutionGuard.InLuxandraExecution = true;
                            // Filter the events manually
                            IEnumerable<LuxandraIncidentDefs> incidentCollectionForPositiveRerolls = LuxandraDefsCollections.AllIncidents;
                            // Attempt to reroll in a positive event
                            if (rerollModeConfig == 0 || rerollModeConfig == 1)
                                incidentCollectionForPositiveRerolls = LuxandraDefsCollections.AllIncidents;

                            List<IncidentDef> sexEventsForPositiveReplacement = LuxandraUtilities.ExtractIncidentsFromCollection(incidentCollectionForPositiveRerolls);
                            bool foundValidSexEventForPositiveReroll = sexEventsForPositiveReplacement
                                .Where(x => x.Worker.CanFireNow(parms))
                                .TryRandomElement(out IncidentDef replacementForPositiveReroll);

                            if (foundValidSexEventForPositiveReroll)
                            {
                                LuxandraDebugActions.DebugLogMessage($"Sexual reroll for positive event successful, replacement found: {replacementForPositiveReroll.defName}");

                                Find.LetterStack.ReceiveLetter(
                                    "Luxandra's Lustful Gaze",
                                    "Luxandra is pleased by your colony!\n" +
                                    "In order to keep your lives interesting, she has replaced a mundane boon into something more... interesting.\n" +
                                    "Though her concept of 'interesting' isn't always devoid of danger...",
                                    LetterDefOf.NeutralEvent
                                );

                                replacementForPositiveReroll.Worker.TryExecute(parms);
                                successfullyRerolledPositiveEvent = true;
                            }
                            else
                            {
                                Log.Warning("[Luxandra Debug] Could not find a valid event to fire. Letting the positive event play.");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"[Luxandra Debug] Error during positive event sexual reroll: {ex}");
                        }
                        finally
                        {
                            LuxandraExecutionGuard.InLuxandraExecution = false;
                            if (successfullyRerolledPositiveEvent)
                            {
                                // Only reset if the event actually successfully swapped!
                                n.ResetSexCountersForRerolls();
                            }
                        }

                        LuxandraDebugActions.DebugLogMessage($"Positive event sexual cycle ended, rerolled: {successfullyRerolledPositiveEvent}");
                        return !successfullyRerolledPositiveEvent;
                    }
                }
                // Event is neutral
                else
                {
                    LuxandraDebugActions.DebugLogMessage($"Event is neutral. Attempting to reroll.");
                    bool successfullyRerolledNeutralEvent = false;

                    try
                    {
                        LuxandraExecutionGuard.InLuxandraExecution = true;
                        // Filter the events manually
                        IEnumerable<LuxandraIncidentDefs> incidentCollectionForNeutralRerolls = LuxandraDefsCollections.AllIncidents;
                        // Attempt to reroll in a negative event
                        if (rerollModeConfig == 0)
                            incidentCollectionForNeutralRerolls = LuxandraDefsCollections.NeutralIncidents;
                        // Attempt to reroll in a positive event
                        else if (rerollModeConfig == 1)
                            incidentCollectionForNeutralRerolls = LuxandraDefsCollections.PositiveIncidents;

                        List<IncidentDef> sexEventsForNeutralReplacement = LuxandraUtilities.ExtractIncidentsFromCollection(incidentCollectionForNeutralRerolls);
                        bool foundValidSexEventForNeutralReroll = sexEventsForNeutralReplacement
                            .Where(x => x.Worker.CanFireNow(parms))
                            .TryRandomElement(out IncidentDef replacementForNeutralReroll);

                        if (foundValidSexEventForNeutralReroll)
                        {
                            LuxandraDebugActions.DebugLogMessage($"Sexual reroll for neutral event successful, replacement found: {replacementForNeutralReroll.defName}");

                            Find.LetterStack.ReceiveLetter(
                                "Luxandra's Lustful Gaze",
                                "Luxandra is pleased by your colony!\n" +
                                "In order to keep your lives interesting, she has replaced a mundane boon into something more... interesting.\n" +
                                "Though her concept of 'interesting' isn't always devoid of danger...",
                                LetterDefOf.NeutralEvent
                            );

                            replacementForNeutralReroll.Worker.TryExecute(parms);
                            successfullyRerolledNeutralEvent = true;
                        }
                        else
                        {
                            Log.Warning("[Luxandra Debug] Could not find a valid event to fire. Letting the neutral event play.");
                        }

                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[Luxandra Debug] Error during neutral event sexual reroll: {ex}");
                    }
                    finally
                    {
                        LuxandraExecutionGuard.InLuxandraExecution = false;
                        if (successfullyRerolledNeutralEvent)
                        {
                            // Only reset if the event actually successfully swapped!
                            n.ResetSexCountersForRerolls();
                        }
                    }

                    LuxandraDebugActions.DebugLogMessage($"Neutral event sexual cycle ended, rerolled: {successfullyRerolledNeutralEvent}");
                    return !successfullyRerolledNeutralEvent;
                }

                LuxandraDebugActions.DebugLogMessage($"Got to the end of the function. This should not happen.");
                LuxandraExecutionGuard.InLuxandraExecution = false;
                return true;
            }
        }
        public static void DebugCleanseQueue()
        {
            // Access the private queue field if necessary, or use the public accessor if available
            var queue = Find.Storyteller.incidentQueue;

            // We need to be careful with modifying a list while iterating, 
            // so we'll build a clean list first.
            List<QueuedIncident> toKeep = new List<QueuedIncident>();
            int removedCount = 0;

            foreach (QueuedIncident q in queue)
            {
                // Check if the incident or its worker is null
                if (q.FiringIncident?.def != null && q.FiringIncident.def.Worker != null)
                {
                    toKeep.Add(q);
                }
                else
                {
                    removedCount++;
                    string name = q.FiringIncident?.def?.defName ?? "NULL DEF";
                    Log.Warning($"[Luxandra] Found poisoned incident: {name}. Removing...");
                }
            }

            if (removedCount > 0)
            {
                // Reflection is often needed to clear/rebuild the queue since the 
                // underlying list is sometimes protected. 
                // If the property is public, just queue.Clear() and re-add.
                queue.Clear();
                foreach (var q in toKeep)
                {
                    queue.Add(q);
                }
                Log.Message($"[Luxandra] Queue cleansed. Removed {removedCount} poisoned incidents.");
            }
            else
            {
                Log.Message("[Luxandra] No poisoned incidents found in queue.");
            }
        }
    }
}
