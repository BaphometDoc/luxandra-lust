using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace LuxandraLust
{
    public static class LuxandraDebugActions
    {
        [DebugAction("Luxandra Lust", "Log Sex Counters", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void LogSexCounters()
        {
            var component = GameComponent_LuxandraLust.Instance;

            if (component == null)
            {
                Log.Error("[Luxandra Debug] Cannot print counters: GameComponent_LuxandraLust is null (Are you currently in a game session?)");
                return;
            }

            Map targetMap = Find.CurrentMap;
            int totalThreshold = 0;
            int thresholdAfterSettings = 0;
            int adultColonistCount = 0;
            int adultSlavesCount = 0;
            float settingsMultiplier = LuxandraModSettings.eventRerollThresholdMultiplier;

            if (targetMap != null)
            {
                adultColonistCount = targetMap.mapPawns.FreeColonistsSpawned.Count(p => LuxandraUtilities.IsAdult(p));
                adultSlavesCount = targetMap.mapPawns.SlavesOfColonySpawned.Count(p => LuxandraUtilities.IsAdult(p));
                totalThreshold = adultColonistCount * 2 + adultSlavesCount;
                thresholdAfterSettings = (int)(totalThreshold * settingsMultiplier);
            }

            // Print the current persistent values beautifully formatted (kinda) to the debug log console
            Log.Message("==================================================");
            Log.Message($"[Luxandra Debug] Current Actions Tracked:");
            Log.Message($" -> Total Sex Actions:       {component.sexActionCounter}");
            Log.Message($" -> Impure Sex Actions:      {component.impureSexActionCounter}");
            Log.Message($" -> Rape Sex Actions:        {component.rapeSexActionCounter}");
            Log.Message($" -> Bestiality Sex Actions:  {component.bestialitySexActionCounter}");
            Log.Message($" -> Necrophilia Sex Actions: {component.necrophiliaSexActionCounter}");
            Log.Message("==================================================");

            // Send a quick message to the top left of the screen
            Messages.Message("Reroll counters printed to debug log console.", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Luxandra Lust", "Log Sex Counters (Rerolls)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void LogSexCountersRerolls()
        {
            var component = GameComponent_LuxandraLust.Instance;

            if (component == null)
            {
                Log.Error("[Luxandra Debug] Cannot print counters: GameComponent_LuxandraLust is null (Are you currently in a game session?)");
                return;
            }

            Map targetMap = Find.CurrentMap;
            int totalThreshold = 0;
            int thresholdAfterSettings = 0;
            int adultColonistCount = 0;
            int adultSlavesCount = 0;
            float settingsMultiplier = LuxandraModSettings.eventRerollThresholdMultiplier;

            if (targetMap != null)
            {
                adultColonistCount = targetMap.mapPawns.FreeColonistsSpawned.Count(p => LuxandraUtilities.IsAdult(p));
                adultSlavesCount = targetMap.mapPawns.SlavesOfColonySpawned.Count(p => LuxandraUtilities.IsAdult(p));
                totalThreshold = GameComponent_LuxandraLust.CalculateSexualRerollThreshold();
            }

            // Print the current persistent values beautifully formatted (kinda) to the debug log console
            Log.Message("==================================================");
            Log.Message($"[Luxandra Debug] Current Reroll Metrics Tracked:");
            Log.Message($" -> Total Favor Points:       {component.colonyFavorPoints}");
            Log.Message("==================================================");
            if (targetMap != null)
            {
                Log.Message($" -> Colony Metric: {adultColonistCount} Adults, {adultSlavesCount} Slaves. Threshold: {totalThreshold}");
                Log.Message($" -> Settings multiplier: {settingsMultiplier} = Effective threshold: {thresholdAfterSettings}");
                Log.Message($" -> Dynamic Target Met? {(component.colonyFavorPoints > thresholdAfterSettings ? "YES" : "NO")}");
            }
            else
            {
                Log.Message(" -> Colony Metric: [No active map found]");
            }
            Log.Message("==================================================");

            // Send a quick message to the top left of the screen
            Messages.Message("Reroll counters printed to debug log console.", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Luxandra Lust", "Log Sex Counters (Cycle)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void LogSexCountersCycle()
        {
            var component = GameComponent_LuxandraLust.Instance;

            if (component == null)
            {
                Log.Error("[Luxandra Debug] Cannot print counters: GameComponent_LuxandraLust is null (Are you currently in a game session?)");
                return;
            }

            // Print the current persistent values beautifully formatted (kinda) to the debug log console
            Log.Message("==================================================");
            Log.Message($"[Luxandra Debug] Current Storyteller Cycle Actions Tracked:");
            Log.Message($" -> Total Sex Actions:       {component.sexActionCounterForCycle}");
            Log.Message($" -> Impure Sex Actions:      {component.impureSexActionCounterForCycle}");
            Log.Message($" -> Rape Sex Actions:        {component.rapeSexActionCounterForCycle}");
            Log.Message($" -> Bestiality Sex Actions:  {component.bestialitySexActionCounterForCycle}");
            Log.Message($" -> Necrophilia Sex Actions: {component.necrophiliaSexActionCounterForCycle}");
            Log.Message("==================================================");

            // Read and show the tracker
            var weeklyCycle = Current.Game?.GetComponent<GameComponent_LuxandraCyclicEvents>();

            if (weeklyCycle != null)
            {
                int ticksRemaining = weeklyCycle.ticksUntilEvent;

                if (ticksRemaining > 0)
                {
                    // 60,000 ticks = 1 vanilla RimWorld day
                    float daysRemaining = (float)ticksRemaining / 60000f;
                    Log.Message($" -> Time Until Next Cycle:   {daysRemaining:F2} days ({ticksRemaining:N0} ticks)");
                }
                else
                {
                    Log.Message(" -> Time Until Next Cycle:   PENDING (Cycle interval is overdue or triggering right now)");
                }
            }
            else
            {
                Log.Message(" -> Time Until Next Cycle:   [Error: GameComponent_WeeklyEventCycle not found on current game instance]");
            }
            Log.Message("==================================================");

            // Send a quick message to the top left of the screen
            Messages.Message("Cycle counters and tracking timeline printed to console.", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Luxandra Lust", "Manage Sex Counters...", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void OpenSexCountersSubmenu()
        {
            List<DebugMenuOption> options = new List<DebugMenuOption>();

            // --- ADD INCREMENT ACTIONS ---
            options.Add(new DebugMenuOption("Add +50 Favor Points", DebugMenuOptionMode.Action, () =>
            {
                var comp = GameComponent_LuxandraLust.Instance;
                if (comp == null) { LogNullError(); return; }

                comp.AddToFavorCounter(50);
                Messages.Message($"Added +50 Favor. New total: {comp.colonyFavorPoints}", MessageTypeDefOf.NeutralEvent, false);
            }));

            // --- ADD INCREMENT ACTIONS ---
            options.Add(new DebugMenuOption("Add +1 Sex Count", DebugMenuOptionMode.Action, () =>
            {
                var comp = GameComponent_LuxandraLust.Instance;
                if (comp == null) { LogNullError(); return; }

                comp.RegisterSexAction();
                Messages.Message($"Added +1 to Total Sex to counters. Favor: {comp.colonyFavorPoints} | Cycle sex counter: {comp.sexActionCounterForCycle}", MessageTypeDefOf.NeutralEvent, false);
            }));

            options.Add(new DebugMenuOption("Add +1 Impure Sex Count", DebugMenuOptionMode.Action, () =>
            {
                var comp = GameComponent_LuxandraLust.Instance;
                if (comp == null) { LogNullError(); return; }

                comp.RegisterImpureSexAction();
                Messages.Message($"Added +1 to Impure Sex. Reroll total: {comp.impureSexActionCounter} | Cycle total: {comp.impureSexActionCounterForCycle}", MessageTypeDefOf.NeutralEvent, false);
            }));

            options.Add(new DebugMenuOption("Add +1 Rape Action Count", DebugMenuOptionMode.Action, () =>
            {
                var comp = GameComponent_LuxandraLust.Instance;
                if (comp == null) { LogNullError(); return; }

                comp.RegisterRapeSexAction();
                Messages.Message($"Added +1 to Rape Actions. Reroll total: {comp.rapeSexActionCounter} | Cycle total: {comp.rapeSexActionCounterForCycle}", MessageTypeDefOf.NeutralEvent, false);
            }));

            options.Add(new DebugMenuOption("Add +1 Bestiality Sex Count", DebugMenuOptionMode.Action, () =>
            {
                var comp = GameComponent_LuxandraLust.Instance;
                if (comp == null)
                {
                    Log.Error("[Luxandra Debug] Cannot modify counters: GameComponent_LuxandraLust is null.");
                    return;
                }

                comp.RegisterBestialitySexAction();
                Messages.Message($"Added +1 to Bestiality. Reroll total: {comp.bestialitySexActionCounter} | Cycle total: {comp.bestialitySexActionCounterForCycle}", MessageTypeDefOf.NeutralEvent, false);
            }));

            options.Add(new DebugMenuOption("Add +1 Necrophilia Sex Count", DebugMenuOptionMode.Action, () =>
            {
                var comp = GameComponent_LuxandraLust.Instance;
                if (comp == null)
                {
                    Log.Error("[Luxandra Debug] Cannot modify counters: GameComponent_LuxandraLust is null.");
                    return;
                }

                comp.RegisterNecrophiliaSexAction();
                Messages.Message($"Added +1 to Necrophilia. Reroll total: {comp.necrophiliaSexActionCounter} | Cycle total: {comp.necrophiliaSexActionCounterForCycle}", MessageTypeDefOf.NeutralEvent, false);
            }));

            // --- ADD RESET ACTIONS ---
            options.Add(new DebugMenuOption("Reset All Reroll Sex Counters", DebugMenuOptionMode.Action, () =>
            {
                var comp = GameComponent_LuxandraLust.Instance;
                if (comp == null) { LogNullError(); return; }

                comp.ResetSexCountersForRerolls();
                Messages.Message("All Luxandra Lust counters for rerolls have been reset to 0.", MessageTypeDefOf.CautionInput, false);
            }));

            options.Add(new DebugMenuOption("Reset All Cycle Sex Counters", DebugMenuOptionMode.Action, () =>
            {
                var comp = GameComponent_LuxandraLust.Instance;
                if (comp == null) { LogNullError(); return; }

                comp.ResetSexCountersForCycle();
                Messages.Message("All Luxandra Lust counters for her cycle have been reset to 0.", MessageTypeDefOf.CautionInput, false);
            }));

            // Open the dynamic layout popup window frame
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        // The [DebugAction] attribute registers this automatically into the Dev Menu!
        [DebugAction("Luxandra Lust", "Force Kink Shift", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ForceKinkShift()
        {
            // Find the active game component handling the clock
            var cycleComponent = Current.Game.GetComponent<GameComponent_LuxandraCyclicEvents>();

            if (cycleComponent != null)
            {
                // Access your private trigger method using a quick public hook, 
                // or simply force the countdown timer to 1 tick so it fires on the very next frame!
                cycleComponent.ForceImmediateKinkShift();

                DebugLogMessage("Dev Menu command issued: Forced storyteller phase rotation successfully.");
            }
            else
            {
                Log.Error("Luxandra Mod Error: Could not force shift because GameComponent_WeeklyEventCycle was not found active in this save.");
            }
        }

        [DebugAction("Luxandra Lust", "Trigger Weekly Event (1 Tick)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ForceWeeklyEventNextTick()
        {
            var cycleComponent = Current.Game.GetComponent<GameComponent_LuxandraCyclicEvents>();

            if (cycleComponent != null)
            {
                cycleComponent.ticksUntilEvent = 1;

                // Safety check in case one's trying to use this without Luxandra
                string activeStoryteller = Find.Storyteller?.def?.defName ?? "None";

                Messages.Message($"[Luxandra Debug] Weekly event timer forced to 1 tick. Active Storyteller: {activeStoryteller}", MessageTypeDefOf.TaskCompletion, false);
                Log.Message($"[Luxandra Debug] Set ticksUntilEvent to 1. Weekly event will fire soon. Current Storyteller: {activeStoryteller}.");
            }
            else
            {
                Log.Error("[Luxandra Debug] Failed to force event: GameComponent_WeeklyEventCycle instance was not found active in this save profile database!");
            }
        }

        [DebugAction("Luxandra Lust", "Print Sexual Event Pool Audit", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AuditEventPoolClassification()
        {
            var allEvents = LuxandraDefsCollections.AllIncidents;
            var positiveEvents = LuxandraDefsCollections.PositiveIncidents;
            var negativeEvents = LuxandraDefsCollections.NegativeIncidents;
            var neutralEvents = LuxandraDefsCollections.NeutralIncidents;
            var raidEvents = LuxandraDefsCollections.Raids;
            var questEvents = LuxandraDefsCollections.Quests;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine("   LUXANDRA STORYTELLER EVENT POOL AUDIT");
            sb.AppendLine("==================================================");

            void LogCategory(string label, IEnumerable<LuxandraIncidentDefs> collection)
            {
                sb.AppendLine($"{label} ({collection.Count()}):");
                foreach (var item in collection)
                {
                    sb.AppendLine($"  -> {item.IncidentDef.defName}");
                }
            }

            LogCategory("POSITIVE EVENTS", positiveEvents);
            LogCategory("NEGATIVE EVENTS", negativeEvents);
            LogCategory("NEUTRAL EVENTS", neutralEvents);
            LogCategory("RAID EVENTS", raidEvents);
            LogCategory("QUEST EVENTS", questEvents);

            sb.AppendLine("==================================================");
            LogCategory("TOTAL EVENTS:", allEvents);
            sb.AppendLine("==================================================");
            Log.Message(sb.ToString());

            Messages.Message("[Luxandra Debug] Audit complete. Check console for breakdown.", MessageTypeDefOf.TaskCompletion, false);
        }

        #region Log stuff
        // Fallback logging method for when the GameComponent_LuxandraLust is not found for whatever reason
        private static void LogNullError()
        {
            Log.Error("[Luxandra Debug] Dev command failed: GameComponent_LuxandraLust was not found in the active game session.");
        }

        /// <summary>
        /// Prints a debug log message with the [Luxandra Debug] prefix if logging is enabled in the mod settings.
        /// </summary>
        /// <param name="message"></param>
        public static void DebugLogMessage(string message)
        {
            if (LuxandraModSettings.enableLogging)
                Log.Message($"[Luxandra Debug] {message}");
        }
        #endregion
    }
}