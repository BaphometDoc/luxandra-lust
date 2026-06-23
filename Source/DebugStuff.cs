using LudeonTK;
using RimWorld;
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

            // Print the current persistent values beautifully formatted to the debug log console
            Log.Message("==================================================");
            Log.Message($"[Luxandra Debug] Current Sex Actions Tracked:");
            Log.Message($" -> Total Sex Actions: {component.sexActionCounter}");
            Log.Message($" -> Impure Sex Actions: {component.impureSexActionCounter}");
            Log.Message($" -> Rape Sex Actions: {component.rapeSexActionCounter}");
            Log.Message("==================================================");

            // Optional: Send a quick message to the top left of the screen so you know it ran successfully
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

        // Fallback logging method for when the GameComponent_LuxandraLust is not found for whatever reason
        private static void LogNullError()
        {
            Log.Error("[Luxandra] Dev command failed: GameComponent_LuxandraLust was not found in the active game session.");
        }
    }
}