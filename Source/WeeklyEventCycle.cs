using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class GameComponent_WeeklyEventCycle : GameComponent
    {
        private int TicksPerCycle => LuxandraModSettings.weeklyCycleDays > 0 ? (LuxandraModSettings.weeklyCycleDays * 60000) : 420000;

        public int ticksUntilEvent = 420000;

        public int ticksUntilKinkChange = 3600;

        public GameComponent_WeeklyEventCycle(Game game)
        {
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (!LuxandraStorytellerCheck.IsActive())
                return;

            // Fix the timer if it was edited outside of the save file
            if (ticksUntilEvent > TicksPerCycle)
            {
                ticksUntilEvent = TicksPerCycle;

                if (LuxandraModSettings.enableLogging)
                {
                    LuxandraDebugActions.DebugLogMessage($"Loaded save timer was higher than global settings. Dropped ticksUntilEvent down to {ticksUntilEvent} ticks.");
                }
            }

            ticksUntilKinkChange--;

            if (ticksUntilKinkChange <= 0)
            {
                TriggerKinkShift(); // Change Luxanna's kink, then reset the countdown
            }

            ticksUntilEvent--;

            if (ticksUntilEvent <= 0)
            {
                TriggerWeeklyEvent();
                ticksUntilEvent = TicksPerCycle; // Reset the 7-day countdown clock
            }
        }

        private void TriggerKinkShift()
        {
            // Roll a new random phase
            RerollKink();

            // Roll a random day counter between 1 and 7 days
            int randomDays = Rand.RangeInclusive(1, 7);

            // Convert days to internal ticks (1 day = 60,000 ticks)
            ticksUntilKinkChange = randomDays * GenDate.TicksPerDay;

            if (LuxandraModSettings.enableLogging)
            {
                LuxandraDebugActions.DebugLogMessage($"Storyteller shifted phase to {CurrentKink}. Next shift scheduled in {randomDays} days ({ticksUntilKinkChange} ticks).");
            }
        }

        private void TriggerWeeklyEvent()
        {
            LuxandraDebugActions.DebugLogMessage("Weekly event reached! Attempting to launch a sexual event...");
            Map targetMap = Find.CurrentMap;
            if (targetMap == null) return;

            // How horny have we been this week?
            float averageSexNeed = LuxandraUtilities.GetAverageColonySexNeed(targetMap);
            LuxandraDebugActions.DebugLogMessage($"Colony Average Sex Need: {averageSexNeed * 100f}%");

            List<IncidentDef> totalPool = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.AllIncidents);
            totalPool.RemoveAll(e => e == null); // Clean up potentially bugged events, i'm paranoic i know
            if (totalPool.Count == 0) return;

            List<IncidentDef> filteredPool = new List<IncidentDef>();

            string letterText = "";
            string letterLabel = "Luxandra's Cycle: ";
            ThoughtDef moodletToApply = null;

            if (averageSexNeed > 0.75f)
            {
                filteredPool = totalPool.Where(e => e.letterDef == LetterDefOf.PositiveEvent).ToList();

                // Flavor text and moodlet for high satisfaction
                letterLabel += "Pleasure & Favor";
                letterText = "Luxandra smiles upon your settlement. Pleased by the overwhelming passion and satisfaction echoing from your colonists, she rewards them with a wave of vital energy...\n\n";
                moodletToApply = DefDatabase<ThoughtDef>.GetNamed("Luxandra_SatisfiedCycle", errorOnFail: false);
            }
            else if (averageSexNeed < 0.25f)
            {
                filteredPool = totalPool.Where(e => e.letterDef == LetterDefOf.NegativeEvent || e.letterDef == LetterDefOf.ThreatBig).ToList();

                // Flavor text and moodlet for low satisfaction
                letterLabel += "Boredom & Spite";
                letterText = "Luxandra grows bored of your colony's lacking energy. Irritated by the absolute lack of passion and growing frustration, she decides that if you will not embrace your deepest desires willingly, you shall do it forcefully...\n\n";
                moodletToApply = DefDatabase<ThoughtDef>.GetNamed("Luxandra_FrustratedCycle", errorOnFail: false);
            }
            else
            {
                filteredPool = totalPool;

                // Flavor text for a neutral state
                letterLabel += "Altered Alignment";
                letterText = "Luxandra glances down at your settlement, idly spinning the wheel of fate to disrupt your colonists' mundane routines...\n\n";
            }

            if (filteredPool.Count == 0)
            {
                LuxandraDebugActions.DebugLogMessage("No event found in the selected pool, swapping to global sexual pool.");
                filteredPool = totalPool;
            }
            IncidentDef chosenIncident = filteredPool.RandomElement();

            // Send the Storyteller's personal announcement letter first
            LetterDef storytellerLetterDef = moodletToApply != null
                ? (averageSexNeed > 0.75f ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent)
                : LetterDefOf.NeutralEvent;

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, storytellerLetterDef);

            // Apply the mood lets to the current living colonists/slaves maps (even children, they should be proud of their parents... or ashamed)
            if (moodletToApply != null)
            {
                var eligiblePawns = targetMap.mapPawns.AllPawnsSpawned.Where(p =>
                    p.RaceProps != null && p.RaceProps.Humanlike && !p.Dead &&
                    (p.IsColonist || p.IsSlave)
                );

                LuxandraDebugActions.DebugLogMessage($"Cycle completed. Moodlet to apply: ${moodletToApply.defName}");
                foreach (Pawn pawn in eligiblePawns)
                {
                    if (pawn.needs?.mood?.thoughts?.memories != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(moodletToApply);
                    }
                }
            }

            // Queue up the incident
            LuxandraDebugActions.DebugLogMessage($"Event chosen: {chosenIncident.defName}");
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(chosenIncident.category, targetMap);
            parms.forced = true;

            Find.Storyteller.incidentQueue.Add(chosenIncident, Find.TickManager.TicksGame, parms);
        }

        public void ForceImmediateKinkShift()
        {
            this.ticksUntilKinkChange = 1;
        }


        public override void ExposeData()
        {
            base.ExposeData();
            // Saves the remaining ticks directly into the player's .rws save files
            Scribe_Values.Look(ref ticksUntilEvent, "ticksUntilEvent", TicksPerCycle);
            Scribe_Values.Look(ref ticksUntilKinkChange, "Luxandra_TicksUntilKinkChange", 3600);
        }
    }
}