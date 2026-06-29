using RimWorld;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    // These totally arent basically RJW nymphs
    // No, really, this used to be a Nymph horde, but RJW just fucks (he he) anything that has "nymph" in their name so... deviants it is.

    // NOTE: with the rapist raider AI actually working, this is kinda obsolete, should probably just convert it to something more interesting.
    public class IncidentWorker_DeviantHordeRaid : IncidentWorker_RaidEnemy
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_DeviantHordeRaid.defName))
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

            LuxandraDebugActions.DebugLogMessage("Attempting to generate a Carnal Deviant raid.");
            Faction deviantFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("Luxandra_DeviantHordeFaction", false));

            // Failsafe 1: Fallback to a hostile tribal faction
            if (deviantFaction == null)
            {
                Log.Warning("[LuxandraLust] Carnal Deviantfaction not found, searching for hostile tribal fallback.");

                deviantFaction = Find.FactionManager.AllFactionsListForReading
                    .FirstOrDefault(f => !f.IsPlayer && f.HostileTo(Faction.OfPlayer) && f.def.techLevel == TechLevel.Neolithic);
            }

            //  Failsafe 2: Abort if absolutely no hostile tribal faction exists
            if (deviantFaction == null)
            {
                Log.Error("[LuxandraLust] No Carnal Deviant faction OR hostile tribal faction found. Aborting Carnal Deviant raid.");
                return false;
            }

            parms.faction = deviantFaction;

            if (parms.points <= 0f)
            {
                parms.points = StorytellerUtility.DefaultThreatPointsNow(map);
            }

            PawnKindDef deviantKind = DefDatabase<PawnKindDef>.GetNamed("Luxandra_CarnalDeviantStriker", false);
            if (deviantKind == null)
            {
                Log.Error("[LuxandraLust] Missing Luxandra_CarnalDeviantStriker PawnKindDef!");
                return false;
            }

            var raidStrategy = DefDatabase<RaidStrategyDef>.GetNamed("Luxandra_DeviantHordeAssault", false);
            if (raidStrategy == null)
            {
                Log.Warning("[LuxandraLust] Raid strategy not found, defaulting to immediate attack.");
                raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            }

            IncidentParms raidParms = new IncidentParms
            {
                target = map,
                points = parms.points,
                faction = deviantFaction,
                forced = true,
                pawnGroupKind = PawnGroupKindDefOf.Combat,
                raidStrategy = raidStrategy,
                raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkInGroups,
                customLetterLabel = "Raid (Deviant Horde)",
                customLetterText = "A horde of completely naked primal humans are entering the area from the map edge! Driven by a carnal frenzy, they are advancing directly onto your colony. Run, or be ready for what is to come...",
                customLetterDef = LetterDefOf.ThreatBig,
                sendLetter = true
            };

            return IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
        }
    }
}