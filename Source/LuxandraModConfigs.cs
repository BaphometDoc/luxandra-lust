using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LuxandraLust
{
    public class LuxandraMod : Mod
    {
        public LuxandraMod(ModContentPack content) : base(content)
        {
            // Forces RimWorld to read the XML file and populate our static variables right at boot
            GetSettings<LuxandraModSettings>();
        }

        public override string SettingsCategory()
        {
            return "Luxandra Lust";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Forward directly to the static drawing method, exactly like RJW does
            LuxandraModSettings.DoWindowContents(inRect);
        }
    }

    public class LuxandraModSettings : ModSettings
    {
        // Multiplier for the event conversion threshold
        public static float eventThresholdMultiplier = 1.0f;
        public static bool enableLogging = false;

        public static void DoWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.maxOneColumn = true;

            listingStandard.Begin(inRect);
            listingStandard.Gap(12f);

            listingStandard.CheckboxLabeled("Enable Debug Logging", ref enableLogging, "Shows most actions in the log. Very spammy, only for debugging.");
            listingStandard.Gap(16f);

            listingStandard.Label($"Event sexualization threshold multiplier: {eventThresholdMultiplier:F1}x");

            listingStandard.Label("Adjusts the event conversion multiplier. A higher multiplier means you will need a higher frequency of sex actions to swap negative storyteller events.");
            listingStandard.Gap(6f);

            eventThresholdMultiplier = listingStandard.Slider(eventThresholdMultiplier, 0.5f, 10.0f);
            listingStandard.Gap(24f);

            // Preview it if there's an actual game running
            if (Current.Game != null && Find.CurrentMap != null && Find.CurrentMap.mapPawns != null)
            {
                try
                {
                    int adultColonists = Find.CurrentMap.mapPawns.FreeColonistsSpawned
                        .Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);
                    int adultSlaves = Find.CurrentMap.mapPawns.SlavesOfColonySpawned
                        .Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);

                    int baseThreshold = (adultColonists * 2) + adultSlaves;
                    int finalThreshold = System.Math.Max(1, Mathf.RoundToInt(baseThreshold * eventThresholdMultiplier));

                    listingStandard.Label("Live Active Colony Preview:");
                    listingStandard.Label($"  • Spawned Adult Colonists: {adultColonists} (x2 weight)");
                    listingStandard.Label($"  • Spawned Adult Slaves: {adultSlaves} (x1 weight)");
                    listingStandard.Label($"  • Base Formula Metric: {baseThreshold}");
                    listingStandard.Label($"  • Final Active Target Threshold: {finalThreshold}");
                }
                catch (System.Exception ex)
                {
                    // Just in case anything goes wrong with the map data check
                    listingStandard.Label($"Preview Error: {ex.Message}</color>");
                }
            }
            else
            {
                listingStandard.Label("Live Preview: Load a save game file to see your active colony threshold preview.");
            }

            listingStandard.Gap(24f);

            // Reset
            if (listingStandard.ButtonText("Reset to Default"))
            {
                eventThresholdMultiplier = 1.0f;
                enableLogging = false;

                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            listingStandard.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref enableLogging, "enableLogging", true);
            Scribe_Values.Look(ref eventThresholdMultiplier, "eventThresholdMultiplier", 1.0f);
        }
    }
}