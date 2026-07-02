using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace LuxandraLust
{
    public class IncidentWorker_DeviantHungerSwarm : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_DeviantHungerSwarm.defName))
            {
                return false;
            }

            Faction deviantFaction = Find.FactionManager.FirstFactionOfDef(LuxandraFactionDefOf.Luxandra_DeviantHordeFaction);
            if (deviantFaction == null) return false;

            Map map = (Map)parms.target;
            // Need at least 1 humanlike adult pawn to even try
            return map.mapPawns.FreeColonistsAndPrisonersSpawned.Any(p => p.RaceProps.Humanlike && LuxandraUtilities.IsAdult(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction deviantFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Luxandra_DeviantHordeFaction"));
            PawnKindDef runtKind = PawnKindDef.Named("Luxandra_CarnalDeviantRunt");
            HediffDef feralHediff = HediffDef.Named("Luxandra_PrimalFeralStarvation");

            if (deviantFaction == null || runtKind == null)
            {
                Log.Warning("[LuxandraLust] Deviant Hunger Swarm incident failed: Missing faction or pawn kind definition.");
                return false;
            }

            // Calculate count and apply our 50-pawn safety cap
            float unitCost = runtKind.combatPower;
            int calculatedCount = Mathf.Max(5, Mathf.RoundToInt(parms.points / unitCost));
            int swarmCount = Mathf.Min(calculatedCount, 50);

            // Find entry point
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Walkable(map) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out IntVec3 spawnCell))
            {
                return false;
            }

            List<Pawn> spawnedRunts = new List<Pawn>();

            for (int i = 0; i < swarmCount; i++)
            {
                // Fix: Use the detailed constructor to guarantee the generation engine doesn't attach an automatic background Lord
                PawnGenerationRequest request = new PawnGenerationRequest(
                    runtKind,
                    deviantFaction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: false,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: true,
                    mustBeCapableOfViolence: true,
                    1f,
                    forceAddFreeWarmLayerIfNeeded: false
                );

                Pawn runt = PawnGenerator.GeneratePawn(request);

                // Force starvation
                if (runt.needs != null && runt.needs.food != null)
                {
                    runt.needs.food.CurLevel = 0f;
                }

                // Apply speed/pain buff
                if (feralHediff != null)
                {
                    runt.health.AddHediff(feralHediff);
                }
                else
                    LuxandraDebugActions.DebugLogMessage("Feral Starvation Hediff not found for Deviant Hunger Swarm incident.");

                GenSpawn.Spawn(runt, spawnCell, map);

                // Clean-up any hidden background map-load registers before hand-off
                if (runt.mindState != null)
                {
                    runt.mindState.canFleeIndividual = false;
                }

                Verse.AI.Group.Lord lord = runt.GetLord();
                if (lord != null)
                {
                    lord.Notify_PawnLost(runt, Verse.AI.Group.PawnLostCondition.ForcedToJoinOtherLord);
                }

                spawnedRunts.Add(runt);
            }

            // Now it is perfectly safe to create our own assault lord group without collision
            if (spawnedRunts.Count > 0)
            {
                LordJob_AssaultColony assaultJob = new LordJob_AssaultColony(deviantFaction, canKidnap: false, canPickUpOpportunisticWeapons: false, canSteal: false);
                LordMaker.MakeNewLord(deviantFaction, assaultJob, map, spawnedRunts);
            }

            // Send a highly urgent threat alert letter
            Find.LetterStack.ReceiveLetter(
                "Deviant Hunger Swarm!",
                $"A starving, ravenous pack of {swarmCount} carnal deviant runts has detected the scent of fresh flesh!\n\nDriven mad by starvation, they are sprinting towards your colony walls to acquire food.",
                LetterDefOf.ThreatBig,
                spawnedRunts[0]
            );

            return true;
        }
    }
}