using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace LuxandraLust
{
    public class RaidStrategyWorker_RapeAssault : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            return new LordJob_RapePillageAssault(parms.faction, true, true);
        }

        public override float SelectionWeight(Map map, float basePoints)
        {
            // The Debug UI calls this with a null map when initializing!
            if (map == null) return 0f;

            // 0.5f is a reasonable default weight for a raid strategy
            return 0.5f;
        }
    }

    // Inherit directly from the vanilla assault engine so the pawns actually have an AI
    public class LordJob_RapePillageAssault : LordJob_AssaultColony
    {
        // Keep constructors aligned with vanilla
        public LordJob_RapePillageAssault() : base() { }
        public LordJob_RapePillageAssault(Faction faction, bool assaultStageIn = true, bool canKidnap = true)
            : base(faction, assaultStageIn, canKidnap) { }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = base.CreateGraph();

            LordToil vanillaAssaultToil = graph.lordToils.FirstOrDefault(t => t is LordToil_AssaultColony && !(t is LordToil_BastardAssault));

            if (vanillaAssaultToil != null)
            {
                LordToil_BastardAssault customToil = new LordToil_BastardAssault();

                int index = graph.lordToils.IndexOf(vanillaAssaultToil);
                graph.lordToils[index] = customToil;

                if (graph.StartingToil == vanillaAssaultToil)
                {
                    graph.StartingToil = customToil;
                }
            }

            return graph;
        }
    }

    public class LordToil_BastardAssault : LordToil_AssaultColony
    {
        public LordToil_BastardAssault() : base(false, false) { }

        public override void UpdateAllDuties()
        {
            DutyDef customDuty = DefDatabase<DutyDef>.GetNamed("Luxandra_BastardAssaultDuty", false);
            if (customDuty == null)
            {
                Log.Error("[LuxandraLust] Crucial Error: Luxandra_BastardAssaultDuty missing from XML definitions!");
                return;
            }

            HediffDef rapistRageHediff = DefDatabase<HediffDef>.GetNamed("Luxandra_RapistRage", false);

            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                Pawn raider = this.lord.ownedPawns[i];
                if (raider == null || raider.Dead) continue;

                if (raider.mindState != null)
                {
                    raider.mindState.duty = new PawnDuty(customDuty);
                }

                if (rapistRageHediff != null && raider.health != null)
                {
                    // Check to avoid duplicate stacking if duties update multiple times
                    if (!raider.health.hediffSet.HasHediff(rapistRageHediff))
                    {
                        LuxandraDebugActions.DebugLogMessage($"Added rapist frenzy to {raider.NameShortColored}.");
                        raider.health.AddHediff(rapistRageHediff, null, null, null);
                    }
                }
            }
        }
    }
}