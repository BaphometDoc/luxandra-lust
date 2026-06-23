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
        // Determine how often the weekly cycle fires... is it even still weekly though?
        public static int weeklyCycleDays = 7;
        // Whether to enable debug logging for the mod
        public static bool enableLogging = false;

        public static void DoWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.maxOneColumn = true;

            listingStandard.Begin(inRect);
            listingStandard.Gap(12f);

            // Debug logging toggle
            if (Prefs.DevMode)
            {
                listingStandard.CheckboxLabeled("Enable Debug Logging", ref enableLogging, "Shows most actions in the log. Very spammy, only for debugging.");
                listingStandard.Gap(16f);
            }

            // Weekly interval modifier
            listingStandard.Label($"Storyteller Special Cycle Interval: {weeklyCycleDays} Days");
            weeklyCycleDays = Mathf.RoundToInt(listingStandard.Slider(weeklyCycleDays, 1f, 15f));

            listingStandard.Label("Determines how frequently Luxandra will review your colony's satisfaction levels and deliver a special cyclic event.");
            listingStandard.Gap(24f);

            if (Current.Game != null)
            {
                var cycleComponent = Current.Game.GetComponent<GameComponent_WeeklyEventCycle>();
                if (cycleComponent != null)
                {
                    int currentMaxTicks = weeklyCycleDays * 60000;

                    if (cycleComponent.ticksUntilEvent > currentMaxTicks)
                    {
                        cycleComponent.ticksUntilEvent = currentMaxTicks;
                        DebugActions_Luxandra.DebugLogMessage($"Timer was higher than global settings. Dropped ticksUntilEvent down to {currentMaxTicks} ticks.");
                    }
                    else if (Prefs.DevMode)
                    {
                        float daysRemaining = (float)cycleComponent.ticksUntilEvent / 60000f;
                        listingStandard.Label($"<color=cyan>  • Current active countdown: {daysRemaining:F1} days remaining.</color>");
                        listingStandard.Gap(24f);
                    }
                }
            }

            // Sexual reroll threshold
            listingStandard.Label($"Event sexualization threshold multiplier: {eventThresholdMultiplier:F1}x");

            listingStandard.Label("Adjusts the event conversion multiplier. A higher multiplier means you will need a higher frequency of sex actions to swap negative storyteller events.");
            listingStandard.Gap(6f);

            eventThresholdMultiplier = Mathf.RoundToInt(listingStandard.Slider(eventThresholdMultiplier, 0.5f, 10.0f));
            listingStandard.Gap(24f);

            if (Prefs.DevMode)
            {
                // Preview it if there's an actual game running
                if (Current.Game != null && Find.CurrentMap != null && Find.CurrentMap.mapPawns != null)
                {
                    try
                    {
                        int adultColonists = Find.CurrentMap.mapPawns.FreeColonistsSpawned
                            .Count(p => LuxandraLustUtilities.IsAdult(p));
                        int adultSlaves = Find.CurrentMap.mapPawns.SlavesOfColonySpawned
                            .Count(p => LuxandraLustUtilities.IsAdult(p));

                        int baseThreshold = (adultColonists * 2) + adultSlaves;
                        int finalThreshold = System.Math.Max(1, Mathf.RoundToInt(baseThreshold * eventThresholdMultiplier));

                        listingStandard.Label("<color=cyan>Live Active Colony Preview:</color>");
                        listingStandard.Label($"  • Spawned Adult Colonists: <color=cyan>{adultColonists}</color> (x2 weight)");
                        listingStandard.Label($"  • Spawned Adult Slaves: <color=cyan>{adultSlaves}</color> (x1 weight)");
                        listingStandard.Label($"  • Base Formula Metric: <color=cyan>{baseThreshold}</color>");
                        listingStandard.Label($"  • Final Active Target Threshold: <color=cyan><b>{finalThreshold}</b></color>");
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
            }

            listingStandard.Gap(24f);

            // Reset
            if (listingStandard.ButtonText("Reset to Default"))
            {
                eventThresholdMultiplier = 1.0f;
                weeklyCycleDays = 7;
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
            Scribe_Values.Look(ref weeklyCycleDays, "weeklyCycleDays", 7);
        }
    }
}