using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
            LordToil_BastardAssault customToil = new LordToil_BastardAssault();

            // Start the rapist raid
            if (vanillaAssaultToil != null)
            {
                int index = graph.lordToils.IndexOf(vanillaAssaultToil);
                graph.lordToils[index] = customToil;

                if (graph.StartingToil == vanillaAssaultToil)
                {
                    graph.StartingToil = customToil;
                }
            }

            // Find the vanilla kidnapping destination state if it exists in the baseline graph
            LordToil kidnapToil = graph.lordToils.FirstOrDefault(t => t is LordToil_KidnapCover);

            if (customToil != null && kidnapToil != null)
            {
                Transition cleanBreakTransition = new Transition(customToil, kidnapToil);

                // Evaluate if the raiders have had enough fun
                cleanBreakTransition.AddTrigger(new Trigger_ColonistsDownedAndSexNeedMet());

                // Evaluate if the raiders have had enough fun
                cleanBreakTransition.AddTrigger(new Trigger_RaidersAreTired());

                // Add a feedback notification
                cleanBreakTransition.AddPreAction(new TransitionAction_Message("The raiders are satisfied with the attack and are kidnapping who they can before leaving!", MessageTypeDefOf.NegativeEvent));

                graph.AddTransition(cleanBreakTransition);
            }

            return graph;
        }
    }

    public class Trigger_ColonistsDownedAndSexNeedMet : Trigger
    {
        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            // Only execute this heavier condition check on standard periodic environment updates
            if (signal.type != TriggerSignalType.Tick) return false;

            // Check every 200 ticks (~3.3 seconds) to keep performance flawless
            if (Find.TickManager.TicksGame % 200 != 0) return false;

            Map map = lord.Map;
            if (map == null) return false;

            // Gather colonist headcount parameters
            var colonists = map.mapPawns.FreeAdultColonistsSpawned;
            var consciousColonists = colonists.Where(p => !p.Dead && !p.Downed);
            int colonistNumber = colonists.Count;

            // Baseline Victory Check: If all colonists are completely defeated
            if (map.mapPawns.FreeColonistsSpawnedCount > 0 && !consciousColonists.Any())
            {
                if (lord.ownedPawns.Count == 0) return false;

                int satisfiedRaiderCount = 0;

                foreach (Pawn raider in lord.ownedPawns)
                {
                    if (raider.Dead || raider.Downed) continue;

                    // SAFETY TIME LOCK: Verify the raider has been in their rage state for at least 5 hours
                    Hediff rageHediff = raider.health?.hediffSet?.hediffs
                        .FirstOrDefault(h => h.def.defName == "Luxandra_RapistRage");

                    if (rageHediff == null || rageHediff.ageTicks < 12500)
                    {
                        // If even one raider hasn't reached the 5-hour active assault threshold, hold the raid group
                        return false;
                    }

                    // Check emotional/physical satisfaction
                    var sexNeed = raider.needs?.TryGetNeed<rjw.Need_Sex>();
                    if (sexNeed != null && sexNeed.CurLevel >= 0.5f)
                    {
                        satisfiedRaiderCount++;
                    }
                }

                // If the raiders have had their fun, leave
                if (satisfiedRaiderCount > colonistNumber)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class Trigger_RaidersAreTired : Trigger
    {
        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            // Only execute this heavier condition check on standard periodic environment updates
            if (signal.type != TriggerSignalType.Tick) return false;

            // Check every 200 ticks (~3.3 seconds) to keep performance flawless
            if (Find.TickManager.TicksGame % 200 != 0) return false;

            Map map = lord.Map;
            if (map == null) return false;

            // Is there any capable colonist capable of fighting back?
            if (lord.ownedPawns.Count == 0) return false;

            int tiredRaiderCount = 0;

            foreach (Pawn raider in lord.ownedPawns)
            {
                if (raider.Dead || raider.Downed) continue;

                // SAFETY TIME LOCK: Verify the raider has been in their rage state for at least 5 hours
                Hediff rageHediff = raider.health?.hediffSet?.hediffs
                    .FirstOrDefault(h => h.def.defName == "Luxandra_RapistRage");

                if (rageHediff == null || rageHediff.ageTicks < 12500)
                {
                    // If even one raider hasn't reached the 5-hour active assault threshold, hold the raid group
                    return false;
                }

                var restNeed = raider.needs?.TryGetNeed(NeedDefOf.Rest);

                if (restNeed != null)
                {
                    if (restNeed.CurLevel < 0.5f)
                    {
                        tiredRaiderCount++;
                    }
                }
            }


            // If less than the pawns are tired, stay
            if (tiredRaiderCount < lord.ownedPawns?.Count / 2)
                return false;

            // If everyone alive meets the metric criteria and the colony is defeated, break away!
            return true;
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