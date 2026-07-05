using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class GameComponent_LuxandraCyclicEvents : GameComponent
    {
        private int TicksPerCycle => LuxandraModSettings.weeklyCycleDays > 0 ? (LuxandraModSettings.weeklyCycleDays * 60000) : 420000;

        public int ticksUntilEvent = 420000;

        public int ticksUntilKinkChange = 3600;

        public static GameComponent_LuxandraCyclicEvents Instance
        {
            get
            {
                if (Current.Game == null) return null;
                return Current.Game.GetComponent<GameComponent_LuxandraCyclicEvents>();
            }
        }

        public GameComponent_LuxandraCyclicEvents(Game game)
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
                LuxandraKinkTracker.TriggerKinkShift(); // Change Luxanna's kink, then reset the countdown
            }

            ticksUntilEvent--;

            if (ticksUntilEvent <= 0)
            {
                TriggerWeeklyEvent();
                ticksUntilEvent = TicksPerCycle; // Reset the 7-day countdown clock
            }
        }

        private bool CheckIfEventIsAvailable(IncidentDef incident, IncidentParms parms)
        {
            try
            {
                bool canFire = incident.Worker.CanFireNow(parms);
                bool isEnabled = LuxandraEventCheck.IsEnabled(incident.defName);
                return canFire && isEnabled;
            }
            catch
            {
                Log.Warning($"[Luxandra Debug] Error evaluating the event ${incident.defName} for the weekly cycle");
                return false;
            }
        }

        // I say "weekly" but that's the default setting, this can be edited
        private void TriggerWeeklyEvent()
        {
            LuxandraDebugActions.DebugLogMessage("Weekly event triggered! Attempting to launch a sexual event...");
            Map targetMap = Find.CurrentMap;
            if (targetMap == null) return;

            float threatPoints = StorytellerUtility.DefaultThreatPointsNow(targetMap) * 0.85f;
            IncidentParms parms = new IncidentParms
            {
                target = targetMap,
                points = threatPoints,
                forced = true,
            };

            // TODO: Scale events based on the recent sexual activites

            // How horny have we been this week?
            float averageSexNeed = LuxandraUtilities.GetAverageColonySexNeed(targetMap);
            LuxandraDebugActions.DebugLogMessage($"Colony Average Sex Need: {averageSexNeed * 100f}%");

            List<IncidentDef> completeEventPool = LuxandraUtilities.ExtractIncidentsFromCollection(LuxandraDefsCollections.AllIncidents);
            completeEventPool.RemoveAll(e => e == null); // Clean up potentially bugged events, i'm paranoic i know

            // Doublecheck the settings as I can't edit the CanFireNow of other mod events
            var totalPool = completeEventPool.Where(e => CheckIfEventIsAvailable(e, parms)).ToList();
            if (totalPool.Count == 0)
            {
                LuxandraDebugActions.DebugLogMessage($"There were no valid events in the event pool. Skipping...");
                return;
            }
            LuxandraDebugActions.DebugLogMessage($"Event pool contains {totalPool.Count} elements.");

            List<IncidentDef> filteredPool = totalPool;

            string letterText = "";
            string letterLabel = "Luxandra's Cycle: ";
            ThoughtDef moodletToApply = null;

            if (averageSexNeed > 0.75f)
            {
                LuxandraDebugActions.DebugLogMessage("Happy mood: selecting positive event.");
                filteredPool = totalPool.Where(e => e.letterDef == LetterDefOf.PositiveEvent).ToList();

                // Flavor text and moodlet for high satisfaction
                letterLabel += "Pleasure & Favor";
                letterText = "Luxandra smiles upon your settlement. Pleased by the overwhelming passion and satisfaction echoing from your colonists, she rewards them with a wave of vital energy...\n\n";
                moodletToApply = DefDatabase<ThoughtDef>.GetNamed("Luxandra_SatisfiedCycle", errorOnFail: false);
            }
            else if (averageSexNeed < 0.25f)
            {
                LuxandraDebugActions.DebugLogMessage("Negative mood: selecting negative event.");
                filteredPool = totalPool.Where(e => e.letterDef == LetterDefOf.NegativeEvent || e.letterDef == LetterDefOf.ThreatBig).ToList();

                // Flavor text and moodlet for low satisfaction
                letterLabel += "Boredom & Spite";
                letterText = "Luxandra grows bored of your colony's lacking energy. Irritated by the absolute lack of passion and growing frustration, she decides that if you will not embrace your deepest desires willingly, you shall do it forcefully...\n\n";
                moodletToApply = DefDatabase<ThoughtDef>.GetNamed("Luxandra_FrustratedCycle", errorOnFail: false);
            }
            else
            {
                LuxandraDebugActions.DebugLogMessage("Neutral mood: selecting random event.");
                // Flavor text for a neutral state
                letterLabel += "Altered Alignment";
                letterText = "Luxandra glances down at your settlement, idly spinning the wheel of fate to disrupt your colonists' mundane routines...\n\n";
            }

            // Failsafe
            if (filteredPool == null || filteredPool.Count == 0)
            {
                LuxandraDebugActions.DebugLogMessage("No event found in the selected pool, swapping to global sexual pool.");
                filteredPool = totalPool;
            }

            IncidentDef chosenIncident = filteredPool.RandomElement();
            LuxandraDebugActions.DebugLogMessage($"Event selected {chosenIncident.defName}");

            // Send the Storyteller's personal announcement letter first
            LetterDef storytellerLetterDef = moodletToApply != null
                ? (averageSexNeed > 0.75f ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent)
                : LetterDefOf.NeutralEvent;


            Find.LetterStack.ReceiveLetter(letterLabel, letterText, storytellerLetterDef);
            // Apply the mood lets to the current living colonists/slaves maps
            if (moodletToApply != null)
            {
                LuxandraDebugActions.DebugLogMessage($"Cycle completed. Moodlet to apply: {moodletToApply.defName}");
                LuxandraDebugActions.DebugLogMessage($"Applying moodlets...");
                // Since I changed the flavour of the moodlet, probably better to keep children off this one
                var eligiblePawns = targetMap.mapPawns.AllPawnsSpawned.Where(p =>
                    p.RaceProps != null && p.RaceProps.Humanlike && !p.Dead &&
                    (p.IsColonist || p.IsSlave) && LuxandraUtilities.IsAdult(p)
                );

                foreach (Pawn pawn in eligiblePawns)
                {
                    if (pawn.needs?.mood?.thoughts?.memories != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(moodletToApply);
                    }
                }
            }
            else
                LuxandraDebugActions.DebugLogMessage($"Cycle completed with neutral result. No moodlet applied.");

            // Queue up the incident
            LuxandraDebugActions.DebugLogMessage($"Event chosen: {chosenIncident.defName}");
            IncidentParms finalParms = StorytellerUtility.DefaultParmsNow(chosenIncident.category, targetMap);


            finalParms.forced = true;

            Find.Storyteller.incidentQueue.Add(chosenIncident, Find.TickManager.TicksGame, finalParms);
        }

        public void ForceImmediateKinkShift()
        {
            ticksUntilKinkChange = 1;
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