using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LuxandraLust
{
    public class LuxandraEventMod : Mod
    {
        public LuxandraEventMod(ModContentPack content) : base(content)
        {
            // Forces RimWorld to read the XML file and populate the static variables at boot
            GetSettings<LuxandraEventSettings>();
        }

        public override string SettingsCategory()
        {
            return "Luxandra Lust - Events";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Forward directly to the static drawing method
            LuxandraEventSettings.DoWindowContents(inRect);
        }
    }

    public class LuxandraEventSettings : ModSettings
    {
        /// <summary>
        /// Whether to enable debug logging for the mod
        /// </summary>
        public static List<string> disabledEventNames;

        private static Vector2 scrollPosition = Vector2.zero;


        public static void DoWindowContents(Rect inRect)
        {
            // --- SECTION 1: HEADER ---
            Listing_Standard headerListing = new Listing_Standard();
            headerListing.Begin(inRect);
            headerListing.Label("Toggle Luxandra's Active Storyteller Events:");
            headerListing.Gap(10f);
            headerListing.End();

            // --- SECTION 2: THE SCROLL VIEW CONTAINER ---
            // Position the scroll window right below the header text and leave room for buttons at the bottom
            Rect outRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 110f);

            // Calculate the total internal height required for all your checkboxes (approx 26f per row)
            float totalViewHeight = LuxandraDefsCollections.AllIncidents.Count * 26f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, totalViewHeight);

            // Begin the scroll area window context
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            Listing_Standard scrollListing = new Listing_Standard();
            scrollListing.Begin(viewRect);

            //foreach (var eventWrapper in LuxandraDefsCollections.AllIncidents)
            //{
            //    if (eventWrapper.IncidentDef == null) continue;

            //    string defName = eventWrapper.IncidentDef.defName;
            //    string label = eventWrapper.IncidentDef.label.CapitalizeFirst();

            //    bool isEnabled = !disabledEventNames.Contains(defName);
            //    bool previousState = isEnabled;

            //    // Render the actual checkbox inside the scroll viewport
            //    scrollListing.CheckboxLabeled($"{label}", ref isEnabled, eventWrapper.IncidentDef.description);

            //    if (isEnabled != previousState)
            //    {
            //        if (isEnabled)
            //        {
            //            disabledEventNames.Remove(defName);
            //        }
            //        else
            //        {
            //            disabledEventNames.Add(defName);
            //        }
            //    }
            //}
            foreach (var eventWrapper in LuxandraDefsCollections.AllIncidents)
            {
                if (eventWrapper.IncidentDef == null) continue;

                string defName = eventWrapper.IncidentDef.defName;
                string label = eventWrapper.IncidentDef.label.CapitalizeFirst();

                bool isEnabled = !disabledEventNames.Contains(defName);
                bool previousState = isEnabled;

                // 1. Grab a clean rectangle for the entire current row line from the listing
                // 24f is the perfect height for standard text and checkboxes
                Rect rowRect = scrollListing.GetRect(24f);

                // 2. Carve out a tiny box on the absolute left for the checkmark icon
                Rect checkRect = new Rect(rowRect.x, rowRect.y, 24f, 24f);

                // 3. Carve out the remaining space to the right of the checkmark for your text label
                // We add +6f of padding so the text isn't stuck right against the box edge
                Rect textRect = new Rect(rowRect.x + 30f, rowRect.y, rowRect.width - 30f, 24f);

                // 4. Draw the actual clickable check/cross widget on the left
                Widgets.Checkbox(checkRect.position, ref isEnabled, 24f);

                // 5. Draw the text label immediately next to it
                Widgets.Label(textRect, $"{label} ({defName})");

                // Optional: Add a tool-tip mouseover description box across the whole row area
                if (Mouse.IsOver(rowRect) && !eventWrapper.IncidentDef.description.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(rowRect, eventWrapper.IncidentDef.description);
                }

                // 6. State evaluation remains identical
                if (isEnabled != previousState)
                {
                    if (isEnabled)
                    {
                        disabledEventNames.Remove(defName);
                    }
                    else
                    {
                        disabledEventNames.Add(defName);
                    }
                }
            }

            scrollListing.End();
            Widgets.EndScrollView();

            Rect resetRect = new Rect(inRect.x, inRect.height - 45f, 140f, 30f);
            if (Widgets.ButtonText(resetRect, "Reset Defaults"))
            {
                disabledEventNames.Clear();
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // This tells RimWorld how to read/write the list to the configuration XML file
            Scribe_Collections.Look(ref disabledEventNames, "disabledEventNames", LookMode.Value);

            // Safety check: ensure it isn't null on a brand-new save/game boot
            if (disabledEventNames == null)
            {
                disabledEventNames = new List<string>();
            }
        }
    }
}