using RimWorld;
using rjw;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class GameComponent_WeeklyEventCycle : GameComponent
    {
        private int TicksPerCycle => LuxandraModSettings.weeklyCycleDays > 0 ? (LuxandraModSettings.weeklyCycleDays * 60000) : 420000;

        public int ticksUntilEvent = 420000;

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
                    DebugActions_Luxandra.DebugLogMessage($"Loaded save timer was higher than global settings. Dropped ticksUntilEvent down to {ticksUntilEvent} ticks.");
                }
            }

            ticksUntilEvent--;

            if (ticksUntilEvent <= 0)
            {
                TriggerWeeklyEvent();
                ticksUntilEvent = TicksPerCycle; // Reset the 7-day countdown clock
            }
        }

        private void TriggerWeeklyEvent()
        {
            DebugActions_Luxandra.DebugLogMessage("Weekly event reached! Attempting to launch a sexual event...");
            Map targetMap = Find.CurrentMap;
            if (targetMap == null) return;

            // How horny have we been this week?
            float averageSexNeed = GetAverageColonySexNeed(targetMap);
            DebugActions_Luxandra.DebugLogMessage($"Colony Average Sex Need: {averageSexNeed * 100f}%");

            List<IncidentDef> totalPool = LuxandraEventPool.GetSexRelatedIncidents();
            totalPool.RemoveAll(e => e == null); // Clean up potentially bugged events, i'm paranoic i know
            if (totalPool.Count == 0) return;

            List<IncidentDef> filteredPool = new List<IncidentDef>();

            string letterText = "";
            string letterLabel = "Luxandra's Cycle: ";
            ThoughtDef moodletToApply = null;

            if (averageSexNeed > 0.75f)
            {
                filteredPool = totalPool.Where(e => e.letterDef?.defName == "PositiveEvent").ToList();

                // Flavor text and moodlet for high satisfaction
                letterLabel = "Pleasure & Favor";
                letterText = "Luxandra smiles upon your settlement. Pleased by the overwhelming passion and satisfaction echoing from your colonists, she rewards them with a wave of vital energy...\n\n";
                moodletToApply = DefDatabase<ThoughtDef>.GetNamed("Luxandra_SatisfiedCycle", errorOnFail: false);
            }
            else if (averageSexNeed < 0.25f)
            {
                filteredPool = totalPool.Where(e => e.letterDef?.defName == "NegativeEvent").ToList();

                // Flavor text and moodlet for low satisfaction
                letterLabel = "Boredom & Spite";
                letterText = "Luxandra grows bored of your colony's lacking energy. Irritated by the absolute lack of passion and growing frustration, she decides that if you will not embrace your deepest desires willingly, you shall do it forcefully...\n\n";
                moodletToApply = DefDatabase<ThoughtDef>.GetNamed("Luxandra_FrustratedCycle", errorOnFail: false);
            }
            else
            {
                filteredPool = totalPool;

                // Flavor text for a neutral state
                letterLabel = "Altered Alignment";
                letterText = "The weekly cosmic alignment shifts. Luxandra glances down at your settlement, idly spinning the wheel of fate to disrupt your colonists' mundane routines...\n\n";
            }

            if (filteredPool.Count == 0)
            {
                DebugActions_Luxandra.DebugLogMessage("No event found in the selected pool, swapping to global sexual pool.");
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

                foreach (Pawn pawn in eligiblePawns)
                {
                    if (pawn.needs?.mood?.thoughts?.memories != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(moodletToApply);
                    }
                }
            }

            // Queue up the incident
            DebugActions_Luxandra.DebugLogMessage($"Event chosen: {chosenIncident.defName}");
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(chosenIncident.category, targetMap);
            parms.forced = true;

            Find.Storyteller.incidentQueue.Add(chosenIncident, Find.TickManager.TicksGame, parms);
        }

        public static float GetAverageColonySexNeed(Map map)
        {
            if (map == null) return 0.5f; // Safe fallback

            var eligiblePawns = map.mapPawns.AllPawnsSpawned.Where(p =>
                p.RaceProps != null && p.RaceProps.Humanlike && !p.Dead &&
                (p.IsColonist || p.IsSlave) &&
                p.DevelopmentalStage == DevelopmentalStage.Adult
            );

            float totalSexNeed = 0f;
            int countWithNeed = 0;

            foreach (Pawn pawn in eligiblePawns)
            {
                var sexNeed = pawn.needs.TryGetNeed<Need_Sex>();
                if (sexNeed != null)
                {
                    totalSexNeed += sexNeed.CurLevelPercentage;
                    countWithNeed++;
                }
            }

            // Divide by countwithneed rather than actual total of pawns to avoid pawns with no sex need (es, Androids)
            return countWithNeed > 0 ? (totalSexNeed / countWithNeed) : 0.5f;
        }


        public override void ExposeData()
        {
            base.ExposeData();
            // Saves the remaining ticks directly into the player's .rws save files
            Scribe_Values.Look(ref ticksUntilEvent, "ticksUntilEvent", TicksPerCycle);
        }
    }
}