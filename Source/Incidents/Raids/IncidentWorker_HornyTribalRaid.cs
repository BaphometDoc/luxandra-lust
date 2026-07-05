using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    // Horny tribal raid. They're horny, and so is the player now
    public class IncidentWorker_HornyTribalRaid : IncidentWorker_RaidEnemy
    {

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_HornyTribalRaid.defName))
            {
                return false;
            }

            Map map = (Map)parms.target;
            // Need at least 1 humanlike adult pawn to even try
            return map.mapPawns.FreeColonistsAndPrisonersSpawned.Any(p => p.RaceProps.Humanlike && LuxandraUtilities.IsAdult(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            Faction deviantFaction = Find.FactionManager.FirstFactionOfDef(LuxandraFactionDefOf.Luxandra_DeviantHordeFaction);

            // Failsafe 1: Fallback to a hostile tribal faction
            if (deviantFaction == null)
            {
                Log.Warning("[Luxandra Debug] Carnal Deviant faction not found, searching for hostile tribal fallback.");

                deviantFaction = Find.FactionManager.AllFactionsListForReading
                    .FirstOrDefault(f => !f.IsPlayer && f.HostileTo(Faction.OfPlayer) && f.def.techLevel < TechLevel.Industrial);
            }

            //  Failsafe 2: Abort if absolutely no hostile tribal faction exists
            if (deviantFaction == null)
            {
                Log.Error("[Luxandra Debug] No Carnal Deviant faction OR hostile tribal faction found. Aborting Carnal Deviant raid.");
                return false;
            }

            RaidStrategyDef raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            RaidStrategyDef customStrategy = DefDatabase<RaidStrategyDef>.GetNamed("Luxandra_RapeAndPillageAssault", false);
            if (customStrategy != null)
                raidStrategy = customStrategy;

            var points = StorytellerUtility.DefaultThreatPointsNow(map);

            IncidentParms raidParms = new IncidentParms
            {
                target = map,
                points = points,
                faction = deviantFaction,
                forced = true,
                canKidnap = false,
                canSteal = false,
                pawnGroupKind = PawnGroupKindDefOf.Combat,
                raidStrategy = raidStrategy,
                raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkInGroups,
                customLetterLabel = "Raid (Carnal Deviants)",
                customLetterText = "A horde of primal humans are entering the area from the map edge! Their war cry clarified their intentions: they are not interested so much in your loots or lives, but rather in all of your colonists' orifices. Brace yourself!",
                customLetterDef = LetterDefOf.ThreatBig,
                sendLetter = true
            };

            bool raidSuccessful = base.TryExecuteWorker(raidParms);

            if (!raidSuccessful)
            {
                LuxandraDebugActions.DebugLogMessage("Failed to generate a Horny raid for the faction " + parms.faction.Name);
                return false;
            }

            // All player controlled pawns become horny
            List<Pawn> playerControlledPawns = map.mapPawns.FreeColonistsSpawned
                .Concat(map.mapPawns.SlavesOfColonySpawned)
                .ToList();

            ThoughtDef customMoodlet = ThoughtDef.Named("Luxandra_WarCry_Panic");

            foreach (Pawn pawn in playerControlledPawns)
            {
                if (pawn == null || pawn.Dead) continue;

                if (pawn.needs != null)
                {
                    var sexNeed = LuxandraUtilities.GetSexNeed(pawn);
                    if (sexNeed != null)
                    {
                        sexNeed.CurLevel = 0f;
                    }
                }

                if (customMoodlet != null && pawn.needs?.mood?.thoughts?.memories != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(customMoodlet);
                }
            }

            return true;
        }
    }
}