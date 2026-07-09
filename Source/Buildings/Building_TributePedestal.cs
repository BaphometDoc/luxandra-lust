using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using rjw.RMB;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace LuxandraLust
{
    public class Building_TributePedestal : Building
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            // Always pass up to base options (e.g., deconstruct, look at, etc.)
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            if (selPawn == null || selPawn.Faction != Faction.OfPlayer || !selPawn.RaceProps.Humanlike || selPawn.Downed || selPawn.Dead)
            {
                yield break;
            }

            // Must be an Adult with a penis
            if (!selPawn.GetPenises().Any() || !LuxandraUtilities.IsAdult(selPawn, false))
            {
                yield break;
            }

            if (LuxandraModChecks.IsRJWGenesActive())
            {
                GeneDef noCumGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_no_fluid", false);
                GeneDef milkCumGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_fluid_milk_penis", false);
                GeneDef jellyCumGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_fluid_insect_spunk", false);

                if (selPawn.genes?.HasActiveGene(noCumGene) == true || selPawn.genes?.HasActiveGene(milkCumGene) == true || selPawn.genes?.HasActiveGene(jellyCumGene) == true)
                {
                    // Greyed out option since they can't produce cum
                    yield return new FloatMenuOption("Offer your semen (Cannot produce Cum)", null);
                    yield break;
                }
            }

            var sexNeed = LuxandraUtilities.GetSexNeed(selPawn);
            // Must be at least a bit horny
            if (sexNeed == null || sexNeed.CurLevel > 0.6f)
            {
                // Greyed out option highlighting the sex level too high
                yield return new FloatMenuOption("Offer your semen (Not horny enough to masturbate)", null);
                yield break;
            }

            // Check if the pedestal is already completely full of "fuel"
            CompRefuelable refuelableComp = this.GetComp<CompRefuelable>();
            if (refuelableComp != null && refuelableComp.IsFull)
            {
                //yield return new FloatMenuOption("Luxandra_PedestalFull".Translate(), null);
                yield return new FloatMenuOption("Offer your semen (The pedestal cannot hold more Cum)", null);
                yield break;
            }

            HediffDef refractoryDef = DefDatabase<HediffDef>.GetNamed("Luxandra_SpirituallyDiluted", false);
            if (refractoryDef != null && selPawn.health.hediffSet.HasHediff(refractoryDef))
            {
                // Greyed out option highlighting the lack of spiritual potency
                yield return new FloatMenuOption("Offer your semen (Semen lacks sacred potency right now)", null);
                yield break;
            }

            IntVec3 targetCell = this.InteractionCell.IsValid ? this.InteractionCell : this.Position;
            FloatMenuOption masturbateOption = new FloatMenuOption("Offer your semen", delegate ()
                 {
                     var masturbateDef = DefDatabase<InteractionDef>.GetNamed("Masturbation_HandjobP", false);
                     var interaction = new SexInteraction(masturbateDef);
                     var props = new SexProps(selPawn, selPawn) { interaction = interaction };
                     var resolved = SexInteractionHelper.ResolveInteraction(props);

                     RMB_Menu.HaveSex(selPawn, xxx.Masturbate, targetCell, masturbateDef, resolved);

                     JobDef pointCallbackDef = DefDatabase<JobDef>.GetNamed("Luxandra_OfferCumTribute", false);
                     if (pointCallbackDef != null)
                     {
                         Job pointJob = JobMaker.MakeJob(pointCallbackDef, this);

                         selPawn.jobs.jobQueue.EnqueueLast(pointJob);
                     }
                 }, MenuOptionPriority.High);
            yield return FloatMenuUtility.DecoratePrioritizedTask(masturbateOption, selPawn, targetCell);
        }
    }

    public class JobDriver_OfferCumTribute : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return new Toil
            {
                initAction = delegate
                {
                    // Fetch your pedestal instance safely from the job's target
                    var pedestal = this.job.targetA.Thing as Building_TributePedestal;
                    if (pedestal != null)
                    {
                        var refuelComp = pedestal.GetComp<RimWorld.CompRefuelable>();

                        var cumAmount = 10f;
                        var cumMultiplier = 1f;
                        if (LuxandraModChecks.IsRJWGenesActive())
                        {
                            GeneDef muchCumGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_much_fluid", false);
                            GeneDef tonsOfCumGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_very_much_fluid", false);

                            if (this.pawn.genes != null && this.pawn.genes.HasActiveGene(muchCumGene))
                                cumMultiplier = 1.2f;
                            if (this.pawn.genes != null && this.pawn.genes.HasActiveGene(tonsOfCumGene))
                                cumMultiplier = 1.5f;
                        }

                        refuelComp?.Refuel(cumAmount * cumMultiplier);

                        Messages.Message($"Luxandra accepts {this.pawn.LabelShort}'s intimate tribute.", MessageTypeDefOf.PositiveEvent);

                        // Apply the 12-hour recovery cooldown
                        HediffDef refractoryDef = DefDatabase<HediffDef>.GetNamed("Luxandra_SpirituallyDiluted", false);
                        if (refractoryDef != null && !this.pawn.health.hediffSet.HasHediff(refractoryDef))
                            this.pawn.health.AddHediff(refractoryDef);

                        // Spawn some cum on the floor to celebrate
                        ThingDef filthDef = DefDatabase<ThingDef>.GetNamed("FilthCum", false)
                            ?? ThingDefOf.Filth_Slime; // Safe vanilla fallback so the script never breaks

                        int filthCount = Rand.RangeInclusive(3, 5);
                        IntVec3 centerPos = pedestal.Position;

                        for (int i = 0; i < filthCount; i++)
                        {
                            // Radial radius of 1 means it spreads to the immediate tiles touching the bed
                            if (CellFinder.TryFindRandomReachableNearbyCell(centerPos, this.Map, 1, TraverseParms.For(this.pawn), null, null, out IntVec3 filthCell))
                            {
                                int filthPerSpawn = 1;
                                if (LuxandraModSettings.allowFullCumStains) filthPerSpawn = 3;
                                FilthMaker.TryMakeFilth(filthCell, this.Map, filthDef, filthPerSpawn, FilthSourceFlags.Pawn);
                            }
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}