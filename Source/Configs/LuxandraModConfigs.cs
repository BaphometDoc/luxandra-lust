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
            // Forces RimWorld to read the XML file and populate the static variables at boot
            GetSettings<LuxandraModSettings>();
        }

        public override string SettingsCategory()
        {
            return "Luxandra Lust";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Forward directly to the static drawing method
            LuxandraModSettings.DoWindowContents(inRect);
        }
    }

    public class LuxandraModSettings : ModSettings
    {
        /// <summary>
        /// Enable debug logging for the mod
        /// </summary>
        public static bool enableLogging = false;
        /// <summary>
        /// Multiplier for the event conversion threshold
        /// </summary>
        public static float eventRerollThresholdMultiplier = 1.0f;
        /// <summary>
        /// Event conversion condition - 0: Disabled, 1: Only Negative, 2: All Events
        /// </summary>
        public static int eventRerollCondition = 1;
        /// <summary>
        /// Determine how often the weekly cycle fires... is it even still weekly though?
        /// </summary>
        public static int weeklyCycleDays = 7;
        /// <summary>
        /// Enable the notification of having pleased Luxandra's kink
        /// </summary>
        public static bool enablePleasedNotification = true;
        /// <summary>
        /// Enable the childbirth appraisal gimmick
        /// </summary>
        public static bool enableChildbirthAppraisal = true;
        /// <summary>
        /// Enable the childbirth appraisal gimmick for animals under colony control
        /// </summary>
        public static bool trackChildbirthAppraisalForAnimals = false;
        /// <summary>
        /// Allows events that would spawn cum filth to spawn the full intended amount
        /// </summary>
        public static bool allowFullCumStains = true;
        /// <summary>
        /// Allows Luxandra's Monument to change shape to match her current kink
        /// </summary>
        public static bool enableMonumentKinkShift = true;
        /// <summary>
        /// Removes the romance restrictions for close relatives
        /// </summary>
        public static bool removeRomanceRestrictions = false;

        private static Vector2 scrollPosition = Vector2.zero;
        private static float dynamicContentHeight = 100f;

        public static void DoWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.maxOneColumn = true;
            Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 50f);

            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, dynamicContentHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);

            listingStandard.Begin(viewRect);

            // Debug logging toggle
            if (Prefs.DevMode)
            {
                listingStandard.CheckboxLabeled(
                    "Luxandra_Setting_EnableLogging_Label".Translate(),
                    ref enableLogging,
                    "Luxandra_Setting_EnableLogging_Desc".Translate()
                );
                listingStandard.Gap(12f);
            }

            // Reset
            if (listingStandard.ButtonText("Luxandra_Setting_Reset_Button".Translate()))
            {
                enableLogging = false;
                eventRerollCondition = 1;
                eventRerollThresholdMultiplier = 1.0f;
                weeklyCycleDays = 7;
                enablePleasedNotification = true;
                enableChildbirthAppraisal = true;
                trackChildbirthAppraisalForAnimals = false;
                allowFullCumStains = true;
                enableMonumentKinkShift = true;
                removeRomanceRestrictions = false;

                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            listingStandard.Gap(16f);

            // Satisfaction notification toggle
            listingStandard.CheckboxLabeled(
                "Luxandra_Setting_ShowNotifications_Label".Translate(),
                ref enablePleasedNotification,
                "Luxandra_Setting_ShowNotifications_Desc".Translate()
            );
            listingStandard.Gap(16f);

            // Enable Childbirth Appraisal
            listingStandard.CheckboxLabeled(
                "Luxandra_Setting_ChildbirthAppraisal_Label".Translate(),
                ref enableChildbirthAppraisal,
                "Luxandra_Setting_ChildbirthAppraisal_Desc".Translate()
            );
            listingStandard.Gap(16f);

            if (enableChildbirthAppraisal)
            {
                // Enable Childbirth Appraisal for colony animals
                listingStandard.CheckboxLabeled(
                    "Luxandra_Setting_AnimalAppraisal_Label".Translate(),
                    ref trackChildbirthAppraisalForAnimals,
                    "Luxandra_Setting_AnimalAppraisal_Desc".Translate()
                );
                listingStandard.Gap(16f);
            }

            // Enable Full Cumstains
            listingStandard.CheckboxLabeled(
                "Luxandra_Setting_AllowCumStains_Label".Translate(),
                ref allowFullCumStains,
                "Luxandra_Setting_AllowCumStains_Desc".Translate()
            );
            listingStandard.Gap(16f);

            // Enable Monument's Kink Shift
            listingStandard.CheckboxLabeled(
                "Luxandra_Setting_MonumentKinkShift_Label".Translate(),
                ref enableMonumentKinkShift,
                "Luxandra_Setting_MonumentKinkShift_Desc".Translate()
            );
            listingStandard.Gap(16f);

            // Enable Romance Patches
            listingStandard.CheckboxLabeled(
                "Luxandra_Setting_RemoveRomanceRestrictions_Label".Translate(),
                ref removeRomanceRestrictions,
                "Luxandra_Setting_RemoveRomanceRestrictions_Desc".Translate()
            );
            listingStandard.Gap(16f);

            // Weekly interval modifier
            listingStandard.Label("Luxandra_Setting_CycleInterval_Label".Translate(weeklyCycleDays));
            weeklyCycleDays = Mathf.RoundToInt(listingStandard.Slider(weeklyCycleDays, 1f, 15f));

            listingStandard.Label("Luxandra_Setting_CycleInterval_Desc".Translate());
            listingStandard.Gap(24f);

            if (Current.Game != null)
            {
                var cycleComponent = Current.Game.GetComponent<GameComponent_LuxandraCyclicEvents>();
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
                        listingStandard.Label("Luxandra_Setting_Countdown_Dev".Translate(daysRemaining.ToString("F1")));
                        listingStandard.Gap(24f);
                    }
                }
            }

            // Sexual reroll mode
            listingStandard.Gap(12f);
            float labelWidth = 200f;
            float buttonWidth = 200f;

            // Translatable options array
            string[] conditionOptions = {
        "Luxandra_Setting_Reroll_Opt_Disabled".Translate(),
        "Luxandra_Setting_Reroll_Opt_Negative".Translate(),
        "Luxandra_Setting_Reroll_Opt_All".Translate()
    };

            DrawSettingRowWithButon(
                listingStandard,
                "Luxandra_Setting_RerollCondition_Label".Translate(),
                conditionOptions,
                eventRerollCondition,
                (val) => eventRerollCondition = val,
                labelWidth, buttonWidth,
                "Luxandra_Setting_RerollCondition_Desc".Translate()
            );

            bool showRerollSettings = eventRerollCondition != 0;

            if (showRerollSettings)
            {
                // Sexual reroll threshold
                listingStandard.Label("Luxandra_Setting_ThresholdMultiplier_Label".Translate(eventRerollThresholdMultiplier.ToString("0.0")));

                eventRerollThresholdMultiplier = listingStandard.Slider(eventRerollThresholdMultiplier, 0.1f, 2.0f);

                listingStandard.Label("Luxandra_Setting_ThresholdMultiplier_Desc1".Translate());
                listingStandard.Gap(6f);
                listingStandard.Label("Luxandra_Setting_ThresholdMultiplier_Desc2".Translate());
                listingStandard.Gap(24f);

                if (Prefs.DevMode)
                {
                    if (Current.Game != null && Find.CurrentMap != null && Find.CurrentMap.mapPawns != null)
                    {
                        try
                        {
                            int adultColonists = Find.CurrentMap.mapPawns.FreeColonistsSpawned.Count(p => LuxandraUtilities.IsAdult(p));
                            int adultSlaves = Find.CurrentMap.mapPawns.SlavesOfColonySpawned.Count(p => LuxandraUtilities.IsAdult(p));
                            int baseThreshold = GameComponent_LuxandraLust.CalculateSexualRerollThreshold();
                            int finalThreshold = System.Math.Max(1, Mathf.RoundToInt(baseThreshold));

                            listingStandard.Label("Luxandra_Setting_Preview_Header".Translate());
                            listingStandard.Label("Luxandra_Setting_Preview_Colonists".Translate(adultColonists));
                            listingStandard.Label("Luxandra_Setting_Preview_Slaves".Translate(adultSlaves));
                            listingStandard.Label("Luxandra_Setting_Preview_BaseMetric".Translate(baseThreshold));
                            listingStandard.Label("Luxandra_Setting_Preview_FinalTarget".Translate(finalThreshold));
                        }
                        catch (System.Exception ex)
                        {
                            listingStandard.Label("Luxandra_Setting_Preview_Error".Translate(ex.Message));
                        }
                    }
                    else
                    {
                        listingStandard.Label("Luxandra_Setting_Preview_NoGame".Translate());
                    }
                }
            }

            float totalHeightUsed = listingStandard.CurHeight;

            listingStandard.End();
            Widgets.EndScrollView();
            dynamicContentHeight = totalHeightUsed + 20f;
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

            Scribe_Values.Look(ref enableLogging, "enableLogging", false);
            Scribe_Values.Look(ref enablePleasedNotification, "enablePleasedNotification", true);
            Scribe_Values.Look(ref eventRerollCondition, "eventRerollCondition", 2);
            Scribe_Values.Look(ref eventRerollThresholdMultiplier, "eventRerollThresholdMultiplier", 1.0f);
            Scribe_Values.Look(ref weeklyCycleDays, "weeklyCycleDays", 7);
            Scribe_Values.Look(ref enableChildbirthAppraisal, "enableChildbirthAppraisal", true);
            Scribe_Values.Look(ref trackChildbirthAppraisalForAnimals, "trackChildbirthAppraisalForAnimals", false);
            Scribe_Values.Look(ref allowFullCumStains, "allowFullCumStains", true);
            Scribe_Values.Look(ref removeRomanceRestrictions, "removeRomanceRestrictions", false);
        }
    }
}