using RimWorld;
using System.Linq;
using Verse;

namespace LuxandraLust
{
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

            GameConditionDef whiteRainDef = DefDatabase<GameConditionDef>.GetNamed("Luxandra_WhiteRain", false);
            // Check if the white rain is already there
            if (map.gameConditionManager.ConditionIsActive(whiteRainDef))
                return false;

            // Need at least 1 humanlike adult pawn to even try
            return map.mapPawns.FreeColonistsAndPrisonersSpawned.Any(p => p.RaceProps.Humanlike && LuxandraUtilities.IsAdult(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;

            // Failsafe cause this apparently breaks if it fires via devmode
            if (map == null)
                return false;

            LuxandraDebugActions.DebugLogMessage("Attempting to generate a Deviant Horde raid.");
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

            parms.faction = deviantFaction;

            if (parms.points <= 0f)
            {
                parms.points = StorytellerUtility.DefaultThreatPointsNow(map) * 2 / 3;
            }

            int durationTicks = 30000;

            // PERFORMANCE LIMIT - Trying to not make the player's PC explode
            // Instead amplify the rain duration to be more annoying
            if (parms.points > 1200f)
            {
                parms.points = 1200f;
                durationTicks = 60000;
            }

            IncidentParms raidParms = new IncidentParms
            {
                target = map,
                points = parms.points,
                faction = deviantFaction,
                forced = true,
                canKidnap = false,
                canSteal = false,
                canTimeoutOrFlee = false,
                pawnGroupKind = PawnGroupKindDefOf.Combat,
                raidStrategy = RaidStrategyDefOf.ImmediateAttack,
                raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkInGroups,
                customLetterLabel = "Raid (Deviant Horde)",
                customLetterText = "A horde of primal humans are entering the area from the map edge! They arrived chanting some profane rite, and you hear the clouds rumbling. Yet it is not rain what they bring... Take cover, or be ready for what is to come...",
                customLetterDef = LetterDefOf.ThreatBig,
                sendLetter = true
            };

            GameConditionDef whiteRainDef = DefDatabase<GameConditionDef>.GetNamed("Luxandra_WhiteRain", false);

            if (whiteRainDef != null)
            {
                // Cause a 12-24 hour white rain
                GameCondition condition = GameConditionMaker.MakeCondition(whiteRainDef, durationTicks);
                map.gameConditionManager.RegisterCondition(condition);
                LuxandraDebugActions.DebugLogMessage("Successfully triggered 12-hour White Rain for the horde.");
            }
            else
            {
                Log.Warning("[Luxandra Debug] Missing Luxandra_WhiteRain GameConditionDef. Skipping condition spawn.");
            }

            return IncidentDefOf.RaidEnemy.Worker.TryExecute(raidParms);
        }
    }
}