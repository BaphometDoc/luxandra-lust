using HarmonyLib;
using Verse;

namespace LuxandraLust
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            var harmony = new Harmony("world87.luxandralust");
            harmony.PatchAll();

            Log.Message("[Luxandra Lust] loaded successfully");

            bool showDetailedLog = LuxandraModSettings.enableLogging;

            // Initialize the incidents
            LuxandraDefsCollections.InizializeLuxandraIncidents();
            if (showDetailedLog)
            {
                // Use the public safe properties now, and swap out .Count() for .Count (performance win)
                Log.Message($"[Luxandra Lust] found {LuxandraDefsCollections.AllIncidents.Count} lustful events.");

                LuxandraDebugActions.DebugLogMessage($"Positive incidents: {LuxandraDefsCollections.PositiveIncidents.Count}");
                LuxandraDebugActions.DebugLogMessage($"Neutral incidents: {LuxandraDefsCollections.NeutralIncidents.Count} (including {LuxandraDefsCollections.Quests.Count} quests)");
                LuxandraDebugActions.DebugLogMessage($"Negative incidents: {LuxandraDefsCollections.NegativeIncidents.Count} (including {LuxandraDefsCollections.Raids.Count} raids)");
                LuxandraDebugActions.DebugLogMessage($"TOTAL: {LuxandraDefsCollections.AllIncidents.Count} incidents available.");
            }

            // Initialize the factions
            LuxandraFactionDefsCollections.InizializeLuxandraFactions();
            if (showDetailedLog)
            {
                var factionsInitialized = LuxandraFactionDefsCollections.AllFactions;

                Log.Message($"[Luxandra Lust] found {factionsInitialized.Count} factions.");
            }
        }
    }
}