using HarmonyLib;
using RimWorld;
using System;
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

        // =============================================================
        // === WARNING: This is a huge mess. Read at your own risk.  ===
        // =============================================================
        // Intercept the storyteller event, and re-evaluate it based on how much sex has been going on
        // Note to self and whoever attempts to read this
        // Return TRUE = continue and execute whatever event was queued
        // Return FALSE = stop and do not execute whatever event was queued
        [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
        public static class Patch_IncidentExecute
        {
            [HarmonyPrefix]
            public static bool Prefix(IncidentWorker __instance, IncidentParms parms)
            {
                // Possibly redundant but i dont trust Rimworld
                if (!LuxandraStorytellerCheck.IsActive())
                {
                    //LuxandraDebugActions.DebugLogMessage("Luxandra was not active. Skipping the incident intercept. Why do you have her debug on again?.");
                    return true;
                }

                // FAILSAFE: recursion guard
                if (LuxandraExecutionGuard.InLuxandraExecution)
                {
                    LuxandraDebugActions.DebugLogMessage("Recursion was caught. If you see this more than once, something has broke, contact the mod dev with a log.");
                    return true;
                }

                // I bet there are weird people who'd do this so better safe than sorry
                var anyEventEnabled = LuxandraEventCheck.IsAnyEventEnabled();
                if (!anyEventEnabled)
                {
                    LuxandraDebugActions.DebugLogMessage("Every special event in the storyteller is disabled. Why are you using this mod again?");
                    return true;
                }

                // Ignore quest threats as well as the weekly cycle from Luxandra herself
                bool isFromQuest = parms.quest != null || parms.forced;
                bool isFromLuxandraPool = LuxandraDefsCollections.AllIncidents.Select(i => i.IncidentDef).Contains(__instance.def);
                if (isFromQuest || isFromLuxandraPool)
                {
                    LuxandraDebugActions.DebugLogMessage("Event is from quest or forced, skipping the reroll.");
                    return true;
                }

                // Prepare the configs and settings for the reroll logic
                bool isSexualRerollEnabled = LuxandraModSettings.eventRerollCondition != 0;

                // Check that the system isnt straight up disabled
                if (!isSexualRerollEnabled)
                {
                    // Exits if the reroll is disabled
                    LuxandraDebugActions.DebugLogMessage("Event rerolling is disabled. Skipping...");
                    return true;
                }

                // Alright if we got so far it means the system is enabled and the event is not from a quest.
                // Check if the conditions for the reroll from the settings are met.

                // Event conversion condition - 0: Disabled, 1: Only Negative, 2: All Events
                int rerollConditionConfig = LuxandraModSettings.eventRerollCondition;
                // Threshold multiplier from settings
                float rerollThresholdMultiplierFromConfigs = LuxandraModSettings.eventRerollThresholdMultiplier;
                LuxandraDebugActions.DebugLogMessage("Configs loaded:");
                LuxandraDebugActions.DebugLogMessage($"rerollConditionConfig: {rerollConditionConfig}");
                LuxandraDebugActions.DebugLogMessage($"rerollThresholdMultiplierFromConfigs: {rerollThresholdMultiplierFromConfigs}");

                var def = __instance.def;
                // Also check if the map is null, because that can happen if the event is not map based (like a caravan event)
                IIncidentTarget target = parms.target;

                // Failsafe if I fucked up something somewhere (trust me it has happened)
                var n = GameComponent_LuxandraLust.Instance;
                if (def == null || n == null || target == null)
                {
                    LuxandraDebugActions.DebugLogMessage($"The event reroll should have never entered here. Something fucked up.");
                    return true;
                }

                // Map should be valid at this point since I checked for null earlier, but just in case, default to the current map if it's invalid
                Map targetMap = parms.target as Map ?? Find.CurrentMap;

                // Determine the threshold for the event conversion
                LuxandraDebugActions.DebugLogMessage("TryExecute intercepted correctly");

                var eventType = def.category;
                LuxandraDebugActions.DebugLogMessage($"Incident about to happen: {__instance.def?.defName} - Event type: {eventType}");

                // This should catch like, 99% of the negative events. If something is negative but not configured as such, I blame
                // the submod dev. If it's from Rimworld, I blame Tynan.
                bool isNegative = eventType == IncidentCategoryDefOf.ThreatBig || eventType == IncidentCategoryDefOf.ThreatSmall || def == IncidentDefOf.RaidEnemy;

                // Exit if the config says to only reroll negative events
                if (rerollConditionConfig == 1 && !isNegative)
                {
                    LuxandraDebugActions.DebugLogMessage("Event not negative. Skipping...");
                    return true;
                }

                int totalThreshold = GameComponent_LuxandraLust.CalculateSexualRerollThreshold();
                LuxandraDebugActions.DebugLogMessage($"Current Favor points: {n.colonyFavorPoints} - Event threshold: {totalThreshold}");

                // Putting this here so if I feel like changing it I don't need 200 copypastes.
                string luxandraRerollName = "Luxandra's Lustful Gaze";
                bool shouldEventRerollHappen = n.colonyFavorPoints >= totalThreshold && isSexualRerollEnabled; // The isSexualRerollEnabled is redundant but you never know...

                // Finally, if all is sorted, we can proceed with asking the player.
                if (shouldEventRerollHappen)
                {
                    LuxandraDebugActions.DebugLogMessage("Luxandra is stepping in to offer a bargain.");

                    int matchTypeCost = totalThreshold / 2;
                    int randomTypeCost = isNegative ? totalThreshold / 3 * 2 : totalThreshold / 3;
                    int positiveSwapCost = totalThreshold;
                    int revealCost = isNegative ? 5 : 1;

                    // Pass the reveal state directly into the creation method so the next window knows its state
                    CreateLuxandraBargainWindow(__instance, parms, matchTypeCost, randomTypeCost, positiveSwapCost, revealCost, isNegative, def, luxandraRerollName, false);

                    return false;
                }
                return true;
            }
        }

        private static void CreateLuxandraBargainWindow(IncidentWorker __instance, IncidentParms parms, int matchTypeCost, int randomTypeCost, int positiveSwapCost, int revealCost, bool isNegative, IncidentDef def, string title, bool hasPaidToReveal)
        {
            var liveComp = GameComponent_LuxandraLust.Instance;
            if (liveComp == null)
            {
                Log.Error("[Luxandra] Cannot open window: GameComponent instance is null.");
                return;
            }

            string eventDescription = isNegative ? "a dangerous fate" : "a ripple";
            string eventNameText = hasPaidToReveal ? $" ({def.label.Colorize(UnityEngine.Color.cyan)})" : "";

            string text = $"Luxandra's gaze falls upon your colony as {eventDescription} approaches{eventNameText}. She is amused by your actions and offers to warp the threads of fate... for a price.\n\n" +
                          $"Current Favor Counter: {liveComp.colonyFavorPoints}\n" +
                          $"• Reshape the timeline to a matching spicy incident: {matchTypeCost} points\n" +
                          $"• Reshape the timeline to a random exhilarating incident: {randomTypeCost} points\n" +
                          $"• Force a completely positive and arousing incident: {positiveSwapCost} points";

            DiaNode rootNode = new DiaNode(text);

            // --- OPTION: PEEK INTO THE FUTURE ---
            if (!hasPaidToReveal)
            {
                DiaOption revealOption = new DiaOption($"Scent the winds of fate to reveal the incoming event (Cost: {revealCost} points)");
                if (liveComp.colonyFavorPoints < revealCost)
                {
                    revealOption.disabled = true;
                    revealOption.disabledReason = "Insufficient favor counter.";
                }
                else
                {
                    revealOption.action = () =>
                    {
                        var clickComp = GameComponent_LuxandraLust.Instance;
                        if (clickComp != null)
                        {
                            clickComp.PayForLuxandraServices(revealCost);

                            // Close the active tree safely without deleting collections mid-loop
                            Find.WindowStack.TryRemove(typeof(Dialog_NodeTree), false);

                            // Spawn the new revealed window smoothly
                            CreateLuxandraBargainWindow(__instance, parms, matchTypeCost, randomTypeCost, positiveSwapCost, revealCost, isNegative, def, title, true);
                        }
                    };
                }
                rootNode.options.Add(revealOption);
            }

            // --- CHOICE 1: LET IT PLAY ---
            DiaOption acceptOption = new DiaOption("Let it play out as is (Cost: Free)");
            acceptOption.action = () =>
            {
                LuxandraExecutionGuard.InLuxandraExecution = true;
                try { __instance.TryExecute(parms); }
                finally { LuxandraExecutionGuard.InLuxandraExecution = false; }
            };
            acceptOption.resolveTree = true;
            rootNode.options.Add(acceptOption);

            // --- CHOICE 2: MATCH TYPE SWAP ---
            DiaOption matchTypeOption = new DiaOption($"Reshape the timeline into a spicy event of the same threat level (Cost: {matchTypeCost})");
            if (liveComp.colonyFavorPoints < matchTypeCost)
            {
                matchTypeOption.disabled = true;
                matchTypeOption.disabledReason = "Insufficient favor counter.";
            }
            else
            {
                matchTypeOption.action = () =>
                {
                    ExecuteLuxandraEventConversion(__instance, parms, matchTypeCost, forcePositive: false, randomResult: false);
                };
            }
            matchTypeOption.resolveTree = true;
            rootNode.options.Add(matchTypeOption);

            // --- CHOICE 3: RANDOM TYPE SWAP ---
            DiaOption randomTypeOption = new DiaOption($"Reshape the timeline into an exhilarating event of any type (Cost: {randomTypeCost})");
            if (liveComp.colonyFavorPoints < randomTypeCost)
            {
                randomTypeOption.disabled = true;
                randomTypeOption.disabledReason = "Insufficient favor counter.";
            }
            else
            {
                randomTypeOption.action = () =>
                {
                    ExecuteLuxandraEventConversion(__instance, parms, randomTypeCost, forcePositive: false, randomResult: true);
                };
            }
            randomTypeOption.resolveTree = true;
            rootNode.options.Add(randomTypeOption);

            // --- CHOICE 4: FORCE POSITIVE SWAP ---
            DiaOption forcePositiveOption = new DiaOption($"Seduce fate to grant a helpful, arousing boon (Cost: {positiveSwapCost})");
            if (liveComp.colonyFavorPoints < positiveSwapCost)
            {
                forcePositiveOption.disabled = true;
                forcePositiveOption.disabledReason = "Insufficient favor counter.";
            }
            else
            {
                forcePositiveOption.action = () =>
                {
                    ExecuteLuxandraEventConversion(__instance, parms, positiveSwapCost, forcePositive: true, randomResult: false);
                };
            }
            forcePositiveOption.resolveTree = true;
            rootNode.options.Add(forcePositiveOption);

            Dialog_NodeTree dialogWindow = new Dialog_NodeTree(rootNode, delayInteractivity: true, radioMode: false, title: title);
            Find.WindowStack.Add(dialogWindow);
        }

        public static void ExecuteLuxandraEventConversion(IncidentWorker __instance, IncidentParms parms, int payment, bool forcePositive = false, bool randomResult = false)
        {
            LuxandraExecutionGuard.InLuxandraExecution = true;
            bool successfullyRerolled = false;
            try
            {
                var def = __instance.def;
                var eventType = def.category;
                // Define what kind of event we got
                bool isNegative = eventType == IncidentCategoryDefOf.ThreatBig || eventType == IncidentCategoryDefOf.ThreatSmall || def == IncidentDefOf.RaidEnemy;
                bool isPositive = def.letterDef == LetterDefOf.PositiveEvent;
                // Change the event to forced to force-skip a potential recursion
                parms.forced = true;

                // Prepare a generic event replacement for the reroll
                List<IncidentDef> sexEventsForReplacement = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.AllIncidents);
                bool foundValidSexEventForReroll = sexEventsForReplacement
                    .Where(x => x.Worker.CanFireNow(parms))
                    .TryRandomElement(out IncidentDef genericIncidentReplacement);

                if (!foundValidSexEventForReroll)
                {
                    // Somehow no valid event was available. This should never happen, but if it does, just let the original event play
                    // and warn the player to report it.
                    Log.Warning("[Luxandra Debug] No valid event was found for Luxandra's event reroll. Letting the original event go off instead. If this shows up please send a log to the dev.");
                    Messages.Message("Luxandra attempted to bend fate, but there was no response. Fate resumed its natural course.", MessageTypeDefOf.NeutralEvent, false);

                    LuxandraExecutionGuard.InLuxandraExecution = false;
                    return;
                }

                // Random reroll was chosen
                if (randomResult)
                {
                    LuxandraDebugActions.DebugLogMessage($"Random incident was chosen. Executing random incident: {genericIncidentReplacement.defName}");
                    Messages.Message("Luxandra spins the wheel of fate with vigorous energy. Fate changed unpredictably, what may be in store for you?", MessageTypeDefOf.NeutralEvent, false);
                    genericIncidentReplacement.Worker.TryExecute(parms);
                    successfullyRerolled = true;
                    return;
                }

                // Event is positive, or it was requested to force positive events
                if (forcePositive || isPositive)
                {
                    List<IncidentDef> availablePositiveEvents = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.PositiveIncidents);
                    bool foundValidPositiveEventAvailable = availablePositiveEvents
                            .Where(x => x.Worker.CanFireNow(parms))
                            .TryRandomElement(out IncidentDef positiveReplacement);

                    if (foundValidPositiveEventAvailable)
                    {
                        LuxandraDebugActions.DebugLogMessage($"Positive replacement incident found: {positiveReplacement.defName}");
                        Messages.Message("Luxandra glees with excitement, sending a divine blessing toward your colonists. Fate smiles upon you.", MessageTypeDefOf.PositiveEvent, false);
                        positiveReplacement.Worker.TryExecute(parms);
                        successfullyRerolled = true;
                    }
                    else
                    {
                        Log.Warning("[Luxandra Debug] No valid positive event was found for Luxandra's event reroll. Letting the a random event go off instead. If this shows up please send a log to the dev.");
                        Messages.Message("Luxandra attempted to bend fate, but the response was unexpected. Fate changed unpredictably.", MessageTypeDefOf.NeutralEvent, false);

                        LuxandraDebugActions.DebugLogMessage($"Positive replacement was not found. Executing random incident: {genericIncidentReplacement.defName}");
                        genericIncidentReplacement.Worker.TryExecute(parms);
                        successfullyRerolled = true;
                    }
                }
                // Event is negative and has to match a negative event
                else if (isNegative)
                {
                    List<IncidentDef> availableNegativeEvents = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.NegativeIncidents);
                    bool foundValidNegativeEventAvailable = availableNegativeEvents
                            .Where(x => x.Worker.CanFireNow(parms))
                            .TryRandomElement(out IncidentDef negativeReplacement);

                    if (foundValidNegativeEventAvailable)
                    {
                        LuxandraDebugActions.DebugLogMessage($"Negative replacement incident found: {negativeReplacement.defName}");
                        negativeReplacement.Worker.TryExecute(parms);
                        successfullyRerolled = true;
                    }
                    else
                    {
                        Log.Warning("[Luxandra Debug] No valid negative event was found for Luxandra's event reroll. Letting the a random event go off instead. If this shows up please send a log to the dev.");
                        Messages.Message("Luxandra attempted to bend fate, but the response was unexpected. Fate changed unpredictably.", MessageTypeDefOf.NeutralEvent, false);

                        LuxandraDebugActions.DebugLogMessage($"Positive replacement was not found. Executing random incident: {genericIncidentReplacement.defName}");
                        genericIncidentReplacement.Worker.TryExecute(parms);
                        successfullyRerolled = true;
                    }
                }
                // Event is neutral and has to match a neutral event
                else
                {
                    List<IncidentDef> availableNeutralEvents = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.NeutralIncidents);
                    bool foundValidNeutralEventAvailable = availableNeutralEvents
                            .Where(x => x.Worker.CanFireNow(parms))
                            .TryRandomElement(out IncidentDef neutralReplacement);

                    if (foundValidNeutralEventAvailable)
                    {
                        LuxandraDebugActions.DebugLogMessage($"Neutral replacement incident found: {neutralReplacement.defName}");
                        neutralReplacement.Worker.TryExecute(parms);
                        successfullyRerolled = true;
                    }
                    else
                    {
                        Log.Warning("[Luxandra Debug] No valid neutral event was found for Luxandra's event reroll. Letting the a random event go off instead. If this shows up please send a log to the dev.");
                        Messages.Message("Luxandra attempted to bend fate, but the response was unexpected. Fate changed unpredictably.", MessageTypeDefOf.NeutralEvent, false);

                        LuxandraDebugActions.DebugLogMessage($"Neutral replacement was not found. Executing random incident: {genericIncidentReplacement.defName}");
                        genericIncidentReplacement.Worker.TryExecute(parms);
                        successfullyRerolled = true;
                    }

                }
            }
            catch (System.Exception ex)
            {
                LuxandraDebugActions.DebugLogMessage($"Error during conversion loop: {ex}");
            }
            finally
            {
                LuxandraExecutionGuard.InLuxandraExecution = false;

                if (successfullyRerolled)
                {
                    GameComponent_LuxandraLust.Instance?.PayForLuxandraServices(payment);

                    LuxandraDebugActions.DebugLogMessage($"Reroll executed correctly. Executing incident...");
                }
                else
                {
                    LuxandraDebugActions.DebugLogMessage($"Reroll failed. Executing original incident...");
                    __instance.TryExecute(parms);
                }
            }
        }
    }
}
