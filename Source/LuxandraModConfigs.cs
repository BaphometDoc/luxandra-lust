using RimWorld;
using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Multiplier for the event conversion threshold
        /// </summary>
        public static float eventThresholdMultiplier = 1.0f;
        /// <summary>
        /// Event conversion condition - 0: Disabled, 1: Only Negative, 2: All Events
        /// </summary>
        public static int eventRerollCondition = 1;
        /// <summary>
        /// Event conversion type - 0: Match type, 1: Always positive, 2: Random
        /// </summary>
        public static int eventConversionMode = 2;
        /// <summary>
        /// Determine how often the weekly cycle fires... is it even still weekly though?
        /// </summary>
        public static int weeklyCycleDays = 7;
        /// <summary>
        /// Whether to enable debug logging for the mod
        /// </summary>
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
                        LuxandraDebugActions.DebugLogMessage($"Timer was higher than global settings. Dropped ticksUntilEvent down to {currentMaxTicks} ticks.");
                    }
                    else if (Prefs.DevMode)
                    {
                        float daysRemaining = (float)cycleComponent.ticksUntilEvent / 60000f;
                        listingStandard.Label($"<color=cyan>  • Current active countdown: {daysRemaining:F1} days remaining.</color>");
                        listingStandard.Gap(24f);
                    }
                }
            }

            // Sexual reroll mode
            listingStandard.Gap(12f);
            float labelWidth = 200f;
            float buttonWidth = 200f;

            string[] conditionOptions = { "Disabled", "Only Negative", "All Events" };
            DrawSettingRowWithButon(listingStandard, "Reroll condition:", conditionOptions,
                eventRerollCondition,
                (val) => eventRerollCondition = val,
                labelWidth, buttonWidth,
                "Determines what events Luxandra will reroll based on your sex acts.");

            bool showRerollSettings = eventRerollCondition != 0;

            // Hide the settings if the reroll is disabled
            if (showRerollSettings)
            {
                string[] conversionOptions = { "Match event type", "Always positive", "Random" };
                DrawSettingRowWithButon(listingStandard, "Event Conversion Mode:", conversionOptions,
                    eventConversionMode,
                    (val) => eventConversionMode = val,
                    labelWidth, buttonWidth,
                    "Determines what type of events Luxandra will choose from her event pool for the rerolls.");

                listingStandard.Gap(12f);

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
                                .Count(p => LuxandraUtilities.IsAdult(p));
                            int adultSlaves = Find.CurrentMap.mapPawns.SlavesOfColonySpawned
                                .Count(p => LuxandraUtilities.IsAdult(p));

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

            }

            listingStandard.Gap(24f);

            // Reset
            if (listingStandard.ButtonText("Reset to Default"))
            {
                eventRerollCondition = 1;
                eventConversionMode = 2;
                eventThresholdMultiplier = 1.0f;
                weeklyCycleDays = 7;
                enableLogging = false;

                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            listingStandard.End();
        }

        private static void DrawSettingRowWithButon(Listing_Standard listing, string label, string[] options, int selectedIndex, Action<int> onSelect, float labelWidth, float buttonWidth, string tooltip = null)
        {
            Rect rect = listing.GetRect(30f);

            // Define the Label Rect
            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            Widgets.Label(labelRect, label);

            // Add Tooltip to the Label area
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(labelRect, tooltip);
            }

            // Draw the Button
            Rect buttonRect = new Rect(rect.x + labelWidth, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(buttonRect, options[selectedIndex]))
            {
                List<FloatMenuOption> floatOptions = new List<FloatMenuOption>();
                for (int i = 0; i < options.Length; i++)
                {
                    int index = i;
                    floatOptions.Add(new FloatMenuOption(options[i], () => onSelect(index)));
                }
                Find.WindowStack.Add(new FloatMenu(floatOptions));
            }

            // Add Tooltip to the Button area too (optional, but often helpful)
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(buttonRect, tooltip);
            }

            listing.Gap(6f);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref enableLogging, "enableLogging", true);
            Scribe_Values.Look(ref eventConversionMode, "eventConversionMode", 1);
            Scribe_Values.Look(ref eventRerollCondition, "eventRerollCondition", 2);
            Scribe_Values.Look(ref eventThresholdMultiplier, "eventThresholdMultiplier", 1.0f);
            Scribe_Values.Look(ref weeklyCycleDays, "weeklyCycleDays", 7);
        }
    }
}