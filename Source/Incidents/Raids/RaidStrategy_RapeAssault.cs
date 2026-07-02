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

            // Find the vanilla escaping destination state if it exists in the baseline graph
            // LordToil escapeToil = graph.lordToils.FirstOrDefault(t => t is LordToil_ExitMapFighting);
            LordToil escapeToil = new LordToil_ExitMapFighting();
            graph.AddToil(escapeToil);

            if (customToil == null)
                LuxandraDebugActions.DebugLogMessage("CustomToil for the raid was bugged. something went wrong.");
            if (escapeToil == null)
                LuxandraDebugActions.DebugLogMessage("EscapeToil for the raid was bugged. something went wrong.");


            if (customToil != null && escapeToil != null)
            {
                LuxandraDebugActions.DebugLogMessage("Creating transitions.....");

                // --- 1. NATURAL COMBAT OUTCOMES ---
                Transition naturalExitTransition = new Transition(customToil, escapeToil);

                // Satisfied (Sex needs met) OR Exhausted (Rest levels tanked)
                naturalExitTransition.AddTrigger(new Trigger_ColonistsDownedAndSexNeedMet());
                naturalExitTransition.AddTrigger(new Trigger_RaidersAreTired());

                naturalExitTransition.AddPreAction(new TransitionAction_Message("The raiders are satisfied with the attack and are leaving the area.", MessageTypeDefOf.NegativeEvent));
                graph.AddTransition(naturalExitTransition);


                // --- 2. FAILSAFE: NO DAMAGE FOR 5 HOURS (12500 Ticks) ---
                Transition stalemateTransition = new Transition(customToil, escapeToil);
                stalemateTransition.AddTrigger(new Trigger_TicksPassedAndNoRecentHarm(12500));

                stalemateTransition.AddPreAction(new TransitionAction_Message("The attackers got bored of the situation and are withdrawing.", MessageTypeDefOf.NeutralEvent));
                graph.AddTransition(stalemateTransition);


                // --- 3. FAILSAFE: 12-HOUR HARD CEILING (30000 Ticks) ---
                Transition timeLimitTransition = new Transition(customToil, escapeToil);
                timeLimitTransition.AddTrigger(new Trigger_TicksPassed(30000));

                timeLimitTransition.AddPreAction(new TransitionAction_Message("The raiders have overstayed their window and are calling off the assault.", MessageTypeDefOf.NeutralEvent));
                graph.AddTransition(timeLimitTransition);
            }

            return graph;
        }
    }

    public class Trigger_ColonistsDownedAndSexNeedMet : Trigger
    {
        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            if (signal.type != TriggerSignalType.Tick) return false;
            if (Find.TickManager.TicksGame % 200 != 0) return false;

            Map map = lord.Map;
            if (map == null) return false;

            var colonists = map.mapPawns.FreeAdultColonistsSpawned;
            bool anyColonistsConscious = colonists.Any(p => !p.Dead && !p.Downed);

            // If there are still active colonists fighting back, the assault isn't won yet
            if (anyColonistsConscious) return false;

            int activeRaiderCount = 0;
            int satisfiedRaiderCount = 0;

            foreach (Pawn raider in lord.ownedPawns)
            {
                if (raider.Dead || raider.Downed) continue;

                // Count active raiders dynamically
                activeRaiderCount++;

                // Filter out individual safety time locks on a per-pawn basis instead of breaking the loop
                Hediff rageHediff = raider.health?.hediffSet?.hediffs
                    .FirstOrDefault(h => h.def.defName == "Luxandra_RapistRage");

                if (rageHediff == null || rageHediff.ageTicks < 12500) continue;

                var sexNeed = raider.needs?.TryGetNeed<rjw.Need_Sex>();
                if (sexNeed != null && sexNeed.CurLevel >= 0.5f)
                {
                    satisfiedRaiderCount++;
                }
            }

            if (activeRaiderCount == 0) return false;

            // Dynamic Cap: If at least 60% of the active, raiders are fully satisfied, wrap up!
            float satisfactionPercent = (float)satisfiedRaiderCount / activeRaiderCount;
            if (satisfactionPercent >= 0.60f)
            {
                LuxandraDebugActions.DebugLogMessage("Threshold for escape due to sexual fulfilment reached.");
                return true;
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

            int activeRaiderCount = 0;
            int tiredRaiderCount = 0;

            foreach (Pawn raider in lord.ownedPawns)
            {
                // Dead and downed pawns shouldn't count toward tactical decisions
                if (raider.Dead || raider.Downed) continue;

                activeRaiderCount++;

                // SAFETY TIME LOCK: Verify the active raider has been in their rage state for at least 5 hours
                Hediff rageHediff = raider.health?.hediffSet?.hediffs
                    .FirstOrDefault(h => h.def.defName == "Luxandra_RapistRage");

                if (rageHediff == null || rageHediff.ageTicks < 12500)
                {
                    // If even one active raider hasn't reached the 5-hour threshold, keep the assault going
                    return false;
                }

                var restNeed = raider.needs?.TryGetNeed(NeedDefOf.Rest);
                if (restNeed != null && restNeed.CurLevel < 0.5f)
                {
                    tiredRaiderCount++;
                }
            }

            // If the entire squad is dead or downed, let another system handle the lord's cleanup
            if (activeRaiderCount == 0) return false;

            // Use floating-point math to completely avoid the integer division truncation trap
            float exhaustionThreshold = activeRaiderCount / 2f;

            if (tiredRaiderCount < exhaustionThreshold)
                return false;

            LuxandraDebugActions.DebugLogMessage("Threshold for escape due to exhaustion reached.");
            // Half or more of the currently living, active raiders are exhausted! Break away!
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