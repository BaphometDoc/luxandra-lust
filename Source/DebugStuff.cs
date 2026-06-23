using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace LuxandraLust
{
    public static class DebugActions_Luxandra
    {
        [DebugAction("Luxandra Lust", "Log Sex Counters", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void LogSexCounters()
        {
            var component = GameComponent_LuxandraLust.Instance;

            if (component == null)
            {
                Log.Error("[Luxandra] Cannot print counters: GameComponent_LuxandraLust is null (Are you currently in a game session?)");
                return;
            }

            Map targetMap = Find.CurrentMap;
            int totalThreshold = 0;
            int thresholdAfterSettings = 0;
            int adultColonistCount = 0;
            int adultSlavesCount = 0;
            float settingsMultiplier = LuxandraModSettings.eventThresholdMultiplier;

            if (targetMap != null)
            {
                adultColonistCount = targetMap.mapPawns.FreeColonistsSpawned
                    .Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);
                adultSlavesCount = targetMap.mapPawns.SlavesOfColonySpawned
                    .Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);


                totalThreshold = adultColonistCount * 2 + adultSlavesCount;

                thresholdAfterSettings = (int)(totalThreshold * settingsMultiplier);
            }

            // Print the current persistent values beautifully formatted to the debug log console
            Log.Message("==================================================");
            Log.Message($"[Luxandra Debug] Current Sex Actions Tracked:");
            Log.Message($" -> Total Sex Actions: {component.sexActionCounter}");
            Log.Message($" -> Impure Sex Actions: {component.impureSexActionCounter}");
            Log.Message($" -> Rape Sex Actions: {component.rapeSexActionCounter}");
            Log.Message("==================================================");
            if (targetMap != null)
            {
                Log.Message($" -> Colony Metric: Adults ({adultColonistCount}) * 2 + Slaves ({adultSlavesCount}) = Threshold: {totalThreshold}");
                Log.Message($" -> Settings multiplier: {settingsMultiplier} = Effective threshold: {thresholdAfterSettings}");
                Log.Message($" -> Dynamic Target Met? {(component.sexActionCounter > thresholdAfterSettings ? "YES (Will convert negative events)" : "NO")}");
            }
            else
            {
                Log.Message(" -> Colony Metric: [No active map found]");
            }
            Log.Message("==================================================");

            // Send a quick message to the top left of the screen
            Messages.Message("Counters printed to debug log console.", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Luxandra Lust", "Add +1 Sex Count", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddOneTotalSex()
        {
            var comp = GameComponent_LuxandraLust.Instance;
            if (comp == null) { LogNullError(); return; }

            comp.RegisterSexAction();
            Messages.Message($"Added +1 to Total Sex. Current total: {comp.sexActionCounter}", MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Luxandra Lust", "Add +1 Impure Sex Count", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddOneImpureSex()
        {
            var comp = GameComponent_LuxandraLust.Instance;
            if (comp == null) { LogNullError(); return; }

            comp.RegisterImpureSexAction();
            Messages.Message($"Added +1 to Impure Sex. Current total: {comp.impureSexActionCounter}", MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Luxandra Lust", "Add +1 Rape Action Count", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddOneRapeSex()
        {
            var comp = GameComponent_LuxandraLust.Instance;
            if (comp == null) { LogNullError(); return; }

            comp.RegisterRapeSexAction();
            Messages.Message($"Added +1 to Rape Actions. Current total: {comp.rapeSexActionCounter}", MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Luxandra Lust", "Reset All Sex Counters", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ResetAllCounters()
        {
            var comp = GameComponent_LuxandraLust.Instance;
            if (comp == null) { LogNullError(); return; }

            comp.ResetSexCounters();
            Messages.Message("All Luxandra Lust counters have been reset to 0.", MessageTypeDefOf.CautionInput, false);
        }

        [DebugAction("Luxandra Lust", "Trigger Weekly Event (1 Tick)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ForceWeeklyEventNextTick()
        {
            var cycleComponent = Current.Game.GetComponent<GameComponent_WeeklyEventCycle>();

            if (cycleComponent != null)
            {
                cycleComponent.ticksUntilEvent = 1;

                // Safety check in case one's trying to use this without Luxandra
                string activeStoryteller = Find.Storyteller?.def?.defName ?? "None";

                Messages.Message($"[Luxandra] Weekly event timer forced to 1 tick. Active Storyteller: {activeStoryteller}", MessageTypeDefOf.TaskCompletion, false);
                Log.Message($"[Luxandra Debug] Set ticksUntilEvent to 1. Weekly event will fire soon. Current Storyteller: {activeStoryteller}.");
            }
            else
            {
                Log.Error("[Luxandra Debug] Failed to force event: GameComponent_WeeklyEventCycle instance was not found active in this save profile database!");
            }
        }

        [DebugAction("Luxandra Lust", "Print Sexual Event Pool", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AuditEventPoolClassification()
        {
            List<IncidentDef> totalPool = LuxandraEventPool.GetSexRelatedIncidents();

            if (totalPool == null || totalPool.Count == 0)
            {
                Log.Error("[Luxandra Debug] Audit failed: The event pool returned by GetSexRelatedIncidents is empty or null!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine($"   LUXANDRA STORYTELLER EVENT POOL AUDIT ({totalPool.Count} Total)");
            sb.AppendLine("==================================================");

            int positiveCount = 0;
            int negativeCount = 0;
            int neutralCount = 0;

            foreach (IncidentDef incident in totalPool)
            {
                if (incident == null)
                {
                    sb.AppendLine("-> [NULL ENTRY] (A definition failed to load correctly from XML!)");
                    continue;
                }

                string classification = "NEUTRAL / MISC";
                string letterDefName = incident.letterDef?.defName ?? "None";

                if (letterDefName == "PositiveEvent")
                {
                    classification = "POSITIVE";
                    positiveCount++;
                }
                else if (letterDefName == "NegativeEvent")
                {
                    classification = "NEGATIVE";
                    negativeCount++;
                }
                else
                {
                    neutralCount++;
                }

                sb.AppendLine($"-> defName: {incident.defName,-30} | Category: {classification,-14} | letterDef: {letterDefName}");
            }

            sb.AppendLine("==================================================");
            sb.AppendLine($" SUMMARY: Positive: {positiveCount} | Negative: {negativeCount} | Neutral/Misc: {neutralCount}");
            sb.AppendLine("==================================================");

            Log.Message(sb.ToString());

            // Send a quick toast message on screen so you know the audit finished successfully
            Messages.Message($"[Luxandra] Audited {totalPool.Count} events. Open the debug console to view the full list breakdown!", MessageTypeDefOf.TaskCompletion, false);
        }

        #region Log stuff
        // Fallback logging method for when the GameComponent_LuxandraLust is not found for whatever reason
        private static void LogNullError()
        {
            Log.Error("[Luxandra] Dev command failed: GameComponent_LuxandraLust was not found in the active game session.");
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