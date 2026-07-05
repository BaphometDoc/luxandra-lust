using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_StarvingAmazons : IncidentWorker_RaidEnemy
    {

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_StarvingAmazons.defName))
            {
                return false;
            }

            // Verify faction exists
            Faction amazonFaction = Find.FactionManager.FirstFactionOfDef(LuxandraFactionDefOf.Luxandra_AmazonTribe);
            if (amazonFaction == null) return false;

            Map map = (Map)parms.target;
            // Need at least 1 humanlike adult pawn to even try
            return map.mapPawns.FreeColonistsAndPrisonersSpawned.Any(p => p.RaceProps.Humanlike && LuxandraUtilities.IsAdult(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            Faction amazonFaction = Find.FactionManager.FirstFactionOfDef(LuxandraFactionDefOf.Luxandra_AmazonTribe);

            if (amazonFaction == null)
            {
                Log.Error("[Luxandra Debug] No Amazon faction found. Aborting raid.");
                return false;
            }

            RaidStrategyDef raidStrategy = RaidStrategyDefOf.ImmediateAttack;

            var points = StorytellerUtility.DefaultThreatPointsNow(map) * 1.3f;

            IncidentParms raidParms = new IncidentParms
            {
                target = map,
                points = points,
                faction = amazonFaction,
                forced = true,
                canKidnap = true,
                canSteal = true,
                canTimeoutOrFlee = false,
                pawnGroupKind = PawnGroupKindDefOf.Combat,
                raidStrategy = raidStrategy,
                raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn,
                customLetterLabel = "Raid (Starving Amazons)",
                customLetterText = "A group of Amazons are assaulting your colony! They seem very debilitated, they are probably desperate for supplies, and will not give up until they are dead... or you are.",
                customLetterDef = LetterDefOf.ThreatBig,
                sendLetter = true
            };

            bool raidSuccessful = base.TryExecuteWorker(raidParms);

            if (!raidSuccessful)
            {
                LuxandraDebugActions.DebugLogMessage("Failed to generate a hungry amazon raid");
                return false;
            }

            // All spawned amazons are hungry and debilitated
            List<Pawn> spawnedAmazons = map.mapPawns.FreeHumanlikesSpawnedOfFaction(amazonFaction); ;

            foreach (Pawn pawn in spawnedAmazons)
            {
                if (pawn == null || pawn.Dead) continue;

                if (pawn.needs != null)
                {
                    var foodNeed = pawn.needs.food;
                    if (foodNeed != null)
                    {
                        foodNeed.CurLevel = 0f;
                    }
                }
            }

            return true;
        }
    }
}