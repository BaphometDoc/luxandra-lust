using RimWorld;
using System;
using Verse;

namespace LuxandraLust
{
    public class ThoughtWorker_LuxandraStature : ThoughtWorker
    {
        private static readonly HediffDef KissDef = HediffDef.Named("Luxandra_DivineKiss");
        private static readonly HediffDef GlowDef = HediffDef.Named("Luxandra_HubristicGlow");

        // FIX: Explicitly override the personal mood method to return Inactive.
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return ThoughtState.Inactive;
        }

        // This handles the actual social opinions
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn otherPawn)
        {
            if (pawn == otherPawn || otherPawn == null || pawn == null)
            {
                return ThoughtState.Inactive;
            }

            var hediffSet = otherPawn.health?.hediffSet;
            if (hediffSet == null)
            {
                return ThoughtState.Inactive;
            }

            try
            {
                if (hediffSet.HasHediff(KissDef))
                {
                    return ThoughtState.ActiveAtStage(0); // Stage 0 in XML: +50
                }

                if (hediffSet.HasHediff(GlowDef))
                {
                    return ThoughtState.ActiveAtStage(1); // Stage 1 in XML: -100 (has to offset the +40 from beautiful)
                }
            }
            catch (Exception ex)
            {
                // This should never happen but you never know
                Log.ErrorOnce($"[Luxandra Debug] ThoughtWorker_LuxandraStature failed: {ex.Message}", 992831);
            }

            return ThoughtState.Inactive;
        }
    }
}