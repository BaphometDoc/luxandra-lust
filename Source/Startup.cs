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
                LuxandraDefsCollections.PrintLuxandraIncidentTotals();
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