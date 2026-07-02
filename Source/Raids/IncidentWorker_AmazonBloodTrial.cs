using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace LuxandraLust
{
    public class IncidentWorker_AmazonBloodTrial : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            Map map = (Map)parms.target;

            // MUST have at least one conscious, living male colonist on the map
            bool hasMaleColonist = map.mapPawns.FreeColonistsSpawned
                .Any(p => p.gender == Gender.Male && LuxandraUtilities.IsAdult(p) && !p.Downed && !p.Dead);
            if (!hasMaleColonist) return false;

            // Verify faction exists
            Faction amazonFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Luxandra_AmazonTribe"));
            if (amazonFaction == null || amazonFaction.HostileTo(Faction.OfPlayer)) return false;

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction amazonFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Luxandra_AmazonTribe"));
            PawnKindDef fledglingKind = PawnKindDef.Named("Luxandra_Amazon_Fledgling");

            HediffDef trialHediff = HediffDef.Named("Luxandra_BloodTrialFrenzy");

            float unitCost = fledglingKind.combatPower;
            int totalFledglings = Mathf.Max(2, Mathf.RoundToInt(parms.points / unitCost));

            // Split the group into 2 separate groups around the map edges
            int groupASize = totalFledglings / 2;
            int groupBSize = totalFledglings - groupASize;

            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Walkable(map) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out IntVec3 spawnCellA))
            {
                return false; // Abort if the map has no open boundaries (idk how you manage this but better safe...)
            }

            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Walkable(map) && !c.Fogged(map) && c.DistanceToSquared(spawnCellA) > 2500, map, CellFinder.EdgeRoadChance_Neutral, out IntVec3 spawnCellB))
            {
                spawnCellB = spawnCellA; // Fallback to same edge if the map layout is completely boxed in
            }

            // Spawn and apply frenzy hediffs to both groups
            List<Pawn> groupAPawns = SpawnAndAugmentGroup(groupASize, fledglingKind, amazonFaction, spawnCellA, map, trialHediff);
            List<Pawn> groupBPawns = SpawnAndAugmentGroup(groupBSize, fledglingKind, amazonFaction, spawnCellB, map, trialHediff);

            // Assign BOTH groups to No-Flee LordJob
            if (groupAPawns.Any())
            {
                LordMaker.MakeNewLord(amazonFaction, new LordJob_BloodTrialAssault(amazonFaction), map, groupAPawns);
            }
            if (groupBPawns.Any())
            {
                LordMaker.MakeNewLord(amazonFaction, new LordJob_BloodTrialAssault(amazonFaction), map, groupBPawns);
            }

            string groupDefiner = totalFledglings > 10 ? "horde" : "squad";

            // Send custom red letter threat box
            Find.LetterStack.ReceiveLetter(
                "Amazon Trial of Blood",
                $"A desperate {groupDefiner} of {totalFledglings} young amazon initiates has breached the map rim from multiple angles!\n\nDriven by tribal honor, they are under a blood trial: return victorious, or die trying. Prepare for the assault!",
                LetterDefOf.ThreatBig,
                groupAPawns.Count > 0 ? groupAPawns[0] : null
            );

            return true;
        }

        private List<Pawn> SpawnAndAugmentGroup(int count, PawnKindDef kind, Faction faction, IntVec3 cell, Map map, HediffDef frenzyHediff)
        {
            List<Pawn> spawnedList = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(kind, faction);
                Pawn fledgling = PawnGenerator.GeneratePawn(request);

                if (frenzyHediff != null)
                {
                    fledgling.health.AddHediff(frenzyHediff);
                }

                GenSpawn.Spawn(fledgling, cell, map);
                spawnedList.Add(fledgling);
            }
            return spawnedList;
        }
    }

    // AI that forces pawns to fight to the absolute last breath
    public class LordJob_BloodTrialAssault : LordJob
    {
        private Faction faction;

        public LordJob_BloodTrialAssault() { }

        public LordJob_BloodTrialAssault(Faction faction)
        {
            this.faction = faction;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();

            // Core tactical state: March directly to the colony core and attack
            LordToil_AssaultColony lordToil_AssaultColony = new LordToil_AssaultColony(canPickUpOpportunisticWeapons: true);
            stateGraph.AddToil(lordToil_AssaultColony);

            // They will stay inside this Assault toil until every single one is dead or downed.
            return stateGraph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
        }
    }
}